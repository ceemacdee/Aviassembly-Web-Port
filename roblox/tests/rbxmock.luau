-- Minimal Roblox environment for running pure-logic modules outside Studio (tests only).
Random = { new = function(seed) return { NextInteger = function(_, a, b) return math.random(a, b) end, NextNumber = function() return math.random() end } end }
local V3 = {}
V3.__index = V3
local function v3(x, y, z) return setmetatable({ X = x or 0, Y = y or 0, Z = z or 0 }, V3) end
V3.__add = function(a, b) return v3(a.X + b.X, a.Y + b.Y, a.Z + b.Z) end
V3.__sub = function(a, b) return v3(a.X - b.X, a.Y - b.Y, a.Z - b.Z) end
V3.__unm = function(a) return v3(-a.X, -a.Y, -a.Z) end
V3.__mul = function(a, b)
	if type(a) == "number" then return v3(b.X * a, b.Y * a, b.Z * a) end
	if type(b) == "number" then return v3(a.X * b, a.Y * b, a.Z * b) end
	return v3(a.X * b.X, a.Y * b.Y, a.Z * b.Z)
end
V3.__div = function(a, b)
	if type(b) == "number" then return v3(a.X / b, a.Y / b, a.Z / b) end
	return v3(a.X / b.X, a.Y / b.Y, a.Z / b.Z)
end
V3.__eq = function(a, b) return a.X == b.X and a.Y == b.Y and a.Z == b.Z end
V3.__tostring = function(a) return string.format("%.3f, %.3f, %.3f", a.X, a.Y, a.Z) end
V3.__index = function(t, k)
	if k == "Magnitude" then return math.sqrt(t.X * t.X + t.Y * t.Y + t.Z * t.Z) end
	if k == "Unit" then local m = math.sqrt(t.X * t.X + t.Y * t.Y + t.Z * t.Z); return v3(t.X / m, t.Y / m, t.Z / m) end
	return V3[k]
end
function V3.Dot(a, b) return a.X * b.X + a.Y * b.Y + a.Z * b.Z end
function V3.Cross(a, b) return v3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X) end
function V3.Lerp(a, b, t) return a + (b - a) * t end
function V3.Min(a, b) return v3(math.min(a.X, b.X), math.min(a.Y, b.Y), math.min(a.Z, b.Z)) end
function V3.Max(a, b) return v3(math.max(a.X, b.X), math.max(a.Y, b.Y), math.max(a.Z, b.Z)) end
Vector3 = { new = v3, zero = v3(0, 0, 0), one = v3(1, 1, 1), xAxis = v3(1, 0, 0), yAxis = v3(0, 1, 0), zAxis = v3(0, 0, 1) }

local V2 = {}
local function v2(x, y) return setmetatable({ X = x or 0, Y = y or 0 }, V2) end
V2.__add = function(a, b) return v2(a.X + b.X, a.Y + b.Y) end
V2.__sub = function(a, b) return v2(a.X - b.X, a.Y - b.Y) end
V2.__mul = function(a, b) if type(a) == "number" then return v2(b.X * a, b.Y * a) end return v2(a.X * b, a.Y * b) end
V2.__div = function(a, b) return v2(a.X / b, a.Y / b) end
V2.__index = function(t, k)
	if k == "Magnitude" then return math.sqrt(t.X * t.X + t.Y * t.Y) end
	return V2[k]
end
function V2.Lerp(a, b, t) return a + (b - a) * t end
Vector2 = { new = v2, zero = v2(0, 0) }

-- CFrame: rotation matrix (columns = right, up, back) + position
local CF = {}
local function cf(p, r) return setmetatable({ Position = p, R = r }, CF) end
CF.__index = function(t, k)
	if k == "RightVector" then return t.R[1] end
	if k == "UpVector" then return t.R[2] end
	if k == "LookVector" then return -t.R[3] end
	if k == "X" then return t.Position.X end
	if k == "Y" then return t.Position.Y end
	if k == "Z" then return t.Position.Z end
	if k == "XVector" then return t.R[1] end
	if k == "YVector" then return t.R[2] end
	if k == "ZVector" then return t.R[3] end
	if k == "Rotation" then return cf(v3(0, 0, 0), t.R) end
	return CF[k]
end
local function rot(r, v) return r[1] * v.X + r[2] * v.Y + r[3] * v.Z end
CF.__add = function(a, b) return cf(a.Position + b, a.R) end
CF.__mul = function(a, b)
	if getmetatable(b) == CF then
		return cf(a.Position + rot(a.R, b.Position), { rot(a.R, b.R[1]), rot(a.R, b.R[2]), rot(a.R, b.R[3]) })
	end
	return a.Position + rot(a.R, b)
end
function CF.VectorToWorldSpace(a, v) return rot(a.R, v) end
function CF.VectorToObjectSpace(a, v) return v3(a.R[1]:Dot(v), a.R[2]:Dot(v), a.R[3]:Dot(v)) end
function CF.PointToObjectSpace(a, p) return a:VectorToObjectSpace(p - a.Position) end
function CF.PointToWorldSpace(a, p) return a * p end
function CF.GetComponents(a)
	local r = a.R
	return a.Position.X, a.Position.Y, a.Position.Z, r[1].X, r[2].X, r[3].X, r[1].Y, r[2].Y, r[3].Y, r[1].Z, r[2].Z, r[3].Z
end
function CF.Inverse(a)
	local r = a.R
	local t = { v3(r[1].X, r[2].X, r[3].X), v3(r[1].Y, r[2].Y, r[3].Y), v3(r[1].Z, r[2].Z, r[3].Z) }
	local inv = cf(v3(0, 0, 0), t)
	return cf(-rot(t, a.Position), t)
end
CFrame = {
	new = function(x, y, z)
		if type(x) == "table" then return cf(x, { v3(1, 0, 0), v3(0, 1, 0), v3(0, 0, 1) }) end
		return cf(v3(x or 0, y or 0, z or 0), { v3(1, 0, 0), v3(0, 1, 0), v3(0, 0, 1) })
	end,
	fromMatrix = function(p, x, y, z)
		z = z or x:Cross(y)
		return cf(p, { x, y, z })
	end,
	Angles = function(rx, ry, rz)
		local function rX(a) local c, s = math.cos(a), math.sin(a); return { v3(1, 0, 0), v3(0, c, s), v3(0, -s, c) } end
		local function rY(a) local c, s = math.cos(a), math.sin(a); return { v3(c, 0, -s), v3(0, 1, 0), v3(s, 0, c) } end
		local function rZ(a) local c, s = math.cos(a), math.sin(a); return { v3(c, s, 0), v3(-s, c, 0), v3(0, 0, 1) } end
		local o = v3(0, 0, 0)
		return cf(o, rX(rx)) * cf(o, rY(ry)) * cf(o, rZ(rz))
	end,
	identity = nil,
}
CFrame.identity = CFrame.new()

Color3 = { new = function(r, g, b) return { R = r, G = g, B = b } end, fromRGB = function(r, g, b) return { R = r / 255, G = g / 255, B = b / 255 } end }
function typeof(v)
	local mt = getmetatable(v)
	if mt == V3 then return "Vector3" elseif mt == CF then return "CFrame" end
	return type(v)
end
math.sign = math.sign or function(x) return x > 0 and 1 or (x < 0 and -1 or 0) end

-- require() by script path: script objects are tables { __path = "src/shared/WorldMap.lua" }
local ROOT = ARGS[1] or "."
local cache = {}
local function node(path)
	return setmetatable({ __path = path }, {
		__index = function(t, k)
			if k == "Parent" then
				return node((path:gsub("/[^/]+$", "")))
			end
			return node(path .. "/" .. k)
		end,
	})
end
local function resolve(path)
	for _, cand in { path .. ".lua", path .. ".luau", path .. "/init.lua" } do
		local ok = pcall(__readfile, ROOT .. "/" .. cand)
		if ok then return cand end
	end
	error("module not found: " .. path)
end
function require(obj)
	local file = resolve(obj.__path)
	if cache[file] == nil then
		local src = __readfile(ROOT .. "/" .. file)
		local dir = file:gsub("%.luau?$", ""):gsub("/init$", "")
		local fn = __loadstring(src, file)
		local env = setmetatable({ script = node(dir) }, { __index = _G })
		setfenv(fn, env)
		cache[file] = fn()
	end
	return cache[file]
end
function shared_node(path) return node(path) end

UDim2 = { new = function(...) return { ... } end, fromScale = function(...) return { ... } end, fromOffset = function(...) return { ... } end }
UDim = { new = function(...) return { ... } end }
TweenInfo = { new = function(...) return { ... } end }
