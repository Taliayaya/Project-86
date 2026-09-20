#!/usr/bin/env python3
"""Self-check for the poster's rendering: python3 tools/leaderboard/test_lb.py"""
import json, os, re, sys, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lb

plain_text = lambda s: re.sub(r"\x1b\[[0-9;]*m", "", s)


def entry(rank, name, score, md=None, pid=None):
    e = {"rank": rank, "playerName": name, "playerId": pid or name, "score": score}
    if md is not None:
        e["metadata"] = md
    return e


# ---- metadata columns -------------------------------------------------------
# Legacy rows (no metadata) render as before: no empty gutters.
legacy = lb.table([entry(0, "RAIDEN#0042", 1500), entry(1, "SHIN#0001", 900)], context=True)
assert "MECH" not in legacy, legacy
assert "RAIDEN#0042" in legacy and "1500" in legacy

rich = lb.table([
    entry(0, "RAIDEN#0042", 1500, {"mech": "REGINLEIF // BLADE", "op": "EF-03", "threat": "PALE RIDER"}),
    entry(1, "SHIN#0001", 900),
], context=True)
assert "MECH" in rich and "OP" in rich and "THREAT" in rich, rich
assert "REGINLEIF // BLADE" in rich and "SHIN#0001" in rich

# Metadata as a JSON string is parsed, not printed raw; junk degrades to blank instead of raising.
as_string = lb.table([entry(0, "KURENA#0007", 42,
                            '{"mech": "JUGGERNAUT", "op": "EF-01", "threat": "SHEPARD"}')], context=True)
assert "JUGGERNAUT" in as_string and "{" not in as_string, as_string
for junk in ("not json", 5, None, []):
    lb.table([entry(0, "X#0001", 1, junk)], context=True)

# TOTAL KILLS never grows the context columns, even if metadata leaks onto it.
assert "MECH" not in lb.table([entry(0, "RAIDEN#0042", 1500, {"mech": "JUGGERNAUT"})], context=False)

# ---- movement ---------------------------------------------------------------
today = [entry(0, "SHIN#0001", 1600), entry(1, "RAIDEN#0042", 1500), entry(2, "FRESH#0003", 800)]
prev = {"SHIN#0001": {"rank": 2, "score": 1200}, "RAIDEN#0042": {"rank": 0, "score": 1500}}

# No previous board: no movement columns at all, because everyone would read NEW.
first = plain_text(lb.table(today))
assert [l for l in first.splitlines() if l.startswith("#")][0].split() == ["#", "PILOT", "KILLS"], first

moved = plain_text(lb.table(today, prev=prev))
assert [l for l in moved.splitlines() if l.startswith("#")][0].split() == \
    ["#", "MV", "PILOT", "KILLS", "+"], moved
rows = {n: next(l for l in moved.splitlines() if n in l)
        for n in ("SHIN#0001", "RAIDEN#0042", "FRESH#0003")}
assert "^2" in rows["SHIN#0001"] and "+400" in rows["SHIN#0001"], rows
assert "NEW" in rows["FRESH#0003"], rows
# Unchanged pilot: no arrow, no gain, and the rest of the row still renders.
assert "^" not in rows["RAIDEN#0042"] and "+" not in rows["RAIDEN#0042"], rows

# A pilot who slipped shows a down arrow.
slipped = plain_text(lb.table([entry(3, "SHIN#0001", 1200)],
                              prev={"SHIN#0001": {"rank": 0, "score": 1200}}))
assert "v3" in slipped, slipped

# ---- the bubble -------------------------------------------------------------
board = [entry(i, f"P{i:02d}#0001", 1000 - i * 10) for i in range(lb.TOP_N + lb.BUBBLE)]
bubble = plain_text(lb.table(board))
assert "-- CONTENDERS --" in bubble
lines = [l for l in bubble.splitlines() if "#0001" in l]
assert len(lines) == lb.TOP_N + lb.BUBBLE
# The rule sits between rank 10 and rank 11, not anywhere else.
body = bubble.splitlines()
rule = body.index("-- CONTENDERS --")
assert body[rule - 1].split()[0] == str(lb.TOP_N), body      # rule sits under the last board row
assert body[rule + 1].split()[0] == str(lb.TOP_N + 1), body  # and above the first contender
assert lb.table(board[:5]).count("CONTENDERS") == 0  # short board, no rule

# ---- headline ---------------------------------------------------------------
assert lb.headline(0, compared=False) == lb.HEADING          # first ever run claims nothing
assert "no new records" in lb.headline(0, compared=True)
assert "1 new sortie record" in lb.headline(1, compared=True)
assert "3 new sortie records" in lb.headline(3, compared=True)

# ---- state round trip -------------------------------------------------------
with tempfile.TemporaryDirectory() as d:
    lb.STATE = os.path.join(d, "state", "prev.json")
    assert lb.load_state() == {}                              # missing file is not an error
    snap = lb.snapshot(today)
    lb.save_state({"best_sortie_kills": snap})
    assert lb.load_state()["best_sortie_kills"]["SHIN#0001"] == {"rank": 0, "score": 1600}
    open(lb.STATE, "w").write("{corrupt")
    assert lb.load_state() == {}                              # corrupt cache degrades to no arrows

# ---- alignment --------------------------------------------------------------
# KILLS right-edges line up under the header whatever trails them.
full = plain_text(lb.table(
    [entry(0, "RAIDEN#0042", 1500, {"mech": "REGINLEIF // BLADE", "op": "EF-03", "threat": "PALE RIDER"}),
     entry(1, "SHIN#0001", 900)], context=True, prev=prev)).splitlines()
head = [l for l in full if l.startswith("#")][0]
edge = head.index("KILLS") + len("KILLS")
for row, want in zip([l for l in full if "#00" in l], ["1500", "900"]):
    assert row[:edge].endswith(want), (row, edge)

# ---- container --------------------------------------------------------------
board13 = [entry(i, f"P{i:02d}#0001", 1000 - i * 10) for i in range(lb.TOP_N + lb.BUBBLE)]
c = lb.container("Best Sortie Kills", 0xE03131, "blurb", board13, True, {}, 47)
assert c["type"] == 17 and c["accent_color"] == 0xE03131
kinds = [b["type"] for b in c["components"]]
assert kinds == [10, 10, 14, 10], kinds                        # head, table, gap, footer
assert "## Best Sortie Kills" in c["components"][0]["content"]
assert "```ansi" in c["components"][1]["content"]
assert "47 pilots ranked" in c["components"][-1]["content"]
# One table, whole board: rank 1 is in it and so is the last contender.
body = plain_text(c["components"][1]["content"]).splitlines()
assert body[2].split()[0] == "1" and body[-2].split()[0] == str(lb.TOP_N + lb.BUBBLE), body

empty = lb.container("Total Kills", 0xF59F00, "blurb", [], False, {}, 0)
assert "_no scores yet_" in empty["components"][1]["content"]

# Under the 40-component cap with both boards plus the header, by a wide margin.
assert 1 + 2 * len(c["components"]) < 40

print("all good")
