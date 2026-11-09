--!strict
-- Flight physics, ported from PlaneContainer, PlaneController, Wing, Engine, Wheel, Balloon, Parachute, Airbreak,
-- AirTurbine, Decoupler, PartExploder and DragSimulator.
--
-- The Roblox world is the Unity world mirrored on X. To keep every formula identical to the original, all
-- maths here runs in Unity space: positions/velocities/forces are converted with U(v) (mirror X) and angular
-- quantities (pseudo-vectors) with A(v). Unity masses are rb.mass (part weight / 15); forces are scaled by
-- k = Roblox assembly mass / Unity mass so accelerations match.
local Workspace = game:GetService("Workspace")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Parts = require(Shared.Data.Parts)
local Constants = require(Shared.Data.Constants)
local Fuselage = require(Shared.Fuselage)
local MathUtil = require(Shared.Util.MathUtil)
local Frames = require(script.Parent.Frames)
local PartVisual = require(script.Parent.PartVisual)
local PlaneDesign = require(script.Parent.PlaneDesign)

local PC = Constants.PlaneContainer
local CTRL = Constants.PlaneController
local DRAG = Constants.DragSimulator

local Flight = {}
Flight.__index = Flight

Flight.Gravity = PC.gravityForce * PC.planeGravityMultiplier -- 25.5 studs/s^2 (1 stud = 1 m)
local GRAVITY_MULT = PC.planeGravityMultiplier
local UNITY_STEP = 0.02 -- Unity fixed timestep the per-step constants were tuned for

local function U(v: Vector3): Vector3
	return Vector3.new(-v.X, v.Y, v.Z)
end
local function A(v: Vector3): Vector3
	return Vector3.new(v.X, -v.Y, -v.Z)
end
local clamp01 = MathUtil.clamp01
local lerp = MathUtil.lerp
local sign = MathUtil.sign
local function safeUnit(v: Vector3): Vector3
	local m = v.Magnitude
	return if m > 1e-9 then v / m else Vector3.zero
end
local function signedAngle(from: Vector3, to: Vector3, axis: Vector3): number
	local fm, tm = from.Magnitude, to.Magnitude
	if fm < 1e-9 or tm < 1e-9 then
		return 0
	end
	local a = math.deg(math.acos(math.clamp(from:Dot(to) / (fm * tm), -1, 1)))
	return if axis:Dot(from:Cross(to)) < 0 then -a else a
end

export type Input = {
	pitch: number,
	roll: number,
	yaw: number,
	throttle: number, -- -1, 0, 1
	space: boolean,
	retractPressed: boolean,
	decouplePressed: boolean,
	autoSteer: boolean,
}

-- ------------------------------------------------------------------------------------------- construction
function Flight.new(design: any, spawnCFrame: CFrame, options: any)
	local self = setmetatable({}, Flight)
	self.options = options or {}
	self.infiniteFuel = self.options.infiniteFuel == true
	self.campaign = self.options.campaign
	self.exploded = false
	self.currentThrottle = 0
	self.throttleVelocity = 0
	self.rudderInputSmooth = 0
	self.rudderVelocity = 0
	self.prevSpeed = 0
	self.prevVelocity = Vector3.zero
	self.liftMultiplier = 1
	self.helicopter = 0
	self.currentLift = 0
	self.retracted = false
	self.yawDrag = PC.yawDrag
	self.rollForce = CTRL.rollForce
	self.yawForce = CTRL.yawForce
	self.time = 0

	local model = Instance.new("Model")
	model.Name = "Plane"
	self.model = model
	self.entries = {}
	self.parts = {} -- per placed part: { entry, built, comps, weight, alive }

	-- Build all parts at their world positions.
	local totalWeight = 0
	local comSum = Vector3.zero
	for _, e in design.parts do
		local cf = spawnCFrame * e.cf
		local built = PartVisual.create(e.name, cf, e.flip, e.fuselage, { color = e.color and Color3.new(e.color[1], e.color[2], e.color[3]) or nil })
		built.model.Parent = model
		local stats = PlaneDesign.partStats(e)
		local center = Vector3.zero
		local n = 0
		for _, v in built.visuals do
			center += v.Position
			n += 1
		end
		center = if n > 0 then center / n else cf.Position
		local p = { entry = e, built = built, weight = stats.weight, cargo = stats.cargo, alive = true, cf = cf, center = center }
		table.insert(self.parts, p)
		totalWeight += stats.weight
		comSum += center * stats.weight
	end
	local com = if totalWeight > 0 then comSum / totalWeight else spawnCFrame.Position

	-- Root at the centre of mass, oriented like the design frame.
	local root = Instance.new("Part")
	root.Name = "PlaneRoot"
	root.Size = Vector3.new(0.2, 0.2, 0.2)
	root.Transparency = 1
	root.CanCollide = false
	root.CanQuery = false
	root.CanTouch = false
	root.Massless = true
	root.CFrame = spawnCFrame.Rotation + com
	root.Parent = model
	model.PrimaryPart = root
	self.root = root

	-- Ballast carries fuel and cargo mass at the centre of mass (rb.mass = parts + fuel*0.4/15 + cargo/15).
	local ballast = Instance.new("Part")
	ballast.Name = "Ballast"
	ballast.Size = Vector3.new(0.5, 0.5, 0.5)
	ballast.Transparency = 1
	ballast.CanCollide = false
	ballast.CanQuery = false
	ballast.CanTouch = false
	ballast.CFrame = root.CFrame
	ballast.Parent = model
	self.ballast = ballast

	-- Colliders get a density so each part's mass is proportional to its weight (keeps the CoM like Unity's).
	for _, p in self.parts do
		local data = Parts[p.entry.name]
		local isWheel = false
		for _, c in data.components do
			if c.type == "Wheel" then
				isWheel = true
			end
		end
		local volume = 0
		for _, c in p.built.colliders do
			volume += c.Size.X * c.Size.Y * c.Size.Z
		end
		local density = math.clamp(p.weight / math.max(volume, 1e-3), 0.01, 100)
		for _, c in p.built.colliders do
			c.CustomPhysicalProperties = PhysicalProperties.new(density, 0.3, 0, 1, 1)
			c.CanCollide = not isWheel
			c.Massless = false
		end
		if #p.built.colliders == 0 then
			-- make sure the part still has mass
			local m = Instance.new("Part")
			m.Name = "Collider"
			m.Size = Vector3.new(0.3, 0.3, 0.3)
			m.Transparency = 1
			m.CanCollide = false
			m.CanQuery = false
			m.CFrame = CFrame.new(p.center)
			m.CustomPhysicalProperties = PhysicalProperties.new(math.clamp(p.weight / 0.027, 0.01, 100), 0.3, 0, 1, 1)
			m.Parent = p.built.model
			table.insert(p.built.colliders, m)
		end
	end
	local spinning = {}
	for _, p in self.parts do
		p.spinners = {}
		for _, s in p.built.spinners do
			spinning[s.part] = { p = p, axis = s.axis }
		end
	end
	for _, d in model:GetDescendants() do
		if d:IsA("BasePart") and d ~= root then
			local spin = spinning[d]
			if spin then
				-- propellers use a Weld whose offset is animated
				local w = Instance.new("Weld")
				w.Part0 = root
				w.Part1 = d
				w.C0 = root.CFrame:Inverse() * d.CFrame
				w.Parent = d
				local axisWorld = spin.p.cf:VectorToWorldSpace(spin.axis)
				table.insert(spin.p.spinners, { weld = w, baseC0 = w.C0, axis = d.CFrame:VectorToObjectSpace(axisWorld).Unit })
			else
				local w = Instance.new("WeldConstraint")
				w.Part0 = root
				w.Part1 = d
				w.Parent = d
			end
			d.Anchored = false
		end
	end
	root.Anchored = false
	model.Parent = Workspace

	-- Physics components in root-local Roblox space.
	local rootInv = root.CFrame:Inverse()
	self.wings, self.engines, self.wheels, self.balloons, self.drags, self.turbines, self.decouplers = {}, {}, {}, {}, {}, {}, {}
	self.fuelCapacity = 1
	self.batteryCapacity = 0.05
	self.cargoVolume = 0
	self.hasBalloons = false
	self.hasRetractableGear = false
	self.cockpitForward = nil :: Vector3?
	for _, p in self.parts do
		local e = p.entry
		local data = Parts[e.name]
		if data.retractable then
			self.hasRetractableGear = true
			p.retractable = true
		end
		if string.find(e.name, "Cock") then
			self.cockpitForward = rootInv:VectorToWorldSpace(p.cf.ZVector)
		end
		self.cargoVolume += math.abs(p.cargo)
		for _, comp in data.components do
			if comp.active ~= false then
				local f = comp.frame
				local lp = rootInv * Frames.worldPoint(p.cf, e.flip, Frames.v(f.pos))
				local function dir(v)
					return rootInv:VectorToWorldSpace(p.cf:VectorToWorldSpace(Frames.v(v)))
				end
				local scaleSign = if comp.path == "" and e.flip then -1 else 1
				local d = comp.data
				local c = { part = p, data = d, pos = lp, right = dir(f.right), up = dir(f.up), forward = dir(f.forward), scaleSign = scaleSign }
				if comp.type == "Wing" then
					local o = d.orientation
					c.oPos = rootInv * Frames.worldPoint(p.cf, e.flip, Frames.v(o.pos))
					c.oRight, c.oUp, c.oForward = dir(o.right), dir(o.up), dir(o.forward)
					c.area = d.area[1] * d.area[2]
					table.insert(self.wings, c)
					self.fuelCapacity += math.abs(d.fuel or 0)
				elseif comp.type == "Engine" then
					c.throttle = 0
					c.maxPower = 1
					table.insert(self.engines, c)
				elseif comp.type == "Wheel" then
					c.prevHit = d.suspentionLength
					c.weightSamples = {}
					table.insert(self.wheels, c)
				elseif comp.type == "FuelTank" then
					self.fuelCapacity += math.abs(d.volume or 0)
				elseif comp.type == "Battery" then
					self.batteryCapacity += math.abs(d.capacity or 0)
				elseif comp.type == "Balloon" then
					self.hasBalloons = true
					table.insert(self.balloons, c)
				elseif comp.type == "Parachute" then
					c.multiplier = 1.5
					table.insert(self.drags, c)
				elseif comp.type == "Airbreak" then
					c.multiplier = 1.25
					table.insert(self.drags, c)
				elseif comp.type == "AirTurbine" then
					table.insert(self.turbines, c)
				elseif comp.type == "Decoupler" then
					table.insert(self.decouplers, c)
				end
			end
		end
	end
	self.fuel = self.fuelCapacity
	self.electricity = self.batteryCapacity

	-- Local bounding box (for ground distance and the camera).
	local minV, maxV = Vector3.one * math.huge, -Vector3.one * math.huge
	for _, p in self.parts do
		for _, v in p.built.visuals do
			local lp = rootInv * v.Position
			local h = v.Size / 2
			minV = minV:Min(lp - h)
			maxV = maxV:Max(lp + h)
		end
	end
	if minV.X == math.huge then
		minV, maxV = -Vector3.one, Vector3.one
	end
	self.boundsCenter = (minV + maxV) / 2
	self.boundsHalf = (maxV - minV) / 2
	self.planeLength = math.max(self.boundsHalf.X, self.boundsHalf.Z)

	-- Forward direction (PlaneContainer.UpdateForwardDirection), in root-local Roblox space.
	local forward = Vector3.zAxis
	if self.cockpitForward then
		forward = self.cockpitForward
	end
	local engineSum = Vector3.zero
	for _, c in self.engines do
		engineSum += self:engineDirLocal(c)
	end
	if #self.engines > 0 then
		local avg = safeUnit(engineSum / #self.engines)
		self.helicopter = math.abs(avg:Dot(Vector3.yAxis))
		if self.helicopter <= 0.5 then
			forward = avg
		end
	end
	self.localForward = safeUnit(forward)
	if #self.wings == 0 then
		self.helicopter = 1
	end

	self:classifyWings()
	self:updateControllability()
	self:updateMass()
	self.dragFactor, self.dragFactorRetracted = self:computeDrag()
	self.drag = self.dragFactor
	return self
end

-- ------------------------------------------------------------------------------------------- helpers (Unity space)
function Flight:engineDirLocal(c: any): Vector3
	local d = if c.data.helicopterRotor == 1 then c.up * c.scaleSign else c.right
	return d
end

function Flight:cf(): CFrame
	return self.root.CFrame
end

function Flight:worldU(localPos: Vector3): Vector3
	return U(self.root.CFrame * localPos)
end

function Flight:dirU(localDir: Vector3): Vector3
	return U(self.root.CFrame:VectorToWorldSpace(localDir))
end

function Flight:comU(): Vector3
	return U(self.root.AssemblyCenterOfMass)
end

function Flight:velocityU(): Vector3
	return U(self.root.AssemblyLinearVelocity)
end

function Flight:pointVelocityU(posU: Vector3): Vector3
	return U(self.root:GetVelocityAtPosition(U(posU)))
end

function Flight:upU(): Vector3
	return U(self.root.CFrame.UpVector)
end

function Flight:forwardU(): Vector3
	return self:dirU(self.localForward)
end

function Flight:unityMass(): number
	local w = 0
	for _, p in self.parts do
		if p.alive then
			w += p.weight
		end
	end
	local cargo = if self.campaign then self.campaign:cargoMass() else 0
	return (w + self.fuel * PC.fuelWeight + cargo) / 15
end

function Flight:updateMass()
	-- Ballast = fuel + cargo, with the same proportion between Roblox and Unity mass as the parts.
	local w = 0
	for _, p in self.parts do
		if p.alive then
			w += p.weight
		end
	end
	local cargo = if self.campaign then self.campaign:cargoMass() else 0
	local extra = self.fuel * PC.fuelWeight + cargo
	local partsMass = 0
	for _, p in self.parts do
		if p.alive then
			for _, c in p.built.colliders do
				partsMass += c:GetMass()
			end
		end
	end
	local perWeight = if w > 0 then partsMass / w else 1
	local targetMass = math.max(extra * perWeight, 0.001)
	local density = math.clamp(targetMass / (0.5 * 0.5 * 0.5), 0.01, 100)
	local size = math.clamp((targetMass / density) ^ (1 / 3), 0.05, 20)
	if math.abs(self.ballast.Size.X - size) > 0.001 then
		self.ballast.Size = Vector3.one * size
	end
	if not self.lastBallastDensity or math.abs(self.lastBallastDensity - density) > 0.005 * density then
		self.ballast.CustomPhysicalProperties = PhysicalProperties.new(density, 0.3, 0, 1, 1)
		self.lastBallastDensity = density
	end
	self.unityMassValue = self:unityMass()
end

function Flight:k(): number
	return self.root.AssemblyMass / math.max(self.unityMassValue or self:unityMass(), 1e-4)
end

-- rb.AddForceAtPosition (Unity-space force and position)
function Flight:addForceAt(forceU: Vector3, posU: Vector3, dt: number)
	if forceU ~= forceU or forceU.Magnitude > 1e7 then
		return
	end
	self.root:ApplyImpulseAtPosition(U(forceU) * self:k() * dt, U(posU))
end

function Flight:addForce(forceU: Vector3, dt: number)
	self:addForceAt(forceU, self:comU(), dt)
end

-- ApplyInertiaTensorAjustedTorque: a torque of I*x is an angular acceleration of x.
function Flight:addAngularAccel(accelU: Vector3, dt: number)
	if accelU ~= accelU then
		return
	end
	self.root.AssemblyAngularVelocity += A(accelU) * dt
end

function Flight:boundsWorldU(): (Vector3, number)
	local cf = self.root.CFrame
	local c = cf * self.boundsCenter
	local h = self.boundsHalf
	local ext = math.abs(cf.RightVector.Y) * h.X + math.abs(cf.UpVector.Y) * h.Y + math.abs(cf.LookVector.Y) * h.Z
	return U(c), ext
end

function Flight:rayParams(): RaycastParams
	if not self.params then
		local params = RaycastParams.new()
		params.FilterType = Enum.RaycastFilterType.Exclude
		params.FilterDescendantsInstances = { self.model }
		params.IgnoreWater = true
		self.params = params
	end
	return self.params
end

function Flight:distanceFromGround(): number
	local centerU, extY = self:boundsWorldU()
	local hit = Workspace:Raycast(U(centerU), Vector3.new(0, -5000, 0), self:rayParams())
	if hit then
		return hit.Distance - extY
	end
	return math.huge
end

function Flight:isGrounded(): boolean
	return self:distanceFromGround() < 1
end

-- ------------------------------------------------------------------------------------------- drag (DragSimulator)
-- Unity rendered the plane from the front with a replacement shader and averaged (1 - _DragFactor). Here a grid
-- of rays along the flight direction finds the first surface hit at each "pixel".
local function srgb(c: number): number
	return if c <= 0.0031308 then c * 12.92 else 1.055 * c ^ (1 / 2.4) - 0.055
end

function Flight:computeDrag(): (number, number)
	local function surfaceDrag(inst: Instance): number
		local model = inst:FindFirstAncestorOfClass("Model")
		local name = model and model:GetAttribute("PartName")
		local data = name and Parts[name]
		if not data then
			return 0
		end
		if data.dragFactor and data.dragFactor > 0 then
			return data.dragFactor
		end
		return if data.category == "Wings" then 0.5 else 0
	end
	local function run(excludeRetractable: boolean): number
		local cf = self.root.CFrame
		local fwd = cf:VectorToWorldSpace(self.localForward)
		local upRef = if math.abs(fwd.Y) > 0.95 then cf.RightVector else Vector3.yAxis
		local right = fwd:Cross(upRef).Unit
		local up = right:Cross(fwd).Unit
		local center = cf * self.boundsCenter
		local radius = self.boundsHalf.Magnitude
		-- projected extents
		local minX, maxX, minY, maxY = math.huge, -math.huge, math.huge, -math.huge
		for _, p in self.parts do
			for _, v in p.built.visuals do
				local d = v.Position - center
				local hx = math.abs(v.CFrame.RightVector:Dot(right)) * v.Size.X / 2 + math.abs(v.CFrame.UpVector:Dot(right)) * v.Size.Y / 2 + math.abs(v.CFrame.LookVector:Dot(right)) * v.Size.Z / 2
				local hy = math.abs(v.CFrame.RightVector:Dot(up)) * v.Size.X / 2 + math.abs(v.CFrame.UpVector:Dot(up)) * v.Size.Y / 2 + math.abs(v.CFrame.LookVector:Dot(up)) * v.Size.Z / 2
				minX = math.min(minX, d:Dot(right) - hx)
				maxX = math.max(maxX, d:Dot(right) + hx)
				minY = math.min(minY, d:Dot(up) - hy)
				maxY = math.max(maxY, d:Dot(up) + hy)
			end
		end
		if minX == math.huge then
			return 0
		end
		local w, h = maxX - minX, maxY - minY
		local area = w * h
		local step = math.max(1 / 30, math.sqrt(area / 3000))
		local include = {}
		for _, p in self.parts do
			if not (excludeRetractable and p.retractable) then
				for _, v in p.built.visuals do
					table.insert(include, v)
				end
			end
		end
		local params = RaycastParams.new()
		params.FilterType = Enum.RaycastFilterType.Include
		params.FilterDescendantsInstances = include
		local sum, count = 0, 0
		local x = minX + step / 2
		while x < maxX do
			local y = minY + step / 2
			while y < maxY do
				local origin = center + right * x + up * y + fwd * (radius + 1)
				local hit = Workspace:Raycast(origin, -fwd * (radius * 2 + 2), params)
				if hit then
					sum += srgb(1 - surfaceDrag(hit.Instance))
				end
				count += 1
				y += step
			end
			x += step
		end
		if count == 0 then
			return 0
		end
		return sum / count * area * DRAG.dragMultiplier
	end
	local extended = lerp(DRAG.defaultDrag, run(false) * 0.04, DRAG.calculateDragWeight)
	local retracted = lerp(DRAG.defaultDrag, run(true) * 0.04, DRAG.calculateDragWeight)
	return extended, retracted
end

-- ------------------------------------------------------------------------------------------- wings & controls
function Flight:classifyWings()
	self.mainWings, self.tailWings = {}, {}
	local comU = self:comU()
	local fwd = self:forwardU()
	local up = self:upU()
	for _, w in self.wings do
		local pos = self:worldU(w.pos)
		local t = (pos - comU):Dot(fwd)
		local closest = comU + fwd * t
		local v = comU - closest
		local mag = v.Magnitude
		local num = fwd:Dot(safeUnit(v))
		if mag > self.planeLength * 0.25 and num > 0 and math.abs(self:dirU(w.oUp):Dot(up)) > 0.75 then
			table.insert(self.tailWings, { wing = w, distance = mag })
		else
			table.insert(self.mainWings, w)
		end
	end
end

function Flight:centerOfPressureU(): Vector3
	local up = self:upU()
	local comU = self:comU()
	local sum, total = Vector3.zero, 0
	for _, w in self.wings do
		if w.part.alive then
			local oUp = self:dirU(w.oUp)
			local d = math.abs(Vector3.yAxis:Dot(oUp))
			local force = w.area * (w.data.liftForce * 100) * d * d
			local t = math.abs(oUp:Dot(up))
			local origin = comU:Lerp(self:worldU(w.oPos), t)
			sum += origin * force
			total += force
		end
	end
	if total < 1e-9 then
		return comU
	end
	return sum / total
end

function Flight:liftOriginU(): Vector3
	local fwd, up = self:forwardU(), self:upU()
	local side = fwd:Cross(up)
	local comU = self:comU()
	local num = (self:centerOfPressureU() - comU):Dot(side)
	return comU + side * num * PC.liftSymmetry
end

function Flight:thrustOriginU(): Vector3
	local comU = self:comU()
	if #self.engines == 0 then
		return comU
	end
	local sum, n = Vector3.zero, 0
	for _, e in self.engines do
		if e.part.alive then
			sum += self:worldU(e.pos)
			n += 1
		end
	end
	if n == 0 then
		return comU
	end
	local t = lerp(PC.thrustSymmetry, 0, self.helicopter)
	return comU:Lerp(sum / n, t)
end

function Flight:updateWing(w: any, dt: number, aoaOffset: number)
	local oPos = self:worldU(w.oPos)
	local oRight, oUp, oForward = self:dirU(w.oRight), self:dirU(w.oUp), self:dirU(w.oForward)
	local pointVelocity = self:pointVelocityU(oPos)
	local airflow = -pointVelocity
	local lx, ly, lz = airflow:Dot(oRight), airflow:Dot(oUp), airflow:Dot(oForward)
	local aoa = math.deg(math.atan2(ly, math.abs(lz))) * (-sign(lz))
	local extra = math.max(aoaOffset - math.abs(aoa), 0)
	local lhs = -safeUnit(airflow):Cross(oRight) * w.scaleSign
	aoa += extra * sign(lhs:Dot(Vector3.yAxis))
	w.currentAngleOfAttack = aoa
	local speed = airflow.Magnitude
	-- ApplyLift
	if speed > w.data.maxSpeed * 0.8 then
		self.warnings["Wing stress too high"] = "Slow down to reduce stress"
	end
	if speed > w.data.maxSpeed then
		self:explodePart(w.part)
		return
	end
	local liftDir = -safeUnit(airflow):Cross(oRight)
	local lift = math.sin(math.abs(math.rad(aoa)) * 2) * sign(aoa) * speed * speed * w.area * w.data.liftForce
	local force = liftDir * lift * w.scaleSign * GRAVITY_MULT * self.liftMultiplier
	self:addForceAt(force, self:liftOriginU(), dt)
	self.currentLift += w.data.liftForce * speed * speed * w.area * 0.2
	-- ApplyDrag
	local drag = (0.5 - math.cos(math.rad(math.abs(aoa))) * 0.5) * speed * speed * w.area * w.data.dragForce
	self:addForceAt(-pointVelocity * drag, self:comU(), dt)
end

function Flight:updateControls(input: Input, dt: number)
	local v = self:velocityU()
	local speed = v.Magnitude
	local fwd, up = self:forwardU(), self:upU()
	local mass15 = self:unityMass() * 15
	local controlHandicap = 0.4 + clamp01(MathUtil.inverseLerp(50, 0, mass15)) * 0.6
	local num2 = safeUnit(v):Dot(fwd)
	local num3 = clamp01(speed / 50)
	local pitchAxis = safeUnit((-fwd):Cross(up))
	local groundFactor = clamp01(self:distanceFromGround() / 30)
	local rollSurface = clamp01(self.rollControlSurfaceLift * 150 * controlHandicap + self.helicopter)
	local roll = self.rollForce * lerp(num3, 1, self.helicopter)
	if input.autoSteer then
		roll *= groundFactor
	end
	self:addAngularAccel(-fwd * input.roll * roll * rollSurface, dt)
	local pitchSurface = clamp01(controlHandicap + self.pitchControlSurfaceLift * 40)
	local pitch = CTRL.pitchForce * lerp(num3 * num2, 1, self.helicopter)
	self:addAngularAccel(pitchAxis * input.pitch * pitch * pitchSurface, dt)
	local num10 = self.yawForce * clamp01(speed / 10) * num2
	local grounded = self:isGrounded()
	local num11 = if grounded then 1.25 else 1
	self:addAngularAccel(up * self.rudderInputSmooth * num10 * num11, dt)
	if input.autoSteer then
		local n2 = safeUnit(Vector3.yAxis:Cross(fwd))
		local n3 = safeUnit(fwd:Cross(n2))
		self.yawDrag = 0
		self.rollForce = 5
		self.yawForce = 1.5
		local currentRoll = self:currentRoll()
		local t = clamp01(MathUtil.inverseLerp(0.95, 0.99, math.abs(currentRoll)))
		local s = t * t * (3 - 2 * t)
		currentRoll *= 1 - s
		if math.abs(fwd:Dot(Vector3.yAxis)) > 0.98 then
			currentRoll = 0
		end
		local f = clamp01(math.abs(currentRoll) - 0.0125) * sign(currentRoll) * CTRL.rollTurnForce
		f = math.sqrt(math.abs(f)) * sign(f)
		self:addAngularAccel(n3 * f, dt)
		local n12 = math.sqrt(math.abs(currentRoll)) * sign(currentRoll)
		self:addAngularAccel(fwd * n12 * CTRL.rollRestoreForce, dt)
		self:addAngularAccel(n3 * (1 - groundFactor) * self.yawForce * num10 * input.roll * num11, dt)
	else
		self.yawDrag = 0.2
		self.rollForce = 3.5
		self.yawForce = 6
	end
end

function Flight:currentRoll(): number
	local fwd, up = self:forwardU(), self:upU()
	local n = safeUnit(MathUtil.projectOnPlane(Vector3.yAxis, fwd))
	local num = 1 - clamp01((up:Dot(n) + 1) * 0.5)
	local s = sign(signedAngle(up, n, fwd))
	return num * s
end

function Flight:stabilise(dt: number)
	local fwd = self:forwardU()
	local v = self:velocityU()
	-- PlaneStabilisationForce
	local m = 1 - self.helicopter
	local axis = safeUnit(safeUnit(v):Cross(fwd))
	local num = (1 - math.abs(fwd:Dot(safeUnit(v)))) * sign(signedAngle(fwd, safeUnit(v), axis))
	num *= v.Magnitude / 50
	num *= CTRL.stablisationForce
	if axis.Magnitude > 0 then
		self:addAngularAccel(axis * num * m, dt)
	end
	-- HelicopterStabilisationForce
	local up = self:upU()
	local hAxis = safeUnit(up:Cross(Vector3.yAxis))
	local hNum = (1 - math.abs(Vector3.yAxis:Dot(up))) * sign(signedAngle(up, Vector3.yAxis, hAxis)) * CTRL.helicopterStabilisationForce
	if hAxis.Magnitude > 0 then
		self:addAngularAccel(hAxis * hNum * self.helicopter, dt)
	end
end

function Flight:updateControllability()
	self.rollControlSurfaceLift = 0
	self.pitchControlSurfaceLift = 0
	for _, w in self.mainWings do
		if w.part.alive then
			self.rollControlSurfaceLift += w.data.liftForce
		end
	end
	for _, t in self.tailWings do
		if t.wing.part.alive then
			self.pitchControlSurfaceLift += t.wing.data.liftForce * t.distance
		end
	end
	self.rollControlSurfaceLift = math.max(self.pitchControlSurfaceLift, self.rollControlSurfaceLift)
end

-- ------------------------------------------------------------------------------------------- engines
function Flight:updateEngine(e: any, input: Input, dt: number, grounded: boolean)
	local num = input.throttle
	e.throttle += num * dt * CTRL.throttleSpeed
	if e.throttle <= 0 then
		e.throttle = MathUtil.moveTowards(e.throttle, num, dt * 10)
	end
	local minThrottle = if grounded or e.data.helicopterRotor == 1 then -0.75 else 0
	e.throttle = math.clamp(e.throttle, minThrottle, 1)
	local electric = e.data.electricEngine == 1
	if (if electric then self.electricity else self.fuel) <= 0 then
		e.effective = 0
		return
	end
	local thrust = e.data.thrust
	local consumption = e.data.fuelConsumption
	if e.data.useAfterburner == 1 then
		local ab = clamp01(e.throttle - 0.8) * 5
		thrust = MathUtil.lerpUnclamped(thrust, e.data.afterburnerThrust, ab)
		consumption = MathUtil.lerpUnclamped(consumption, e.data.afterburnerFuelConsumption, ab)
	end
	if #e.data.maxSpeed > 2 then
		thrust *= MathUtil.evaluateCurve(e.data.maxSpeed, self:velocityU().Magnitude)
	end
	-- Thrust acts at the thrust origin, at the centre of mass height.
	local rootCF = self.root.CFrame
	local originLocal = rootCF:PointToObjectSpace(U(self:thrustOriginU()))
	local comLocal = rootCF:PointToObjectSpace(self.root.AssemblyCenterOfMass)
	local at = U(rootCF * Vector3.new(originLocal.X, comLocal.Y, originLocal.Z))
	local dir = self:dirU(self:engineDirLocal(e))
	local handicap = clamp01(1 - (e.data.thrustHandicap or 0))
	self:addForceAt(dir * e.throttle * thrust * e.maxPower * handicap, at, dt)
	e.effective = e.throttle
	local use = if self.infiniteFuel then 0 else 1
	if electric then
		self.electricity -= math.abs(e.throttle) * consumption * e.maxPower * dt * use
	else
		self.fuel -= math.abs(e.throttle) * consumption * e.maxPower * dt * use
		self.electricity = math.min(self.electricity + dt * math.abs(e.throttle) * (e.data.electricityGeneration or 0), self.batteryCapacity)
	end
end

-- ------------------------------------------------------------------------------------------- wheels
function Flight:updateWheel(w: any, dt: number)
	local rootCF = self.root.CFrame
	local posR = rootCF * w.pos
	local upR = rootCF:VectorToWorldSpace(w.up)
	local downR = -upR * w.scaleSign
	local d = w.data
	local params = self:rayParams()
	local hit = Workspace:Raycast(posR - downR * d.rayUpOffset, downR * (d.suspentionLength + d.rayUpOffset), params)
	if not hit then
		w.prevHit = d.suspentionLength
		w.weightSamples = {}
		w.contact = nil
		return
	end
	local posU = U(posR)
	local normalU = U(hit.Normal)
	local rightU = self:dirU(w.right)
	local dist = math.clamp(hit.Distance - d.rayUpOffset, 0, d.suspentionLength)
	local compression = 1 - dist / d.suspentionLength
	local pv = self:pointVelocityU(posU)
	local compressionSpeed = (w.prevHit - dist) / dt
	local spring = d.suspensionStrenth * GRAVITY_MULT * compression
	local suspension = spring + d.suspensionDamping * compressionSpeed
	self:addForceAt(normalU * suspension, posU, dt)
	local lateral = rightU:Dot(pv)
	self:addForceAt(-rightU * lateral * d.traction * spring, posU, dt)
	local rolling = normalU:Cross(rightU)
	local f = rolling:Dot(pv)
	local friction = math.min(math.abs(f), d.maxFriction) * sign(f)
	friction *= lerp(3, 1, math.abs(self:forwardU():Dot(self:totalThrustU())))
	self:addForceAt(-rolling * friction * spring, posU, dt)
	w.prevHit = dist
	w.contact = hit.Position
	-- CheckWheelFailure
	local load = (suspension * normalU):Dot(Vector3.yAxis) / Flight.Gravity * 15
	if load > d.maxWeightCapacity then
		self.warnings["Wheel load too heavy"] = "Add more wheels or add stronger wheels"
	end
	local impact = pv:Dot(-normalU)
	if impact < 2 then
		table.insert(w.weightSamples, { t = self.time, v = load })
		while #w.weightSamples > 0 and self.time - w.weightSamples[1].t > 3 do
			table.remove(w.weightSamples, 1)
		end
		local minV = math.huge
		for _, s in w.weightSamples do
			minV = math.min(minV, s.v)
		end
		if #w.weightSamples > 30 and self.time - w.weightSamples[1].t > 2.9 and minV > d.maxWeightCapacity then
			self:explodePart(w.part)
		end
	end
	if impact > d.maxImpactVelocity then
		self:explodePart(w.part)
	end
end

function Flight:totalThrustU(): Vector3
	local sum, n = Vector3.zero, 0
	for _, e in self.engines do
		if e.part.alive then
			local alive = if self.fuel > 0 then 1 else 0
			sum += self:dirU(self:engineDirLocal(e)) * (e.throttle or 0) * alive
			n += 1
		end
	end
	return if n > 0 then sum / n else sum
end

-- ------------------------------------------------------------------------------------------- explosions
function Flight:explodeAt(position: Vector3, size: number)
	local ex = Instance.new("Explosion")
	ex.BlastPressure = 0
	ex.BlastRadius = 0
	ex.DestroyJointRadiusPercent = 0
	ex.ExplosionType = Enum.ExplosionType.NoCraters
	ex.Position = position
	ex.Parent = Workspace
end

-- PartExploder.ExplodePart: the part and everything attached to it break off.
function Flight:explodePart(p: any, quiet: boolean?)
	if not p.alive then
		return
	end
	local list = { p }
	local function addChildren(id: number)
		for _, other in self.parts do
			if other.alive and other.entry.parent == id and not table.find(list, other) then
				table.insert(list, other)
				addChildren(other.entry.id)
			end
		end
	end
	addChildren(p.entry.id)
	for _, q in list do
		q.alive = false
		for _, d in q.built.model:GetDescendants() do
			if d:IsA("WeldConstraint") or d:IsA("Weld") then
				d:Destroy()
			elseif d:IsA("BasePart") then
				d.CanCollide = d.Name == "Collider"
			end
		end
		task.delay(8, function()
			q.built.model:Destroy()
		end)
	end
	if not quiet and #list > 0 then
		local pos = list[1].built.visuals[1] and list[1].built.visuals[1].Position or self.root.Position
		self:explodeAt(pos, 2)
	end
	-- ReInitializePlane: mass, cargo space, centre of mass.
	self.cargoVolume = 0
	for _, q in self.parts do
		if q.alive then
			self.cargoVolume += math.abs(q.cargo)
		end
	end
	if self.campaign then
		self.campaign:fitCargo(self.cargoVolume)
	end
	self:classifyWings()
	self:updateMass()
end

function Flight:explodePlane()
	if self.exploded then
		return
	end
	self.exploded = true
	self:explodeAt(self.root.Position, self.planeLength * 0.2)
	for _, p in self.parts do
		if p.alive then
			self:explodePart(p, true)
		end
	end
	if self.onExploded then
		self.onExploded()
	end
end

-- ------------------------------------------------------------------------------------------- main step
function Flight:step(dt: number, input: Input)
	if self.exploded or not self.root.Parent then
		return
	end
	dt = math.clamp(dt, 1 / 240, 1 / 15)
	self.time += dt
	self.warnings = {}
	self:updateMass()

	-- Throttle and rudder smoothing (PlaneController.UpdateThrottle)
	self.currentThrottle, self.throttleVelocity = MathUtil.smoothDamp(self.currentThrottle, input.throttle, self.throttleVelocity, CTRL.throttleSpeed, dt)
	self.rudderInputSmooth, self.rudderVelocity = MathUtil.smoothDamp(self.rudderInputSmooth, input.yaw, self.rudderVelocity, CTRL.rudderSmoothTime, dt)
	if input.yaw ~= 0 then
		self.rudderInputSmooth = input.yaw
	end

	-- Retractable gear / decoupler
	if input.retractPressed and self.hasRetractableGear then
		self.retracted = not self.retracted
		self.drag = if self.retracted then self.dragFactorRetracted else self.dragFactor
	end
	if input.decouplePressed then
		for _, d in self.decouplers do
			if d.part.alive then
				for _, other in self.parts do
					if other.alive and other.entry.parent == d.part.entry.id then
						self:explodePart(other, true)
					end
				end
			end
		end
	end

	local grounded = self:isGrounded()

	-- PlaneController.FixedUpdate
	self:updateControls(input, dt)
	self:stabilise(dt)
	self:updateControllability()
	local fwd, up = self:forwardU(), self:upU()
	local v = self:velocityU()
	local side = safeUnit(fwd:Cross(up))
	local traction = side:Dot(v) * CTRL.airTraction * self:unityMass()
	local upness = math.abs(up:Dot(Vector3.yAxis))
	self:addForce(-side * traction * upness, dt)
	local speed = v.Magnitude
	local t = (Vector3.yAxis:Dot(up) + 1) * 0.5
	local trigger = lerp(CTRL.minTriggerVelocity, CTRL.maxTriggerVelocity, t)
	if speed - self.prevSpeed < 0 and grounded then
		self.liftMultiplier = 1 - CTRL.landingHelper
	else
		self.liftMultiplier = 1
	end
	if math.abs(speed - self.prevSpeed) > trigger and self.time > 0.5 then
		self:explodePlane()
		return
	end
	if self.root.Position.Y < -12 then
		self:explodePlane()
		return
	end
	self.prevSpeed = speed

	-- PlaneContainer.FixedUpdate
	self.airbreakDrag = 1
	self.currentLift = 0
	local aoaOffset = if input.space then lerp(10, 0, clamp01(self:distanceFromGround() / 10)) else 0
	for _, w in self.wings do
		if w.part.alive then
			self:updateWing(w, dt, aoaOffset)
			if self.exploded then
				return
			end
		end
	end
	for _, e in self.engines do
		if e.part.alive then
			self:updateEngine(e, input, dt, grounded)
		end
	end
	for _, b in self.balloons do
		if b.part.alive then
			self:addForceAt(Vector3.yAxis * b.data.lift, self:comU(), dt)
			self.currentLift += up:Dot(Vector3.yAxis * b.data.lift)
		end
	end
	local braking = input.throttle < 0 and self:totalThrustU().Magnitude < 0.01 and v:Dot(fwd) > 0
	self.braking = braking
	for _, d in self.drags do
		if d.part.alive and braking then
			self.airbreakDrag += d.multiplier
		end
	end
	for _, tu in self.turbines do
		if tu.part.alive then
			self.electricity = math.clamp(self.electricity + speed / 200 * dt * (tu.data.capacity or 0), 0, self.batteryCapacity)
		end
	end
	for _, w in self.wheels do
		if w.part.alive and not (self.retracted and w.part.retractable) then
			self:updateWheel(w, dt)
		end
	end

	-- ApplyAngularDrag (pitch, yaw, roll in local axes; per Unity step)
	local omega = A(self.root.AssemblyAngularVelocity)
	local right = self:dirU(-Vector3.xAxis)
	local fwdLocalAxis = self:dirU(Vector3.zAxis)
	local wx, wy, wz = omega:Dot(right), omega:Dot(up), omega:Dot(fwdLocalAxis)
	local rate = dt / UNITY_STEP
	local dOmega = -(right * wx * PC.pitchDrag + up * wy * self.yawDrag + fwdLocalAxis * wz * PC.rollDrag) * rate
	-- rb.angularDamping = 2
	dOmega -= omega * (1 - 1 / (1 + 2 * dt))
	self.root.AssemblyAngularVelocity += A(dOmega)

	-- ApplyLiniarDrag(helicopter * verticalLiniarDrag) and rb.linearDamping
	local vertical = self.helicopter * PC.verticalLiniarDrag
	local b = (1 - math.abs(fwd:Dot(safeUnit(v)))) * PC.angleDragMultiplier + 1
	local num = lerp(1 - clamp01(fwd:Dot(-Vector3.yAxis)), b, self.helicopter)
	local damping = self.drag * num * self.airbreakDrag
	local vel = self.root.AssemblyLinearVelocity
	vel = Vector3.new(vel.X, vel.Y - vel.Y * vertical * rate, vel.Z)
	vel *= 1 / (1 + damping * dt)
	self.root.AssemblyLinearVelocity = vel

	-- Helicopter factor from the current engine direction
	local sum = Vector3.zero
	for _, e in self.engines do
		sum += self:dirU(self:engineDirLocal(e))
	end
	if #self.engines > 0 then
		self.helicopter = math.abs(up:Dot(safeUnit(sum / #self.engines)))
	end
	if #self.wings == 0 then
		self.helicopter = 1
	end

	-- Visuals: spin propellers (PropellerVisual)
	for _, e in self.engines do
		if e.part.alive then
			e.spin = (e.spin or 0) + (e.throttle or 0) * dt * 40
			for _, s in e.part.spinners or {} do
				if s.weld.Parent then
					s.weld.C0 = s.baseC0 * CFrame.fromAxisAngle(s.axis, e.spin)
				end
			end
		end
	end
	self.prevVelocity = self.root.AssemblyLinearVelocity
end

-- Values the HUD shows.
function Flight:telemetry()
	local v = self.root.AssemblyLinearVelocity
	local thr = 0
	local n = 0
	for _, e in self.engines do
		if e.part.alive then
			thr += e.throttle or 0
			n += 1
		end
	end
	return {
		speed = v.Magnitude,
		altitude = self.root.Position.Y,
		height = self:distanceFromGround(),
		throttle = if n > 0 then thr / n else 0,
		fuel = self.fuel,
		fuelCapacity = self.fuelCapacity,
		electricity = self.electricity,
		batteryCapacity = self.batteryCapacity,
		hasElectric = self.batteryCapacity > 0.05 + 1e-6,
		cargoVolume = self.cargoVolume,
		grounded = self:isGrounded(),
		warnings = self.warnings or {},
		retracted = self.retracted,
		braking = self.braking,
	}
end

function Flight:refuel()
	self.fuel = self.fuelCapacity
	self.electricity = self.batteryCapacity
end

function Flight:destroy()
	self.exploded = true
	if self.model then
		self.model:Destroy()
	end
end

return Flight
