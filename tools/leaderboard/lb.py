#!/usr/bin/env python3
"""Post UGS leaderboards to Discord. All config via env vars."""
import base64, json, os, sys, urllib.request as u

API = "https://services.api.unity.com"
# leaderboard id -> display title
BOARDS = {"best_sortie_kills": "Best Sortie Kills", "total_kills": "Total Kills"}
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


def main():
    pid, envid = env("UGS_PROJECT"), env("UGS_ENV")
    # The Admin API takes service-account Basic auth directly. Do NOT exchange for a
    # stateless token here -- that one is player-scoped and gets rejected as "untrusted issuer".
    basic = base64.b64encode(f"{env('UGS_KEY_ID')}:{env('UGS_SECRET')}".encode()).decode()
    auth = {"Authorization": f"Basic {basic}"}

    blocks = []
    for lb_id, title in BOARDS.items():
        res = call(f"{API}/leaderboards/v1/projects/{pid}/environments/{envid}"
                   f"/leaderboards/{lb_id}/scores?offset=0&limit={TOP_N}", headers=auth)
        rows = "\n".join(
            f"{e['rank'] + 1:>2}. {(e.get('playerName') or e['playerId'][:8]):<20} {e['score']:g}"
            for e in res["results"])
        blocks.append(f"**{title}**\n```\n{rows or 'no scores yet'}\n```")

    call(env("DISCORD_WEBHOOK"),
         json.dumps({"content": "\n".join(blocks)}).encode(),
         {"Content-Type": "application/json"})
    print("posted", len(blocks), "leaderboards")


if __name__ == "__main__":
    main()
