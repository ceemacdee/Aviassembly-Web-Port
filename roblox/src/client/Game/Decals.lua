--!strict
-- Plane decals (DecalPlacer / Decal / MeshDecal). Each decal is a thin invisible part sitting on the surface
-- with a Roblox Decal on its front face. The images come from the "Decals.glb" import: the importer uploads
-- each image and the game reuses the TextureID of the matching "Decal_<name>" mesh. Before that import, the
-- decal shows its name as text so placement still works.
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Decorations = require(Shared.Data.Decorations)
local PartVisual = require(script.Parent.PartVisual)

local Decals = {}
Decals.List = Decorations.decals
Decals.DefaultColor = Color3.new(0.192, 0.2, 0.192) -- Decal.mat colour
Decals.DefaultSize = 0.6

export type DecalData = {
	part: number, -- PlaneDesign entry id
	pos: { number }, -- part-local position (port convention, before flip)
	normal: { number }, -- part-local surface normal
	up: { number }, -- part-local up direction of the image
	size: number,
	color: { number },
	image: string,
	mirror: number?, -- index of the mirrored decal
}

function Decals.textureFor(image: string): string?
	for _, d in Decals.List do
		if d.id == image then
			local mesh = PartVisual.findMesh(d.mesh)
			if mesh and mesh:IsA("MeshPart") and mesh.TextureID ~= "" then
				return mesh.TextureID
			end
			local sa = mesh and mesh:FindFirstChildOfClass("SurfaceAppearance")
			if sa and sa.ColorMap ~= "" then
				return sa.ColorMap
			end
		end
	end
	return nil
end

local FALLBACK_TEXT = { Circle_0 = "●", Triangle = "▲", Rect = "■", Hexagon = "⬢", Star = "★", Star5Point = "★", Diamond = "◆", Hearth = "♥", Arrow = "➜", Alert = "!", Electricity = "⚡", HalfCircle = "◗", Cresent = "☾" }

-- World CFrame of a decal given the CFrame of its part.
function Decals.cframe(d: any, partCFrame: CFrame, flip: boolean): CFrame
	local p = Vector3.new(d.pos[1], d.pos[2], d.pos[3])
	local n = Vector3.new(d.normal[1], d.normal[2], d.normal[3])
	local u = Vector3.new(d.up[1], d.up[2], d.up[3])
	if flip then
		p = Vector3.new(p.X, -p.Y, p.Z)
		n = Vector3.new(n.X, -n.Y, n.Z)
		u = Vector3.new(u.X, -u.Y, u.Z)
	end
	local wp = partCFrame * p
	local wn = partCFrame:VectorToWorldSpace(n).Unit
	local wu = partCFrame:VectorToWorldSpace(u)
	wu = (wu - wn * wu:Dot(wn))
	if wu.Magnitude < 1e-3 then
		wu = if math.abs(wn.Y) < 0.9 then Vector3.yAxis else Vector3.xAxis
	end
	return CFrame.lookAt(wp + wn * 0.012, wp + wn * 2, wu.Unit)
end

function Decals.create(d: any, worldCFrame: CFrame, parent: Instance): BasePart
	local holder = Instance.new("Part")
	holder.Name = "Decal"
	holder.Size = Vector3.new(d.size, d.size, 0.02)
	holder.CFrame = worldCFrame
	holder.Transparency = 1
	holder.Anchored = true
	holder.CanCollide = false
	holder.CanTouch = false
	holder.CanQuery = true
	holder.Massless = true
	local color = Color3.new(d.color[1], d.color[2], d.color[3])
	local texture = Decals.textureFor(d.image)
	if texture then
		local decal = Instance.new("Decal")
		decal.Face = Enum.NormalId.Front
		decal.Texture = texture
		decal.Color3 = color
		decal.Parent = holder
	else
		local gui = Instance.new("SurfaceGui")
		gui.Face = Enum.NormalId.Front
		gui.CanvasSize = Vector2.new(100, 100)
		gui.LightInfluence = 1
		local label = Instance.new("TextLabel")
		label.BackgroundTransparency = 1
		label.Size = UDim2.fromScale(1, 1)
		label.Text = FALLBACK_TEXT[d.image] or d.image
		label.TextScaled = true
		label.Font = Enum.Font.GothamBlack
		label.TextColor3 = color
		label.Parent = gui
		gui.Parent = holder
	end
	holder:SetAttribute("DecalImage", d.image)
	holder.Parent = parent
	return holder
end

return Decals
