--!strict
-- Terrain noise, ported from NoiceGenerator.cs. Unity's Mathf.PerlinNoise is closed source, so this is
-- classic improved Perlin noise remapped to Unity's [0, 1] range; terrain has the same character and scale
-- as the original but not identical hills.
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

local function grad(hash: number, x: number, y: number): number
	local h = hash % 8
	local u = if h < 4 then x else y
	local v = if h < 4 then y else x
	return (if h % 2 == 0 then u else -u) + (if math.floor(h / 2) % 2 == 0 then 2 * v else -2 * v)
end

-- 2D Perlin noise in [0, 1] (Unity convention).
function Noise.perlin(x: number, y: number): number
	local xi = math.floor(x)
	local yi = math.floor(y)
	local xf = x - xi
	local yf = y - yi
	xi = xi % 256
	yi = yi % 256
	local u = fade(xf)
	local v = fade(yf)
	local aa = perm[perm[xi] + yi]
	local ab = perm[perm[xi] + yi + 1]
	local ba = perm[perm[xi + 1] + yi]
	local bb = perm[perm[xi + 1] + yi + 1]
	local x1 = grad(aa, xf, yf) + (grad(ba, xf - 1, yf) - grad(aa, xf, yf)) * u
	local x2 = grad(ab, xf, yf - 1) + (grad(bb, xf - 1, yf - 1) - grad(ab, xf, yf - 1)) * u
	local n = x1 + (x2 - x1) * v
	-- grad() output spans roughly [-2.2, 2.2]; scale into Unity's 0..1 with mean 0.5.
	return math.clamp(0.5 + n * 0.25, -0.1, 1.1)
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
