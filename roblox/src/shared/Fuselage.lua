--!strict
-- Procedural fuselage ("Body" part), ported from ProceduralFuselageMesh / ProceduralFuselage / FuselageData.
-- Geometry is generated in Unity part space and returned mirrored on X (Roblox part space).
local Fuselage = {}

export type Side = { radius: Vector2, roundness: { number }, lengthOffset: Vector3 }
export type Settings = { side1: Side, side2: Side }

-- Resolution per side of the cross-section. The original used 64 (smooth); Roblox draws triangles with
-- wedge parts, so a lower resolution keeps the part count reasonable while staying visually round.
Fuselage.Resolution = 4

function Fuselage.default(): Settings
	return {
		side1 = { radius = Vector2.new(0.5, 0.5), roundness = { 1, 1, 1, 1 }, lengthOffset = Vector3.new(0, 0, 0.5) },
		side2 = { radius = Vector2.new(0.5, 0.5), roundness = { 1, 1, 1, 1 }, lengthOffset = Vector3.new(0, 0, 0.5) },
	}
end

function Fuselage.copy(s: Settings): Settings
	return {
		side1 = { radius = s.side1.radius, roundness = table.clone(s.side1.roundness), lengthOffset = s.side1.lengthOffset },
		side2 = { radius = s.side2.radius, roundness = table.clone(s.side2.roundness), lengthOffset = s.side2.lengthOffset },
	}
end

local function closestCornerIndex(p: Vector3, corners: { Vector3 }): number
	local best, bestD = 1, math.huge
	for i, c in corners do
		local d = (p - c).Magnitude
		if d < bestD then
			bestD = d
			best = i
		end
	end
	return best
end

-- GenerateVertices: a rounded rectangle ring at depth position.Z.
local function ring(size: Vector2, position: Vector3, bevel: { number }, resolution: number): { Vector3 }
	local corners = {
		Vector3.new(-size.X + position.X, -size.Y + position.Y, position.Z),
		Vector3.new(-size.X + position.X, size.Y + position.Y, position.Z),
		Vector3.new(size.X + position.X, size.Y + position.Y, position.Z),
		Vector3.new(size.X + position.X, -size.Y + position.Y, position.Z),
	}
	local stepX = size.X * 2 / resolution
	local stepY = size.Y * 2 / resolution
	local pts = {}
	local p = corners[1]
	local dir = (corners[2] - p).Unit
	local count = 0
	for _ = 1, resolution * 4 do
		table.insert(pts, p)
		local step = if math.abs(dir.X) < 0.1 then stepY else stepX
		p += dir * step
		count += 1
		if count == resolution then
			-- rotate -90 degrees about Z (Unity): (x, y) -> (y, -x)
			dir = Vector3.new(dir.Y, -dir.X, 0)
			count = 0
		end
	end
	local minSize = math.min(size.X, size.Y)
	for i, v in pts do
		local index = closestCornerIndex(v, corners)
		local corner = corners[index]
		local distance = (v - corner).Magnitude
		local r = math.clamp(bevel[index], 0, 1) * minSize
		if distance < r then
			local n = Vector3.new(math.sign(corner.X - position.X), math.sign(corner.Y - position.Y), 0).Unit
			local center = corner - n * math.sqrt(r * r + r * r)
			pts[i] = center + (v - center).Unit * r
		end
	end
	return pts
end

local function center(points: { Vector3 }): Vector3
	local sum = Vector3.zero
	for _, p in points do
		sum += p
	end
	return sum / #points
end

-- Returns triangles { {a, b, c}, ... } in Unity part space.
function Fuselage.triangles(s: Settings, resolution: number?): { { Vector3 } }
	local res = resolution or Fuselage.Resolution
	local r1 = ring(s.side1.radius, Vector3.new(s.side1.lengthOffset.X, s.side1.lengthOffset.Y, s.side1.lengthOffset.Z), s.side1.roundness, res)
	local r2 = ring(s.side2.radius, Vector3.new(s.side2.lengthOffset.X, s.side2.lengthOffset.Y, -s.side2.lengthOffset.Z), s.side2.roundness, res)
	local tris = {}
	local n = #r1
	for i = 1, n do
		local j = i % n + 1
		table.insert(tris, { r1[i], r1[j], r2[j] })
		table.insert(tris, { r1[i], r2[j], r2[i] })
	end
	local c1, c2 = center(r1), center(r2)
	for i = 1, n do
		local j = i % n + 1
		table.insert(tris, { c1, r1[j], r1[i] })
		table.insert(tris, { c2, r2[i], r2[j] })
	end
	return tris
end

-- Signed-volume sum, like GetMeshVolume (computed on the full 64-resolution mesh in the original).
function Fuselage.volume(s: Settings): number
	local total = 0
	for _, t in Fuselage.triangles(s, 16) do
		local a, b, c = t[1], t[2], t[3]
		total += a:Dot(b:Cross(c)) / 6
	end
	return math.abs(total)
end

local DEFAULT_VOLUME = Fuselage.volume(Fuselage.default())

-- FuselageData.ChangeVolume: cargo, weight and price scale with the body's volume.
function Fuselage.stats(s: Settings, baseCargo: number, baseWeight: number, basePrice: number)
	local v = Fuselage.volume(s)
	local k = v / DEFAULT_VOLUME
	return { cargoVolume = baseCargo * k, weight = baseWeight * k, price = basePrice * k, volume = v }
end

-- Snap points p1 / p2 and the attach point, in Unity part space.
function Fuselage.endPoints(s: Settings): (Vector3, Vector3)
	local p1 = Vector3.new(s.side1.lengthOffset.X, s.side1.lengthOffset.Y, s.side1.lengthOffset.Z)
	local p2 = Vector3.new(s.side2.lengthOffset.X, s.side2.lengthOffset.Y, -s.side2.lengthOffset.Z)
	return p1, p2
end

function Fuselage.length(s: Settings): number
	return math.abs(s.side1.lengthOffset.Z + s.side2.lengthOffset.Z)
end

-- Serialisation helpers (DataStore friendly).
function Fuselage.encode(s: Settings): { number }
	local a, b = s.side1, s.side2
	return {
		a.radius.X, a.radius.Y, a.lengthOffset.X, a.lengthOffset.Y, a.lengthOffset.Z, a.roundness[1], a.roundness[2], a.roundness[3], a.roundness[4],
		b.radius.X, b.radius.Y, b.lengthOffset.X, b.lengthOffset.Y, b.lengthOffset.Z, b.roundness[1], b.roundness[2], b.roundness[3], b.roundness[4],
	}
end

function Fuselage.decode(t: { number }?): Settings
	if not t or #t < 18 then
		return Fuselage.default()
	end
	return {
		side1 = { radius = Vector2.new(t[1], t[2]), lengthOffset = Vector3.new(t[3], t[4], t[5]), roundness = { t[6], t[7], t[8], t[9] } },
		side2 = { radius = Vector2.new(t[10], t[11]), lengthOffset = Vector3.new(t[12], t[13], t[14]), roundness = { t[15], t[16], t[17], t[18] } },
	}
end

return Fuselage
