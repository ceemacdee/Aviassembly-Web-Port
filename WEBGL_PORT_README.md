# Aviassembly – WebGL port

This is the AssetRipper export of Aviassembly (Unity 6000.0.69f1, Built-in render pipeline), prepared so
it builds and runs as a WebGL (browser) game.

## Play on localhost (one click)

- **Windows:** double-click `PLAY_LOCALHOST.bat`.
- **macOS / Linux:** run `./play_localhost.sh`.

The first run builds the WebGL version into `Builds/WebGL`. That needs Unity 6000.0.69f1 with WebGL Build
Support, installed through Unity Hub while you're signed in, and takes roughly 15–40 minutes. Every run after
that starts a local server and opens **http://localhost:8000** straight away. Run `PLAY_LOCALHOST.bat rebuild`
(or `./play_localhost.sh rebuild`) after changing the project. If Unity is installed somewhere non-standard,
set `UNITY_PATH`. If something goes wrong, check `build.log`.

## Build it manually

1. Open the folder in **Unity 6000.0.69f1** with the **WebGL Build Support** module installed.
2. Menu **Aviassembly → WebGL → Build WebGL**, then pick an output folder.
   - This switches the platform to WebGL, applies the player settings, and builds all six scenes.
   - On first use it also downloads Unity's `com.unity.postprocessing` package and restores the real
     post-processing shaders (see below). This needs internet access. If you're offline, download
     `com.unity.postprocessing-3.4.0.tgz` and use **Aviassembly → WebGL → Restore Post-Processing Shaders (from .tgz)**.
3. Serve the output folder over HTTP, for example `python -m http.server` inside it, and open it in a browser.
   It won't run from `file://`. It loads on any static host (itch.io, GitHub Pages, Netlify) because it uses
   gzip with a decompression fallback.

Command-line build:

```
Unity -batchmode -quit -projectPath <this folder> -executeMethod AviassemblyWebGLBuild.BuildFromCommandLine -buildPath Builds/WebGL
```

## What was changed and why

Every change is either wrapped in `#if UNITY_WEBGL` or has no effect on desktop behaviour.

| Area | Change |
| --- | --- |
| **Steam** | `Facepunch.Steamworks.Win64.dll` is a native Windows plugin. It now has a `!UNITY_WEBGL` define constraint. `Scripts/WebGL/SteamworksWebGLStubs.cs` provides the few Steam types other scripts reference. On WebGL, `SteamworksManager` reports Steam as unavailable, which the existing UI already handles (the Workshop upload button is disabled and only local designs are listed). Achievements are recorded in PlayerPrefs. |
| **Terrain threads** | Browsers can't run managed threads. `TerrainGenerationManager` queues chunk heightmaps and generates them on the main thread within a 6 ms per-frame budget. Results still go through the original callback queue. |
| **Saves** | Save games, plane designs, thumbnails, settings and keybinds are written to IndexedDB (`Plugins/WebGL/WebGLFileSync.jslib`, called after every write and delete, plus when the page is hidden). |
| **Screen** | The resolution picker is hidden and `Screen.SetResolution` is skipped (the canvas follows the window). Fullscreen uses the browser's fullscreen, which requires a click. Quit buttons are hidden. |
| **Shaders** | See below. |
| **Drag overlay** | The drag depth texture now uses point filtering (`DragSimulator.cs`), because it stores packed values. |
| **Fog** | `Fog.cs` passes the sun direction to the fog shader. |
| **Settings** | WebGL uses the same quality level as desktop (Ultra). There's a WebGL 2 template (`Assets/WebGLTemplates/Aviassembly`) that blocks the right-click menu, and a `link.xml` that protects the decompiled code from IL2CPP stripping. |

## Shaders – please review

The shipped game contains compiled shaders only, so AssetRipper exported **every** shader as a placeholder
(`//DummyShaderTextExporter`) that just draws the texture times the colour.

- **TextMeshPro (3 shaders):** replaced with Unity's own TMP shaders for Unity 6. The property lists match exactly.
- **Post-processing (26 shaders):** restored from Unity's official package by the editor tool, which writes into
  the existing files so their GUIDs stay the same. Note that the ambient occlusion mode in the profiles
  (Multi-scale) needs compute shaders, which WebGL lacks, so the post-processing stack automatically skips AO in the browser.
- **The game's own 21 shaders** (`Custom/*`, `Unlit/*`) **were rewritten from scratch**, because the originals
  can't be recovered from a build. They follow each material's properties and what the scripts feed them:
  - *Stylized Standard / Wheel / Terrain / Decal:* toon lighting. `_Shade` is the brightness of the shadowed side,
    `_Smoothness` is how soft the light-to-shadow edge is, and `_TextureOpacity` blends between flat colour and the texture.
  - *Sky + Fog:* horizon `fogColor` blends to `skyColor` with `exponent` (the `CustomFog` globals), with the
    cloud cubemap on top. Distance fog runs from `fogOffset` to the draw distance, with sun glow from `Fog.cs`.
  - *Drag:* the drag camera writes `1 − _DragFactor` (so retracted gear and parachutes add nothing, wings
    add less than fuselage). The building overlay colours airflow-facing surfaces from blue (low drag) to red (high drag).
  - *Water, glass, highlight outlines, map, explosion and grass:* stylized versions built from their properties.

  **If you still have your original `.shader` files** (an old backup, another machine, version control), copy them
  over the files in `Assets/Shader` with the same names. The `.meta` files keep the GUIDs, so materials pick
  them up automatically and the look will then match the Steam version exactly. Without them, compare against the Steam build and adjust:
  overall look (`AviassemblyCommon.cginc`, `AviassemblyStylized.cginc`), and **drag values**
  (`Custom_Drag_DragFactorReplacement.shader`). Drag feeds flight physics through `DragSimulator.calculateDragWeight = 0.6`.

## Not available in the browser

- Steam Workshop (upload and subscribed planes), Steam achievements, the Steam overlay.
- Multi-scale ambient occlusion (needs compute shaders).
- The volumetric cloud prototype (`CloudMaster`/`NoiseGenerator`, compute shaders). It's disabled in the shipped game anyway.
