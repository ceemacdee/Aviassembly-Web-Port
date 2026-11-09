-- A tiny stand-in for Roblox instances + rigid-body physics, enough to run Flight.lua outside Studio.
task = { wait = function() end, delay = function() end, spawn = function(f, ...) f(...) end }
warn = function(...) end
-- Ground is the plane y = groundY. One rigid assembly (the plane root and everything welded to it).
local FE = { groundY = 0, parts = {} }

local function inst(class)
	local o = { ClassName = class, Name = class, _children = {}, _attrs = {}, Parent = nil }
	return o
end

local Instance_mt = {}
local function newInstance(class)
	local self = setmetatable(inst(class), Instance_mt)
	if class == "Part" or class == "WedgePart" or class == "MeshPart" then
		self.Size = Vector3.new(1, 1, 1)
		self.CFrame = CFrame.new()
		self.Anchored = false
		self.Massless = false
		self.CustomPhysicalProperties = nil
		self.AssemblyLinearVelocity = Vector3.zero
		self.AssemblyAngularVelocity = Vector3.zero
		self._isPart = true
	end
	return self
end
local EVENTS = { MouseButton1Down = true, MouseButton1Up = true, MouseDrag = true, Ended = true, Activated = true, InputBegan = true, InputEnded = true, InputChanged = true, RenderStepped = true, Heartbeat = true, PreSimulation = true, MouseButton1Click = true, Changed = true }
local function signal()
	return { Connect = function(_, f) return { Disconnect = function() end } end }
end
Instance_mt.__index = function(t, k)
	if k == "Position" then local cf = rawget(t, "CFrame") return cf and cf.Position end
	if k == "Parent" then return rawget(t, "_parent") end
	if EVENTS[k] then local s = signal(); rawset(t, k, s); return s end
	return Instance_mt[k]
end
Instance_mt.__newindex = function(t, k, v)
	if k == "Parent" then
		local old = rawget(t, "_parent")
		if old then
			for i, c in old._children do if c == t then table.remove(old._children, i) break end end
		end
		rawset(t, "_parent", v)
		if v then table.insert(v._children, t) end
		return
	end
	if k == "Position" and rawget(t, "CFrame") then
		local cf = rawget(t, "CFrame")
		rawset(t, "CFrame", cf.Rotation and cf or cf)
		local r = rawget(t, "CFrame")
		rawset(t, "CFrame", CFrame.fromMatrix(v, r.RightVector, r.UpVector, -r.LookVector))
		return
	end
	rawset(t, k, v)
end
function Instance_mt:IsA(c)
	if c == "BasePart" then return self._isPart == true end
	if c == "Model" then return self.ClassName == "Model" end
	if c == "GuiButton" or c == "GuiObject" then return false end
	return self.ClassName == c
end
function Instance_mt:GetChildren() return table.clone(self._children) end
function Instance_mt:GetDescendants()
	local out = {}
	local function rec(o) for _, c in o._children do table.insert(out, c); rec(c) end end
	rec(self)
	return out
end
function Instance_mt:FindFirstChildOfClass(c) for _, x in self._children do if x.ClassName == c then return x end end end
function Instance_mt:FindFirstAncestorOfClass(c)
	local p = self.Parent
	while p do if p.ClassName == c then return p end p = p.Parent end
end
function Instance_mt:FindFirstAncestorWhichIsA(c) return self:FindFirstAncestorOfClass(c) end
function Instance_mt:SetAttribute(k, v) self._attrs[k] = v end
function Instance_mt:GetAttribute(k) return self._attrs[k] end
function Instance_mt:Destroy() self.Parent = nil; self._destroyed = true end
function Instance_mt:Clone() local c = newInstance(self.ClassName) for k, v in pairs(self) do if k ~= "_children" and k ~= "_parent" then rawset(c, k, v) end end return c end
function Instance_mt:GetMass()
	if self.Massless or not self.CustomPhysicalProperties then
		local d = 0.7
		if self.Massless then return 0 end
		return d * self.Size.X * self.Size.Y * self.Size.Z
	end
	return self.CustomPhysicalProperties.Density * self.Size.X * self.Size.Y * self.Size.Z
end
function Instance_mt:PivotTo(cf) self.WorldPivot = cf end
function Instance_mt:ApplyImpulseAtPosition(J, p)
	local _, com = FE.massProps(self)
	self._linImpulse = (self._linImpulse or Vector3.zero) + J
	self._angImpulse = (self._angImpulse or Vector3.zero) + (p - com):Cross(J)
end
function Instance_mt:GetVelocityAtPosition(p)
	local _, com = FE.massProps(self)
	return self.AssemblyLinearVelocity + self.AssemblyAngularVelocity:Cross(p - com)
end

-- the assembly: computed from the root's welds
function FE.assembly(root)
	local parts = {}
	local model = root.Parent
	for _, d in model:GetDescendants() do
		if d._isPart and d ~= root then
			local welded = false
			for _, w in d._children do
				if (w.ClassName == "WeldConstraint" or w.ClassName == "Weld") and w.Part0 == root then welded = true end
			end
			if welded then table.insert(parts, d) end
		end
	end
	return parts
end

function FE.massProps(root)
	local parts = FE.assembly(root)
	local m, com = 0, Vector3.zero
	for _, p in parts do
		local pm = p:GetMass()
		m += pm
		com += p.CFrame.Position * pm
	end
	if m <= 0 then return 1e-3, root.CFrame.Position end
	return m, com / m
end

-- Root-specific API (the root part represents the assembly)
local function installRootAPI(root)
	local state = root
	state._impulses = Vector3.zero
	function root:ApplyImpulseAtPosition(J, p)
		local m, com = FE.massProps(self)
		self._linImpulse = (self._linImpulse or Vector3.zero) + J
		self._angImpulse = (self._angImpulse or Vector3.zero) + (p - com):Cross(J)
	end
	function root:GetVelocityAtPosition(p)
		local _, com = FE.massProps(self)
		return self.AssemblyLinearVelocity + self.AssemblyAngularVelocity:Cross(p - com)
	end
end

local Assembly_mt_index = Instance_mt.__index
Instance_mt.__index = function(t, k)
	if k == "AssemblyMass" then return (FE.massProps(t)) end
	if k == "AssemblyCenterOfMass" then local _, c = FE.massProps(t) return c end
	return Assembly_mt_index(t, k)
end

-- integrate one step for the plane rooted at `root`
function FE.integrate(root, dt, gravity)
	local parts = FE.assembly(root)
	local m, com = FE.massProps(root)
	-- inertia: point masses about the CoM (+ small box term), in world space, diagonal approximation in local axes
	local cf = root.CFrame
	local I = Vector3.new(1e-3, 1e-3, 1e-3)
	for _, p in parts do
		local pm = p:GetMass()
		local r = cf:VectorToObjectSpace(p.CFrame.Position - com)
		local s = p.Size
		I += Vector3.new(pm * (r.Y ^ 2 + r.Z ^ 2 + (s.Y ^ 2 + s.Z ^ 2) / 12), pm * (r.X ^ 2 + r.Z ^ 2 + (s.X ^ 2 + s.Z ^ 2) / 12), pm * (r.X ^ 2 + r.Y ^ 2 + (s.X ^ 2 + s.Y ^ 2) / 12))
	end
	local v = root.AssemblyLinearVelocity + (root._linImpulse or Vector3.zero) / m + Vector3.new(0, -gravity * dt, 0)
	local L = cf:VectorToObjectSpace(root._angImpulse or Vector3.zero)
	local w = root.AssemblyAngularVelocity + cf:VectorToWorldSpace(Vector3.new(L.X / I.X, L.Y / I.Y, L.Z / I.Z))
	root._linImpulse, root._angImpulse = nil, nil
	-- crude ground contact for colliding parts: stop downward motion if any collider is below ground
	local lowest = math.huge
	for _, p in parts do
		if p.CanCollide then lowest = math.min(lowest, p.CFrame.Position.Y - p.Size.Y / 2) end
	end
	if lowest < FE.groundY and v.Y < 0 then
		FE.lastImpact = -v.Y
		v = Vector3.new(v.X * 0.9, 0, v.Z * 0.9)
	end
	root.AssemblyLinearVelocity = v
	root.AssemblyAngularVelocity = w
	-- move everything rigidly about the CoM
	local angle = w.Magnitude * dt
	local rot = if angle > 1e-9 then CFrame.fromAxisAngle(w.Unit, angle) else CFrame.new()
	local function move(p)
		local rel = p.CFrame.Position - com
		local newPos = com + v * dt + rot:VectorToWorldSpace(rel)
		local r = p.CFrame
		p.CFrame = CFrame.fromMatrix(newPos, rot:VectorToWorldSpace(r.RightVector), rot:VectorToWorldSpace(r.UpVector), rot:VectorToWorldSpace(-r.LookVector))
	end
	move(root)
	for _, p in parts do move(p) end
end

-- Services
local function service(name)
	return setmetatable({ Name = name, _children = {} }, Instance_mt)
end
local Workspace = service("Workspace")
Workspace.Gravity = 25.5
local function rayBox(origin, dir, part)
	local cf = part.CFrame
	local o = cf:PointToObjectSpace(origin)
	local d = cf:VectorToObjectSpace(dir)
	local h = part.Size / 2
	local tmin, tmax = 0, 1
	local nAxis, nSign = "Y", 1
	for _, ax in { "X", "Y", "Z" } do
		if math.abs(d[ax]) < 1e-12 then
			if math.abs(o[ax]) > h[ax] then return nil end
		else
			local t1 = (-h[ax] - o[ax]) / d[ax]
			local t2 = (h[ax] - o[ax]) / d[ax]
			local s = -1
			if t1 > t2 then t1, t2 = t2, t1; s = 1 end
			if t1 > tmin then tmin = t1; nAxis = ax; nSign = s end
			tmax = math.min(tmax, t2)
			if tmin > tmax then return nil end
		end
	end
	local nLocal = Vector3.new(nAxis == "X" and nSign or 0, nAxis == "Y" and nSign or 0, nAxis == "Z" and nSign or 0)
	return tmin, cf:VectorToWorldSpace(nLocal)
end
function Workspace:Raycast(origin, dir, params)
	if params and params.FilterType == "RaycastFilterType.Include" then
		local best, bestT, bestN = nil, math.huge, nil
		for _, p in params.FilterDescendantsInstances or {} do
			if p._isPart then
				local t, n = rayBox(origin, dir, p)
				if t and t < bestT then bestT = t; best = p; bestN = n end
			end
		end
		if best then return { Position = origin + dir * bestT, Normal = bestN, Distance = dir.Magnitude * bestT, Instance = best } end
		return nil
	end
	if dir.Y >= 0 then return nil end
	local t = (FE.groundY - origin.Y) / dir.Y
	if t < 0 or t > 1 then return nil end
	return { Position = origin + dir * t, Normal = Vector3.new(0, 1, 0), Distance = dir.Magnitude * t, Instance = Workspace }
end
local ReplicatedStorage = shared_node("src/shared")
local RS = setmetatable({}, { __index = function(_, k)
	if k == "WaitForChild" then return function(_, n) return shared_node("src/shared") end end
	if k == "GetDescendants" then return function() return {} end end
	if k == "FindFirstChild" then return function() return nil end end
end })
local camera = setmetatable({ _children = {} }, Instance_mt)
FE.mouseRay = { Origin = Vector3.new(0, 10, 0), Direction = Vector3.new(0, -1, 0) }
function camera:ViewportPointToRay() return FE.mouseRay end
Workspace.CurrentCamera = camera
local playerGui = setmetatable({ _children = {} }, Instance_mt)
local localPlayer = setmetatable({ _children = {} }, Instance_mt)
function localPlayer:WaitForChild() return playerGui end
local Players = service("Players")
Players.LocalPlayer = localPlayer
local UIS = service("UserInputService")
function UIS:GetMouseLocation() return Vector2.new(0, 0) end
function UIS:IsKeyDown() return false end
local Tween = service("TweenService")
function Tween:Create() return { Play = function() end } end
FE.services = { Workspace = Workspace, SoundService = service("SoundService"), Lighting = service("Lighting"), Players = Players, UserInputService = UIS, TweenService = Tween, ReplicatedStorage = RS, RunService = service("RunService") }
game = { GetService = function(_, n) return FE.services[n] end }
workspace = Workspace
Instance = { new = function(class) local o = newInstance(class); if class == "Part" then end return o end }
PhysicalProperties = { new = function(d) return { Density = d } end }
RaycastParams = { new = function() return {} end }
Enum = setmetatable({}, { __index = function(_, k) return setmetatable({}, { __index = function(_, v) return k .. "." .. v end }) end })
CFrame.fromAxisAngle = function(axis, angle)
	local a = axis.Unit
	local c, s = math.cos(angle), math.sin(angle)
	local function rotv(v) return v * c + a:Cross(v) * s + a * (a:Dot(v)) * (1 - c) end
	return CFrame.fromMatrix(Vector3.zero, rotv(Vector3.new(1, 0, 0)), rotv(Vector3.new(0, 1, 0)), rotv(Vector3.new(0, 0, 1)))
end
FE.newInstance = newInstance
FE.installRootAPI = installRootAPI
function Vector3.new_min(a, b) end
return FE
