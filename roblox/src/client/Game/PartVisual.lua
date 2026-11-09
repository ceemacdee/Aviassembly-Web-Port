--!strict
-- Builds the Roblox instances for one placed part: imported meshes (or coloured stand-in blocks when the meshes
-- have not been imported yet), invisible physics colliders, and the procedural fuselage for "Body".
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Parts = require(Shared.Data.Parts)
local Fuselage = require(Shared.Fuselage)
local Frames = require(script.Parent.Frames)
local Triangles = require(script.Parent.Triangles)

local PartVisual = {}

-- Name of the folder (anywhere under ReplicatedStorage) holding the models imported from the .glb files.
PartVisual.MeshFolderName = "AviassemblyMeshes"
-- If Studio's importer turned the meshes around, set a correction here (applied to every imported mesh).
PartVisual.MeshRotationFix = CFrame.identity

local meshIndex: { [string]: MeshPart }? = nil
local missingWarned = false

function PartVisual.refreshMeshIndex()
	local index: { [string]: MeshPart } = {}
	for _, d in ReplicatedStorage:GetDescendants() do
		if d:IsA("MeshPart") then
			index[d.Name] = d
		end
	end
	meshIndex = index
	return index
end

function PartVisual.findMesh(name: string): MeshPart?
	local index = meshIndex or PartVisual.refreshMeshIndex()
	return index[name]
end

function PartVisual.hasImportedMeshes(): boolean
	local index = meshIndex or PartVisual.refreshMeshIndex()
	return next(index) ~= nil
end

local function color(t: { number }?): Color3
	if not t then
		return Color3.fromRGB(220, 220, 220)
	end
	return Color3.new(t[1], t[2], t[3])
end

-- Creates a visual for one exported mesh piece (part node or airport chunk).
function PartVisual.meshPiece(node: any, worldCFrame: CFrame, parent: Instance, meshName: string?): BasePart
	local source = PartVisual.findMesh(meshName or node.name)
	local piece: BasePart
	if source then
		local m = source:Clone()
		m.Size = Vector3.new(node.size[1], node.size[2], node.size[3])
		piece = m
		worldCFrame = worldCFrame * PartVisual.MeshRotationFix
	else
		if not missingWarned then
			missingWarned = true
			warn("[Aviassembly] Imported meshes not found under ReplicatedStorage." .. PartVisual.MeshFolderName .. " - using stand-in blocks. See README (Importing your models).")
		end
		local p = Instance.new("Part")
		p.Size = Vector3.new(math.max(node.size[1], 0.05), math.max(node.size[2], 0.05), math.max(node.size[3], 0.05))
		p.Color = color(node.color)
		p.Material = Enum.Material.SmoothPlastic
		p.TopSurface = Enum.SurfaceType.Smooth
		p.BottomSurface = Enum.SurfaceType.Smooth
		piece = p
	end
	piece.Name = node.name
	piece.Transparency = math.clamp(1 - (node.alpha or 1), 0, 0.85)
	piece.CFrame = worldCFrame
	piece.Anchored = true
	piece.CanCollide = false
	piece.CanTouch = false
	piece.CanQuery = true
	piece.Massless = true
	piece.Parent = parent
	return piece
end

export type Built = {
	model: Model,
	visuals: { BasePart },
	colliders: { BasePart },
	spinners: { { part: BasePart, axis: Vector3 } },
}

local BODY_COLOR = Color3.new(0.915, 0.915, 0.915)

local function buildFuselage(settings: any, cf: CFrame, flip: boolean, model: Model, built: Built, bodyColor: Color3?)
	local tris = Fuselage.triangles(settings)
	local centerLocal = Vector3.zero
	for _, t in tris do
		local pts = {}
		for i, p in t do
			-- Unity part space -> mirrored Roblox part space -> world
			pts[i] = Frames.worldPoint(cf, flip, Vector3.new(-p.X, p.Y, p.Z))
		end
		local centroid = (pts[1] + pts[2] + pts[3]) / 3
		local outward = centroid - cf * centerLocal
		local w1, w2 = Triangles.draw(pts[1], pts[2], pts[3], { color = bodyColor or BODY_COLOR, thickness = 0.03, outward = outward, parent = model })
		for _, w in { w1, w2 } do
			if w then
				w.Massless = true
				w.CanQuery = true
				table.insert(built.visuals, w)
			end
		end
	end
	-- Collider: the box around the loft.
	local s = settings
	local w = math.max(s.side1.radius.X, s.side2.radius.X) * 2
	local h = math.max(s.side1.radius.Y, s.side2.radius.Y) * 2
	local p1, p2 = Fuselage.endPoints(s)
	local mid = (p1 + p2) / 2
	local len = math.max(math.abs(p1.Z - p2.Z), 0.1)
	local box = Instance.new("Part")
	box.Name = "Collider"
	box.Size = Vector3.new(w, h, len)
	box.CFrame = cf * CFrame.new(Frames.localPoint(Vector3.new(-mid.X, mid.Y, mid.Z), flip))
	table.insert(built.colliders, box)
end

-- Build the instances for a part placed at `cf` (port convention, see Frames). Everything is created anchored;
-- Flight unanchors and welds them.
function PartVisual.create(partName: string, cf: CFrame, flip: boolean, fuselageSettings: any?, opts: { color: Color3? }?): Built
	local data = Parts[partName]
	assert(data, "unknown part " .. tostring(partName))
	local model = Instance.new("Model")
	model.Name = partName
	local built: Built = { model = model, visuals = {}, colliders = {}, spinners = {} }

	if data.procedural then
		buildFuselage(fuselageSettings or Fuselage.default(), cf, flip, model, built, opts and opts.color)
	end

	local spinPath = data.propeller and data.propeller.path
	local spinAxis = Vector3.xAxis
	for _, comp in data.components do
		if comp.type == "Engine" then
			local f = comp.frame
			spinAxis = if comp.data.helicopterRotor == 1 then Frames.v(f.up) else Frames.v(f.right)
		end
	end
	local paint = opts and opts.color
	for _, node in data.nodes do
		local center = Frames.localPoint(Frames.v(node.center), flip)
		-- Mirrored copies use the top-to-bottom flipped mesh exported for them (Unity used a negative scale).
		local meshName = nil
		if flip and node.mirror and PartVisual.findMesh(node.mirror) then
			meshName = node.mirror
		end
		local piece = PartVisual.meshPiece(node, cf * CFrame.new(center), model, meshName)
		if node.paintable then
			piece:SetAttribute("Paintable", true)
			if paint then
				piece.Color = paint
			end
		end
		table.insert(built.visuals, piece)
		if spinPath and node.path and (node.path == spinPath or string.sub(node.path, 1, #spinPath + 1) == spinPath .. "/") then
			table.insert(built.spinners, { part = piece, axis = spinAxis })
		end
	end

	for _, c in data.colliders do
		if c.active then
			local part: BasePart
			if c.shape == "ball" then
				local b = Instance.new("Part")
				b.Shape = Enum.PartType.Ball
				part = b
			else
				part = Instance.new("Part")
			end
			part.Name = "Collider"
			part.Size = Vector3.new(math.max(c.size[1], 0.05), math.max(c.size[2], 0.05), math.max(c.size[3], 0.05))
			local center = Frames.localPoint(Frames.v(c.center), flip)
			local right, up = Frames.v(c.right), Frames.v(c.up)
			local z = right:Cross(up)
			if z.Magnitude < 1e-6 then
				z = Vector3.zAxis
			end
			part.CFrame = cf * CFrame.fromMatrix(center, right.Unit, up.Unit, z.Unit)
			table.insert(built.colliders, part)
		end
	end
	for _, c in built.colliders do
		c.Transparency = 1
		c.Anchored = true
		c.CanCollide = false
		c.CanTouch = false
		c.CanQuery = false
		c.Parent = model
	end
	model:SetAttribute("PartName", partName)
	model.WorldPivot = cf
	return built
end

-- PartPainter: only the paintable surfaces (and the procedural body) change colour.
function PartVisual.setColor(built: Built, color: Color3)
	for _, v in built.visuals do
		if v:GetAttribute("Paintable") or v:IsA("WedgePart") then
			v.Color = color
		end
	end
end

-- Default paint colour of a part (its first paintable surface), for the colour picker.
function PartVisual.defaultColor(partName: string): Color3
	local data = Parts[partName]
	if data.procedural then
		return BODY_COLOR
	end
	for _, node in data.nodes do
		if node.paintable then
			return color(node.color)
		end
	end
	return BODY_COLOR
end

function PartVisual.isPaintable(partName: string): boolean
	local data = Parts[partName]
	if data.procedural then
		return true
	end
	for _, node in data.nodes do
		if node.paintable then
			return true
		end
	end
	return false
end

return PartVisual
