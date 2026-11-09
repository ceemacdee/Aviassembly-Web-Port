--!strict
-- Terrain noise, ported from NoiceGenerator.cs.
-- Mathf.PerlinNoise is implemented in Unity's native engine. Community analysis says it is Ken Perlin's
-- "improved noise" (reference permutation and gradients) evaluated at z = 0, rescaled to roughly 0..1 with
-- value = noise / 1.483 + 0.46527309. That reconstruction is used here.
local Noise = {}

local perm: { number } = {}
do
	local p = {
		151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240,
		21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88,
		237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231,
		83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1,
		216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173,
		186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16,
		58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
		172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34,
		242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199,
		106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72,
		243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
	}
	for i = 0, 511 do
		perm[i] = p[(i % 256) + 1]
	end
end

local function fade(t: number): number
	return t * t * t * (t * (t * 6 - 15) + 10)
end

-- Ken Perlin's reference grad(hash, x, y, z) with z = 0.
local function grad(hash: number, x: number, y: number): number
	local h = hash % 16
	local u = if h < 8 then x else y
	local v = if h < 4 then y elseif h == 12 or h == 14 then x else 0
	return (if h % 2 == 0 then u else -u) + (if math.floor(h / 2) % 2 == 0 then v else -v)
end

local function lerp(t: number, a: number, b: number): number
	return a + t * (b - a)
end

-- Improved noise at z = 0, in [-1, 1].
function Noise.improved(x: number, y: number): number
	local X = math.floor(x)
	local Y = math.floor(y)
	x -= X
	y -= Y
	X %= 256
	Y %= 256
	local u, v = fade(x), fade(y)
	local A = perm[X] + Y
	local AA, AB = perm[A], perm[A + 1]
	local B = perm[X + 1] + Y
	local BA, BB = perm[B], perm[B + 1]
	return lerp(v, lerp(u, grad(perm[AA], x, y), grad(perm[BA], x - 1, y)), lerp(u, grad(perm[AB], x, y - 1), grad(perm[BB], x - 1, y - 1)))
end

-- Mathf.PerlinNoise
function Noise.perlin(x: number, y: number): number
	return Noise.improved(x, y) / 1.483 + 0.46527309
end

local function perlinOffset(x: number, y: number): number
	return Noise.perlin(x + 10000, y + 10000)
end

local function fbm(x: number, y: number, octaves: number): number
	local total = 0
	for i = 0, octaves - 1 do
		local f = 2 ^ i
		total += (1 / f) * perlinOffset(f * x, f * y)
	end
	return total
end

-- NoiceGenerator.GetNoice: domain-warped fractional Brownian motion.
function Noise.getNoise(x: number, y: number, octaves: number): number
	local qx = fbm(x, y, octaves)
	local qy = fbm(x + 5.2, y + 1.3, octaves)
	return fbm(x + 4 * qx, y + 4 * qy, octaves) - 0.5
end

return Noise
