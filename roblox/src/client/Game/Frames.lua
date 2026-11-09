--!strict
-- Helpers for the coordinate conventions of this port.
--
-- All part data is Unity data mirrored on X. A part's Roblox CFrame `cf` maps mirrored part-local data to the
-- world, and the original "Unity" axes of a transform appear as:
--   unity forward = cf.ZVector,  unity up = cf.YVector,  unity right = -cf.XVector
-- Mirrored (symmetry) copies keep a proper rotation plus a `flip` flag that negates local Y on *positions*
-- only, exactly like Unity's negative localScale.y (directions/rotations ignore the scale sign in Unity too).
local Frames = {}

function Frames.v(t: { number }?): Vector3
	if not t then
		return Vector3.zero
	end
	return Vector3.new(t[1], t[2], t[3])
end

-- Equivalent of Unity's Quaternion.LookRotation(forward, up) in this port's convention.
function Frames.lookRotation(forward: Vector3, up: Vector3, position: Vector3?): CFrame
	local z = forward.Unit
	local x = up:Cross(z)
	if x.Magnitude < 1e-6 then
		x = (if math.abs(z.Y) < 0.9 then Vector3.yAxis else Vector3.xAxis):Cross(z)
	end
	x = x.Unit
	local y = z:Cross(x)
	return CFrame.fromMatrix(position or Vector3.zero, x, y, z)
end

-- Rotation taking vector `from` onto vector `to` (Quaternion.FromToRotation).
function Frames.fromToRotation(from: Vector3, to: Vector3): CFrame
	local f, t = from.Unit, to.Unit
	local d = math.clamp(f:Dot(t), -1, 1)
	if d > 0.99999 then
		return CFrame.identity
	end
	if d < -0.99999 then
		local axis = Vector3.xAxis:Cross(f)
		if axis.Magnitude < 1e-6 then
			axis = Vector3.yAxis:Cross(f)
		end
		return CFrame.fromAxisAngle(axis.Unit, math.pi)
	end
	return CFrame.fromAxisAngle(f:Cross(t).Unit, math.acos(d))
end

-- A local position inside a (possibly flipped) part.
function Frames.localPoint(p: Vector3, flip: boolean): Vector3
	return if flip then Vector3.new(p.X, -p.Y, p.Z) else p
end

function Frames.worldPoint(cf: CFrame, flip: boolean, p: Vector3): Vector3
	return cf * Frames.localPoint(p, flip)
end

function Frames.worldDir(cf: CFrame, d: Vector3): Vector3
	return cf:VectorToWorldSpace(d)
end

-- World CFrame of a stored frame {pos, right, up, forward} inside a part (rotation from directions,
-- position flipped if the part is flipped). Returned in the port convention (Z = unity forward).
function Frames.frameWorld(cf: CFrame, flip: boolean, frame: any): CFrame
	local pos = Frames.worldPoint(cf, flip, Frames.v(frame.pos))
	local forward = cf:VectorToWorldSpace(Frames.v(frame.forward))
	local up = cf:VectorToWorldSpace(Frames.v(frame.up))
	return Frames.lookRotation(forward, up, pos)
end

function Frames.reflect(v: Vector3, normal: Vector3): Vector3
	return v - normal * (2 * v:Dot(normal))
end

function Frames.encodeCFrame(cf: CFrame): { number }
	local x, y, z, r00, r01, r02, r10, r11, r12, r20, r21, r22 = cf:GetComponents()
	return { x, y, z, r00, r01, r02, r10, r11, r12, r20, r21, r22 }
end

function Frames.decodeCFrame(t: { number }): CFrame
	return CFrame.new(table.unpack(t))
end

return Frames
