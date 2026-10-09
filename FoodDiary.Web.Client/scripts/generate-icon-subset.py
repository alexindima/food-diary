"""Regenerate the checked-in icon subset; requires fonttools[woff]==4.63.0."""

import json
import re
from pathlib import Path

from fontTools import subset
from fontTools.ttLib import TTFont

root = Path(__file__).resolve().parents[1]
icon_dir = root / "projects/fd-ui-kit/src/lib/icon"
source = root / "node_modules/@material-design-icons/font/material-icons.woff2"
font = TTFont(source, recalcTimestamp=False)
cmap = font.getBestCmap()
characters = {glyph: chr(code) for code, glyph in cmap.items() if code < 128}
ligatures = {}
for lookup in font["GSUB"].table.LookupList.Lookup:
    for table in lookup.SubTable:
        table = getattr(table, "ExtSubTable", table)
        for first, values in getattr(table, "ligatures", {}).items():
            for value in values:
                components = [first, *value.Component]
                if all(glyph in characters for glyph in components):
                    name = "".join(characters[glyph] for glyph in components)
                    ligatures[name] = value.LigGlyph

names = set()
for source_dir in [root / "src/app", root / "projects/fd-ui-kit/src"]:
    for file in source_dir.rglob("*"):
        if file.suffix not in {".ts", ".html", ".json"} or file.name == "material-icons-subset.json":
            continue
        text = file.read_text(encoding="utf-8-sig")
        names.update(re.findall(r"['\"]([a-z][a-z0-9_]*)['\"]", text))
names = sorted(names.intersection(ligatures))
if not names:
    raise RuntimeError("No icon ligatures discovered in frontend sources.")

options = subset.Options()
options.layout_closure = False
options.recalc_timestamp = False
options.glyph_names = True
subsetter = subset.Subsetter(options=options)
subsetter.populate(
    glyphs=[ligatures[name] for name in names],
    unicodes=[ord(char) for char in set("".join(names))],
)
subsetter.subset(font)
font.flavor = "woff2"
target = icon_dir / "material-icons-subset.woff2"
font.save(target)
(icon_dir / "material-icons-subset.json").write_text(json.dumps(names, indent=4) + "\n", encoding="utf-8")
(icon_dir / "material-icons-LICENSE.txt").write_bytes((source.parent / "LICENSE").read_bytes())
print(f"Icon subset: {len(names)} names, {source.stat().st_size} -> {target.stat().st_size} bytes")
