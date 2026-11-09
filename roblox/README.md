# Aviassembly for Roblox

A Luau rewrite of Aviassembly for Roblox: build planes from parts in the hangar, fly them with the original flight model, and carry cargo between the airports of the campaign.

## What's in this folder

| Path | What it is |
| --- | --- |
| `Aviassembly.rbxlx` | Ready-to-open Roblox place with all scripts. **Start here.** |
| `meshes/*.glb` | Your 3D models (86 plane parts and the 24 airport sets), exported from the Unity project for Roblox Studio's 3D importer. |
| `src/` | The Luau source (Rojo project). `src/shared/Data` is generated from the Unity project. |
| `default.project.json` | Rojo project file, if you prefer working with Rojo. |
| `tests/` | Logic tests that run outside Roblox (world, campaign, fuselage). |

## 1. Open the game (Windows or Mac)

Roblox Studio doesn't run on a Chromebook, so do this part on the Windows computer.

1. Install **Roblox Studio** and sign in.
2. Double-click `Aviassembly.rbxlx` (or File → Open from File).
3. Press **Play** (F5). The game works straight away with **coloured stand-in blocks** for parts and buildings.

## 2. Import your 3D models

1. In Studio: **Home → Import 3D** (the "3D Importer").
2. Pick `meshes/Parts_Cockpits.glb` and click **Import** with the default settings.
3. In the Explorer, drag the imported model into **ReplicatedStorage → AviassemblyMeshes**.
4. Repeat for every `.glb` in `meshes/` (6 `Parts_*` files and 4 `Airports_*` files).
5. Press Play again. Parts and airports now use your models.

The game looks pieces up by name, so don't rename the imported parts. They can sit in any sub-folder of ReplicatedStorage.

If every imported mesh shows up turned around, set `PartVisual.MeshRotationFix` in `StarterPlayer → StarterPlayerScripts → Aviassembly → Game → PartVisual`. For example `CFrame.Angles(0, math.pi, 0)`.

## 3. Saving

Progress, the current plane and five design slots save with DataStores. In Studio, turn on **Game Settings → Security → Enable Studio Access to API Services** to test saving. In a published game it works automatically.

## 4. Publish and play on a Chromebook

1. In Studio: **File → Publish to Roblox**. Make it private (or friends-only) if you like.
2. On the Chromebook, open the game from roblox.com or the Roblox app and press Play. It runs in the Roblox app (Android or web launcher). Chromebooks with Play Store support run the Roblox app with keyboard and mouse.

## Controls

**Flying:** W/S pitch, A/D roll, Q/E yaw, Space throttle up (hold near the ground to help take-off), Ctrl or Shift throttle down / brake / reverse, G landing gear, F decouple, M map, Enter airport menu, C cockpit camera, right-drag look around, wheel zoom.

**Building:** click a part in the palette, then click the plane to attach it. R/T rotate, X mirror mode, click a part to select it, Delete removes it, Ctrl+Z undo, right-drag orbit.

## How it maps to the original

- **Flight model:** ported formula by formula from PlaneContainer, PlaneController, Wing, Engine, Wheel, Balloon, Parachute, Airbreak and AirTurbine, using the original tuning values. The maths runs in Unity's coordinate space, and forces are scaled so accelerations match. Drag uses the same "look at the plane from the front" idea as DragSimulator, done with a grid of rays instead of a render.
- **Campaign:** all 30 airports, 23 cargo types and every scripted quest chain from the latest world asset (version 24). That includes the earthquake, the weather station and research facility gates, the restricted desert, the mysterious radio messages, scrap piles, perishable and fragile cargo, partial deliveries, random jobs after the ending, and research with scrap and advanced scrap.
- **Terrain:** the same continent layout, airport flattening and noise maths. Unity's built-in Perlin noise is closed source, so the hills are similar in character but not identical. Terrain is drawn as low-poly triangles.
- **Parts:** all 85 buildable parts with their real stats, attachment points and colliders. The procedural fuselage ("Body") is generated from the original algorithm and can be resized in the hangar.

## Known differences

- No sounds or music. Roblox requires audio to be uploaded to your account first.
- No paint, decals or trees yet. The rotation joint part is placed but doesn't rotate.
- Mirrored copies of asymmetric parts use the same mesh flipped upside down, because Roblox can't mirror meshes. Most parts are symmetric, so this is rarely visible.
- Building is simplified compared with the Unity gizmos: snap points plus surface attach, with 45° rotation steps. Body size uses +/- buttons.
- No ending cutscene. There's a message instead.
