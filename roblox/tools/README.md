# Exporter (Unity project → Roblox data)

Python scripts that read the AssetRipper-exported Unity project and generate `src/shared/Data/*.lua` and `meshes/*.glb`.
Set `ROOT` in `unityyaml.py` to the Unity project's `Assets` folder, then run from this folder:

```
python3 extract_world.py world.json
python3 extract_parts.py parts.json geo.pkl
python3 export_meshes.py ../meshes
python3 to_lua.py
```

`components.json` (tuning values from the scenes) is produced by a small one-off script; see `to_lua.py` for the
fields it expects. Requires Python 3 with Pillow and NumPy.
