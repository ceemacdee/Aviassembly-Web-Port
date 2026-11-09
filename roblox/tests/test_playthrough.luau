__loadstring(__readfile("tests/rbxmock.luau"), "rbxmock")()
local Campaign = require(shared_node("src/shared/Campaign"))
local WorldMap = require(shared_node("src/shared/WorldMap"))
local c = Campaign.new("Normal")
local msgs = 0
c.onMessage = function() msgs += 1 end
local function pos(a) return Vector3.new(-a.position.X, 0, a.position.Z) end
local function storyTick(n, p)
	for _ = 1, n do
		c:updateStory({ dt = 0.1, position = p or Vector3.new(0, 300, 1000), heightAboveGround = 300, atAirport = false, exploded = false, messageOpen = false })
	end
end
local function cheatCargo(q)
	if q.completed then return end
	if q.kind == "container" then
		for _, ch in q.children do cheatCargo(ch) end
	else
		c.cargo[q.cargo] = (c.cargo[q.cargo] or 0) + q.amount
		c.cargoVolume += WorldMap.CargoTypes[q.cargo].cargoSpace * q.amount
	end
end
local rounds = 0
for round = 1, 60 do
	local progress = false
	for _, a in WorldMap.Airports do
		local ctx = { position = pos(a), speed = 0, airport = a, maxCargoVolume = 1000 }
		c:landed(a, ctx)
		local state = c.airports[a.id]
		local q = state.currentQuest
		if q and state.questInitialized and not q.completed then
			local stage, target = c:questStage(q, 1000)
			assert(stage, "no stage for " .. q.id)
			cheatCargo(q)
			c:landed(a, ctx)
			if q.completed then progress = true end
		end
	end
	storyTick(300)
	rounds = round
	if not progress and c.story.playedEndCutscene then break end
end
local done, total = 0, 0
for _, a in WorldMap.Airports do
	for _, q in c.airports[a.id].allQuests do
		total += 1
		if q.completed then done += 1 end
	end
end
print(("rounds=%d quests completed %d/%d money=%d scrap=%d adv=%d messages=%d"):format(rounds, done, total, c.money, c.scrap, c.advancedScrap, msgs))
print("story:", "post", c.story.postEarthquake, "snow", c.story.snowUnlocked, "desert", c.story.desertUnlocked, "final msg", c.story.finalMessagePlayed, "end", c.story.playedEndCutscene)
for _, a in WorldMap.Airports do
	local left = 0
	for _, q in c.airports[a.id].allQuests do if not q.completed and not q.random then left += 1 end end
	if left > 0 then print("  incomplete:", a.name, left, c.airports[a.id].questBlocked and "blocked" or "") end
end
-- after the end, random jobs appear
local farm = WorldMap.findAirport("Farm")
c:landed(farm, { position = pos(farm), speed = 0, airport = farm, maxCargoVolume = 1000 })
local rq = c.airports[farm.id].currentQuest
print("post-game job at farm:", rq and rq.name, rq and rq.random)
local blob = c:serialize()
local c2 = Campaign.deserialize(blob)
local d2 = 0 for _, a in WorldMap.Airports do for _, q in c2.airports[a.id].allQuests do if q.completed then d2 += 1 end end end
print("reload completed quests:", d2)
