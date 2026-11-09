--!strict
-- Small math helpers matching the Unity functions the original game used (Mathf.*, AnimationCurve, etc.).
local MathUtil = {}

function MathUtil.clamp01(x: number): number
	if x < 0 then
		return 0
	elseif x > 1 then
		return 1
	end
	return x
end

function MathUtil.lerp(a: number, b: number, t: number): number
	return a + (b - a) * MathUtil.clamp01(t)
end

function MathUtil.lerpUnclamped(a: number, b: number, t: number): number
	return a + (b - a) * t
end

function MathUtil.inverseLerp(a: number, b: number, v: number): number
	if a == b then
		return 0
	end
	return MathUtil.clamp01((v - a) / (b - a))
end

function MathUtil.moveTowards(current: number, target: number, maxDelta: number): number
	if math.abs(target - current) <= maxDelta then
		return target
	end
	return current + math.sign(target - current) * maxDelta
end

function MathUtil.sign(x: number): number
	-- Unity's Mathf.Sign returns 1 for 0.
	return if x >= 0 then 1 else -1
end

function MathUtil.snapToIncrement(value: number, increment: number): number
	return math.floor(value / increment + 0.5) * increment
end

-- Unity's Mathf.SmoothDamp. Returns newValue, newVelocity.
function MathUtil.smoothDamp(current: number, target: number, velocity: number, smoothTime: number, dt: number): (number, number)
	smoothTime = math.max(0.0001, smoothTime)
	local omega = 2 / smoothTime
	local x = omega * dt
	local exp = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x)
	local change = current - target
	local originalTo = target
	local temp = (velocity + omega * change) * dt
	velocity = (velocity - omega * temp) * exp
	local output = target + (change + temp) * exp
	if (originalTo - current > 0) == (output > originalTo) then
		output = originalTo
		velocity = (output - originalTo) / dt
	end
	return output, velocity
end

-- Evaluate a Unity AnimationCurve exported as { {time, value, inSlope, outSlope}, ... } (clamped outside the keys).
function MathUtil.evaluateCurve(keys: { { number } }, t: number): number
	local n = #keys
	if n == 0 then
		return 0
	end
	if t <= keys[1][1] then
		return keys[1][2]
	end
	if t >= keys[n][1] then
		return keys[n][2]
	end
	for i = 1, n - 1 do
		local k0, k1 = keys[i], keys[i + 1]
		if t <= k1[1] then
			local dt = k1[1] - k0[1]
			local s = (t - k0[1]) / dt
			local m0 = k0[4] * dt
			local m1 = k1[3] * dt
			local s2, s3 = s * s, s * s * s
			return (2 * s3 - 3 * s2 + 1) * k0[2] + (s3 - 2 * s2 + s) * m0 + (-2 * s3 + 3 * s2) * k1[2] + (s3 - s2) * m1
		end
	end
	return keys[n][2]
end

-- Signed angle (degrees) from a to b around axis, like Vector3.SignedAngle.
function MathUtil.signedAngle(a: Vector3, b: Vector3, axis: Vector3): number
	local am, bm = a.Magnitude, b.Magnitude
	if am < 1e-9 or bm < 1e-9 then
		return 0
	end
	local cos = math.clamp(a:Dot(b) / (am * bm), -1, 1)
	local angle = math.deg(math.acos(cos))
	-- Same formula as Unity. All vectors in this port are Unity vectors mirrored on X; axes built from
	-- cross products pick up the matching sign, so the physics formulas carry over unchanged.
	local s = axis:Dot(a:Cross(b))
	return if s < 0 then -angle else angle
end

function MathUtil.safeUnit(v: Vector3): Vector3
	local m = v.Magnitude
	if m < 1e-9 then
		return Vector3.zero
	end
	return v / m
end

function MathUtil.projectOnPlane(v: Vector3, normal: Vector3): Vector3
	local n = MathUtil.safeUnit(normal)
	return v - n * v:Dot(n)
end

function MathUtil.formatNumber(n: number): string
	local s = tostring(math.floor(n + 0.5))
	local formatted = s:reverse():gsub("(%d%d%d)", "%1,"):reverse()
	if formatted:sub(1, 1) == "," then
		formatted = formatted:sub(2)
	end
	return formatted
end

return MathUtil
