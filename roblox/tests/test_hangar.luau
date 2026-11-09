__loadstring(__readfile("tests/rbxmock.luau"), "rbxmock")()
local FE = __loadstring(__readfile("tests/fakeengine.luau"), "fakeengine")()
local Campaign = require(shared_node("src/shared/Campaign"))
local Hangar = require(shared_node("src/client/Game/Hangar"))
local Flight = require(shared_node("src/client/Game/Flight"))
local PlaneDesign = require(shared_node("src/client/Game/PlaneDesign"))
local campaign = Campaign.new("Normal", true) -- creative: everything unlocked, money not spent
local design = PlaneDesign.new()
local origin = CFrame.new(0, 3.5, 0)
local hangar = Hangar.new(campaign, design, origin, { fly = function() end, menu = function() end, designs = function() end, changed = function() end })
local function aim(from, to)
	FE.mouseRay = { Origin = from, Direction = (to - from).Unit }
end
local function place(name, from, to)
	hangar:setTool(name)
	aim(from, to)
	local before = #design.parts
	hangar:updateGhost()
	hangar:place()
	local added = #design.parts - before
	print(("%-14s placed %d part(s)%s"):format(name, added, if added == 0 then "  <-- FAILED" else ""))
	return design.parts[#design.parts - added + 1], added
end
-- 1. a cockpit as the base part, then a body behind it via snap points
local cockpit = place("SmallPlaneCockpit", Vector3.new(0, 10, 0), origin.Position)
local cpSnaps = require(shared_node("src/shared/Data/Parts")).SmallPlaneCockpit.snapPoints
for i, sp in cpSnaps do print("  cockpit snap", i, "pos", sp.pos[1], sp.pos[2], sp.pos[3], "fwd", sp.forward[1], sp.forward[2], sp.forward[3]) end
-- aim at the cockpit's back snap point
local back = nil
for _, sp in cpSnaps do if sp.forward[3] < -0.5 then back = Vector3.new(sp.pos[1], sp.pos[2], sp.pos[3]) end end
local body = place("Body", origin.Position + back + Vector3.new(0, 6, -3), origin.Position + back)
if body then print("  body cf", tostring(body.cf.Position), "look Z", tostring(body.cf.ZVector), "parent", body.parent) end
-- 2. engine on the cockpit's front snap
local front = nil
for _, sp in cpSnaps do if sp.forward[3] > 0.5 then front = Vector3.new(sp.pos[1], sp.pos[2], sp.pos[3]) end end
if front then place("SmallPropEngine", origin.Position + front + Vector3.new(0, 4, 4), origin.Position + front) end
-- 3. wings on the body sides (mirror mode creates the other side)
local bodyCenter = origin * body.cf.Position
local wing = place("Wing", bodyCenter + Vector3.new(-8, 0, 0), bodyCenter + Vector3.new(0, 0, 0))
if wing then
	local Frames = require(shared_node("src/client/Game/Frames"))
	local o = require(shared_node("src/shared/Data/Parts")).Wing.components[1].data.orientation
	print("  wing pos", tostring(wing.cf.Position), "flip", wing.flip, "orientation up (world)", tostring(wing.cf:VectorToWorldSpace(Frames.v(o.up))))
	local m = design:get(wing.mirror)
	print("  mirror pos", tostring(m.cf.Position), "flip", m.flip, "orientation up (world)", tostring(m.cf:VectorToWorldSpace(Frames.v(o.up))))
end
-- 4. tail wings at the back of the body
place("TailWing", bodyCenter + Vector3.new(-6, 0, -0.35), bodyCenter + Vector3.new(0, 0, -0.35))
-- 5. wheels under the cockpit and under the tail
place("RearWheel", origin.Position + Vector3.new(-0.6, -6, 0.2), origin.Position + Vector3.new(-0.6, 0, 0.2))
place("RearWheel", bodyCenter + Vector3.new(0, -6, -0.35), bodyCenter + Vector3.new(0, 0, -0.35))
print("design parts:", #design.parts)
local saved = design:serialize()
local back2 = PlaneDesign.deserialize(saved)
print("serialize round trip parts:", #back2.parts)

-- fly it
local lowest = 0
for _, b in hangar.built do
	for _, v in b.visuals do lowest = math.min(lowest, v.Position.Y - v.Size.Y / 2 - origin.Y) end
end
local flight = Flight.new(design, CFrame.new(0, -lowest + 0.6, 0), { campaign = campaign })
print(("mass %.2f t, wings %d (main %d tail %d), engines %d, wheels %d, drag %.3f, fwd %s"):format(flight:unityMass() * 15, #flight.wings, #flight.mainWings, #flight.tailWings, #flight.engines, #flight.wheels, flight.dragFactor, tostring(flight.localForward)))
local input = { pitch = 0, roll = 0, yaw = 0, throttle = 1, space = true, retractPressed = false, decouplePressed = false, autoSteer = true }
local dt, t, took = 1 / 60, 0, nil
for i = 1, 60 * 30 do
	if t > 15 then input.pitch = -0.3 end
	flight:step(dt, input)
	FE.integrate(flight.root, dt, Flight.Gravity)
	t += dt
	local tel = flight:telemetry()
	if not took and tel.height > 3 then took = t end
	if i % 180 == 0 or flight.exploded then
		print(("t=%4.1fs speed=%6.1f height=%7.1f exploded=%s"):format(t, tel.speed, tel.height, tostring(flight.exploded)))
	end
	if flight.exploded then break end
end
print("took off at", took)
hangar:destroy()

-- paint: click the body in paint mode
local hangar2 = Hangar.new(campaign, design, origin, { fly = function() end, menu = function() end, designs = function() end, changed = function() end })
hangar2:setMode("paint")
hangar2.paintColor = { 0.86, 0.29, 0.17 }
aim(bodyCenter + Vector3.new(0, 6, 0), bodyCenter)
hangar2:paintUnderMouse()
print("painted body colour:", design:get(body.id).color and table.concat(design:get(body.id).color, ","))
-- decals: place one on the wing (mirror mode makes two)
hangar2:setMode("decal")
hangar2.decalImage = "Star"
local wingCenter = origin * wing.cf.Position
aim(wingCenter + Vector3.new(0, 6, 0.3), wingCenter + Vector3.new(0, 0, 0.3))
hangar2:placeDecal()
print("decals placed:", #design.decals, "visuals:", #hangar2.decalVisuals)
local round = PlaneDesign.deserialize(design:serialize())
print("decals after save/load:", #round.decals)
hangar2:destroy()

-- rotation joint carrying an engine: J turns the thrust direction
local d2 = PlaneDesign.new()
d2:add({ name = "Body", cf = CFrame.new(), flip = false, fuselage = require(shared_node("src/shared/Fuselage")).default() })
d2:add({ name = "RotationJoint", cf = CFrame.new(0, 0.8, 0), flip = false, parent = 1 })
d2:add({ name = "SmallPropEngine", cf = CFrame.new(0, 1.6, 0), flip = false, parent = 2 })
local f2 = Flight.new(d2, CFrame.new(0, 50, 0), {})
local function engineDir() return f2:dirU(f2:engineDirLocal(f2.engines[1])) end
local before = engineDir()
local inp = { pitch = 0, roll = 0, yaw = 0, throttle = 0, space = false, retractPressed = false, decouplePressed = false, rotator = 1, autoSteer = true }
for _ = 1, 30 do f2:step(1 / 60, inp) end
local after = engineDir()
print(("rotators=%d moving welds=%d angle=%.1f° engine dir before=%s after=%s"):format(#f2.rotators, #f2.movingWelds, f2.rotators[1] and f2.rotators[1].angle or -1, tostring(before), tostring(after)))
