--!strict
-- Build mode (PartPlacer, PartBar, CategoryBar, PlaneStats, ProceduralFuselage gizmos, ResearchPanel).
local Workspace = game:GetService("Workspace")
local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local UserInputService = game:GetService("UserInputService")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Parts = require(Shared.Data.Parts)
local Research = require(Shared.Data.Research)
local Fuselage = require(Shared.Fuselage)
local MathUtil = require(Shared.Util.MathUtil)
local Frames = require(script.Parent.Frames)
local PartVisual = require(script.Parent.PartVisual)
local PlaneDesign = require(script.Parent.PlaneDesign)
local UI = require(script.Parent.UI)
local Audio = require(script.Parent.Audio)
local Decals = require(script.Parent.Decals)

local C = UI.Colors
local Hangar = {}
Hangar.__index = Hangar

local SNAP_DISTANCE = 0.75
local ROTATION_STEP = 15
-- Paint colours (ColorPickerUI presets + the original part colours)
local SWATCHES = {
	{ 0.915, 0.915, 0.915 }, { 0.6, 0.6, 0.6 }, { 0.3, 0.3, 0.32 }, { 0.12, 0.12, 0.13 },
	{ 0.86, 0.29, 0.17 }, { 0.92, 0.37, 0.24 }, { 0.78, 0.48, 0.17 }, { 0.91, 0.76, 0.27 },
	{ 0.95, 0.88, 0.55 }, { 0.52, 0.62, 0.3 }, { 0.32, 0.42, 0.35 }, { 0.2, 0.5, 0.35 },
	{ 0.25, 0.62, 0.86 }, { 0.3, 0.43, 0.6 }, { 0.18, 0.25, 0.5 }, { 0.55, 0.35, 0.7 },
	{ 0.85, 0.45, 0.6 }, { 0.48, 0.35, 0.18 }, { 0.61, 0.52, 0.46 }, { 1, 1, 1 },
}
local CATEGORIES = { "Cockpits", "Engines", "Wings", "Wheels", "Utility", "Electric" }
local CATEGORY_TITLES = { Cockpits = "Body", Engines = "Engines", Wings = "Wings", Wheels = "Wheels", Utility = "Misc", Electric = "Electric" }
-- Part order inside each category, from the original PartBars.
local ORDER = {
	Cockpits = { "Body", "BiplaneCockpit", "Cockpit", "JetCockpit", "PrivateJetCockpit", "BigCockpit", "SmallHelicopterCockpit", "SmallLargeCockpit", "JetlineCockpit", "GliderCockpit", "JetCockpit02", "SmallPlaneCockpit", "AreoDynamicCockpit", "HugeCockpit", "HelicopterCockpit", "LargeBody", "Connector", "Nose", "SharpNose", "Tail" },
	Engines = { "WoodEngine", "Engine", "SmallPropEngine", "TurboProp", "SmallJetEngine", "JetEngine", "FighterJetEngine", "RocketEngine", "ElectricEngine", "BigElectricEngine", "PropfanEngine", "RamJet", "HelicopterEngine", "HelicopterEngineSmall", "HugePropEngine" },
	Wings = { "WoodWing", "BiplaneWing", "TailWing", "Wing", "ElectronWing", "StraightWing", "SmallJetWing", "BigWing", "Tailwing02", "DeltaWing01", "Tailwing03", "DeltaWing02", "Tailwing04", "SmallCanard", "SmallRecreationalWing", "GliderWing" },
	Wheels = { "BikeWheel", "SmallRearWheel", "RearWheel", "OldSteelWheel", "BigOldWheel", "AeroWheel", "AeroWheelLong", "Wheel01", "SmallRetractableGear", "Wheel03", "BigRetractableGear", "TinyRetractableGear", "HelicopterLandingGear" },
	Utility = { "WoodStrut", "WoodConnection", "SmallFuelTank", "ExternalFuelTank", "FuelTank", "BigExternalFuelTank", "SmallBalloon", "BigBalloon", "Parachute", "Airbreak", "AirIntake", "AirIntake2", "AirOutlet" },
	Electric = { "SmallBatteries", "BigBatteries", "RotationJoint", "Decoupler", "SolarPanel", "AirTurbine", "Camera", "TempratureFan" },
}

local function v(t: any): Vector3
	return Frames.v(t)
end

local collisionPointFor: (string, any?) -> any?

function Hangar.new(campaign: any, design: any, originCFrame: CFrame, callbacks: any)
	local self = setmetatable({}, Hangar)
	self.campaign = campaign
	self.design = design
	self.origin = originCFrame
	self.callbacks = callbacks
	self.built = {} :: { [number]: any }
	self.tool = nil :: string?
	self.ghost = nil :: any
	self.ghostMirror = nil :: any
	self.mirrorMode = true
	self.rotation = 0
	self.selected = nil :: number?
	self.undo = {} :: { any }
	self.category = "Cockpits"
	self.connections = {}
	self.mode = "build" -- build | paint | decal
	self.paintColor = SWATCHES[5]
	self.decalImage = Decals.List[1] and Decals.List[1].id or "Circle_0"
	self.decalColor = { Decals.DefaultColor.R, Decals.DefaultColor.G, Decals.DefaultColor.B }
	self.decalSize = Decals.DefaultSize
	self.decalRotation = 0
	self.decalVisuals = {} :: { BasePart }
	self.camYaw, self.camPitch, self.camDistance = math.rad(140), math.rad(20), 14

	self.folder = Instance.new("Model")
	self.folder.Name = "HangarPlane"
	self.folder.Parent = Workspace

	-- Building platform & grid (BuildingGridVisual)
	local floor = Instance.new("Part")
	floor.Name = "BuildPlatform"
	floor.Anchored = true
	floor.Size = Vector3.new(40, 0.4, 40)
	floor.CFrame = originCFrame * CFrame.new(0, -3.2, 0)
	floor.Color = Color3.fromRGB(70, 74, 80)
	floor.Material = Enum.Material.Concrete
	floor.Transparency = 0.2
	floor.CanQuery = false
	floor.Parent = self.folder
	-- grid lines (BuildingGridVisual)
	for i = -10, 10 do
		for _, axis in { "X", "Z" } do
			local line = Instance.new("Part")
			line.Anchored = true
			line.CanCollide = false
			line.CanQuery = false
			line.Material = Enum.Material.Neon
			line.Color = Color3.fromRGB(150, 160, 170)
			line.Transparency = if i % 5 == 0 then 0.4 else 0.75
			line.Size = if axis == "X" then Vector3.new(40, 0.02, 0.04) else Vector3.new(0.04, 0.02, 40)
			line.CFrame = floor.CFrame * CFrame.new(if axis == "X" then 0 else i * 2, 0.21, if axis == "X" then i * 2 else 0)
			line.Parent = self.folder
		end
	end

	self.highlight = Instance.new("Highlight")
	self.highlight.FillTransparency = 1
	self.highlight.OutlineColor = C.accent
	self.highlight.Parent = Workspace

	for _, e in design.parts do
		self:buildEntry(e)
	end
	self:renderDecals()
	self:buildHandles()
	self:buildUI()
	self:bindInput()
	self:refresh()
	return self
end

-- ------------------------------------------------------------------------------------------- visuals
function Hangar:buildEntry(e: any)
	local built = PartVisual.create(e.name, self.origin * e.cf, e.flip, e.fuselage, { color = e.color and Color3.new(e.color[1], e.color[2], e.color[3]) or nil })
	built.model:SetAttribute("EntryId", e.id)
	built.model.Parent = self.folder
	self.built[e.id] = built
end

function Hangar:removeVisual(id: number)
	local b = self.built[id]
	if b then
		b.model:Destroy()
		self.built[id] = nil
	end
end

function Hangar:rebuildEntry(e: any)
	self:removeVisual(e.id)
	self:buildEntry(e)
	self:renderDecals()
end

-- ------------------------------------------------------------------------------------------- decals
function Hangar:renderDecals()
	for _, d in self.decalVisuals do
		d:Destroy()
	end
	self.decalVisuals = {}
	for index, d in self.design.decals do
		local e = self.design:get(d.part)
		if e then
			local holder = Decals.create(d, Decals.cframe(d, self.origin * e.cf, e.flip), self.folder)
			holder:SetAttribute("DecalIndex", index)
			table.insert(self.decalVisuals, holder)
		end
	end
end

-- Surface under the mouse on the plane: world hit, entry id.
function Hangar:decalPlacement(): (any?, CFrame?)
	local hit, id = self:hitPlane()
	if not hit or not id then
		return nil, nil
	end
	local e = self.design:get(id)
	if not e then
		return nil, nil
	end
	local partCF = self.origin * e.cf
	local normalW = hit.Normal
	-- image "up": world up projected on the surface (DecalPlacer.OrientProjectionVolume), then the user rotation
	local up = Vector3.yAxis - normalW * Vector3.yAxis:Dot(normalW)
	if up.Magnitude < 0.05 then
		up = (self.origin:VectorToWorldSpace(Vector3.zAxis) - normalW * self.origin:VectorToWorldSpace(Vector3.zAxis):Dot(normalW))
	end
	up = CFrame.fromAxisAngle(normalW, math.rad(self.decalRotation)):VectorToWorldSpace(up.Unit)
	local function toLocal(v: Vector3, isPoint: boolean): { number }
		local l = if isPoint then partCF:PointToObjectSpace(v) else partCF:VectorToObjectSpace(v)
		if e.flip then
			l = Vector3.new(l.X, -l.Y, l.Z)
		end
		return { l.X, l.Y, l.Z }
	end
	local d = { part = id, pos = toLocal(hit.Position, true), normal = toLocal(normalW, false), up = toLocal(up, false), size = self.decalSize, color = table.clone(self.decalColor), image = self.decalImage }
	return d, Decals.cframe(d, partCF, e.flip)
end

function Hangar:placeDecal()
	local d = self:decalPlacement()
	if not d then
		UI.toast(self.gui, "Point at the plane to place a decal", C.bad)
		return
	end
	table.insert(self.design.decals, d)
	-- mirror copy (DecalPlacer.MakeMirrorCopy)
	local e = self.design:get(d.part)
	if self.mirrorMode and e and e.mirror and self.design:get(e.mirror) then
		local m = self.design:get(e.mirror)
		local worldCF = Decals.cframe(d, self.origin * e.cf, e.flip)
		local local0 = self.origin:ToObjectSpace(worldCF)
		local center, n = self:symmetryPlane()
		local pos = local0.Position - n * (2 * (local0.Position - center):Dot(n))
		local look = Frames.reflect(local0.LookVector, n)
		local upv = Frames.reflect(local0.UpVector, n)
		local mcf = self.origin * CFrame.lookAt(pos, pos + look, upv)
		local partCF = self.origin * m.cf
		local function toLocal(v: Vector3, isPoint: boolean): { number }
			local l = if isPoint then partCF:PointToObjectSpace(v) else partCF:VectorToObjectSpace(v)
			if m.flip then
				l = Vector3.new(l.X, -l.Y, l.Z)
			end
			return { l.X, l.Y, l.Z }
		end
		table.insert(self.design.decals, { part = m.id, pos = toLocal(mcf.Position - mcf.LookVector * 0.012, true), normal = toLocal(mcf.LookVector, false), up = toLocal(mcf.UpVector, false), size = d.size, color = table.clone(d.color), image = d.image })
	end
	Audio.play("paint")
	self:renderDecals()
end

function Hangar:removeDecalUnderMouse(): boolean
	local origin, dir = self:designRay()
	local params = RaycastParams.new()
	params.FilterType = Enum.RaycastFilterType.Include
	params.FilterDescendantsInstances = self.decalVisuals
	local hit = Workspace:Raycast(origin, dir * 500, params)
	local index = hit and hit.Instance:GetAttribute("DecalIndex")
	if not index then
		return false
	end
	table.remove(self.design.decals, index)
	Audio.play("remove")
	self:renderDecals()
	return true
end

-- ------------------------------------------------------------------------------------------- paint (PartPainter)
function Hangar:paintUnderMouse()
	local _, id = self:hitPlane()
	local e = id and self.design:get(id)
	if not e then
		return
	end
	if not PartVisual.isPaintable(e.name) then
		UI.toast(self.gui, "This part can't be painted", C.bad)
		return
	end
	local targets = { e }
	if self.mirrorMode and e.mirror and self.design:get(e.mirror) then
		table.insert(targets, self.design:get(e.mirror))
	end
	for _, t in targets do
		t.color = table.clone(self.paintColor)
		local b = self.built[t.id]
		if b then
			PartVisual.setColor(b, Color3.new(t.color[1], t.color[2], t.color[3]))
		end
	end
	Audio.play("paint")
end

-- ------------------------------------------------------------------------------------------- rotate / resize handles
function Hangar:buildHandles()
	local proxy = Instance.new("Part")
	proxy.Name = "HandleProxy"
	proxy.Anchored = true
	proxy.CanCollide = false
	proxy.CanQuery = false
	proxy.Transparency = 1
	proxy.Size = Vector3.new(1, 1, 1)
	proxy.Parent = self.folder
	self.handleProxy = proxy
	local playerGui = Players.LocalPlayer:WaitForChild("PlayerGui")
	local arcs = Instance.new("ArcHandles")
	arcs.Color3 = C.accent
	arcs.Visible = false
	arcs.Parent = playerGui
	self.arcHandles = arcs
	local sizer = Instance.new("Handles")
	sizer.Style = Enum.HandlesStyle.Resize
	sizer.Color3 = Color3.fromRGB(90, 170, 230)
	sizer.Visible = false
	sizer.Parent = playerGui
	self.sizeHandles = sizer

	-- RotationGizmo: rotate the selected part (and everything attached to it) around its attach point.
	local dragStart = nil
	table.insert(self.connections, arcs.MouseButton1Down:Connect(function()
		local e = self.selected and self.design:get(self.selected)
		if not e then
			return
		end
		local snapshot = {}
		local function collect(entry)
			snapshot[entry.id] = entry.cf
			for _, c in self.design:children(entry.id) do
				collect(c)
			end
		end
		collect(e)
		if self.mirrorMode and e.mirror and self.design:get(e.mirror) then
			collect(self.design:get(e.mirror))
		end
		dragStart = { pivot = self.handleProxy.CFrame, snapshot = snapshot }
	end))
	table.insert(self.connections, arcs.MouseDrag:Connect(function(axis, angle)
		local e = self.selected and self.design:get(self.selected)
		if not e or not dragStart then
			return
		end
		local step = if UserInputService:IsKeyDown(Enum.KeyCode.LeftShift) then 1 else ROTATION_STEP
		local snapped = math.rad(math.floor(math.deg(angle) / step + 0.5) * step)
		local axisVector = ({ [Enum.Axis.X] = Vector3.xAxis, [Enum.Axis.Y] = Vector3.yAxis, [Enum.Axis.Z] = Vector3.zAxis })[axis]
		local pivotLocal = self.origin:ToObjectSpace(dragStart.pivot)
		local rot = pivotLocal * CFrame.fromAxisAngle(axisVector, snapped) * pivotLocal:Inverse()
		local mirrorId = e.mirror
		local function apply(entry)
			local start = dragStart.snapshot[entry.id]
			if start then
				entry.cf = rot * start
				local b = self.built[entry.id]
				if b then
					b.model:PivotTo(self.origin * entry.cf)
				end
			end
			for _, c in self.design:children(entry.id) do
				apply(c)
			end
		end
		apply(e)
		if self.mirrorMode and mirrorId and self.design:get(mirrorId) then
			local function mirrorApply(entry)
				if entry.mirror then
					local twin = self.design:get(entry.mirror)
					if twin and dragStart.snapshot[entry.id] then
						local mcf = self:mirrorOf(entry.cf, entry.flip)
						twin.cf = mcf
						local b = self.built[twin.id]
						if b then
							b.model:PivotTo(self.origin * twin.cf)
						end
					end
				end
				for _, c in self.design:children(entry.id) do
					mirrorApply(c)
				end
			end
			mirrorApply(e)
		end
	end))
	table.insert(self.connections, arcs.MouseButton1Up:Connect(function()
		dragStart = nil
		self:renderDecals()
		self:placeHandles()
	end))

	-- ProceduralSizeGizmo: drag the faces of a Body to resize it.
	local sizeStart = nil
	table.insert(self.connections, sizer.MouseButton1Down:Connect(function()
		local e = self.selected and self.design:get(self.selected)
		if e and e.fuselage then
			sizeStart = { settings = Fuselage.copy(e.fuselage), last = 0 }
		end
	end))
	table.insert(self.connections, sizer.MouseDrag:Connect(function(face, distance)
		local e = self.selected and self.design:get(self.selected)
		if not e or not sizeStart then
			return
		end
		local d = math.floor(distance / 0.05 + 0.5) * 0.05
		if d == sizeStart.last then
			return
		end
		sizeStart.last = d
		local start = sizeStart.settings
		self:resizeBody(e.id, function(s)
			local function copyFrom(src)
				s.side1.radius, s.side2.radius = src.side1.radius, src.side2.radius
				s.side1.lengthOffset, s.side2.lengthOffset = src.side1.lengthOffset, src.side2.lengthOffset
			end
			copyFrom(start)
			if face == Enum.NormalId.Back then
				s.side1.lengthOffset += Vector3.new(0, 0, d)
			elseif face == Enum.NormalId.Front then
				s.side2.lengthOffset += Vector3.new(0, 0, d)
			elseif face == Enum.NormalId.Left or face == Enum.NormalId.Right then
				s.side1.radius += Vector2.new(d / 2, 0)
				s.side2.radius += Vector2.new(d / 2, 0)
			else
				s.side1.radius += Vector2.new(0, d / 2)
				s.side2.radius += Vector2.new(0, d / 2)
			end
		end, true)
	end))
	table.insert(self.connections, sizer.MouseButton1Up:Connect(function()
		sizeStart = nil
		self:placeHandles()
	end))
end

-- Put the handles on the selected part (rotation around its attach point; resize box around a Body).
function Hangar:placeHandles()
	local e = self.mode == "build" and self.selected and self.design:get(self.selected)
	if not e or not self.arcHandles then
		if self.arcHandles then
			self.arcHandles.Visible = false
			self.sizeHandles.Visible = false
		end
		return
	end
	local cp = collisionPointFor(e.name, e.fuselage)
	local pivotLocal = if cp then Frames.localPoint(cp.pos, e.flip) else Vector3.zero
	local proxy = self.handleProxy
	local data = Parts[e.name]
	if data.procedural and e.fuselage then
		local s = e.fuselage
		local p1, p2 = Fuselage.endPoints(s)
		local w = math.max(s.side1.radius.X, s.side2.radius.X) * 2
		local h = math.max(s.side1.radius.Y, s.side2.radius.Y) * 2
		local len = math.abs(p1.Z - p2.Z)
		local mid = (p1 + p2) / 2
		proxy.Size = Vector3.new(w, h, len)
		proxy.CFrame = self.origin * e.cf * CFrame.new(Frames.localPoint(Vector3.new(-mid.X, mid.Y, mid.Z), e.flip))
		self.sizeHandles.Adornee = proxy
		self.sizeHandles.Visible = true
		self.arcHandles.Visible = false
		return
	end
	self.sizeHandles.Visible = false
	proxy.Size = Vector3.one * 0.6
	proxy.CFrame = self.origin * e.cf * CFrame.new(pivotLocal)
	self.arcHandles.Adornee = proxy
	self.arcHandles.Visible = e.parent ~= nil
end

-- Snap points of a placed (or candidate) part in design space: { pos, forward, owner }
local function snapPointsFor(name: string, cf: CFrame, flip: boolean, fus: any?): { any }
	local data = Parts[name]
	local out = {}
	if data.procedural then
		local s = fus or Fuselage.default()
		local p1, p2 = Fuselage.endPoints(s)
		table.insert(out, { pos = cf * Frames.localPoint(Vector3.new(-p1.X, p1.Y, p1.Z), flip), forward = cf:VectorToWorldSpace(Vector3.zAxis), localPos = Vector3.new(-p1.X, p1.Y, p1.Z), localForward = Vector3.zAxis })
		table.insert(out, { pos = cf * Frames.localPoint(Vector3.new(-p2.X, p2.Y, p2.Z), flip), forward = cf:VectorToWorldSpace(-Vector3.zAxis), localPos = Vector3.new(-p2.X, p2.Y, p2.Z), localForward = -Vector3.zAxis })
		return out
	end
	for _, sp in data.snapPoints do
		local lp = v(sp.pos)
		table.insert(out, { pos = cf * Frames.localPoint(lp, flip), forward = cf:VectorToWorldSpace(v(sp.forward)), localPos = lp, localForward = v(sp.forward) })
	end
	return out
end

function collisionPointFor(name: string, fus: any?): any?
	local data = Parts[name]
	if data.procedural then
		local s = fus or Fuselage.default()
		local p1, p2 = Fuselage.endPoints(s)
		local a = p1 - Vector3.new(s.side1.radius.X, 0, 0)
		local b = p2 - Vector3.new(s.side2.radius.X, 0, 0)
		local mid = (a + b) / 2
		-- the attach point sits on the body's side (its unity "right" points into the parent)
		return { pos = Vector3.new(-mid.X, mid.Y, mid.Z), forward = Vector3.new(1, 0, 0), up = Vector3.yAxis }
	end
	if not data.collisionPoint then
		return nil
	end
	return { pos = v(data.collisionPoint.pos), forward = v(data.collisionPoint.forward), up = v(data.collisionPoint.up) }
end

-- Unity forward of a part (BuildingPart.GetForwardOrientation) in design space.
local function forwardOrientation(e: any): Vector3
	local f = v(Parts[e.name].forwardOrientation)
	if f.Magnitude < 0.5 then
		f = Vector3.zAxis
	end
	return e.cf:VectorToWorldSpace(f.Unit)
end

-- ------------------------------------------------------------------------------------------- placement solving
function Hangar:symmetryPlane(): (Vector3, Vector3)
	local base = self.design:basePart()
	if not base then
		return Vector3.zero, Vector3.xAxis
	end
	local b = self.built[base.id]
	local center = base.cf.Position
	if b and #b.visuals > 0 then
		local sum = Vector3.zero
		for _, p in b.visuals do
			sum += self.origin:PointToObjectSpace(p.Position)
		end
		center = sum / #b.visuals
	end
	return center, base.cf.XVector
end

function Hangar:mirrorOf(cf: CFrame, flip: boolean): (CFrame, boolean)
	local center, normal = self:symmetryPlane()
	local pos = cf.Position
	local mpos = pos - normal * (2 * (pos - center):Dot(normal))
	local f = Frames.reflect(cf.ZVector, normal)
	local u = Frames.reflect(cf.YVector, normal)
	return Frames.lookRotation(f, -u, mpos), not flip
end

function Hangar:designRay(): (Vector3, Vector3)
	local mouse = UserInputService:GetMouseLocation()
	local ray = Workspace.CurrentCamera:ViewportPointToRay(mouse.X, mouse.Y)
	return ray.Origin, ray.Direction
end

function Hangar:hitPlane(): (RaycastResult?, number?)
	local origin, dir = self:designRay()
	local include = {}
	for _, b in self.built do
		for _, p in b.visuals do
			table.insert(include, p)
		end
	end
	if #include == 0 then
		return nil, nil
	end
	local params = RaycastParams.new()
	params.FilterType = Enum.RaycastFilterType.Include
	params.FilterDescendantsInstances = include
	local hit = Workspace:Raycast(origin, dir * 500, params)
	if not hit then
		return nil, nil
	end
	local model = hit.Instance:FindFirstAncestorWhichIsA("Model")
	while model and model:GetAttribute("EntryId") == nil do
		model = model:FindFirstAncestorWhichIsA("Model")
	end
	return hit, model and model:GetAttribute("EntryId")
end

-- Returns cf (design space), flip, parentId or nil when the part can't be placed here.
function Hangar:solve(name: string): (CFrame?, boolean, number?)
	local data = Parts[name]
	local fus = if data.procedural then Fuselage.default() else nil
	if #self.design.parts == 0 then
		if not data.basePart then
			return nil, false, nil
		end
		return CFrame.Angles(0, math.rad(self.rotation), 0), false, nil
	end
	local rayO, rayD = self:designRay()
	-- 1. snap points (SolveSnapping)
	local candidateSnaps = snapPointsFor(name, CFrame.identity, false, fus)
	if #candidateSnaps > 0 then
		local best, bestD = nil, math.huge
		for _, e in self.design.parts do
			local taken = {}
			for _, other in self.design.parts do
				if other.parent == e.id then
					for _, sp in snapPointsFor(other.name, other.cf, other.flip, other.fuselage) do
						table.insert(taken, sp.pos)
					end
				end
			end
			if e.parent then
				local parent = self.design:get(e.parent)
				if parent then
					for _, sp in snapPointsFor(parent.name, parent.cf, parent.flip, parent.fuselage) do
						table.insert(taken, sp.pos)
					end
				end
			end
			for _, target in snapPointsFor(e.name, e.cf, e.flip, e.fuselage) do
				local free = true
				for _, t in taken do
					if (t - target.pos).Magnitude < 0.05 then
						free = false
					end
				end
				if free then
					local worldTarget = self.origin * target.pos
					local toPoint = worldTarget - rayO
					local along = toPoint:Dot(rayD.Unit)
					local d = (toPoint - rayD.Unit * along).Magnitude
					if along > 0 and d < bestD then
						bestD = d
						best = { target = target, owner = e }
					end
				end
			end
		end
		if best and bestD < SNAP_DISTANCE * 1.5 then
			-- choose the candidate snap point that faces the target best
			local own = candidateSnaps[1]
			local rot = Frames.fromToRotation(own.localForward, -best.target.forward)
			local spin = CFrame.fromAxisAngle(own.localForward, math.rad(self.rotation))
			local r = rot * spin
			local pos = best.target.pos - r * own.localPos
			local cf = r + pos
			if data.tryOrientUpwards then
				cf = Frames.lookRotation(cf.ZVector, Vector3.yAxis, cf.Position)
			end
			if data.tryMatchParentRotation then
				cf = Frames.lookRotation(cf.ZVector, best.owner.cf.YVector, cf.Position)
			end
			return cf, false, best.owner.id
		end
	end
	-- 2. surface attach via the collision point
	local cp = collisionPointFor(name, fus)
	if not cp then
		return nil, false, nil
	end
	local hit, ownerId = self:hitPlane()
	if not hit or not ownerId then
		return nil, false, nil
	end
	local owner = self.design:get(ownerId)
	if not owner then
		return nil, false, nil
	end
	local normalW = hit.Normal
	local normal = self.origin:VectorToObjectSpace(normalW)
	local point = self.origin:PointToObjectSpace(hit.Position)
	local lhs = forwardOrientation(owner)
	local planeForward = Vector3.zAxis
	if lhs:Dot(planeForward) < -0.99 and Parts[owner.name].symetrical then
		lhs = planeForward
	end
	-- Unity: Cross(lhs, normal) used as the attach point's up; mirrored space flips the cross product.
	local snapped = Vector3.new(MathUtil.snapToIncrement(normal.X, 1), MathUtil.snapToIncrement(normal.Y, 1), MathUtil.snapToIncrement(normal.Z, 1))
	if snapped.Magnitude < 0.5 then
		snapped = normal
	end
	snapped = snapped.Unit
	local up = snapped:Cross(lhs)
	if up.Magnitude < 1e-3 then
		up = snapped:Cross(if math.abs(snapped.Y) < 0.9 then Vector3.yAxis else Vector3.xAxis)
	end
	-- Rotation that puts the attach point's forward into the surface and its up along `up`.
	local target = Frames.lookRotation(-snapped, up.Unit)
	local localCP = Frames.lookRotation(cp.forward, cp.up)
	local base = target * localCP:Inverse()
	local spin = CFrame.fromAxisAngle(cp.forward, math.rad(self.rotation))
	local rot = base * spin
	local flip = false
	local pos = point - rot * cp.pos
	-- snap onto the symmetry plane when very close
	local center, n = self:symmetryPlane()
	local dist = (pos - center):Dot(n)
	if math.abs(dist) < 0.075 then
		pos -= n * dist
	end
	if data.flipSymmetry then
		local side = lhs:Cross(Vector3.yAxis)
		local toParent = (owner.cf.Position - pos)
		flip = side:Dot(toParent) > 0
	end
	return rot + pos, flip, owner.id
end

-- ------------------------------------------------------------------------------------------- tools
function Hangar:setTool(name: string?)
	self.tool = name
	self.selected = nil
	self:clearGhost()
	if name then
		self.ghost = PartVisual.create(name, CFrame.new(0, -1000, 0), false, if Parts[name].procedural then Fuselage.default() else nil)
		for _, p in self.ghost.visuals do
			p.Transparency = math.max(p.Transparency, 0.45)
			p.CanQuery = false
		end
		self.ghost.model.Parent = Workspace
		self.ghostMirror = PartVisual.create(name, CFrame.new(0, -1000, 0), true, if Parts[name].procedural then Fuselage.default() else nil)
		for _, p in self.ghostMirror.visuals do
			p.Transparency = math.max(p.Transparency, 0.6)
			p.CanQuery = false
		end
		self.ghostMirror.model.Parent = Workspace
	end
	self:refresh()
end

function Hangar:clearGhost()
	if self.ghost then
		self.ghost.model:Destroy()
		self.ghost = nil
	end
	if self.ghostMirror then
		self.ghostMirror.model:Destroy()
		self.ghostMirror = nil
	end
	self.ghostFlip = nil
end

local function moveBuilt(built: any, name: string, cf: CFrame, flip: boolean)
	-- rebuilding is simplest and robust to flips; ghosts are cheap (a handful of pieces)
	local data = Parts[name]
	local i = 0
	for _, node in data.nodes do
		i += 1
		local piece = built.visuals[i]
		if piece then
			piece.CFrame = cf * CFrame.new(Frames.localPoint(v(node.center), flip))
		end
	end
	if data.procedural then
		built.model:PivotTo(cf)
	end
end

function Hangar:updateGhost()
	if not self.tool or not self.ghost then
		return
	end
	local cf, flip, _ = self:solve(self.tool)
	local data = Parts[self.tool]
	if not cf then
		self.ghost.model:PivotTo(CFrame.new(0, -1000, 0))
		self.ghostMirror.model:PivotTo(CFrame.new(0, -1000, 0))
		self.pending = nil
		return
	end
	if data.procedural then
		self.ghost.model:PivotTo(self.origin * cf)
	else
		moveBuilt(self.ghost, self.tool, self.origin * cf, flip)
	end
	local pending = { cf = cf, flip = flip }
	if self.mirrorMode and #self.design.parts > 0 then
		local center, n = self:symmetryPlane()
		if math.abs((cf.Position - center):Dot(n)) > 0.08 then
			local mcf, mflip = self:mirrorOf(cf, flip)
			if data.procedural then
				self.ghostMirror.model:PivotTo(self.origin * mcf)
			else
				moveBuilt(self.ghostMirror, self.tool, self.origin * mcf, mflip)
			end
			pending.mirror = { cf = mcf, flip = mflip }
		else
			self.ghostMirror.model:PivotTo(CFrame.new(0, -1000, 0))
		end
	else
		self.ghostMirror.model:PivotTo(CFrame.new(0, -1000, 0))
	end
	self.pending = pending
end

function Hangar:priceOf(name: string, fus: any?): number
	return PlaneDesign.partStats({ name = name, fuselage = fus }).price
end

function Hangar:place()
	local name = self.tool
	if not name then
		return
	end
	local cf, flip, parentId = self:solve(name)
	if not cf then
		UI.toast(self.gui, if #self.design.parts == 0 then "Start with a cockpit or a body" else "Point at the plane to attach this part", C.bad)
		return
	end
	if not self.campaign:isUnlocked(name) then
		UI.toast(self.gui, "Research this part first", C.bad)
		return
	end
	local fus = if Parts[name].procedural then Fuselage.default() else nil
	local count = if self.pending and self.pending.mirror then 2 else 1
	local price = self:priceOf(name, fus) * count
	if not self.campaign:hasMoney(price) then
		UI.toast(self.gui, "Not enough money", C.bad)
		return
	end
	self.campaign:changeMoney(-price)
	local e = self.design:add({ name = name, cf = cf, flip = flip, parent = parentId, fuselage = fus })
	self:buildEntry(e)
	local added = { e }
	if self.pending and self.pending.mirror then
		local m = self.design:add({ name = name, cf = self.pending.mirror.cf, flip = self.pending.mirror.flip, parent = parentId, fuselage = fus and Fuselage.copy(fus), mirror = e.id })
		e.mirror = m.id
		self:buildEntry(m)
		table.insert(added, m)
		-- the mirrored part attaches to the mirrored parent when there is one
		local parent = parentId and self.design:get(parentId)
		if parent and parent.mirror then
			m.parent = parent.mirror
		end
	end
	table.insert(self.undo, { kind = "place", ids = { e.id, added[2] and added[2].id or nil }, price = price })
	Audio.play("placement")
	self:refresh()
end

function Hangar:deleteSelected()
	local id = self.selected
	if not id then
		return
	end
	local removed = self.design:remove(id, self.mirrorMode)
	local refund = 0
	for _, r in removed do
		refund += PlaneDesign.partStats(r).price
		self:removeVisual(r.id)
	end
	self.campaign:changeMoney(refund)
	table.insert(self.undo, { kind = "remove", entries = removed, price = refund })
	self.selected = nil
	Audio.play("remove")
	self:renderDecals()
	self:refresh()
end

function Hangar:undoLast()
	local u = table.remove(self.undo)
	if not u then
		return
	end
	if u.kind == "place" then
		for _, id in u.ids do
			if id then
				for _, r in self.design:remove(id, false) do
					self:removeVisual(r.id)
				end
			end
		end
		self.campaign:changeMoney(u.price)
	elseif u.kind == "remove" then
		if not self.campaign:hasMoney(u.price) then
			UI.toast(self.gui, "Not enough money to undo", C.bad)
			table.insert(self.undo, u)
			return
		end
		self.campaign:changeMoney(-u.price)
		for _, e in u.entries do
			table.insert(self.design.parts, e)
			self:buildEntry(e)
		end
	end
	self.selected = nil
	self:refresh()
end

-- Body resizing (ProceduralFuselage gizmos), with the price difference charged or refunded.
function Hangar:resizeBody(id: number, change: (any) -> (), quiet: boolean?)
	local e = self.design:get(id)
	if not e or not Parts[e.name].procedural then
		return
	end
	local entries = { e }
	if e.mirror and self.design:get(e.mirror) then
		table.insert(entries, self.design:get(e.mirror))
	end
	local before = 0
	for _, x in entries do
		before += PlaneDesign.partStats(x).price
	end
	local new = Fuselage.copy(e.fuselage or Fuselage.default())
	change(new)
	-- keep sizes sensible
	for _, side in { new.side1, new.side2 } do
		side.radius = Vector2.new(math.clamp(side.radius.X, 0.15, 4), math.clamp(side.radius.Y, 0.15, 4))
		side.lengthOffset = Vector3.new(side.lengthOffset.X, side.lengthOffset.Y, math.clamp(side.lengthOffset.Z, 0.1, 12))
		for i = 1, 4 do
			side.roundness[i] = math.clamp(side.roundness[i], 0, 1)
		end
	end
	local after = 0
	for _, x in entries do
		after += PlaneDesign.partStats({ name = x.name, fuselage = new }).price
	end
	if after > before and not self.campaign:hasMoney(after - before) then
		if not quiet then
			UI.toast(self.gui, "Not enough money", C.bad)
		end
		return
	end
	self.campaign:changeMoney(before - after)
	for _, x in entries do
		x.fuselage = Fuselage.copy(new)
		self:rebuildEntry(x)
	end
	self:refresh()
end

-- ------------------------------------------------------------------------------------------- UI
function Hangar:buildUI()
	local gui = UI.screen("Hangar", 4)
	self.gui = gui
	-- top bar
	local top = UI.panel({ Size = UDim2.new(0, 0, 0, 44), AutomaticSize = Enum.AutomaticSize.X, Position = UDim2.new(0, 16, 0, 16), Parent = gui })
	UI.padding(10).Parent = top
	UI.list(Enum.FillDirection.Horizontal, 18).Parent = top
	self.moneyLabel = UI.label("", 20, { AutomaticSize = Enum.AutomaticSize.X, Size = UDim2.new(0, 0, 1, 0), Font = UI.FontBold, Parent = top })
	self.scrapLabel = UI.label("", 18, { AutomaticSize = Enum.AutomaticSize.X, Size = UDim2.new(0, 0, 1, 0), Parent = top })

	-- part palette (bottom)
	local palette = UI.panel({ Size = UDim2.new(1, -420, 0, 204), AnchorPoint = Vector2.new(0, 1), Position = UDim2.new(0, 16, 1, -16), Parent = gui })
	UI.padding(10).Parent = palette
	local tabs = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 32), Parent = palette })
	self.tabs = tabs
	UI.list(Enum.FillDirection.Horizontal, 6).Parent = tabs
	self.tabButtons = {}
	for i, cat in CATEGORIES do
		local b = UI.button(CATEGORY_TITLES[cat], function()
			self.category = cat
			self:refresh()
		end, { Size = UDim2.new(0, 100, 0, 30), LayoutOrder = i, Parent = tabs })
		self.tabButtons[cat] = b
	end
	local scroll = UI.new("ScrollingFrame", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, 0, 1, -40),
		Position = UDim2.new(0, 0, 0, 40),
		CanvasSize = UDim2.new(0, 0, 0, 0),
		AutomaticCanvasSize = Enum.AutomaticSize.X,
		ScrollingDirection = Enum.ScrollingDirection.X,
		ScrollBarThickness = 6,
		Parent = palette,
	})
	UI.list(Enum.FillDirection.Horizontal, 8).Parent = scroll
	self.partList = scroll

	-- stats (right)
	local stats = UI.panel({ Size = UDim2.new(0, 380, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(1, 0), Position = UDim2.new(1, -16, 0, 16), Parent = gui })
	UI.padding(12).Parent = stats
	UI.list(nil, 4).Parent = stats
	UI.label("PLANE", 12, { TextColor3 = C.accent, Font = UI.FontBold, LayoutOrder = 0, Parent = stats })
	self.statsLabel = UI.label("", 15, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), LayoutOrder = 1, Parent = stats })
	self.selPanel = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, LayoutOrder = 2, Parent = stats })
	UI.list(nil, 6).Parent = self.selPanel

	-- toolbar (bottom right)
	local bar = UI.panel({ Size = UDim2.new(0, 380, 0, 204), AnchorPoint = Vector2.new(1, 1), Position = UDim2.new(1, -16, 1, -16), Parent = gui })
	UI.padding(10).Parent = bar
	local grid = UI.new("UIGridLayout", { CellSize = UDim2.new(0, 112, 0, 40), CellPadding = UDim2.new(0, 6, 0, 6), SortOrder = Enum.SortOrder.LayoutOrder, Parent = bar })
	local _ = grid
	self.mirrorButton = UI.button("Mirror: on", function()
		self.mirrorMode = not self.mirrorMode
		self:refresh()
	end, { LayoutOrder = 1, Parent = bar })
	self.modeButtons = {}
	for i, m in { { "build", "Build" }, { "paint", "Paint" }, { "decal", "Decals" } } do
		self.modeButtons[m[1]] = UI.button(m[2], function()
			self:setMode(m[1])
		end, { LayoutOrder = -10 + i, Parent = bar })
	end
	UI.button("Undo", function()
		self:undoLast()
	end, { LayoutOrder = 3, Parent = bar })
	UI.button("Research", function()
		self:openResearch()
	end, { LayoutOrder = 4, Parent = bar })
	UI.button("Designs", function()
		self.callbacks.designs()
	end, { LayoutOrder = 5, Parent = bar })
	UI.button("Controls", function()
		UI.modal("Controls", UI.ControlsText)
	end, { LayoutOrder = 6, Parent = bar })
	self.flyButton = UI.button("<b>FLY ✈</b>", function()
		self.callbacks.fly()
	end, { LayoutOrder = 7, BackgroundColor3 = C.accent, TextColor3 = C.text, Parent = bar })
	UI.button("Menu", function()
		self.callbacks.menu()
	end, { LayoutOrder = 8, Parent = bar })
end

function Hangar:setMode(mode: string)
	self.mode = mode
	if mode ~= "build" then
		self:setTool(nil)
		self.selected = nil
	end
	Audio.play("click")
	self:refresh()
end

function Hangar:swatches(order: number, current: { number }, onPick: ({ number }) -> ())
	for i, c in SWATCHES do
		local selected = math.abs(c[1] - current[1]) + math.abs(c[2] - current[2]) + math.abs(c[3] - current[3]) < 0.01
		local b = UI.button("", function()
			onPick(c)
			self:refresh()
		end, { Size = UDim2.new(0, 44, 0, 44), BackgroundColor3 = Color3.new(c[1], c[2], c[3]), LayoutOrder = order + i, Parent = self.partList })
		if selected then
			UI.new("UIStroke", { Color = C.accent, Thickness = 3, Parent = b })
		end
	end
end

function Hangar:partButton(name: string, order: number)
	local data = Parts[name]
	if not data then
		return
	end
	local unlocked = self.campaign:isUnlocked(name)
	local price = self:priceOf(name, if data.procedural then Fuselage.default() else nil)
	local c1, c2 = (require(Shared.Campaign) :: any).researchCost(name)
	local text = "<b>" .. (data.displayName or name) .. "</b>\n💰 " .. math.floor(price + 0.5)
	if not unlocked then
		text = "<b>" .. (data.displayName or name) .. "</b>\n🔒 ⚙" .. c1 .. (if c2 > 0 then " ✦" .. c2 else "")
	end
	local b = UI.button(text, function()
		if not unlocked then
			self:openResearch(name)
			return
		end
		if self.tool == name then
			self:setTool(nil)
		else
			self:setTool(name)
		end
	end, {
		Size = UDim2.new(0, 120, 0, 110),
		TextSize = 14,
		TextWrapped = true,
		LayoutOrder = order,
		BackgroundColor3 = if self.tool == name then C.accent elseif unlocked then C.panelLight else Color3.fromRGB(120, 120, 125),
		TextColor3 = if self.tool == name then C.text else C.textDark,
		Parent = self.partList,
	})
	return b
end

function Hangar:refresh()
	local campaign = self.campaign
	self.moneyLabel.Text = "💰 " .. MathUtil.formatNumber(campaign.money)
	self.scrapLabel.Text = "⚙ " .. campaign.scrap .. "   ✦ " .. campaign.advancedScrap
	for cat, b in self.tabButtons do
		b.BackgroundColor3 = if cat == self.category then C.accent else C.panelLight
		b.TextColor3 = if cat == self.category then C.text else C.textDark
	end
	for _, c in self.partList:GetChildren() do
		if c:IsA("GuiButton") then
			c:Destroy()
		end
	end
	self.tabs.Visible = self.mode == "build"
	for m, b in self.modeButtons do
		b.BackgroundColor3 = if m == self.mode then C.accent else C.panelLight
		b.TextColor3 = if m == self.mode then C.text else C.textDark
	end
	if self.mode == "build" then
		for i, name in ORDER[self.category] do
			if Parts[name] then
				self:partButton(name, i)
			end
		end
	elseif self.mode == "paint" then
		UI.label("Pick a colour, then click a part to paint it" .. (if self.mirrorMode then " (and its mirror)" else "") .. ".", 14, { Size = UDim2.new(0, 230, 0, 120), LayoutOrder = 0, Parent = self.partList })
		self:swatches(0, self.paintColor, function(c)
			self.paintColor = c
		end)
	else
		UI.label(string.format("Click the plane to place.\nDelete removes the decal under the mouse.\nSize %.1f  ·  Rotation %d°", self.decalSize, self.decalRotation), 13, { Size = UDim2.new(0, 210, 0, 120), LayoutOrder = 0, Parent = self.partList })
		for i, spec in { { "−", -0.1, 0 }, { "+", 0.1, 0 }, { "⟲", 0, -15 }, { "⟳", 0, 15 } } do
			UI.button(spec[1], function()
				self.decalSize = math.clamp(self.decalSize + spec[2], 0.1, 4)
				self.decalRotation = (self.decalRotation + spec[3]) % 360
				self:refresh()
			end, { Size = UDim2.new(0, 44, 0, 44), LayoutOrder = i, Parent = self.partList })
		end
		for i, d in Decals.List do
			local texture = Decals.textureFor(d.id)
			local b = UI.button(if texture then "" else d.id, function()
				self.decalImage = d.id
				self:refresh()
			end, { Size = UDim2.new(0, 56, 0, 56), TextSize = 12, LayoutOrder = 10 + i, BackgroundColor3 = if self.decalImage == d.id then C.accent else C.panelLight, Parent = self.partList })
			if texture then
				UI.new("ImageLabel", { BackgroundTransparency = 1, Image = texture, Size = UDim2.fromScale(0.8, 0.8), Position = UDim2.fromScale(0.1, 0.1), ImageColor3 = Color3.new(0.1, 0.1, 0.1), Parent = b })
			end
		end
		self:swatches(200, self.decalColor, function(c)
			self.decalColor = c
		end)
	end
	self.mirrorButton.Text = "Mirror: " .. (if self.mirrorMode then "on" else "off")

	local s = self.design:stats()
	local lines = {
		string.format("Parts: %d   Value: 💰%d", s.parts, math.floor(s.cost + 0.5)),
		string.format("Weight: %.1f t", s.weight),
		string.format("Thrust: %.0f  (thrust/weight %.2f)", s.thrust, if s.weight > 0 then s.thrust / (s.weight / 15 * 25.5) else 0),
		string.format("Lift: %d", math.floor(s.lift + 0.5)),
		string.format("Fuel: %d   Battery: %.2f   Cargo space: %d", math.floor(s.fuel + 1.5), s.battery, math.floor(s.cargo + 0.5)),
	}
	local warnings = {}
	if s.parts == 0 then
		table.insert(warnings, "Start with a cockpit or a body.")
	else
		if not s.hasEngine then
			table.insert(warnings, "No engine.")
		end
		if not s.hasWing then
			table.insert(warnings, "No wings (fine for helicopters and balloons).")
		end
		if not s.hasWheel then
			table.insert(warnings, "No wheels.")
		end
	end
	if #warnings > 0 then
		table.insert(lines, '<font color="#E0A040">' .. table.concat(warnings, " ") .. "</font>")
	end
	self.statsLabel.Text = table.concat(lines, "\n")
	self:refreshSelection()
end

function Hangar:refreshSelection()
	for _, c in self.selPanel:GetChildren() do
		if c:IsA("GuiObject") then
			c:Destroy()
		end
	end
	local e = self.selected and self.design:get(self.selected)
	self:placeHandles()
	if not e then
		self.highlight.Adornee = nil
		return
	end
	local b = self.built[e.id]
	self.highlight.Adornee = b and b.model or nil
	local data = Parts[e.name]
	local ps = PlaneDesign.partStats(e)
	UI.label("SELECTED", 12, { TextColor3 = C.accent, Font = UI.FontBold, Parent = self.selPanel })
	UI.label(string.format("<b>%s</b>  · %.2f t · 💰%d", data.displayName or e.name, ps.weight, math.floor(ps.price + 0.5)), 15, { Parent = self.selPanel })
	UI.button("Remove (Del)", function()
		self:deleteSelected()
	end, { BackgroundColor3 = C.bad, TextColor3 = C.text, Parent = self.selPanel })
	if data.procedural then
		local id = e.id
		local function row(label: string, f: (any, number) -> ())
			local r = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 30), Parent = self.selPanel })
			UI.label(label, 14, { Size = UDim2.new(1, -110, 1, 0), Parent = r })
			UI.button("−", function()
				self:resizeBody(id, function(s)
					f(s, -1)
				end)
			end, { Size = UDim2.new(0, 50, 0, 28), Position = UDim2.new(1, -106, 0, 1), Parent = r })
			UI.button("+", function()
				self:resizeBody(id, function(s)
					f(s, 1)
				end)
			end, { Size = UDim2.new(0, 50, 0, 28), Position = UDim2.new(1, -52, 0, 1), Parent = r })
		end
		row("Length (front)", function(s, d)
			s.side1.lengthOffset += Vector3.new(0, 0, 0.25 * d)
		end)
		row("Length (back)", function(s, d)
			s.side2.lengthOffset += Vector3.new(0, 0, 0.25 * d)
		end)
		row("Width (front / back)", function(s, d)
			s.side1.radius += Vector2.new(0.1 * d, 0)
			s.side2.radius += Vector2.new(0.1 * d, 0)
		end)
		row("Height (front / back)", function(s, d)
			s.side1.radius += Vector2.new(0, 0.1 * d)
			s.side2.radius += Vector2.new(0, 0.1 * d)
		end)
		row("Front size (taper)", function(s, d)
			s.side1.radius += Vector2.new(0.1 * d, 0.1 * d)
		end)
		row("Back size (taper)", function(s, d)
			s.side2.radius += Vector2.new(0.1 * d, 0.1 * d)
		end)
		row("Roundness", function(s, d)
			for i = 1, 4 do
				s.side1.roundness[i] += 0.25 * d
				s.side2.roundness[i] += 0.25 * d
			end
		end)
	end
end

-- ResearchPanel: unlock parts with scrap (⚙) and advanced scrap (✦).
function Hangar:openResearch(focusPart: string?)
	if self.researchGui then
		self.researchGui:Destroy()
	end
	local gui = UI.screen("Research", 15)
	self.researchGui = gui
	local frame = UI.panel({ Size = UDim2.new(0.75, 0, 0.75, 0), AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = gui })
	UI.padding(16).Parent = frame
	UI.label("RESEARCH — ⚙ " .. self.campaign.scrap .. "   ✦ " .. self.campaign.advancedScrap, 20, { Font = UI.FontBold, Parent = frame })
	UI.button("Close", function()
		gui:Destroy()
		self.researchGui = nil
	end, { Position = UDim2.new(1, -140, 0, 0), Parent = frame })
	local scroll = UI.new("ScrollingFrame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 1, -50), Position = UDim2.new(0, 0, 0, 50), AutomaticCanvasSize = Enum.AutomaticSize.Y, CanvasSize = UDim2.new(), ScrollBarThickness = 6, Parent = frame })
	UI.new("UIGridLayout", { CellSize = UDim2.new(0, 170, 0, 96), CellPadding = UDim2.new(0, 8, 0, 8), SortOrder = Enum.SortOrder.LayoutOrder, Parent = scroll })
	local items = table.clone(Research)
	table.sort(items, function(a, b)
		if a.x ~= b.x then
			return a.x < b.x
		end
		return a.y < b.y
	end)
	for i, item in items do
		local name = item.part
		if name and Parts[name] then
			local unlocked = self.campaign:isUnlocked(name)
			local data = Parts[name]
			local text = "<b>" .. (data.displayName or name) .. "</b>\n" .. (if unlocked then "Unlocked ✓" else "⚙ " .. item.cost1 .. (if item.cost2 > 0 then "   ✦ " .. item.cost2 else ""))
			UI.button(text, function()
				if unlocked then
					return
				end
				if self.campaign:unlock(name) then
					UI.toast(gui, (data.displayName or name) .. " unlocked!", C.good)
					self:openResearch()
					self:refresh()
					self.callbacks.changed()
				else
					UI.toast(gui, "Not enough scrap. Deliver cargo and collect scrap to earn more.", C.bad)
				end
			end, {
				LayoutOrder = i,
				TextWrapped = true,
				TextSize = 14,
				BackgroundColor3 = if unlocked then C.good elseif name == focusPart then C.accent else C.panelLight,
				TextColor3 = if unlocked or name == focusPart then C.text else C.textDark,
				Parent = scroll,
			})
		end
	end
end

-- ------------------------------------------------------------------------------------------- input & camera
function Hangar:bindInput()
	local camera = Workspace.CurrentCamera
	camera.CameraType = Enum.CameraType.Scriptable
	local dragging = false
	table.insert(self.connections, UserInputService.InputBegan:Connect(function(input, processed)
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			dragging = true
		end
		if processed then
			return
		end
		if input.UserInputType == Enum.UserInputType.MouseButton1 then
			if self.mode == "paint" then
				self:paintUnderMouse()
			elseif self.mode == "decal" then
				self:placeDecal()
			elseif self.tool then
				self:place()
			else
				local _, id = self:hitPlane()
				self.selected = id
				if id then
					Audio.play("clickSelection")
				end
				self:refresh()
			end
		elseif input.KeyCode == Enum.KeyCode.R then
			self.rotation = (self.rotation + ROTATION_STEP) % 360
		elseif input.KeyCode == Enum.KeyCode.T then
			self.rotation = (self.rotation - ROTATION_STEP) % 360
		elseif input.KeyCode == Enum.KeyCode.X then
			self.mirrorMode = not self.mirrorMode
			self:refresh()
		elseif input.KeyCode == Enum.KeyCode.Delete or input.KeyCode == Enum.KeyCode.Backspace then
			if self.mode == "decal" then
				self:removeDecalUnderMouse()
			else
				self:deleteSelected()
			end
		elseif input.KeyCode == Enum.KeyCode.Z and (UserInputService:IsKeyDown(Enum.KeyCode.LeftControl) or UserInputService:IsKeyDown(Enum.KeyCode.RightControl)) then
			self:undoLast()
		elseif input.KeyCode == Enum.KeyCode.Escape then
			self:setTool(nil)
		end
	end))
	table.insert(self.connections, UserInputService.InputEnded:Connect(function(input)
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			dragging = false
		end
	end))
	table.insert(self.connections, UserInputService.InputChanged:Connect(function(input, processed)
		if input.UserInputType == Enum.UserInputType.MouseMovement and dragging then
			self.camYaw -= input.Delta.X * 0.008
			self.camPitch = math.clamp(self.camPitch + input.Delta.Y * 0.008, -1.2, 1.4)
		elseif input.UserInputType == Enum.UserInputType.MouseWheel and not processed then
			self.camDistance = math.clamp(self.camDistance * (1 - input.Position.Z * 0.1), 3, 80)
		elseif input.UserInputType == Enum.UserInputType.Touch and dragging then
			self.camYaw -= input.Delta.X * 0.008
		end
	end))
	table.insert(self.connections, RunService.RenderStepped:Connect(function()
		UserInputService.MouseBehavior = if dragging then Enum.MouseBehavior.LockCurrentPosition else Enum.MouseBehavior.Default
		local focus = self.origin.Position
		local n = 0
		local sum = Vector3.zero
		for _, b in self.built do
			for _, p in b.visuals do
				sum += p.Position
				n += 1
			end
		end
		if n > 0 then
			focus = sum / n
		end
		local offset = CFrame.Angles(0, self.camYaw, 0) * CFrame.Angles(-self.camPitch, 0, 0) * Vector3.new(0, 0, self.camDistance)
		camera.CFrame = CFrame.lookAt(focus + offset, focus)
		self:updateGhost()
		if self.mode == "decal" then
			local d, cf = self:decalPlacement()
			if self.decalGhost then
				self.decalGhost:Destroy()
				self.decalGhost = nil
			end
			if d and cf then
				self.decalGhost = Decals.create(d, cf, Workspace)
				self.decalGhost.CanQuery = false
			end
		elseif self.decalGhost then
			self.decalGhost:Destroy()
			self.decalGhost = nil
		end
	end))
end

function Hangar:destroy()
	for _, c in self.connections do
		c:Disconnect()
	end
	self:clearGhost()
	if self.researchGui then
		self.researchGui:Destroy()
	end
	self.gui:Destroy()
	self.highlight:Destroy()
	if self.arcHandles then
		self.arcHandles:Destroy()
		self.sizeHandles:Destroy()
	end
	if self.decalGhost then
		self.decalGhost:Destroy()
	end
	self.folder:Destroy()
	UserInputService.MouseBehavior = Enum.MouseBehavior.Default
end

return Hangar
