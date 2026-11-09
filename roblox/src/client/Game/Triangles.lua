--!strict
-- Draws arbitrary triangles with two WedgeParts each (the standard Roblox technique). Used for the procedural
-- fuselage and the low-poly terrain. `outward` picks which side of the triangle is the visible surface; the
-- wedges are pushed `thickness` behind it.
local Triangles = {}

local function makeWedge(template: WedgePart?): WedgePart
	local w = if template then template:Clone() else Instance.new("WedgePart")
	w.Anchored = true
	w.CanCollide = false
	w.CanTouch = false
	w.TopSurface = Enum.SurfaceType.Smooth
	w.BottomSurface = Enum.SurfaceType.Smooth
	w.CastShadow = true
	return w
end

export type Props = {
	color: Color3,
	material: Enum.Material?,
	thickness: number?,
	outward: Vector3?, -- preferred normal direction of the visible face
	parent: Instance,
	anchored: boolean?,
	canCollide: boolean?,
	canQuery: boolean?,
	transparency: number?,
	template: WedgePart?,
}

-- Returns the two wedges (or nothing for degenerate triangles).
function Triangles.draw(a: Vector3, b: Vector3, c: Vector3, props: Props): (WedgePart?, WedgePart?)
	local ab, ac, bc = b - a, c - a, c - b
	local abd, acd, bcd = ab:Dot(ab), ac:Dot(ac), bc:Dot(bc)
	if abd > acd and abd > bcd then
		c, a = a, c
	elseif acd > bcd and acd > abd then
		a, b = b, a
	end
	ab, ac, bc = b - a, c - a, c - b
	local cross = ac:Cross(ab)
	if cross.Magnitude < 1e-7 or bc.Magnitude < 1e-6 then
		return nil, nil
	end
	local right = cross.Unit
	local up = bc:Cross(right).Unit
	local back = bc.Unit
	local height = math.abs(ab:Dot(up))
	if height < 1e-4 then
		return nil, nil
	end
	local thickness = props.thickness or 0.05
	local outward = props.outward
	local normal = right
	if outward and normal:Dot(outward) < 0 then
		normal = -normal
	end
	local offset = -normal * (thickness / 2)

	local w1 = makeWedge(props.template)
	w1.Size = Vector3.new(thickness, height, math.abs(ab:Dot(back)))
	w1.CFrame = CFrame.fromMatrix((a + b) / 2 + offset, right, up, back)
	local w2 = makeWedge(props.template)
	w2.Size = Vector3.new(thickness, height, math.abs(ac:Dot(back)))
	w2.CFrame = CFrame.fromMatrix((a + c) / 2 + offset, -right, up, -back)
	for _, w in { w1, w2 } do
		w.Color = props.color
		w.Material = props.material or Enum.Material.SmoothPlastic
		w.Anchored = if props.anchored == nil then true else props.anchored
		w.CanCollide = props.canCollide == true
		w.CanQuery = props.canQuery ~= false
		w.Transparency = props.transparency or 0
		w.Parent = props.parent
	end
	return w1, w2
end

return Triangles
