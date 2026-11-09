--!strict
-- Streams the world around the camera: low-poly terrain chunks (TerrainGenerationManager / Chunk), the sea,
-- airport models (Chunk.SpawnAirport) and scrap piles (ScrapSpawner).
-- Terrain triangles are drawn with wedges; near chunks are detailed and collidable, far chunks are coarse.
local Workspace = game:GetService("Workspace")
local RunService = game:GetService("RunService")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local WorldMap = require(Shared.WorldMap)
local Noise = require(Shared.Util.Noise)
local Triangles = require(script.Parent.Triangles)
local PartVisual = require(script.Parent.PartVisual)

local WorldStreamer = {}
WorldStreamer.__index = WorldStreamer

local CHUNK = WorldMap.ChunkSize
local NEAR_RES = 20 -- cells per chunk side near the plane (50 m cells, colliding)
local FAR_RES = 8 -- cells per chunk side further out
local NEAR_RADIUS = 1
local FAR_RADIUS = 3
local AIRPORT_LOAD = 3500
local AIRPORT_UNLOAD = 4500
local BUDGET = 0.006 -- seconds of generation work per frame

local function c3(t: { number }?, fallback: Color3): Color3
	if not t then
		return fallback
	end
	return Color3.new(t[1], t[2], t[3])
end

function WorldStreamer.new(campaign: any?)
	local self = setmetatable({}, WorldStreamer)
	self.campaign = campaign
	self.folder = Instance.new("Folder")
	self.folder.Name = "World"
	self.folder.Parent = Workspace
	self.chunks = {} :: { [string]: any }
	self.airports = {} :: { [number]: any }
	self.scrap = {} :: { [any]: Instance }
	self.queue = {} :: { any }
	self.focus = Vector3.zero

	local sea = Instance.new("Part")
	sea.Name = "Sea"
	sea.Anchored = true
	sea.CanCollide = false
	sea.CanQuery = false
	sea.CanTouch = false
	sea.Size = Vector3.new(2048, 1, 2048)
	sea.Material = Enum.Material.Glass
	sea.Color = Color3.new(0.33, 0.44, 0.55)
	sea.Transparency = 0.15
	sea.Parent = self.folder
	self.sea = sea
	local farSea = sea:Clone()
	farSea.Name = "FarSea"
	farSea.Size = Vector3.new(2048, 0.5, 2048)
	self.farSeas = {}
	for i = 1, 8 do
		local s = farSea:Clone()
		s.Parent = self.folder
		table.insert(self.farSeas, s)
	end

	self.connection = RunService.Heartbeat:Connect(function()
		self:update()
	end)
	return self
end

-- Colour of a terrain triangle (the original terrain shader blended grass/sand/dirt/rock by height and slope).
local function triangleColor(continentColors: any, ux: number, uz: number, height: number, slope: number): Color3
	local grass = c3(continentColors and continentColors.grass, Color3.new(0.567, 0.567, 0.39))
	local sand = c3(continentColors and continentColors.sand, Color3.new(0.78, 0.66, 0.5))
	local dirt = c3(continentColors and continentColors.dirt, Color3.new(0.6, 0.5, 0.36))
	local rock = c3(continentColors and continentColors.rock, Color3.new(0.7, 0.68, 0.66))
	local strength = (continentColors and continentColors.noise) or 0.05
	local jitter = (Noise.perlin(ux * 0.0025, uz * 0.0025) - 0.5) * strength
	local col = grass
	if slope > 0.12 - jitter then
		col = dirt
	end
	if slope > 0.3 - jitter then
		col = rock
	end
	if height < 2.5 + jitter * 40 then
		col = sand
	end
	local k = 1 + jitter * 2
	return Color3.new(math.clamp(col.R * k, 0, 1), math.clamp(col.G * k, 0, 1), math.clamp(col.B * k, 0, 1))
end

local function chunkKey(cx: number, cz: number): string
	return cx .. "," .. cz
end

-- Builds one chunk (yields between rows so it never blocks a frame for long).
function WorldStreamer:buildChunk(cx: number, cz: number, res: number, collide: boolean)
	local key = chunkKey(cx, cz)
	local model = Instance.new("Model")
	model.Name = "Chunk_" .. key .. "_" .. res
	local step = CHUNK / res
	local x0 = cx * CHUNK - CHUNK / 2
	local z0 = cz * CHUNK - CHUNK / 2
	local heights = {}
	local start = os.clock()
	local function maybeYield()
		if os.clock() - start > BUDGET then
			task.wait()
			start = os.clock()
		end
	end
	for i = 0, res do
		heights[i] = {}
		for j = 0, res do
			heights[i][j] = WorldMap.height(x0 + i * step, z0 + j * step)
		end
		maybeYield()
	end
	local colors = WorldMap.terrainColors(cx * CHUNK, cz * CHUNK)
	local anyAboveSea = false
	for i = 0, res - 1 do
		for j = 0, res - 1 do
			local h00, h10, h01, h11 = heights[i][j], heights[i + 1][j], heights[i][j + 1], heights[i + 1][j + 1]
			if math.max(h00, h10, h01, h11) > WorldMap.SeaLevel - 30 then
				anyAboveSea = anyAboveSea or math.max(h00, h10, h01, h11) > WorldMap.SeaLevel
				local ux, uz = x0 + i * step, z0 + j * step
				-- Unity -> Roblox: mirror X
				local p00 = Vector3.new(-ux, h00, uz)
				local p10 = Vector3.new(-(ux + step), h10, uz)
				local p01 = Vector3.new(-ux, h01, uz + step)
				local p11 = Vector3.new(-(ux + step), h11, uz + step)
				for _, tri in { { p00, p11, p10 }, { p00, p01, p11 } } do
					local a, b, c = tri[1], tri[2], tri[3]
					local n = (b - a):Cross(c - a)
					if n.Y < 0 then
						n = -n
					end
					local slope = 1 - math.clamp(n.Unit.Y, 0, 1)
					local h = (a.Y + b.Y + c.Y) / 3
					local col = triangleColor(colors, ux + step / 2, uz + step / 2, h, slope)
					Triangles.draw(a, b, c, { color = col, thickness = 2, outward = Vector3.yAxis, parent = model, canCollide = collide, canQuery = collide, material = Enum.Material.SmoothPlastic })
				end
			end
		end
		maybeYield()
	end
	model:SetAttribute("Res", res)
	return model
end

function WorldStreamer:wantedChunks(focus: Vector3)
	-- focus is in Roblox space; chunks are indexed in Unity space
	local ux, uz = -focus.X, focus.Z
	local cx, cz = math.floor(ux / CHUNK + 0.5), math.floor(uz / CHUNK + 0.5)
	local wanted = {}
	for i = -FAR_RADIUS, FAR_RADIUS do
		for j = -FAR_RADIUS, FAR_RADIUS do
			local near = math.abs(i) <= NEAR_RADIUS and math.abs(j) <= NEAR_RADIUS
			wanted[chunkKey(cx + i, cz + j)] = { cx = cx + i, cz = cz + j, res = if near then NEAR_RES else FAR_RES, collide = near, dist = math.max(math.abs(i), math.abs(j)) }
		end
	end
	return wanted
end

function WorldStreamer:setFocus(position: Vector3)
	self.focus = position
end

-- Synchronously make sure the ground under a position exists (used before spawning a plane).
function WorldStreamer:ensureNear(position: Vector3)
	self.focus = position
	local wanted = self:wantedChunks(position)
	for key, w in wanted do
		if w.collide then
			local existing = self.chunks[key]
			if not existing or existing.res ~= w.res then
				local model = self:buildChunk(w.cx, w.cz, w.res, w.collide)
				model.Parent = self.folder
				if existing and existing.model then
					existing.model:Destroy()
				end
				self.chunks[key] = { model = model, res = w.res }
			end
		end
	end
	self:updateAirports(true)
end

function WorldStreamer:update()
	local focus = self.focus
	self.sea.Position = Vector3.new(math.floor(focus.X / 100) * 100, WorldMap.SeaLevel - 0.5, math.floor(focus.Z / 100) * 100)
	local k = 1
	for i = -1, 1 do
		for j = -1, 1 do
			if i ~= 0 or j ~= 0 then
				local s = self.farSeas[k]
				s.Position = self.sea.Position + Vector3.new(i * 2048, -0.3, j * 2048)
				k += 1
			end
		end
	end

	if not self.working then
		local wanted = self:wantedChunks(focus)
		-- unload chunks we no longer need
		for key, c in self.chunks do
			if not wanted[key] then
				if c.model then
					c.model:Destroy()
				end
				self.chunks[key] = nil
			end
		end
		-- pick the closest missing / wrong-resolution chunk
		local best = nil
		for key, w in wanted do
			local c = self.chunks[key]
			if not c or c.res ~= w.res then
				if not best or w.dist < best.dist then
					best = w
					best.key = key
				end
			end
		end
		if best then
			self.working = true
			task.spawn(function()
				local ok, model = pcall(function()
					return self:buildChunk(best.cx, best.cz, best.res, best.collide)
				end)
				if ok and model then
					local old = self.chunks[best.key]
					model.Parent = self.folder
					if old and old.model then
						old.model:Destroy()
					end
					self.chunks[best.key] = { model = model, res = best.res }
				else
					warn("[Aviassembly] terrain chunk failed:", model)
				end
				self.working = false
			end)
		end
	end
	self:updateAirports(false)
	self:updateScrap()
end

function WorldStreamer:spawnAirport(airport: any)
	local model = Instance.new("Model")
	model.Name = "Airport_" .. airport.name
	local cf = WorldMap.airportCFrame(airport)
	local meshes = airport.data.meshes or {}
	local imported = PartVisual.hasImportedMeshes()
	if imported then
		for _, node in meshes do
			local piece = PartVisual.meshPiece(node, cf * CFrame.new(node.center[1], node.center[2], node.center[3]), model)
			piece.CanCollide = true
			piece.CanQuery = true
		end
	end
	-- Runway pad: always present so landing works even before the models are imported.
	local pad = Instance.new("Part")
	pad.Name = "Runway"
	pad.Anchored = true
	local fp = airport.flatness.size
	pad.Size = Vector3.new(fp.X * 2, 0.4, fp.Y * 2)
	pad.CFrame = cf * CFrame.new(0, -0.15, 0)
	pad.Color = Color3.fromRGB(90, 92, 96)
	pad.Material = Enum.Material.Asphalt
	pad.Transparency = if imported then 1 else 0
	pad.CanCollide = true
	pad.Parent = model
	if airport.data.offshore then
		-- ships: put the deck at sea level when there is no model
		pad.CFrame = cf * CFrame.new(0, WorldMap.SeaLevel + 8, 0)
	end
	model.Parent = self.folder
	return model
end

function WorldStreamer:updateAirports(force: boolean)
	local focus = self.focus
	for _, airport in WorldMap.Airports do
		local p = airport.position
		local dx, dz = -p.X - focus.X, p.Z - focus.Z
		local d = math.sqrt(dx * dx + dz * dz)
		local loaded = self.airports[airport.index]
		if d < AIRPORT_LOAD and not loaded then
			self.airports[airport.index] = self:spawnAirport(airport)
		elseif d > AIRPORT_UNLOAD and loaded then
			loaded:Destroy()
			self.airports[airport.index] = nil
		end
	end
end

function WorldStreamer:updateScrap()
	local campaign = self.campaign
	if not campaign then
		return
	end
	local alive = {}
	for _, s in campaign.scrapSpots do
		alive[s] = true
		if not self.scrap[s] then
			local h = WorldMap.height(s.x, s.z)
			local beacon = Instance.new("Part")
			beacon.Name = "Scrap"
			beacon.Anchored = true
			beacon.CanCollide = false
			beacon.CanQuery = false
			beacon.Material = Enum.Material.Neon
			beacon.Color = Color3.fromRGB(255, 170, 60)
			beacon.Shape = Enum.PartType.Cylinder
			beacon.Size = Vector3.new(400, 3, 3)
			beacon.Transparency = 0.4
			beacon.CFrame = CFrame.new(-s.x, math.max(h, WorldMap.SeaLevel) + 200, s.z) * CFrame.Angles(0, 0, math.pi / 2)
			local pile = Instance.new("Part")
			pile.Name = "ScrapPile"
			pile.Anchored = true
			pile.CanCollide = false
			pile.Size = Vector3.new(8, 4, 8)
			pile.Color = Color3.fromRGB(120, 110, 100)
			pile.Material = Enum.Material.CorrodedMetal
			pile.CFrame = CFrame.new(-s.x, math.max(h, WorldMap.SeaLevel) + 2, s.z)
			pile.Parent = beacon
			beacon.Parent = self.folder
			self.scrap[s] = beacon
		end
	end
	for s, inst in self.scrap do
		if not alive[s] then
			inst:Destroy()
			self.scrap[s] = nil
		end
	end
end

function WorldStreamer:destroy()
	if self.connection then
		self.connection:Disconnect()
	end
	self.folder:Destroy()
end

return WorldStreamer
