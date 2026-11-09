__loadstring(__readfile("tests/rbxmock.luau"), "rbxmock")()
Random = { new = function(seed) local s = seed; return { NextInteger = function(_, a, b) return math.random(a, b) end, NextNumber = function() return math.random() end } end }
os.time = os.time or function() return 0 end
local Campaign = require(shared_node("src/shared/Campaign"))
local WorldMap = require(shared_node("src/shared/WorldMap"))
local c = Campaign.new("Normal")
local msgs = {}
c.onMessage = function(t, k) table.insert(msgs, k .. ": " .. t) end
local unlocked = 0 for _ in c.unlocked do unlocked += 1 end
print("money", c.money, "unlocked parts", unlocked, "scrap spots", #c.scrapSpots)
local hosp = WorldMap.findAirport("Hospital")
local function robloxPos(a) return Vector3.new(-a.position.X, 0, a.position.Z) end
-- land at hospital: get quest
c:landed(hosp, { position = robloxPos(hosp), speed = 0, airport = hosp, maxCargoVolume = 10 })
local q = c.airports[hosp.id].currentQuest
print("quest:", q.name, "|", q.description, "| reward", q.reward, "cargo", q.cargo, q.amount)
local stage, target = c:questStage(q, 10)
print("stage:", stage, target and target.name)
-- pick up batteries at turbines
local turb = target
print("add cargo", c:addCargo(q.cargo, 10, turb.id), c:addCargo(q.cargo, 10, turb.id), "count", c:cargoCount(q.cargo), "vol", c.cargoVolume)
c:landed(hosp, { position = robloxPos(hosp), speed = 0, airport = hosp, maxCargoVolume = 10 })
print("completed:", q.completed, "money", c.money, "scrap", c.scrap)
local nq = c.airports[hosp.id].currentQuest
print("next quest:", nq.kind, nq.name, #(nq.children or {}))
for _, m in msgs do print("  msg>", m) end
-- story: earthquake
for i = 1, 1000 do
	local r = c:updateStory({ dt = 0.05, position = Vector3.new(0, 100, 1000), heightAboveGround = 100, atAirport = false, exploded = false, messageOpen = false })
end
print("postEarthquake", c.story.postEarthquake, "state", c:storyState())
local save = c:serialize()
local c2 = Campaign.deserialize(save)
print("roundtrip money", c2.money, "hospital quest", c2.airports[hosp.id].currentQuest.id, "completed first", c2.airports[hosp.id].allQuests[1].completed, "post", c2.story.postEarthquake)
-- restricted zone
local r = c:updateStory({ dt = 0.05, position = Vector3.new(-3571, 100, 19857), heightAboveGround = 100, atAirport = false, exploded = false, messageOpen = false })
print("desert explode:", r.explode)
