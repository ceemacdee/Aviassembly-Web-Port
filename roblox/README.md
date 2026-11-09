# Aviassembly for Roblox

A Luau rewrite of Aviassembly for Roblox: build planes from parts in the hangar, fly them with the original flight model, and carry cargo between the airports of the campaign.

## What's in this folder

| Path | What it is |
| --- | --- |
| `Aviassembly.rbxlx` | Ready-to-open Roblox place with all scripts. **Start here.** |
| `meshes/*.glb` | Your 3D models, exported from the Unity project for Roblox Studio's 3D importer: plane parts, mirrored copies of the parts, airports, trees and rocks, and the 60 decal images. |
| `sounds/*.ogg` | The game's 39 sound effects and music tracks, ready to import. |
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
4. Repeat for every `.glb` in `meshes/` (15 files: `Parts_*`, `Parts_Mirrored_*`, `Airports_*`, `Trees.glb` and `Decals.glb`).
5. Press Play again. Parts and airports now use your models.

The game looks pieces up by name, so don't rename the imported parts. They can sit in any sub-folder of ReplicatedStorage.

If every imported mesh shows up turned around, set `PartVisual.MeshRotationFix` in `StarterPlayer → StarterPlayerScripts → Aviassembly → Game → PartVisual`. For example `CFrame.Angles(0, math.pi, 0)`.

## 3. Import the sounds and music

Roblox only plays audio that's uploaded to your account, so do this once in Studio:

1. **View → Asset Manager**, then the **Bulk Import** button. Select all 39 files in `sounds/`.
2. When they finish uploading, open the **Audio** folder in the Asset Manager, select all of them and drag them into **ReplicatedStorage → AviassemblySounds** in the Explorer. Studio creates a Sound object for each one, named after the file.
3. Press Play. The game finds each sound by name, so keep the names.

Roblox limits how many audio files an account can upload each month. Accounts that aren't ID-verified get far fewer uploads than verified ones. If you hit the limit, import the ones you care most about first: the engine sounds (`PropellerEngine`, `JetEngine`, `RocketEngine`, `Helisound 1`), `Wind`, `Explosion`, then the music. The menu track `Ambiment.ogg` was trimmed from 23 minutes to a 6½-minute loop to fit Roblox's length limit.

## 4. Saving

Progress, the current plane and five design slots save with DataStores. In Studio, turn on **Game Settings → Security → Enable Studio Access to API Services** to test saving. In a published game it works automatically.

## 5. Publish and play on a Chromebook

1. In Studio: **File → Publish to Roblox**. Make it private (or friends-only) if you like.
2. On the Chromebook, open the game from roblox.com or the Roblox app and press Play. It runs in the Roblox app (Android or web launcher). Chromebooks with Play Store support run the Roblox app with keyboard and mouse.

## Controls

**Flying:** W/S pitch, A/D roll, Q/E yaw, Space throttle up (hold near the ground to help take-off), Ctrl or Shift throttle down / brake / reverse, G landing gear, F decouple, H/J rotation joints, M map, Enter airport menu, C cockpit camera, right-drag look around, wheel zoom.

**Building:**
- **Build mode:** pick a part, then click the plane to attach it. R/T rotate 15°, X mirror mode.
- **Selecting a part:** click it to show handles. Drag the orange rings to rotate it (Shift for 1° steps), or the blue arrows on a Body to resize it. Delete removes it, Ctrl+Z undoes.
- **Paint mode:** pick a colour and click parts.
- **Decals mode:** pick an image, colour, size and angle, then click the plane. Delete removes the decal under the mouse.

## How it maps to the original

- **Flight model:** ported formula by formula from PlaneContainer, PlaneController, Wing, Engine, Wheel, Balloon, Parachute, Airbreak and AirTurbine, using the original tuning values. The maths runs in Unity's coordinate space, and forces are scaled so accelerations match. Drag uses the same "look at the plane from the front" idea as DragSimulator, done with a grid of rays instead of a render.
- **Campaign:** all 30 airports, 23 cargo types and every scripted quest chain from the latest world asset (version 24). That includes the earthquake, the weather station and research facility gates, the restricted desert, the mysterious radio messages, scrap piles, perishable and fragile cargo, partial deliveries, random jobs after the ending, and research with scrap and advanced scrap.
- **Terrain:** the same continent layout, airport flattening and noise maths. Unity's `Mathf.PerlinNoise` is native code, so it's reconstructed as Ken Perlin's improved noise at z = 0, rescaled by Unity's constants (it returns 0.4652731 at whole-number coordinates, like Unity). Terrain is drawn as low-poly triangles, with trees, rocks and cacti scattered by the original rules.
- **Parts:** all 85 buildable parts with their real stats, attachment points and colliders. The procedural fuselage ("Body") is generated from the original algorithm and can be resized in the hangar.

## Remaining differences

- The noise reconstruction matches Unity's documented behaviour, but there's no published bit-for-bit reference, so treat the hills as "very close" rather than guaranteed identical.
- Terrain, trees and rocks load in a 3 × 3 km area around the plane, with coarser terrain beyond that. Unity streamed further out, but Roblox would struggle with that many parts. Lower **Settings → Trees & rocks** if the game runs slowly on the Chromebook.
- Painted surfaces use flat colours. The original's subtle texture on paintable panels is baked into the default colour instead, because Roblox can't tint a textured mesh.
