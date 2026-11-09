--!strict
-- Campaign state and rules: money, scrap/research, cargo, airports, quests and the story.
-- Ported from MoneyManager, ResearchManager, CargoInventory, Airport, Quest/DeliveryQuest/ContainerQuest,
-- QuestData, QuestGenerator, StoryState and StoryManager. UI-free: it reports what happened through
-- `self.onMessage(text, kind)` and returns flags the client acts on (camera shake, plane explosion).
local Shared = script.Parent
local WorldMap = require(Shared.WorldMap)
local Parts = require(Shared.Data.Parts)
local Research = require(Shared.Data.Research)
local Constants = require(Shared.Data.Constants)
local MathUtil = require(Shared.Util.MathUtil)

local Campaign = {}
Campaign.__index = Campaign

-- StoryStateID
local POST_EARTHQUAKE, PRE_EARTHQUAKE, SNOW_UNLOCKED, DESERT_UNLOCKED = 0, 1, 2, 3
Campaign.Difficulty = { Relaxed = "Relaxed", Normal = "Normal", Hard = "Hard" }

local STORY = Constants.StoryManager
local CARGO = Constants.CargoInventory

-- ------------------------------------------------------------------------------------------- quests
export type Quest = any

local function cargoName(id: string): string
	local c = WorldMap.CargoTypes[id]
	return if c then c.name else id
end

local function replaceAll(s: string, key: string, value: string): string
	local out = s:gsub(key:gsub("%p", "%%%0"), (value:gsub("%%", "%%%%")))
	return out
end

local function difficultyFactor(difficulty: string): number
	if difficulty == "Relaxed" then
		return 1.15
	elseif difficulty == "Hard" then
		return 0.66
	end
	return 1
end

local function initTexts(q: Quest, data: any)
	q.name = data.name or ""
	q.description = data.description or ""
	q.giveMessage = data.giveMessage or ""
	q.completeMessage = data.completeMessage or ""
	q.nameOriginal, q.descriptionOriginal, q.giveOriginal, q.completeOriginal = q.name, q.description, q.giveMessage, q.completeMessage
	q.scrap = data.scrap or 0
	q.advancedScrap = data.advancedScrap or 0
	q.storyState = data.storyState or 0
	for _, field in { "giveMessage", "description", "completeMessage" } do
		q[field] = replaceAll(q[field], "[scrapReward]", tostring(q.scrap))
		q[field] = replaceAll(q[field], "[advancedScrapReward]", tostring(q.advancedScrap))
	end
end

local function isEmpty(s: string?): boolean
	return s == nil or s:match("^%s*$") ~= nil
end

local function updateDeliveryText(q: Quest, completed: boolean)
	local name = cargoName(q.cargo)
	q.name = replaceAll(replaceAll(q.nameOriginal, "[amount]", " " .. q.amount .. " "), "[cargoType]", name)
	q.description = replaceAll(replaceAll(q.descriptionOriginal, "[amount]", tostring(q.amount)), "[cargoType]", name)
	if not isEmpty(q.completeOriginal) then
		local m = replaceAll(q.completeOriginal, "[reward]", tostring(math.floor(q.reward + 0.5)))
		m = replaceAll(m, "[scrapReward]", tostring(q.scrap))
		q.completeMessage = replaceAll(m, "[advancedScrapReward]", tostring(q.advancedScrap))
	else
		q.completeMessage = "Thank you for the " .. name .. ". Here are <b> " .. math.floor(q.reward + 0.5) .. " </b>coins for the effort"
	end
	if completed then
		q.name = replaceAll(replaceAll(q.nameOriginal, "[amount]", " "), "[cargoType]", name)
	end
end

function Campaign:createQuest(data: any, airport: any, path: string): Quest
	local q: Quest = { id = path, kind = if data.type == "ContainerQuestData" then "container" else "delivery", airportId = airport.id, completed = false, reward = 0 }
	initTexts(q, data)
	if q.kind == "delivery" then
		q.cargo = data.cargo
		q.amount = data.amount or 1
		local price = WorldMap.cargoPrice(airport, data.cargo)
		q.reward = MathUtil.snapToIncrement((price * q.amount + (data.rewardOffset or 0)) * difficultyFactor(self.difficulty), 5)
		local name = cargoName(data.cargo)
		if isEmpty(data.name) then
			q.name = "Delivering " .. name
		end
		if isEmpty(data.giveMessage) then
			q.giveMessage = "Welcome to " .. airport.name .. ". We need <b>" .. q.amount .. " " .. name .. "</b>."
		end
		if isEmpty(data.description) then
			q.description = "Deliver " .. q.amount .. " " .. name .. " to this airport"
		end
		if isEmpty(data.completeMessage) then
			q.completeMessage = "Thank you for the " .. name .. ". Here are <b> " .. q.reward .. " </b>coins for the effort"
		end
		q.rewardPerCargo = q.reward / q.amount
		for _, field in { "name", "giveMessage", "completeMessage", "description" } do
			q[field] = replaceAll(q[field], "[amount]", if field == "name" then " " .. q.amount .. " " else tostring(q.amount))
			q[field] = replaceAll(q[field], "[cargoType]", name)
		end
		q.nameOriginal = if isEmpty(data.name) then "Delivering [cargoType]" else data.name
		q.descriptionOriginal = if isEmpty(data.description) then "Deliver [amount] [cargoType] to this airport" else data.description
	else
		q.children = {}
		for i, sub in data.subQuests or {} do
			table.insert(q.children, self:createQuest(sub, airport, path .. "/s" .. i))
		end
	end
	local list = self.airports[airport.id].allQuests
	table.insert(list, q)
	if data.followup then
		q.followup = self:createQuest(data.followup, airport, path .. "/f")
	end
	return q
end

-- ------------------------------------------------------------------------------------------- construction
function Campaign.new(difficulty: string?, creative: boolean?)
	local self = setmetatable({}, Campaign)
	self.difficulty = difficulty or "Normal"
	self.creative = creative == true
	self.money = Constants.MoneyManager.money + (if self.creative then 2000000 else 0)
	self.scrap = 0
	self.advancedScrap = 0
	self.unlocked = {} :: { [string]: boolean }
	self.cargo = {} :: { [string]: number }
	self.cargoVolume = 0
	self.expirationTimer = 0
	self.expirationDuration = 0
	self.lastBreakTime = -math.huge
	self.discovered = {} :: { [string]: boolean }
	self.collectedScrap = {} :: { [string]: number }
	self.scrapSpots = {} :: { any }
	self.lastAirportId = nil :: string?
	self.story = {
		postEarthquake = false,
		earthquakeStarted = false,
		desertUnlocked = false,
		snowUnlocked = false,
		mysteriousMessageIndex = 0,
		mysteriousMessageIndex2 = 0,
		mysteriousMessageIndex3 = 0,
		mysteriousMessageTimer = 70,
		playedEndCutscene = false,
		finalMessagePlayed = false,
	}
	self.runtime = { earthquake = 0, earthquakeTimer = 0, earthquakeDelay = 0, postEarthquakeMessagePlayed = false, finalMessageTimer = 15, messagePlayed = false, finalWarningPlayed = false }
	self.onMessage = function(_text: string, _kind: string) end
	self.onReward = function(_info: any) end

	-- ResearchManager.Awake: parts not in the research tree, and tree items costing nothing, start unlocked.
	local inTree = {}
	for _, item in Research do
		if item.part then
			inTree[item.part] = item
		end
	end
	for name in Parts do
		if not inTree[name] then
			self.unlocked[name] = true
		end
	end
	for _, item in Research do
		if item.part and item.cost1 == 0 and item.cost2 == 0 then
			self.unlocked[item.part] = true
		end
	end
	if self.creative then
		for name in Parts do
			self.unlocked[name] = true
		end
	end

	-- Airports & their scripted quest chains (Airport.SetAirportData).
	self.airports = {}
	for _, a in WorldMap.Airports do
		self.airports[a.id] = { id = a.id, allQuests = {}, currentQuest = nil, questInitialized = false, questBlocked = false, cargoLocked = a.data.lockedCargo, landingMessage = a.data.landingMessage, hospitalMessagePlayed = false }
	end
	for _, a in WorldMap.Airports do
		local state = self.airports[a.id]
		if #a.data.quests > 0 then
			state.currentQuest = self:createQuest(a.data.quests[1], a, a.id .. "/q1")
		end
	end

	self.hospital = WorldMap.closestAirport(0, 3000)
	self.weatherStation = WorldMap.findAirport("Weather")
	self.researchFacility = WorldMap.findAirport("Research Facility")
	self.storage = WorldMap.findAirport("Storage")
	self.explosionSite = WorldMap.findAirport("Site")
	self:spawnScrap()
	return self
end

-- ------------------------------------------------------------------------------------------- money & research
function Campaign:changeMoney(delta: number)
	if self.creative then
		return
	end
	self.money += delta
end

function Campaign:hasMoney(amount: number): boolean
	return math.floor(self.money + 0.5) >= amount
end

function Campaign.researchCost(partName: string): (number, number)
	for _, item in Research do
		if item.part == partName then
			return item.cost1, item.cost2
		end
	end
	return 0, 0
end

function Campaign:isUnlocked(partName: string): boolean
	return self.unlocked[partName] == true
end

function Campaign:unlock(partName: string): boolean
	local c1, c2 = Campaign.researchCost(partName)
	if self.scrap < c1 or self.advancedScrap < c2 or self.unlocked[partName] then
		return false
	end
	self.scrap -= c1
	self.advancedScrap -= c2
	self.unlocked[partName] = true
	return true
end

-- ------------------------------------------------------------------------------------------- cargo (CargoInventory)
function Campaign:cargoCount(id: string): number
	return self.cargo[id] or 0
end

function Campaign:cargoMass(): number
	local m = 0
	for id, n in self.cargo do
		m += WorldMap.CargoTypes[id].weight * n
	end
	return m
end

function Campaign:enoughSpace(id: string, maxVolume: number): boolean
	return self.cargoVolume + WorldMap.CargoTypes[id].cargoSpace <= math.floor(maxVolume + 0.5)
end

function Campaign:airportCargoUnlocked(airportId: string): boolean
	local a = self.airports[airportId]
	return a ~= nil and not a.cargoLocked
end

function Campaign:addCargo(id: string, maxVolume: number, airportId: string): boolean
	if not self:airportCargoUnlocked(airportId) or not self:enoughSpace(id, maxVolume) then
		return false
	end
	local cargo = WorldMap.CargoTypes[id]
	self.cargo[id] = (self.cargo[id] or 0) + 1
	self.cargoVolume += cargo.cargoSpace
	if cargo.expires and self.difficulty ~= "Relaxed" then
		self.expirationTimer = cargo.expirationTime
		self.expirationDuration = cargo.expirationTime
	end
	return true
end

function Campaign:removeCargo(id: string, amount: number)
	local have = self.cargo[id] or 0
	if have >= amount then
		self.cargo[id] = have - amount
		if self.cargo[id] == 0 then
			self.cargo[id] = nil
		end
		self.cargoVolume -= WorldMap.CargoTypes[id].cargoSpace * amount
	end
end

-- Drop cargo that no longer fits (CargoInventory.RecalculateCargo), e.g. after a fuselage broke off.
function Campaign:fitCargo(maxVolume: number)
	local over = self.cargoVolume - maxVolume
	if over <= 0 then
		return
	end
	for id, n in self.cargo do
		if over <= 0 then
			break
		end
		local space = WorldMap.CargoTypes[id].cargoSpace
		local drop = math.min(n, math.ceil(over / space))
		self:removeCargo(id, drop)
		over -= drop * space
	end
end

-- Per-frame cargo update: expiring cargo and fragile cargo breaking on hard bumps.
-- velocityChange: |v - v_prev| over the last physics step. Returns a message or nil.
function Campaign:updateCargo(dt: number, velocityChange: number, now: number): string?
	if self.difficulty == "Relaxed" then
		return nil
	end
	local message = nil
	local hasExpiring = false
	for id, n in self.cargo do
		if n > 0 and WorldMap.CargoTypes[id].expires then
			hasExpiring = true
		end
	end
	self.expirationTimer = math.max(self.expirationTimer - dt, 0)
	if hasExpiring and self.expirationTimer <= 0 then
		for id, n in table.clone(self.cargo) do
			if WorldMap.CargoTypes[id].expires and n > 0 then
				self:removeCargo(id, n)
				message = "Your " .. cargoName(id):lower() .. " went bad. Deliver it faster next time!"
			end
		end
	end
	local breakPct = MathUtil.inverseLerp(CARGO.minFragileBreakTreshhold, CARGO.maxFragileBreakTreshhold, velocityChange)
	if now - self.lastBreakTime < CARGO.breakageCooldown then
		breakPct = 0
	end
	local fragile = 0
	for id, n in self.cargo do
		if WorldMap.CargoTypes[id].fragile then
			fragile += n
		end
	end
	local toBreak = math.floor(breakPct * fragile + 0.5)
	if toBreak > 0 then
		self.lastBreakTime = now
		local left = toBreak
		for id, n in table.clone(self.cargo) do
			if WorldMap.CargoTypes[id].fragile and n > 0 and left > 0 then
				local k = math.min(n, left)
				self:removeCargo(id, k)
				left -= k
				message = toBreak .. " " .. cargoName(id):lower() .. " broke during that rough landing!"
			end
		end
	end
	return message
end

-- ------------------------------------------------------------------------------------------- story state
function Campaign:storyState(): number
	local s = self.story
	local state = PRE_EARTHQUAKE
	if s.postEarthquake then
		state = POST_EARTHQUAKE
	end
	if s.snowUnlocked then
		state = SNOW_UNLOCKED
	end
	if s.desertUnlocked then
		state = DESERT_UNLOCKED
	end
	return state
end

function Campaign:questAvailable(q: Quest): boolean
	local current = self:storyState()
	if q.storyState == POST_EARTHQUAKE and current == PRE_EARTHQUAKE then
		return false
	end
	if q.storyState == PRE_EARTHQUAKE and current == POST_EARTHQUAKE then
		return true
	end
	return q.storyState <= current
end

function Campaign:allQuestsCompleted(airport: any?): boolean
	if not airport then
		return false
	end
	for _, q in self.airports[airport.id].allQuests do
		if not q.completed and not q.random then
			return false
		end
	end
	return true
end

function Campaign:completedCount(airport: any?): number
	if not airport then
		return 0
	end
	local n = 0
	for _, q in self.airports[airport.id].allQuests do
		if q.completed then
			n += 1
		end
	end
	return n
end

-- ------------------------------------------------------------------------------------------- quest checks
export type LandingContext = { position: Vector3, speed: number, airport: any, maxCargoVolume: number }

-- Unity-space distance from the plane to an airport's flat rectangle.
local function distanceToAirport(airport: any, robloxPosition: Vector3): number
	return WorldMap.distanceToRect(airport.flatness, -robloxPosition.X, robloxPosition.Z)
end

function Campaign:checkQuest(q: Quest, ctx: LandingContext): boolean
	if q.completed then
		return true
	end
	local done = false
	if q.kind == "container" then
		done = true
		for _, child in q.children do
			if not self:checkQuest(child, ctx) then
				done = false
			end
		end
	else
		local airport = WorldMap.airportById(q.airportId)
		local near = airport ~= nil and distanceToAirport(airport, ctx.position) < 100
		local slow = ctx.speed < 25
		local count = self:cargoCount(q.cargo)
		done = near and slow and count >= q.amount
		if done then
			self:removeCargo(q.cargo, q.amount)
			updateDeliveryText(q, true)
		elseif count > 0 and near and slow then
			-- Partial delivery: paid at 70%.
			self:removeCargo(q.cargo, count)
			local pay = math.floor(count * q.rewardPerCargo * 0.7 + 0.5)
			local before = self:rewardSnapshot()
			self:changeMoney(pay)
			q.reward -= pay
			q.amount -= count
			updateDeliveryText(q, false)
			if pay > 0 then
				local left = if q.amount > 1 then tostring(q.amount) else "one"
				self.onMessage("Thank you for the delivery. Here are <b>" .. pay .. "</b> coins for the effort. We only need <b>" .. left:lower() .. "</b> more " .. cargoName(q.cargo):lower() .. ".", "QuestCompleted")
				self.onReward(self:rewardDiff(before, false))
			end
		end
	end
	if done then
		q.completed = true
		self.onMessage(q.completeMessage, "QuestCompleted")
		local before = self:rewardSnapshot()
		self:changeMoney(q.reward)
		self.scrap += q.scrap
		self.advancedScrap += q.advancedScrap
		self.onReward(self:rewardDiff(before, true))
		local state = self.airports[q.airportId]
		if q.followup and state then
			state.currentQuest = q.followup
			state.questInitialized = false
		end
	end
	return done
end

function Campaign:rewardSnapshot()
	return { money = self.money, scrap = self.scrap, advancedScrap = self.advancedScrap }
end

function Campaign:rewardDiff(before: any, completed: boolean)
	return { completed = completed, money = self.money - before.money, scrap = self.scrap - before.scrap, advancedScrap = self.advancedScrap - before.advancedScrap }
end

-- QuestGenerator: once the campaign is finished, airports hand out random delivery jobs.
function Campaign:generateQuest(airport: any): Quest?
	local options = {}
	for _, id in WorldMap.CargoOrder do
		local source = WorldMap.closestAirport(airport.position.X, airport.position.Z, id)
		if source and source ~= airport then
			table.insert(options, id)
		end
	end
	if #options == 0 then
		return nil
	end
	local id = options[math.random(1, #options)]
	local cargo = WorldMap.CargoTypes[id]
	local amount = math.clamp(math.floor(20 / math.max(cargo.cargoSpace, 1)), 1, 6)
	amount = math.random(1, amount)
	local data = { type = "DeliveryQuestData", cargo = id, amount = amount, rewardOffset = 0, storyState = DESERT_UNLOCKED, name = "", description = "", giveMessage = "", completeMessage = "", scrap = 1, advancedScrap = 0 }
	local q = self:createQuest(data, airport, airport.id .. "/r" .. tostring(os.time()) .. math.random(1, 1e6))
	q.random = true
	return q
end

-- Airport.PlaneLandedAtAirport
function Campaign:landed(airport: any, ctx: LandingContext)
	local state = self.airports[airport.id]
	self.lastAirportId = airport.id
	if state.currentQuest and state.questInitialized then
		self:checkQuest(state.currentQuest, ctx)
	end
	if (state.currentQuest == nil or state.currentQuest.completed) and not airport.data.baseAirport and self.story.playedEndCutscene then
		state.currentQuest = self:generateQuest(airport)
		state.questInitialized = false
	end
	local q = state.currentQuest
	if not state.questInitialized and q and self:questAvailable(q) and not state.questBlocked then
		if not isEmpty(q.giveMessage) then
			self.onMessage(q.giveMessage, "QuestAdded")
		end
		state.questInitialized = true
		for _, loc in self:relatedAirports(q) do
			self:reveal(loc.position.X, loc.position.Z, 1)
		end
	end
	if not state.questInitialized and q and q.storyState == POST_EARTHQUAKE and not self:questAvailable(q) and not state.hospitalMessagePlayed then
		self.onMessage("We’ve heard that the hospital needs your help.", "Misc")
		state.hospitalMessagePlayed = true
	end
	if not isEmpty(state.landingMessage) then
		self.onMessage(state.landingMessage, "Misc")
		state.landingMessage = nil
	end
end

function Campaign:relatedAirports(q: Quest): { any }
	local out = {}
	if q.kind == "delivery" then
		local src = WorldMap.closestAirport(0, 0, q.cargo)
		if src then
			-- GetCargoAirport: the airport that sells this cargo.
			for _, a in WorldMap.Airports do
				if table.find(a.cargoTypes, q.cargo) then
					src = a
					break
				end
			end
			table.insert(out, src)
		end
	else
		for _, child in q.children do
			for _, a in self:relatedAirports(child) do
				table.insert(out, a)
			end
		end
	end
	return out
end

function Campaign:cargoSource(id: string): any?
	for _, a in WorldMap.Airports do
		if table.find(a.cargoTypes, id) then
			return a
		end
	end
	return nil
end

-- Current objective text + marker airport for the HUD (Quest.GetCurrentStageDescription / marker).
function Campaign:questStage(q: Quest, maxVolume: number): (string?, any?)
	if q == nil or q.completed then
		return nil, nil
	end
	if q.kind == "container" then
		for _, child in q.children do
			if not child.completed then
				return self:questStage(child, maxVolume)
			end
		end
		return nil, nil
	end
	local count = self:cargoCount(q.cargo)
	local name = cargoName(q.cargo):lower()
	local giver = WorldMap.airportById(q.airportId)
	if count >= q.amount or (count > 0 and not self:enoughSpace(q.cargo, maxVolume)) then
		return "Deliver the " .. name .. " at " .. (giver and giver.name:lower() or "?"), giver
	end
	local src = self:cargoSource(q.cargo)
	return "Collect <b>" .. name .. "</b> at " .. (src and src.name:lower() or "?"), src
end

function Campaign:activeQuests(): { { airport: any, quest: Quest } }
	local out = {}
	for _, a in WorldMap.Airports do
		local state = self.airports[a.id]
		if state.currentQuest and state.questInitialized and not state.currentQuest.completed then
			table.insert(out, { airport = a, quest = state.currentQuest })
		end
	end
	return out
end

-- ------------------------------------------------------------------------------------------- fog of war & scrap
function Campaign:reveal(x: number, z: number, radiusCells: number?)
	local r = radiusCells or 1
	local cx, cz = math.floor(x / 1000 + 0.5), math.floor(z / 1000 + 0.5)
	for i = -r, r do
		for j = -r, r do
			self.discovered[(cx + i) .. "," .. (cz + j)] = true
		end
	end
end

function Campaign:isDiscovered(x: number, z: number): boolean
	return self.discovered[math.floor(x / 1000 + 0.5) .. "," .. math.floor(z / 1000 + 0.5)] == true
end

-- ScrapSpawner: a few scrap piles per continent, away from airports, worth 5 scrap each.
function Campaign:spawnScrap()
	self.scrapSpots = {}
	local rng = Random.new(os.time())
	for _, c in WorldMap.Continents do
		if c.baseAirport and #c.hull >= 3 then
			local left = math.max(0, (c.type.scrapAmount or 0) - (self.collectedScrap[c.name] or 0))
			for _ = 1, left do
				for _try = 1, 200 do
					local a = c.hull[rng:NextInteger(1, #c.hull)]
					local b = c.hull[rng:NextInteger(1, #c.hull)]
					local t = rng:NextNumber()
					local p = a:Lerp(b, t):Lerp(Vector2.new(c.origin.X, c.origin.Z), rng:NextNumber() * 0.8)
					local nearest = WorldMap.closestAirport(p.X, p.Y)
					if nearest and (Vector2.new(nearest.position.X, nearest.position.Z) - p).Magnitude > 1000 then
						table.insert(self.scrapSpots, { x = p.X, z = p.Y, continent = c.name })
						break
					end
				end
			end
		end
	end
end

-- Returns the collected spot (if any) so the client can remove its marker.
function Campaign:collectScrapNear(x: number, z: number): any?
	for i, s in self.scrapSpots do
		local dx, dz = s.x - x, s.z - z
		if dx * dx + dz * dz < 175 * 175 then
			table.remove(self.scrapSpots, i)
			self.scrap += 5
			self.collectedScrap[s.continent] = (self.collectedScrap[s.continent] or 0) + 1
			return s
		end
	end
	return nil
end

-- ------------------------------------------------------------------------------------------- story (StoryManager.Update)
export type StoryContext = {
	dt: number,
	position: Vector3, -- Roblox space
	heightAboveGround: number,
	atAirport: boolean,
	exploded: boolean,
	messageOpen: boolean,
}

-- Returns { shake = 0..1, explode = bool, finished = bool }
function Campaign:updateStory(ctx: StoryContext)
	local s, rt = self.story, self.runtime
	local result = { shake = 0, explode = false, finished = false }
	if self.creative then
		return result
	end
	local dt = ctx.dt
	local ux, uz = -ctx.position.X, ctx.position.Z

	-- Earthquake after the first hospital quest.
	if self:completedCount(self.hospital) > 0 and not ctx.messageOpen and not s.postEarthquake then
		rt.earthquakeDelay += dt
		if rt.earthquakeDelay > 2 then
			if rt.earthquakeTimer <= 8 then
				rt.earthquake += dt * 0.1
			end
			rt.earthquakeTimer += dt
			s.earthquakeStarted = true
		end
	end
	if rt.earthquakeTimer > 8 then
		rt.earthquake -= dt * 0.2
		if not s.postEarthquake and not rt.postEarthquakeMessagePlayed and rt.earthquake < 0.2 then
			self.onMessage("What was that?!... That seemed like an earthquake!!", "Misc")
			self.onMessage("Can you check around the island if people need help?", "Misc")
			self.onMessage("You should also visit the weather station to find out what is going on.", "Misc")
			rt.postEarthquakeMessagePlayed = true
		end
	end
	if rt.earthquakeTimer > 12 and rt.earthquake < 0.01 and not ctx.messageOpen then
		s.postEarthquake = true
	end
	rt.earthquake = MathUtil.clamp01(rt.earthquake)
	result.shake = rt.earthquake

	-- Restricted zones (desert until the research facility is done; snow gated behind the weather station).
	local weather, research = self.weatherStation, self.researchFacility
	if weather and research then
		local wState = self.airports[weather.id]
		local rState = self.airports[research.id]
		wState.cargoLocked = not self:allQuestsCompleted(weather)
		local weatherDone = self:allQuestsCompleted(weather)
		local desertOpen = self:allQuestsCompleted(research) or s.desertUnlocked
		if weatherDone then
			self:reveal(research.position.X, research.position.Z, 1)
			s.snowUnlocked = true
		end
		if s.postEarthquake then
			self:reveal(weather.position.X, weather.position.Z, 1)
		end
		if desertOpen then
			s.desertUnlocked = true
		end
		rState.questBlocked = not weatherDone
		local dx, dz = research.position.X - ux, research.position.Z - uz
		if math.sqrt(dx * dx + dz * dz) > 3000 then
			rState.landingMessage = if not weatherDone then "Our friends at the weather station need your help" else nil
		end
		local desert = WorldMap.Continents[2]
		local ddx, ddz = desert.origin.X - ux, desert.origin.Z - uz
		local flat = math.sqrt(ddx * ddx + ddz * ddz)
		local warn = not desertOpen and flat < desert.radius + 2000
		local final = not desertOpen and flat < desert.radius + 1000
		local inside = not desertOpen and flat < desert.radius
		if warn and not rt.messagePlayed then
			if weatherDone then
				self.onMessage("This is a restricted zone. Complete research facility missions first!", "Misc")
			else
				self.onMessage("This is a restricted zone. Complete weather station missions first!", "Misc")
			end
			rt.messagePlayed = true
		end
		if final and not rt.finalWarningPlayed then
			self.onMessage("This is your final warning! Turn back now!!", "Misc")
			rt.finalWarningPlayed = true
		end
		if inside and not ctx.exploded then
			result.explode = true
		end
		if not warn then
			rt.messagePlayed = false
			rt.finalWarningPlayed = false
		end
	end

	-- End of the campaign: all waste storage jobs done.
	if self.storage and self:allQuestsCompleted(self.storage) and not s.playedEndCutscene and not ctx.messageOpen then
		s.playedEndCutscene = true
		self.onMessage("You did it! The radioactive waste is safely stored and the islands are saved. Thank you, pilot!", "Mystery")
		self.onMessage("Airports will keep offering delivery jobs. Keep flying!", "Misc")
		result.finished = true
	end

	-- Mysterious radio messages.
	if s.postEarthquake and not self:allQuestsCompleted(research) and ctx.heightAboveGround > 50 and not ctx.atAirport then
		s.mysteriousMessageTimer -= dt
	end
	if s.mysteriousMessageTimer < 0 then
		if not ctx.messageOpen and not ctx.atAirport then
			if self:allQuestsCompleted(weather) then
				if s.mysteriousMessageIndex2 < #STORY.mysteriousMessages2 then
					s.mysteriousMessageIndex2 += 1
					self.onMessage(STORY.mysteriousMessages2[s.mysteriousMessageIndex2], "Mystery")
				end
			elseif self:completedCount(research) > 3 then
				if s.mysteriousMessageIndex3 < #STORY.mysteriousMessages3 then
					s.mysteriousMessageIndex3 += 1
					self.onMessage(STORY.mysteriousMessages3[s.mysteriousMessageIndex3], "Mystery")
				end
			elseif s.mysteriousMessageIndex < #STORY.mysteriousMessages then
				s.mysteriousMessageIndex += 1
				self.onMessage(STORY.mysteriousMessages[s.mysteriousMessageIndex], "Mystery")
			end
		end
		s.mysteriousMessageTimer = STORY.mysteriousMessagesInterval + STORY.mysteriousMessagesIntervalIncrease * s.mysteriousMessageIndex
	end

	-- The final radio message reveals the explosion site.
	if not s.finalMessagePlayed and self:allQuestsCompleted(research) then
		if ctx.heightAboveGround > 50 and not ctx.messageOpen and not ctx.atAirport then
			rt.finalMessageTimer -= dt
		end
		if rt.finalMessageTimer < 0 then
			s.finalMessagePlayed = true
			self.onMessage(STORY.finalMessage, "Mystery")
			if self.explosionSite then
				self:reveal(self.explosionSite.position.X, self.explosionSite.position.Z, 1)
			end
		end
	end
	return result
end

-- ------------------------------------------------------------------------------------------- save / load
local function questProgress(self: any, out: { [string]: any })
	for id, state in self.airports do
		for _, q in state.allQuests do
			if q.completed or q.kind == "delivery" then
				out[q.id] = { c = q.completed, a = q.amount, r = q.reward }
			end
		end
	end
end

function Campaign:serialize(): any
	local quests = {}
	questProgress(self, quests)
	local airports = {}
	for id, state in self.airports do
		airports[id] = { cq = state.currentQuest and state.currentQuest.id or nil, qi = state.questInitialized, lm = state.landingMessage }
	end
	local discovered = {}
	for k in self.discovered do
		table.insert(discovered, k)
	end
	local unlocked = {}
	for k in self.unlocked do
		table.insert(unlocked, k)
	end
	return {
		v = 1,
		difficulty = self.difficulty,
		creative = self.creative,
		money = self.money,
		scrap = self.scrap,
		advancedScrap = self.advancedScrap,
		unlocked = unlocked,
		cargo = self.cargo,
		cargoVolume = self.cargoVolume,
		story = self.story,
		quests = quests,
		airports = airports,
		discovered = discovered,
		collectedScrap = self.collectedScrap,
		lastAirportId = self.lastAirportId,
	}
end

function Campaign.deserialize(data: any)
	local self = Campaign.new(data.difficulty, data.creative)
	self.money = data.money or self.money
	self.scrap = data.scrap or 0
	self.advancedScrap = data.advancedScrap or 0
	for _, name in data.unlocked or {} do
		self.unlocked[name] = true
	end
	self.cargo = data.cargo or {}
	self.cargoVolume = data.cargoVolume or 0
	for k, v in data.story or {} do
		self.story[k] = v
	end
	for _, k in data.discovered or {} do
		self.discovered[k] = true
	end
	self.collectedScrap = data.collectedScrap or {}
	self.lastAirportId = data.lastAirportId
	local byId = {}
	for _, state in self.airports do
		for _, q in state.allQuests do
			byId[q.id] = q
		end
	end
	for id, p in data.quests or {} do
		local q = byId[id]
		if q then
			q.completed = p.c == true
			if q.kind == "delivery" then
				q.amount = p.a or q.amount
				q.reward = p.r or q.reward
				updateDeliveryText(q, q.completed)
			end
		end
	end
	for id, a in data.airports or {} do
		local state = self.airports[id]
		if state then
			state.currentQuest = (a.cq and byId[a.cq]) or state.currentQuest
			-- Walk past completed quests in the chain (Airport.Load).
			local guard = 0
			while state.currentQuest and state.currentQuest.completed and state.currentQuest.followup and guard < 1000 do
				state.currentQuest = state.currentQuest.followup
				guard += 1
			end
			state.questInitialized = a.qi == true
			state.landingMessage = a.lm
		end
	end
	self:spawnScrap()
	return self
end

return Campaign
