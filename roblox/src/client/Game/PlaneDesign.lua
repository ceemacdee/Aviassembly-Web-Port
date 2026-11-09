--!strict
-- A plane design: the list of placed parts (in design space), their parent/child links and mirror pairs.
-- Mirrors PlaneStorage / PartContainer from the original.
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Parts = require(Shared.Data.Parts)
local Fuselage = require(Shared.Fuselage)
local Frames = require(script.Parent.Frames)

local PlaneDesign = {}
PlaneDesign.__index = PlaneDesign

export type Entry = {
	id: number,
	name: string,
	cf: CFrame,
	flip: boolean,
	parent: number?,
	mirror: number?,
	fuselage: any?,
	color: { number }?,
}

function PlaneDesign.new()
	local self = setmetatable({}, PlaneDesign)
	self.parts = {} :: { Entry }
	self.nextId = 1
	return self
end

function PlaneDesign:get(id: number): Entry?
	for _, e in self.parts do
		if e.id == id then
			return e
		end
	end
	return nil
end

function PlaneDesign:add(e: any): Entry
	e.id = self.nextId
	self.nextId += 1
	table.insert(self.parts, e)
	return e
end

function PlaneDesign:children(id: number): { Entry }
	local out = {}
	for _, e in self.parts do
		if e.parent == id then
			table.insert(out, e)
		end
	end
	return out
end

-- Removes a part, everything attached to it, and (optionally) the mirrored copies. Returns removed entries.
function PlaneDesign:remove(id: number, withMirror: boolean?): { Entry }
	local removed = {}
	local function rec(pid: number)
		for _, c in self:children(pid) do
			rec(c.id)
		end
		for i, e in self.parts do
			if e.id == pid then
				table.insert(removed, table.remove(self.parts, i))
				break
			end
		end
	end
	local e = self:get(id)
	if not e then
		return removed
	end
	local mirror = e.mirror
	rec(id)
	if withMirror and mirror and self:get(mirror) then
		for _, r in self:remove(mirror, false) do
			table.insert(removed, r)
		end
	end
	return removed
end

function PlaneDesign:basePart(): Entry?
	for _, e in self.parts do
		if e.parent == nil then
			return e
		end
	end
	return nil
end

-- Per-part derived stats (Body scales with its volume).
function PlaneDesign.partStats(e: Entry)
	local data = Parts[e.name]
	local weight, price, cargo = data.weight, data.price, 0
	for _, c in data.components do
		if c.type == "Fuselage" then
			cargo += math.abs(c.data.cargoVolume or 0)
		end
	end
	if data.procedural and e.fuselage then
		local s = Fuselage.stats(e.fuselage, cargo, weight, price)
		weight, price, cargo = s.weight, s.price, s.cargoVolume
	end
	return { weight = weight, price = price, cargo = cargo }
end

function PlaneDesign:cost(): number
	local total = 0
	for _, e in self.parts do
		total += PlaneDesign.partStats(e).price
	end
	return total
end

-- Summary stats for the hangar UI (PlaneStats / BuildingPartTooltip).
function PlaneDesign:stats()
	local s = { weight = 0, fuel = 0, battery = 0, cargo = 0, thrust = 0, lift = 0, parts = #self.parts, hasEngine = false, hasWing = false, hasWheel = false, cost = 0 }
	for _, e in self.parts do
		local ps = PlaneDesign.partStats(e)
		s.weight += ps.weight
		s.cargo += ps.cargo
		s.cost += ps.price
		for _, c in Parts[e.name].components do
			local d = c.data
			if c.type == "FuelTank" then
				s.fuel += math.abs(d.volume or 0)
			elseif c.type == "Wing" then
				s.fuel += math.abs(d.fuel or 0)
				s.lift += (d.liftForce or 0) * d.area[1] * d.area[2] * 10000
				s.hasWing = true
			elseif c.type == "Battery" then
				s.battery += math.abs(d.capacity or 0)
			elseif c.type == "Engine" then
				s.thrust += if d.useAfterburner == 1 then d.afterburnerThrust else d.thrust
				s.hasEngine = true
			elseif c.type == "Wheel" then
				s.hasWheel = true
			end
		end
	end
	return s
end

-- ---------------------------------------------------------------------------------------------- serialisation
function PlaneDesign:serialize(): any
	local out = { v = 1, parts = {} }
	for _, e in self.parts do
		table.insert(out.parts, {
			i = e.id,
			n = e.name,
			c = Frames.encodeCFrame(e.cf),
			f = e.flip,
			p = e.parent,
			m = e.mirror,
			b = if e.fuselage then Fuselage.encode(e.fuselage) else nil,
			col = e.color,
		})
	end
	return out
end

function PlaneDesign.deserialize(data: any)
	local self = PlaneDesign.new()
	if type(data) ~= "table" or type(data.parts) ~= "table" then
		return self
	end
	for _, p in data.parts do
		if Parts[p.n] then
			table.insert(self.parts, {
				id = p.i,
				name = p.n,
				cf = Frames.decodeCFrame(p.c),
				flip = p.f == true,
				parent = p.p,
				mirror = p.m,
				fuselage = if p.b then Fuselage.decode(p.b) else nil,
				color = p.col,
			})
			self.nextId = math.max(self.nextId, p.i + 1)
		end
	end
	return self
end

return PlaneDesign
