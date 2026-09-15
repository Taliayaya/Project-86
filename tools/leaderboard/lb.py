#!/usr/bin/env python3
"""Post UGS leaderboards to Discord. All config via env vars."""
import base64, json, os, sys, urllib.request as u
from datetime import datetime, timezone

API = "https://services.api.unity.com"
# leaderboard id -> (display title, embed colour)
BOARDS = {
    "best_sortie_kills": ("Best Sortie Kills", 0xE03131),
    "total_kills": ("Total Kills", 0xF59F00),
}
TOP_N = 10
NAME_CAP = 22          # keeps the table narrow enough not to wrap on mobile
ESC = "\x1b"
# Discord renders ```ansi blocks. Colour beats emoji for a podium here: emoji are
# double-width and would break the column alignment that makes this read as a table.
PODIUM = {0: f"{ESC}[1;33m", 1: f"{ESC}[1;37m", 2: f"{ESC}[0;33m"}
HEADER = f"{ESC}[4;37m"
RESET = f"{ESC}[0m"


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


def table(results):
    """Monospace table. Names keep their #1234 tag -- several pilots share a name."""
    names = [(e.get("playerName") or e["playerId"][:8])[:NAME_CAP] for e in results]
    scores = [f"{e['score']:g}" for e in results]
    w_name = max((len(n) for n in names), default=5)
    w_score = max(max((len(s) for s in scores), default=0), 5)
    rows = [f"{HEADER}{'#':>2}  {'PILOT':<{w_name}}  {'KILLS':>{w_score}}{RESET}"]
    for e, name, score in zip(results, names, scores):
        colour = PODIUM.get(e["rank"], "")
        end = RESET if colour else ""
        rows.append(f"{colour}{e['rank'] + 1:>2}  {name:<{w_name}}  {score:>{w_score}}{end}")
    return "```ansi\n" + "\n".join(rows) + "\n```"


def main():
    pid, envid = env("UGS_PROJECT"), env("UGS_ENV")
    # The Admin API takes service-account Basic auth directly. Do NOT exchange for a
    # stateless token here -- that one is player-scoped and gets rejected as "untrusted issuer".
    basic = base64.b64encode(f"{env('UGS_KEY_ID')}:{env('UGS_SECRET')}".encode()).decode()
    auth = {"Authorization": f"Basic {basic}"}
    now = datetime.now(timezone.utc).isoformat()

    embeds = []
    for lb_id, (title, colour) in BOARDS.items():
        res = call(f"{API}/leaderboards/v1/projects/{pid}/environments/{envid}"
                   f"/leaderboards/{lb_id}/scores?offset=0&limit={TOP_N}", headers=auth)
        embeds.append({
            "title": title,
            "color": colour,
            "description": table(res["results"]) if res["results"] else "_no scores yet_",
            "footer": {"text": f"{res.get('total', len(res['results']))} pilots ranked"},
            "timestamp": now,
        })

    call(env("DISCORD_WEBHOOK"),
         json.dumps({"embeds": embeds}).encode(),
         {"Content-Type": "application/json"})
    print("posted", len(embeds), "leaderboards")


if __name__ == "__main__":
    main()
