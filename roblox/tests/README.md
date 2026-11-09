# Tests

These run the game logic outside Roblox with a small Luau runner and mocks of the Roblox API
(`rbxmock.luau`, `fakeengine.luau` with a simple rigid-body integrator):

- `test_world` – continent layout, airports, terrain heights
- `test_fuselage` – procedural fuselage geometry and volume scaling
- `test_campaign` – quests, cargo, partial deliveries, story gates, save/load
- `test_playthrough` – plays the whole campaign (every scripted quest chain) to the ending
- `test_flight` – a hand-placed plane takes off, climbs, runs out of fuel; a crash explodes it; terrain chunks build
- `test_hangar` – builds a plane through the hangar's placement code (snap points, surface attach, mirroring) and flies it

Run with any Luau runner that provides `__readfile`/`__loadstring`/`ARGS`, from the `roblox/` folder:
`luaurun tests/test_campaign.luau .`
