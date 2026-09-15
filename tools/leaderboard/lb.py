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
MEDALS = ["\U0001F947", "\U0001F948", "\U0001F949"]  # gold, silver, bronze
TOP_N = 10


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


def line(entry):
    """One ranked row. Names keep their #1234 tag -- several pilots share a name."""
    name, _, tag = (entry.get("playerName") or entry["playerId"][:8]).partition("#")
    rank = entry["rank"]
    badge = MEDALS[rank] if rank < len(MEDALS) else f"`{rank + 1:>2}`"
    tag = f"`#{tag}`" if tag else ""
    return f"{badge}  **{name}**{tag} — `{entry['score']:g}`"


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
        rows = [line(e) for e in res["results"]]
        embeds.append({
            "title": title,
            "color": colour,
            "description": "\n".join(rows) or "_no scores yet_",
            "footer": {"text": f"{res.get('total', len(rows))} pilots ranked"},
            "timestamp": now,
        })

    call(env("DISCORD_WEBHOOK"),
         json.dumps({"embeds": embeds}).encode(),
         {"Content-Type": "application/json"})
    print("posted", len(embeds), "leaderboards")


if __name__ == "__main__":
    main()
