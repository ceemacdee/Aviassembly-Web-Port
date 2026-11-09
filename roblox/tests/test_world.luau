local src = __readfile("tests/rbxmock.luau")
__loadstring(src, "rbxmock")()
local WorldMap = require(shared_node("src/shared/WorldMap"))
local t0 = os.clock()
print("continents", #WorldMap.Continents, "airports", #WorldMap.Airports)
for _, c in WorldMap.Continents do
	print(string.format("%-8s origin=%s radius=%.0f hull=%d base=%s", c.name, tostring(c.origin), c.radius, #c.hull, c.baseAirport and c.baseAirport.name or "-"))
end
local n = 0
local mn, mx = math.huge, -math.huge
for x = -3000, 3000, 250 do
	for z = -1000, 12000, 250 do
		local h = WorldMap.height(x, z)
		mn = math.min(mn, h); mx = math.max(mx, h); n += 1
	end
end
print(string.format("height samples=%d min=%.1f max=%.1f  (%.1f us/sample)", n, mn, mx, (os.clock() - t0) / n * 1e6))
for _, a in WorldMap.Airports do
	print(string.format("%-28s pos=(%6.0f,%6.0f) h=%6.1f yaw=%5.1f base=%s", a.name, a.position.X, a.position.Z, WorldMap.airportHeight(a), WorldMap.airportYaw(a), tostring(a.isBase)))
end
print("ocean far away:", WorldMap.height(30000, 30000), WorldMap.isOcean(30000, 30000))
local hosp = WorldMap.findAirport("Hospital")
print("price SmallBatteries @hospital", WorldMap.cargoPrice(hosp, "SmallBatteries"))
