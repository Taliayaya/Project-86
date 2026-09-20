#!/usr/bin/env python3
"""Post UGS leaderboards to Discord. All config via env vars."""
import base64, json, os, sys, urllib.request as u
from datetime import datetime, timezone

API = "https://services.api.unity.com"
HEADING = "Today's leaderboard result"
# id -> (display title, container accent colour, blurb, show the sortie context columns)
# Context is best-sortie only: that board is KeepBest, so its metadata belongs to the run that set
# the record. TOTAL KILLS sums every sortie ever flown, so one mech label there would be a lie.
BOARDS = {
    "best_sortie_kills": ("Best Sortie Kills", 0xE03131,
                          "Most Legion destroyed by a single Processor in one sortie.", True),
    "total_kills": ("Total Kills", 0xF59F00,
                    "Total Legion destroyed by a Processor across every sortie flown.", False),
}
# The board proper, plus the pilots sitting just off it. Without the bubble the post is only ever
# about ten people and whoever is 11th has nothing visible to chase.
TOP_N = 10
BUBBLE = 3
NAME_CAP = 22          # keeps the table narrow enough not to wrap on mobile
CAPS = {"PILOT": NAME_CAP, "MECH": 18, "OP": 6, "THREAT": 11}
RIGHT = {"#", "MV", "KILLS", "+"}  # numeric columns right-align, everything else reads left
# Previous run's standings, so the post can show movement instead of restating the same table every
# day. Restored and saved by actions/cache; losing it costs one report's arrows, nothing more.
STATE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "state", "prev.json")
ESC = "\x1b"
# Discord renders ```ansi blocks. One table holds the whole board, podium included: monospace is the
# only way to get columns that line up, and the ranks read as one list rather than two designs.
# Colour beats emoji for the podium here: emoji are double-width and would break that alignment.
PODIUM = {0: f"{ESC}[1;33m", 1: f"{ESC}[1;37m", 2: f"{ESC}[0;33m"}
HEADER = f"{ESC}[4;37m"
DIM = f"{ESC}[0;30m"
RESET = f"{ESC}[0m"
# Components v2. The flag swaps `content`/`embeds` out for a component tree and cannot be unset on a
# message afterwards; the query param is what lets a plain channel webhook send components at all.
V2_FLAG = 1 << 15
WITH_COMPONENTS = "with_components=true"


def env(name):
    v = os.environ.get(name)
    if not v:
        sys.exit(f"missing env var: {name}")
    return v


def call(url, data=None, headers=None):
    # Discord's edge 403s urllib's default User-Agent, so always send a real one.
    hdrs = {"User-Agent": "p86-leaderboard/1.0", **(headers or {})}
    req = u.Request(url, data=data, headers=hdrs,
                    method="POST" if data is not None else "GET")
    with u.urlopen(req, timeout=30) as r:
        body = r.read()          # Discord webhooks answer 204 with an empty body
        return json.loads(body) if body else None


def load_state():
    try:
        with open(STATE) as f:
            return json.load(f)
    except (OSError, ValueError):
        return {}


def save_state(state):
    os.makedirs(os.path.dirname(STATE), exist_ok=True)
    with open(STATE, "w") as f:
        json.dump(state, f)


def snapshot(results):
    """What this run should remember for the next one. Keyed by playerId, which survives a rename."""
    return {e["playerId"]: {"rank": e["rank"], "score": e["score"]} for e in results}


def move(entry, prev):
    """(rank movement, score gain) against the last posted board. NEW means the pilot was not on that
    board, which is not the same as never having scored: someone climbing from 20th to 8th reads as
    NEW because ranks below the bubble were never recorded. ASCII arrows, not the prettier triangles,
    for the same reason this file uses colour instead of emoji: width has to stay predictable."""
    was = prev.get(entry["playerId"])
    if not was:
        return "NEW", ""
    d = was["rank"] - entry["rank"]
    arrow = f"^{d}" if d > 0 else f"v{-d}" if d < 0 else ""
    gain = entry["score"] - was["score"]
    return arrow, (f"+{gain:g}" if gain > 0 else "")


def meta(entry):
    """Score metadata, defensively. Absent on every record set before the metadata build shipped,
    and the service may hand it back as a JSON string rather than an object."""
    m = entry.get("metadata")
    if isinstance(m, str):
        try:
            m = json.loads(m)
        except ValueError:
            m = None
    return m if isinstance(m, dict) else {}


def table(results, context=False, prev=None):
    """Monospace table. Names keep their #1234 tag -- several pilots share a name."""
    prev = prev or {}
    rows = []
    for e in results:
        m = meta(e)
        arrow, gain = move(e, prev)
        rows.append({
            "#": str(e["rank"] + 1),
            "MV": arrow,
            "PILOT": (e.get("playerName") or e["playerId"][:8]),
            "KILLS": f"{e['score']:g}",
            "+": gain,
            "MECH": m.get("mech") or "",
            "OP": m.get("op") or "",
            "THREAT": m.get("threat") or "",
        })
    for r in rows:
        for col, cap in CAPS.items():
            r[col] = r[col][:cap]

    cols = ["#", "PILOT", "KILLS"]
    # Movement columns only once there is a previous board to compare against (on the very first run
    # every pilot would read NEW, which says nothing) and only when this slice of the board actually
    # moved. An all-blank column is a gutter, not information.
    if prev:
        if any(r["MV"] for r in rows):
            cols.insert(1, "MV")
        if any(r["+"] for r in rows):
            cols.append("+")
    # Widen only once records actually carry context. Until a build with the metadata submit is out
    # every entry is blank, and three empty gutters read as a bug rather than as a pending feature.
    if context and any(r["MECH"] or r["OP"] or r["THREAT"] for r in rows):
        cols += ["MECH", "OP", "THREAT"]

    w = {c: max(len(c), max((len(r[c]) for r in rows), default=0)) for c in cols}

    def line(cells, colour="", end=""):
        # The last column is never padded: trailing spaces would ride along invisibly on every row.
        body = "  ".join(cells[c].rjust(w[c]) if c in RIGHT else
                         cells[c].ljust(w[c] if c != cols[-1] else 0) for c in cols)
        return colour + body.rstrip() + end

    out = [line({c: c for c in cols}, HEADER, RESET)]
    for e, r in zip(results, rows):
        # The bubble is ruled off so the board proper still reads as the board.
        if e["rank"] == TOP_N:
            out.append(f"{DIM}-- CONTENDERS --{RESET}")
        colour = PODIUM.get(e["rank"], "")
        out.append(line(r, colour, RESET if colour else ""))
    return "```ansi\n" + "\n".join(out) + "\n```"


def container(title, colour, blurb, results, context, prev, total):
    """One board as a components-v2 container: coloured spine, heading, table, pilot count."""
    body = [{"type": 10, "content": f"## {title}\n-# {blurb}"},
            {"type": 10, "content": table(results, context, prev) if results else "_no scores yet_"},
            {"type": 14, "divider": False, "spacing": 1},
            {"type": 10, "content": f"-# {total} pilots ranked"}]
    return {"type": 17, "accent_color": colour, "components": body}


def headline(records, compared):
    """The one line people read before deciding whether to open the post. An honest 'nothing happened'
    beats a table that looks new and is not."""
    if not compared:
        return HEADING
    if records:
        return f"{HEADING} · {records} new sortie record{'s' if records > 1 else ''}"
    return f"{HEADING} · no new records since the last report"


def main():
    pid, envid = env("UGS_PROJECT"), env("UGS_ENV")
    # The Admin API takes service-account Basic auth directly. Do NOT exchange for a
    # stateless token here -- that one is player-scoped and gets rejected as "untrusted issuer".
    basic = base64.b64encode(f"{env('UGS_KEY_ID')}:{env('UGS_SECRET')}".encode()).decode()
    auth = {"Authorization": f"Basic {basic}"}
    prev, state, records = load_state(), {}, 0
    blocks = []
    for lb_id, (title, colour, blurb, context) in BOARDS.items():
        res = call(f"{API}/leaderboards/v1/projects/{pid}/environments/{envid}"
                   f"/leaderboards/{lb_id}/scores?offset=0&limit={TOP_N + BUBBLE}"
                   f"&includeMetadata=true", headers=auth)
        was = prev.get(lb_id, {})
        state[lb_id] = snapshot(res["results"])
        # Only BEST SORTIE counts as a record: it is KeepBest, so a higher score there means somebody
        # actually beat their own best run. TOTAL KILLS grows for anyone who simply played.
        if lb_id == "best_sortie_kills":
            # A higher score is a beaten personal best; NEW is a pilot who reached the board at all.
            # A rank climb on its own is not counted, because on KeepBest nobody's score ever falls.
            records = sum(1 for e in res["results"]
                          for arrow, gain in [move(e, was)] if arrow == "NEW" or gain)
        blocks.append(container(title, colour, blurb, res["results"], context, was,
                                res.get("total", len(res["results"]))))

    head = f"# {headline(records, bool(prev))}\n-# {datetime.now(timezone.utc):%d %b %Y}"
    hook = env("DISCORD_WEBHOOK")
    hook += ("&" if "?" in hook else "?") + WITH_COMPONENTS
    call(hook,
         json.dumps({"flags": V2_FLAG,
                     "components": [{"type": 10, "content": head}] + blocks}).encode(),
         {"Content-Type": "application/json"})
    save_state(state)
    print("posted", len(blocks), "boards")


if __name__ == "__main__":
    main()
