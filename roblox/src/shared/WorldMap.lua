--!strict
-- The campaign world: continents, airports and the terrain height function.
-- Ported from ContinentManager, ContinentData, AirportSpawner, AirportManager, TerrainGenerationManager,
-- FlatnessRectangle and ConvexHull2D. Internally everything runs in *Unity* coordinates (x not mirrored) so the
-- noise and layout match the original; use WorldMap.toRoblox / fromRoblox at the boundaries.
local Shared = script.Parent
local World = require(Shared.Data.World)
local Constants = require(Shared.Data.Constants)
local Noise = require(Shared.Util.Noise)
local MathUtil = require(Shared.Util.MathUtil)

local WorldMap = {}

export type Rect = {
	center: Vector2,
	size: Vector2,
	rotation: number,
	cosR: number,
	sinR: number,
	minX: number,
	maxX: number,
	minY: number,
	maxY: number,
}

export type Airport = {
	index: number,
	id: string,
	name: string,
	data: any,
	continent: any,
	position: Vector3, -- Unity space, y = 0
	isBase: boolean,
	rotation: number, -- yaw in degrees (Unity)
	flatness: Rect,
	cargoTypes: { string },
}

local TERRAIN = Constants.Terrain
local CHUNK = TERRAIN.chunkSize
local NOISE_SCALE = TERRAIN.noiseScale
local OCTAVES = TERRAIN.octaves
WorldMap.SeaLevel = -8
WorldMap.ChunkSize = CHUNK

function WorldMap.toRoblox(p: Vector3): Vector3
	return Vector3.new(-p.X, p.Y, p.Z)
end

function WorldMap.fromRoblox(p: Vector3): Vector3
	return Vector3.new(-p.X, p.Y, p.Z)
end

-- ---------------------------------------------------------------------------------------------- rectangles
local function newRect(size: Vector2, center: Vector2, rotation: number): Rect
	local r = math.rad(rotation)
	return {
		center = center,
		size = size,
		rotation = rotation,
		cosR = math.cos(r),
		sinR = math.sin(r),
		minX = center.X - size.X,
		maxX = center.X + size.X,
		minY = center.Y - size.Y,
		maxY = center.Y + size.Y,
	}
end

function WorldMap.distanceToRect(rect: Rect, x: number, z: number): number
	local dx = x - rect.center.X
	local dz = z - rect.center.Y
	local rx = dx * rect.cosR - dz * rect.sinR + rect.center.X
	local rz = dx * rect.sinR + dz * rect.cosR + rect.center.Y
	local cx = math.max(rect.minX, math.min(rx, rect.maxX))
	local cz = math.max(rect.minY, math.min(rz, rect.maxY))
	local ex, ez = rx - cx, rz - cz
	return math.sqrt(ex * ex + ez * ez)
end

-- ---------------------------------------------------------------------------------------------- convex hull
local function convexHull(points: { Vector2 }): { Vector2 }
	if #points < 3 then
		return table.clone(points)
	end
	local pts = table.clone(points)
	table.sort(pts, function(a, b)
		return a.X < b.X or (a.X == b.X and a.Y < b.Y)
	end)
	local function cross(o: Vector2, a: Vector2, b: Vector2)
		return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X)
	end
	local lower, upper = {}, {}
	for _, p in pts do
		while #lower >= 2 and cross(lower[#lower - 1], lower[#lower], p) <= 0 do
			table.remove(lower)
		end
		table.insert(lower, p)
	end
	for i = #pts, 1, -1 do
		local p = pts[i]
		while #upper >= 2 and cross(upper[#upper - 1], upper[#upper], p) <= 0 do
			table.remove(upper)
		end
		table.insert(upper, p)
	end
	table.remove(lower)
	table.remove(upper)
	for _, p in upper do
		table.insert(lower, p)
	end
	return lower
end

local function pointInHull(hull: { Vector2 }, x: number, z: number): boolean
	local n = #hull
	if n < 3 then
		return false
	end
	for i = 1, n do
		local a, b = hull[i], hull[i % n + 1]
		if (b.X - a.X) * (z - a.Y) - (b.Y - a.Y) * (x - a.X) < 0 then
			return false
		end
	end
	return true
end

local function distanceToSegment(px: number, pz: number, a: Vector2, b: Vector2): number
	local abx, abz = b.X - a.X, b.Y - a.Y
	local len2 = abx * abx + abz * abz
	local t = 0
	if len2 > 0 then
		t = math.clamp(((px - a.X) * abx + (pz - a.Y) * abz) / len2, 0, 1)
	end
	local cx, cz = a.X + abx * t - px, a.Y + abz * t - pz
	return math.sqrt(cx * cx + cz * cz)
end

local function hullDistance(continent: any, x: number, z: number): number
	local hull = continent.hull
	if #hull == 0 then
		local o = continent.origin
		local dx, dz = o.X - x, o.Z - z
		return math.sqrt(dx * dx + dz * dz)
	elseif #hull == 1 then
		local dx, dz = hull[1].X - x, hull[1].Y - z
		return math.sqrt(dx * dx + dz * dz)
	end
	if pointInHull(hull, x, z) then
		return 0
	end
	local best = math.huge
	local n = #hull
	for i = 1, n do
		local d = distanceToSegment(x, z, hull[i], hull[i % n + 1])
		if d < best then
			best = d
		end
	end
	return best
end

-- ---------------------------------------------------------------------------------------------- continents & airports
WorldMap.Continents = {} :: { any }
WorldMap.Airports = {} :: { Airport }
WorldMap.CargoTypes = {} :: { [string]: any }
WorldMap.CargoOrder = {} :: { string }
local flatObjects: { { rect: Rect, height: number, blending: number, position: Vector3 } } = {}

for _, cargo in World.cargoTypes do
	WorldMap.CargoTypes[cargo.id] = cargo
	table.insert(WorldMap.CargoOrder, cargo.id)
end

local function airportUnityPosition(data: any): Vector3
	-- World.lua stores positions mirrored for Roblox; undo that for the Unity-space simulation.
	return Vector3.new(-data.position[1], 0, data.position[3])
end

do
	-- ContinentManager.Init: continent 0 sits in front of the origin, the others at the mean of their airports.
	for index, c in World.continents do
		local continent = {
			index = index,
			type = c,
			name = c.name,
			radius = c.radius,
			minHeight = c.minHeight,
			maxHeight = c.maxHeight,
			airports = {},
			hull = {},
			origin = Vector3.zero,
		}
		if index == 1 then
			continent.origin = Vector3.new(0, 0, 1) * (c.radius - 750)
		else
			local sum, count = Vector3.zero, 0
			for _, a in c.airports do
				if not a.offshore then
					sum += airportUnityPosition(a)
					count += 1
				end
			end
			local mean = if count > 0 then sum / count else Vector3.zero
			local maxDist = 0
			for _, a in c.airports do
				if not a.offshore then
					maxDist = math.max(maxDist, (mean - airportUnityPosition(a)).Magnitude)
				end
			end
			continent.radius = math.max(1000, maxDist * 1.1)
			continent.origin = mean
		end
		table.insert(WorldMap.Continents, continent)
	end

	for _, continent in WorldMap.Continents do
		for _, a in continent.type.airports do
			local airport: Airport = {
				index = #WorldMap.Airports + 1,
				id = (a.id .. continent.type.id):gsub("%s+", ""),
				name = a.name,
				data = a,
				continent = continent,
				position = airportUnityPosition(a),
				isBase = false,
				rotation = 0,
				flatness = newRect(Vector2.new(100, 300), Vector2.zero, 0),
				cargoTypes = a.cargoTypes or {},
			}
			table.insert(continent.airports, airport)
			table.insert(WorldMap.Airports, airport)
		end
		-- UpdateBaseAirport: the airport closest to the world origin is the continent's base.
		local base, best = nil, math.huge
		for _, airport in continent.airports do
			local m = airport.position.Magnitude
			if m < best then
				best = m
				base = airport
			end
		end
		if base then
			base.isBase = true
			base.cargoTypes = {}
			continent.baseAirport = base
		end
		-- UpdateConvexHull (offshore airports excluded)
		local pts = {}
		for _, airport in continent.airports do
			if not airport.data.offshore then
				table.insert(pts, Vector2.new(airport.position.X, airport.position.Z))
			end
		end
		continent.hull = convexHull(pts)
	end
end

function WorldMap.closestContinent(x: number, z: number): any
	local best, bestD = WorldMap.Continents[1], math.huge
	for _, c in WorldMap.Continents do
		local d = hullDistance(c, x, z)
		if d < bestD then
			bestD = d
			best = c
		end
	end
	return best, bestD
end

local function continentFalloff(d: number): number
	return 1 - MathUtil.clamp01(d * 0.0003)
end

-- TerrainGenerationManager.GetTerrainHeightFast (without flattening) for a Unity-space point.
function WorldMap.baseHeight(x: number, z: number): number
	local noise = Noise.getNoise(x / CHUNK * NOISE_SCALE, z / CHUNK * NOISE_SCALE, OCTAVES)
	local continent, d = WorldMap.closestContinent(x, z)
	-- continentFalloff curve is linear 0..1 (keys (0,0) and (1,1) with slope 1).
	noise -= 1 - continentFalloff(d)
	return MathUtil.lerp(continent.minHeight, continent.maxHeight, MathUtil.clamp01(noise))
end

local function flatAdjusted(x: number, z: number, base: number): number
	local blend = 1
	local target = base
	for _, f in flatObjects do
		local t = MathUtil.clamp01(WorldMap.distanceToRect(f.rect, x, z) / f.blending)
		if t < 0.99 then
			blend = t
			target = f.height
		end
	end
	local t = MathUtil.clamp01(blend - 0.25) * 1.33333
	return MathUtil.lerp(target, base, t)
end

function WorldMap.height(x: number, z: number): number
	return flatAdjusted(x, z, WorldMap.baseHeight(x, z))
end

-- Roblox-space convenience.
function WorldMap.heightAt(robloxPosition: Vector3): number
	return WorldMap.height(-robloxPosition.X, robloxPosition.Z)
end

-- GetAvarageTerrainHeight, including the original's quirk of sampling around z = 0 (it used position.y).
local function averageHeight(position: Vector3): number
	local samples = 5
	local spacing = 1000 / samples
	local cx = math.floor(position.X / 1000) * 1000
	local cz = math.floor(position.Y / 1000) * 1000
	local total = 0
	for i = 0, samples - 1 do
		for j = 0, samples - 1 do
			total += WorldMap.baseHeight(cx - 500 + i * spacing, cz - 500 + j * spacing)
		end
	end
	return math.max(math.floor(total / (samples * samples) + 0.5), 0)
end

-- AirportManager.InitFlatObjects
for _, airport in WorldMap.Airports do
	local data = airport.data
	local rotation = (airport.position.X + airport.position.Z - 110) % 360
	if data.rotationOverride then
		rotation = data.rotation
	end
	if airport.isBase then
		rotation = 0
	end
	airport.rotation = rotation
	local fp = Vector2.new(data.footprint[1], data.footprint[2])
	local size = if fp.Magnitude < 10 then Vector2.new(100, 300) else fp
	local blending = if data.blendingDistance < 10 then 200 else data.blendingDistance
	if airport.isBase then
		blending = 1000
	end
	if airport.position.Magnitude < 200 then
		blending = 200
	end
	airport.flatness = newRect(size, Vector2.new(airport.position.X, airport.position.Z), rotation)
	if not data.offshore then
		table.insert(flatObjects, {
			rect = airport.flatness,
			height = averageHeight(airport.position),
			blending = blending,
			position = airport.position,
		})
	end
end

-- AirportManager.GetAirportRotation (yaw in Unity degrees).
function WorldMap.airportYaw(airport: Airport): number
	local yaw = 0
	local _closest = WorldMap.closestAirport(airport.position.X, airport.position.Z)
	if not (_closest and _closest.isBase) then
		yaw = (airport.position.X + airport.position.Z - 110) % 360
	end
	if airport.data.rotationOverride then
		yaw = airport.data.rotation
	end
	return yaw
end

function WorldMap.closestAirport(x: number, z: number, cargoId: string?): Airport?
	local best, bestD = nil, math.huge
	for _, airport in WorldMap.Airports do
		if cargoId == nil or table.find(airport.cargoTypes, cargoId) then
			local dx, dz = airport.position.X - x, airport.position.Z - z
			local d = dx * dx + dz * dz
			if d < bestD then
				bestD = d
				best = airport
			end
		end
	end
	return best
end

-- Where the airport model sits (Chunk.SpawnAirport): terrain height under the centre, never below -8.
function WorldMap.airportHeight(airport: Airport): number
	return math.max(WorldMap.height(airport.position.X, airport.position.Z), -8)
end

-- Roblox CFrame of the airport model root.
function WorldMap.airportCFrame(airport: Airport): CFrame
	local p = airport.position
	return CFrame.new(-p.X, WorldMap.airportHeight(airport), p.Z) * CFrame.Angles(0, -math.rad(WorldMap.airportYaw(airport)), 0)
end

-- Airport.GetPrice: cargo gets more valuable the further it has to travel.
function WorldMap.cargoPrice(airport: Airport, cargoId: string): number
	local cargo = WorldMap.CargoTypes[cargoId]
	local source = WorldMap.closestAirport(airport.position.X, airport.position.Z, cargoId)
	local distance = 0
	if source then
		distance = (source.position - airport.position).Magnitude
	end
	local steps = math.floor(distance / 500) * 500 / 3000
	local factor = if cargo.expires then 0.5 else 0.25
	return cargo.basePrice * (1 + steps * factor)
end

function WorldMap.isOcean(x: number, z: number): boolean
	local c = WorldMap.closestContinent(x, z)
	local dx, dz = x - c.origin.X, z - c.origin.Z
	return math.sqrt(dx * dx + dz * dz) > c.radius * 0.9
end

function WorldMap.airportById(id: string): Airport?
	for _, airport in WorldMap.Airports do
		if airport.id == id then
			return airport
		end
	end
	return nil
end

function WorldMap.findAirport(nameFragment: string): Airport?
	for _, airport in WorldMap.Airports do
		if string.find(airport.name, nameFragment, 1, true) then
			return airport
		end
	end
	return nil
end

-- Terrain colours, from each continent's terrain material.
function WorldMap.terrainColors(x: number, z: number): any
	local c = WorldMap.closestContinent(x, z)
	return c.type.terrain
end

return WorldMap
