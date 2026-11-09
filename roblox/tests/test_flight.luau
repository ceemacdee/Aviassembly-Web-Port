__loadstring(__readfile("tests/rbxmock.luau"), "rbxmock")()
local FE = __loadstring(__readfile("tests/fakeengine.luau"), "fakeengine")()
local Parts = require(shared_node("src/shared/Data/Parts"))
local Flight = require(shared_node("src/client/Game/Flight"))
local PlaneDesign = require(shared_node("src/client/Game/PlaneDesign"))
local Fuselage = require(shared_node("src/shared/Fuselage"))
local Frames = require(shared_node("src/client/Game/Frames"))
FE.services.Workspace.Gravity = Flight.Gravity

-- Rotation mapping local vectors (a1, a2) onto world vectors (b1, b2).
local function orient(a1, a2, b1, b2)
	local function basis(x, y)
		x = x.Unit; local z = x:Cross(y).Unit; y = z:Cross(x)
		return CFrame.fromMatrix(Vector3.zero, x, y, z)
	end
	return basis(b1, b2) * basis(a1, a2):Inverse()
end
local function comp(name, typ)
	for _, c in Parts[name].components do if c.type == typ then return c end end
end
-- Unity +X (right) is Roblox -X; flight direction +Z; up +Y.
local RIGHT_U = Vector3.new(-1, 0, 0)
local design = PlaneDesign.new()
local body = Fuselage.default()
body.side1.lengthOffset = Vector3.new(0, 0, 2)
body.side2.lengthOffset = Vector3.new(0, 0, 2)
design:add({ name = "Body", cf = CFrame.new(0, 0, 0), flip = false, fuselage = body })
local eng = comp("Engine", "Engine").frame
local engRot = orient(Frames.v(eng.right), Frames.v(eng.up), Vector3.new(0, 0, 1), Vector3.new(0, 1, 0))
design:add({ name = "Engine", cf = CFrame.new(0, 0, 2.6) * engRot, flip = false, parent = 1 })
local wing = comp("Wing", "Wing").data.orientation
local wingRot = orient(Frames.v(wing.right), Frames.v(wing.up), RIGHT_U, Vector3.new(0, 1, 0))
-- place the wing roots at the body sides; wings extend sideways
design:add({ name = "Wing", cf = CFrame.new(-2.2, 0, 0.5) * wingRot, flip = false, parent = 1 })
design:add({ name = "Wing", cf = CFrame.new(2.2, 0, 0.5) * wingRot, flip = false, parent = 1 })
local tail = comp("TailWing", "Wing").data.orientation
local tailRot = orient(Frames.v(tail.right), Frames.v(tail.up), RIGHT_U, Vector3.new(0, 1, 0))
design:add({ name = "TailWing", cf = CFrame.new(-0.8, 0, -2.2) * tailRot, flip = false, parent = 1 })
design:add({ name = "TailWing", cf = CFrame.new(0.8, 0, -2.2) * tailRot, flip = false, parent = 1 })
local wheel = comp("RearWheel", "Wheel").frame
local wheelRot = orient(Frames.v(wheel.up), Frames.v(wheel.forward), Vector3.new(0, 1, 0), Vector3.new(0, 0, 1))
for _, p in { Vector3.new(-1, -0.9, 1.2), Vector3.new(1, -0.9, 1.2), Vector3.new(0, -0.9, -1.8) } do
	design:add({ name = "RearWheel", cf = CFrame.new(p) * wheelRot, flip = false, parent = 1 })
end
print("design parts", #design.parts, "cost", design:cost())

local spawnY = 1.6
local flight = Flight.new(design, CFrame.new(0, spawnY, 0), {})
print(("mass unity=%.3f roblox=%.3f k=%.2f drag=%.4f retracted=%.4f fuel=%.1f wings=%d (main %d tail %d) engines=%d wheels=%d heli=%.2f fwd=%s"):format(
	flight:unityMass(), flight.root.AssemblyMass, flight:k(), flight.dragFactor, flight.dragFactorRetracted, flight.fuelCapacity, #flight.wings, #flight.mainWings, #flight.tailWings, #flight.engines, #flight.wheels, flight.helicopter, tostring(flight.localForward)))

local input = { pitch = 0, roll = 0, yaw = 0, throttle = 1, space = true, retractPressed = false, decouplePressed = false, autoSteer = true }
local dt = 1 / 60
local t = 0
local tookOff = nil
for i = 1, 60 * 40 do
	if t > 14 then input.pitch = -0.4 end -- pull up gently after the take-off run
	if t > 17 then input.pitch = 0 end
	flight:step(dt, input)
	FE.integrate(flight.root, dt, Flight.Gravity)
	t += dt
	local tel = flight:telemetry()
	if not tookOff and tel.height > 5 then tookOff = t end
	if i % 120 == 0 or flight.exploded then
		print(("t=%5.1fs speed=%6.1f m/s alt=%7.1f height=%6.1f thr=%.2f fuel=%.1f grounded=%s exploded=%s"):format(t, tel.speed, tel.altitude, tel.height, tel.throttle, tel.fuel, tostring(tel.grounded), tostring(flight.exploded)))
	end
	if flight.exploded then break end
end
print("took off at", tookOff)

-- crash test: dive into the ground
local crashed = false
flight.onExploded = function() crashed = true end
local input2 = { pitch = 1, roll = 0, yaw = 0, throttle = 1, space = false, retractPressed = false, decouplePressed = false, autoSteer = true }
for i = 1, 60 * 60 do
	flight:step(dt, input2)
	FE.integrate(flight.root, dt, Flight.Gravity)
	if flight.exploded then break end
end
print("crash test exploded:", flight.exploded, "callback:", crashed, "impact speed", FE.lastImpact)

-- terrain chunk building (WorldStreamer) with the mock instances
local WorldStreamer = require(shared_node("src/client/Game/WorldStreamer"))
local fake = setmetatable({ folder = FE.newInstance("Folder"), chunks = {}, airports = {}, scrap = {} }, { __index = WorldStreamer })
local t0 = os.clock()
local model = fake:buildChunk(0, 3, 20, true)
local wedges = 0
for _, c in model:GetDescendants() do if c.ClassName == "WedgePart" then wedges += 1 end end
print(("near chunk: %d wedges in %.2fs"):format(wedges, os.clock() - t0))
t0 = os.clock()
local far = fake:buildChunk(0, 5, 8, false)
local w2 = 0
for _, c in far:GetDescendants() do if c.ClassName == "WedgePart" then w2 += 1 end end
print(("far chunk: %d wedges in %.2fs"):format(w2, os.clock() - t0))
local ap = fake:spawnAirport(require(shared_node("src/shared/WorldMap")).Airports[2])
print("airport model children:", #ap:GetChildren())
