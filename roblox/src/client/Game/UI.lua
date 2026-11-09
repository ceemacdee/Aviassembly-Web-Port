--!strict
-- Interface built entirely from code: shared widgets plus the message queue (QuestAdditionUI), flight HUD
-- (PlaneHUD / FlyingUIController), airport panel (AirportInspector / CargoInventoryUI), map, crash screen and
-- main menu.
local Players = game:GetService("Players")
local TweenService = game:GetService("TweenService")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local WorldMap = require(Shared.WorldMap)
local MathUtil = require(Shared.Util.MathUtil)

local UI = {}

UI.Colors = {
	panel = Color3.fromRGB(28, 32, 38),
	panelLight = Color3.fromRGB(242, 242, 242),
	text = Color3.fromRGB(240, 240, 240),
	textDark = Color3.fromRGB(32, 32, 32),
	muted = Color3.fromRGB(150, 156, 165),
	accent = Color3.fromRGB(200, 123, 44), -- the original's selected-button orange
	good = Color3.fromRGB(90, 170, 100),
	bad = Color3.fromRGB(200, 70, 60),
	fuel = Color3.fromRGB(230, 180, 60),
	electric = Color3.fromRGB(90, 170, 230),
}
local C = UI.Colors
UI.Font = Enum.Font.GothamMedium
UI.FontBold = Enum.Font.GothamBold

function UI.new(class: string, props: { [string]: any }?, children: { Instance }?): any
	local inst = Instance.new(class)
	for k, v in props or {} do
		(inst :: any)[k] = v
	end
	for _, c in children or {} do
		c.Parent = inst
	end
	return inst
end

function UI.corner(radius: number?): UICorner
	return UI.new("UICorner", { CornerRadius = UDim.new(0, radius or 8) })
end

function UI.padding(p: number): UIPadding
	return UI.new("UIPadding", { PaddingLeft = UDim.new(0, p), PaddingRight = UDim.new(0, p), PaddingTop = UDim.new(0, p), PaddingBottom = UDim.new(0, p) })
end

function UI.list(dir: Enum.FillDirection?, pad: number?, align: Enum.HorizontalAlignment?): UIListLayout
	return UI.new("UIListLayout", {
		FillDirection = dir or Enum.FillDirection.Vertical,
		Padding = UDim.new(0, pad or 6),
		SortOrder = Enum.SortOrder.LayoutOrder,
		HorizontalAlignment = align or Enum.HorizontalAlignment.Left,
	})
end

function UI.panel(props: { [string]: any }?): Frame
	local f = UI.new("Frame", { BackgroundColor3 = C.panel, BackgroundTransparency = 0.12, BorderSizePixel = 0 })
	for k, v in props or {} do
		(f :: any)[k] = v
	end
	UI.corner(10).Parent = f
	return f
end

function UI.label(text: string, size: number?, props: { [string]: any }?): TextLabel
	local l = UI.new("TextLabel", {
		BackgroundTransparency = 1,
		Text = text,
		TextColor3 = C.text,
		Font = UI.Font,
		TextSize = size or 16,
		RichText = true,
		TextXAlignment = Enum.TextXAlignment.Left,
		Size = UDim2.new(1, 0, 0, (size or 16) + 6),
		TextWrapped = true,
	})
	for k, v in props or {} do
		(l :: any)[k] = v
	end
	return l
end

function UI.button(text: string, onClick: () -> (), props: { [string]: any }?): TextButton
	local b = UI.new("TextButton", {
		Text = text,
		Font = UI.FontBold,
		TextSize = 16,
		TextColor3 = C.textDark,
		BackgroundColor3 = C.panelLight,
		AutoButtonColor = true,
		Size = UDim2.new(0, 140, 0, 36),
		BorderSizePixel = 0,
		RichText = true,
	})
	for k, v in props or {} do
		(b :: any)[k] = v
	end
	UI.corner(8).Parent = b
	b.Activated:Connect(onClick)
	return b
end

function UI.bar(color: Color3, props: { [string]: any }?): (Frame, Frame)
	local back = UI.new("Frame", { BackgroundColor3 = Color3.fromRGB(60, 64, 70), BorderSizePixel = 0, Size = UDim2.new(1, 0, 0, 10) })
	for k, v in props or {} do
		(back :: any)[k] = v
	end
	UI.corner(5).Parent = back
	local fill = UI.new("Frame", { BackgroundColor3 = color, BorderSizePixel = 0, Size = UDim2.new(0.5, 0, 1, 0), Parent = back })
	UI.corner(5).Parent = fill
	return back, fill
end

-- Unity rich text -> Roblox rich text
function UI.rich(text: string): string
	text = text:gsub("<color=([#%w]+)>", function(c)
		local named = { black = "#202020", white = "#FFFFFF", red = "#D04030", green = "#40A050", orange = "#C87B2C" }
		return '<font color="' .. (named[c] or c) .. '">'
	end)
	text = text:gsub("</color>", "</font>")
	text = text:gsub("<size=[^>]*>", ""):gsub("</size>", "")
	return text
end

function UI.screen(name: string, order: number?): ScreenGui
	local player = Players.LocalPlayer
	local gui = UI.new("ScreenGui", { Name = name, ResetOnSpawn = false, IgnoreGuiInset = true, DisplayOrder = order or 1, ZIndexBehavior = Enum.ZIndexBehavior.Sibling })
	gui.Parent = player:WaitForChild("PlayerGui")
	return gui
end

-- ------------------------------------------------------------------------------------------- messages
-- Quest / story messages appear one at a time and wait for "OK" (QuestAdditionUI).
local Messages = {}
Messages.__index = Messages
UI.Messages = Messages

function Messages.new()
	local self = setmetatable({}, Messages)
	self.queue = {}
	self.gui = UI.screen("Messages", 20)
	local box = UI.panel({ Size = UDim2.new(0, 520, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(0.5, 1), Position = UDim2.new(0.5, 0, 1, -110), Visible = false, Parent = self.gui })
	UI.padding(16).Parent = box
	UI.list(Enum.FillDirection.Vertical, 10).Parent = box
	self.title = UI.label("", 14, { TextColor3 = C.accent, Font = UI.FontBold, LayoutOrder = 1, Parent = box })
	self.text = UI.label("", 18, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), LayoutOrder = 2, Parent = box })
	self.ok = UI.button("OK", function()
		self:advance()
	end, { LayoutOrder = 3, Parent = box, BackgroundColor3 = C.accent, TextColor3 = C.text })
	self.box = box
	return self
end

function Messages:push(text: string, kind: string?)
	if text == nil or text == "" then
		return
	end
	table.insert(self.queue, { text = text, kind = kind or "Misc" })
	if not self.box.Visible then
		self:show()
	end
end

function Messages:show()
	local m = self.queue[1]
	if not m then
		self.box.Visible = false
		return
	end
	local titles = { QuestAdded = "NEW JOB", QuestCompleted = "JOB COMPLETE", Mystery = "INCOMING RADIO SIGNAL...", Misc = "MESSAGE", Warning = "WARNING" }
	self.title.Text = titles[m.kind] or "MESSAGE"
	self.title.TextColor3 = if m.kind == "Mystery" then Color3.fromRGB(150, 200, 255) else C.accent
	self.text.Text = UI.rich(m.text)
	self.box.Visible = true
end

function Messages:advance()
	table.remove(self.queue, 1)
	self:show()
end

function Messages:isOpen(): boolean
	return self.box.Visible
end

-- ------------------------------------------------------------------------------------------- toasts (FlightWarningManager)
function UI.toast(gui: ScreenGui, text: string, color: Color3?)
	local t = UI.label(text, 18, {
		Size = UDim2.new(0, 420, 0, 34),
		AnchorPoint = Vector2.new(0.5, 0),
		Position = UDim2.new(0.5, 0, 0, 90),
		BackgroundTransparency = 0.2,
		BackgroundColor3 = color or C.panel,
		TextXAlignment = Enum.TextXAlignment.Center,
		Parent = gui,
	})
	UI.corner(8).Parent = t
	task.delay(3, function()
		TweenService:Create(t, TweenInfo.new(0.5), { TextTransparency = 1, BackgroundTransparency = 1 }):Play()
		task.wait(0.6)
		t:Destroy()
	end)
end

-- ------------------------------------------------------------------------------------------- flight HUD
local HUD = {}
HUD.__index = HUD
UI.HUD = HUD

function HUD.new(callbacks: any)
	local self = setmetatable({}, HUD)
	self.gui = UI.screen("FlightHUD", 5)
	local left = UI.panel({ Size = UDim2.new(0, 230, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, Position = UDim2.new(0, 16, 1, -16), AnchorPoint = Vector2.new(0, 1), Parent = self.gui })
	UI.padding(12).Parent = left
	UI.list(nil, 6).Parent = left
	self.speed = UI.label("0 km/h", 26, { Font = UI.FontBold, LayoutOrder = 1, Parent = left })
	self.altitude = UI.label("ALT 0 m", 14, { TextColor3 = C.muted, LayoutOrder = 2, Parent = left })
	UI.label("THROTTLE", 12, { TextColor3 = C.muted, LayoutOrder = 3, Parent = left })
	local _, thr = UI.bar(C.accent, { LayoutOrder = 4, Parent = left })
	self.throttle = thr
	self.fuelLabel = UI.label("FUEL", 12, { TextColor3 = C.muted, LayoutOrder = 5, Parent = left })
	local _, fuel = UI.bar(C.fuel, { LayoutOrder = 6, Parent = left })
	self.fuel = fuel
	self.elecLabel = UI.label("BATTERY", 12, { TextColor3 = C.muted, LayoutOrder = 7, Parent = left })
	local eback, elec = UI.bar(C.electric, { LayoutOrder = 8, Parent = left })
	self.elecBack = eback
	self.elec = elec
	self.cargo = UI.label("", 14, { LayoutOrder = 9, Parent = left })
	self.status = UI.label("", 13, { TextColor3 = C.muted, LayoutOrder = 10, Parent = left })

	local top = UI.panel({ Size = UDim2.new(0, 0, 0, 40), AutomaticSize = Enum.AutomaticSize.X, Position = UDim2.new(0, 16, 0, 16), Parent = self.gui })
	UI.padding(10).Parent = top
	UI.list(Enum.FillDirection.Horizontal, 18).Parent = top
	self.money = UI.label("", 18, { AutomaticSize = Enum.AutomaticSize.X, Size = UDim2.new(0, 0, 1, 0), Font = UI.FontBold, Parent = top })
	self.scrapLabel = UI.label("", 18, { AutomaticSize = Enum.AutomaticSize.X, Size = UDim2.new(0, 0, 1, 0), Parent = top })

	local obj = UI.panel({ Size = UDim2.new(0, 360, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(1, 0), Position = UDim2.new(1, -16, 0, 16), Parent = self.gui })
	UI.padding(12).Parent = obj
	UI.list(nil, 4).Parent = obj
	UI.label("JOBS", 12, { TextColor3 = C.accent, Font = UI.FontBold, LayoutOrder = 0, Parent = obj })
	self.objectives = UI.label("Land at an airport to get a job.", 15, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), LayoutOrder = 1, Parent = obj })
	self.objectivePanel = obj

	self.warning = UI.label("", 18, { Font = UI.FontBold, TextColor3 = Color3.fromRGB(255, 200, 80), Size = UDim2.new(0, 600, 0, 50), AnchorPoint = Vector2.new(0.5, 0), Position = UDim2.new(0.5, 0, 0, 20), TextXAlignment = Enum.TextXAlignment.Center, TextStrokeTransparency = 0.5, Parent = self.gui })

	local buttons = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(0, 0, 0, 40), AutomaticSize = Enum.AutomaticSize.X, AnchorPoint = Vector2.new(1, 1), Position = UDim2.new(1, -16, 1, -16), Parent = self.gui })
	UI.list(Enum.FillDirection.Horizontal, 8).Parent = buttons
	self.airportButton = UI.button("Airport (Enter)", callbacks.openAirport, { Size = UDim2.new(0, 160, 0, 40), BackgroundColor3 = C.accent, TextColor3 = C.text, Visible = false, Parent = buttons })
	UI.button("Map (M)", callbacks.openMap, { Size = UDim2.new(0, 110, 0, 40), Parent = buttons })
	UI.button("Controls", callbacks.showControls, { Size = UDim2.new(0, 110, 0, 40), Parent = buttons })
	return self
end

function HUD:update(t: any, campaign: any?, objectives: string?, nearAirport: any?)
	self.speed.Text = string.format("%d km/h", math.floor(t.speed * 3.6 + 0.5))
	self.altitude.Text = string.format("ALT %d m   GROUND %s m", math.floor(t.altitude + 0.5), if t.height > 4000 then "--" else tostring(math.floor(t.height + 0.5)))
	self.throttle.Size = UDim2.new(math.clamp(math.abs(t.throttle), 0, 1), 0, 1, 0)
	self.throttle.BackgroundColor3 = if t.throttle < 0 then C.bad else C.accent
	self.fuel.Size = UDim2.new(math.clamp(t.fuel / math.max(t.fuelCapacity, 1e-3), 0, 1), 0, 1, 0)
	self.fuelLabel.Text = string.format("FUEL  %d / %d", math.floor(t.fuel + 0.5), math.floor(t.fuelCapacity + 0.5))
	self.elecBack.Visible = t.hasElectric
	self.elecLabel.Visible = t.hasElectric
	self.elec.Size = UDim2.new(math.clamp(t.electricity / math.max(t.batteryCapacity, 1e-3), 0, 1), 0, 1, 0)
	if campaign then
		local parts = {}
		for id, n in campaign.cargo do
			table.insert(parts, n .. " " .. (WorldMap.CargoTypes[id] and WorldMap.CargoTypes[id].name or id))
		end
		self.cargo.Text = string.format("CARGO %d / %d  %s", campaign.cargoVolume, math.floor(t.cargoVolume + 0.5), table.concat(parts, ", "))
		self.money.Text = "💰 " .. MathUtil.formatNumber(campaign.money)
		self.scrapLabel.Text = "⚙ " .. campaign.scrap .. (if campaign.advancedScrap > 0 then "   ✦ " .. campaign.advancedScrap else "")
	end
	local status = {}
	if t.retracted then
		table.insert(status, "Gear up")
	end
	if t.braking then
		table.insert(status, "Air brakes")
	end
	self.status.Text = table.concat(status, " · ")
	local warn = nil
	for title, hint in t.warnings do
		warn = title .. " — " .. hint
	end
	if t.fuel <= 0 and t.fuelCapacity > 0 then
		warn = "Out of fuel"
	end
	self.warning.Text = warn or ""
	if objectives then
		self.objectives.Text = UI.rich(objectives)
	end
	self.airportButton.Visible = nearAirport ~= nil
	if nearAirport then
		self.airportButton.Text = nearAirport.name .. " (Enter)"
	end
end

function HUD:destroy()
	self.gui:Destroy()
end

-- ------------------------------------------------------------------------------------------- airport panel
local AirportPanel = {}
AirportPanel.__index = AirportPanel
UI.AirportPanel = AirportPanel

function AirportPanel.new(callbacks: any)
	local self = setmetatable({}, AirportPanel)
	self.callbacks = callbacks
	self.gui = UI.screen("AirportPanel", 10)
	local frame = UI.panel({ Size = UDim2.new(0, 460, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Visible = false, Parent = self.gui })
	UI.padding(16).Parent = frame
	UI.list(nil, 10).Parent = frame
	self.frame = frame
	self.title = UI.label("", 24, { Font = UI.FontBold, LayoutOrder = 1, Parent = frame })
	self.quest = UI.label("", 16, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), LayoutOrder = 2, Parent = frame })
	UI.label("CARGO FOR PICKUP", 12, { TextColor3 = C.accent, Font = UI.FontBold, LayoutOrder = 3, Parent = frame })
	self.cargoList = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, LayoutOrder = 4, Parent = frame })
	UI.list(nil, 6).Parent = self.cargoList
	self.space = UI.label("", 14, { TextColor3 = C.muted, LayoutOrder = 5, Parent = frame })
	local row = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 40), LayoutOrder = 6, Parent = frame })
	UI.list(Enum.FillDirection.Horizontal, 8).Parent = row
	self.refuel = UI.button("Refuel", function()
		callbacks.refuel()
		self:refresh()
	end, { Parent = row })
	self.hangar = UI.button("Hangar", callbacks.hangar, { Parent = row })
	UI.button("Take off", function()
		self:close()
	end, { Parent = row, BackgroundColor3 = C.accent, TextColor3 = C.text })
	return self
end

function AirportPanel:open(airport: any, campaign: any, flight: any)
	self.airport, self.campaign, self.flight = airport, campaign, flight
	self.frame.Visible = true
	self:refresh()
end

function AirportPanel:refresh()
	local airport, campaign, flight = self.airport, self.campaign, self.flight
	if not airport then
		return
	end
	self.title.Text = airport.name
	local state = campaign.airports[airport.id]
	local q = state.currentQuest
	if q and state.questInitialized and not q.completed then
		local stage = campaign:questStage(q, flight.cargoVolume)
		self.quest.Text = UI.rich("<b>" .. q.name .. "</b>\n" .. q.description .. (if stage then "\n<i>" .. stage .. "</i>" else ""))
	elseif state.questBlocked then
		self.quest.Text = "No work available here yet."
	else
		self.quest.Text = "No jobs right now."
	end
	for _, c in self.cargoList:GetChildren() do
		if c:IsA("Frame") then
			c:Destroy()
		end
	end
	local locked = not campaign:airportCargoUnlocked(airport.id)
	for _, id in airport.cargoTypes do
		local cargo = WorldMap.CargoTypes[id]
		local row = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 34), Parent = self.cargoList })
		local info = string.format("<b>%s</b>  · %d space · %.1f t%s%s", cargo.name, cargo.cargoSpace, cargo.weight, if cargo.fragile then " · fragile" else "", if cargo.expires then " · perishable" else "")
		UI.label(info, 15, { Size = UDim2.new(1, -170, 1, 0), Parent = row })
		UI.label("x" .. campaign:cargoCount(id), 16, { Size = UDim2.new(0, 40, 1, 0), Position = UDim2.new(1, -165, 0, 0), Parent = row })
		UI.button("−", function()
			campaign:removeCargo(id, 1)
			self.callbacks.cargoChanged()
			self:refresh()
		end, { Size = UDim2.new(0, 50, 0, 30), Position = UDim2.new(1, -115, 0, 2), Parent = row })
		local add = UI.button("+", function()
			if not campaign:addCargo(id, flight.cargoVolume, airport.id) then
				UI.toast(self.gui, if locked then "This cargo is not available yet" else "Not enough cargo space", C.bad)
			end
			self.callbacks.cargoChanged()
			self:refresh()
		end, { Size = UDim2.new(0, 50, 0, 30), Position = UDim2.new(1, -55, 0, 2), Parent = row })
		add.AutoButtonColor = not locked
	end
	if #airport.cargoTypes == 0 then
		UI.label("Nothing to pick up here.", 14, { TextColor3 = C.muted, Parent = self.cargoList })
	elseif locked then
		UI.label("Cargo here is locked for now.", 14, { TextColor3 = C.bad, Parent = self.cargoList })
	end
	self.space.Text = string.format("Cargo space used: %d / %d", campaign.cargoVolume, math.floor(flight.cargoVolume + 0.5))
	local canRefuel = airport.data.refuel or airport.isBase
	self.refuel.Visible = canRefuel
	self.refuel.Text = if canRefuel then "Refuel" else "No fuel here"
	self.hangar.Visible = airport.isBase
end

function AirportPanel:close()
	self.frame.Visible = false
	self.airport = nil
end

function AirportPanel:isOpen(): boolean
	return self.frame.Visible
end

-- ------------------------------------------------------------------------------------------- map
local MapView = {}
MapView.__index = MapView
UI.MapView = MapView

function MapView.new()
	local self = setmetatable({}, MapView)
	self.gui = UI.screen("Map", 12)
	local frame = UI.panel({ Size = UDim2.new(0.8, 0, 0.85, 0), AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Visible = false, BackgroundColor3 = Color3.fromRGB(70, 104, 150), BackgroundTransparency = 0, ClipsDescendants = true, Parent = self.gui })
	self.frame = frame
	UI.label("MAP — press M to close", 14, { Position = UDim2.new(0, 12, 0, 8), Size = UDim2.new(1, -24, 0, 20), Parent = frame })
	self.canvas = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, -40, 1, -50), Position = UDim2.new(0, 20, 0, 36), Parent = frame })
	-- world bounds (Unity space)
	self.minX, self.maxX, self.minZ, self.maxZ = -20000, 11000, -3000, 28000
	return self
end

function MapView:toCanvas(ux: number, uz: number): UDim2
	-- North (+Z) up. Unity +X is to the right on the map, matching the original game's map.
	local fx = (ux - self.minX) / (self.maxX - self.minX)
	local fz = 1 - (uz - self.minZ) / (self.maxZ - self.minZ)
	return UDim2.new(fx, 0, fz, 0)
end

function MapView:toggle(campaign: any?, planePosition: Vector3, heading: Vector3, targets: { any })
	self.frame.Visible = not self.frame.Visible
	if self.frame.Visible then
		self:draw(campaign, planePosition, heading, targets)
	end
end

function MapView:draw(campaign: any?, planePosition: Vector3, heading: Vector3, targets: { any })
	self.canvas:ClearAllChildren()
	for _, c in WorldMap.Continents do
		local discovered = campaign == nil
		for _, a in c.airports do
			if campaign and campaign:isDiscovered(a.position.X, a.position.Z) then
				discovered = true
			end
		end
		if discovered then
			local size = c.radius * 2 / (self.maxX - self.minX)
			local blob = UI.new("Frame", {
				AnchorPoint = Vector2.new(0.5, 0.5),
				Position = self:toCanvas(c.origin.X, c.origin.Z),
				Size = UDim2.new(size, 0, size * (self.maxX - self.minX) / (self.maxZ - self.minZ), 0),
				BackgroundColor3 = Color3.new(table.unpack(c.type.surfaceColor or { 0.5, 0.6, 0.4 })),
				BackgroundTransparency = 0.15,
				BorderSizePixel = 0,
				Parent = self.canvas,
			})
			UI.corner(9999).Parent = blob
		end
	end
	for _, a in WorldMap.Airports do
		if campaign == nil or campaign:isDiscovered(a.position.X, a.position.Z) then
			local dot = UI.new("Frame", { AnchorPoint = Vector2.new(0.5, 0.5), Position = self:toCanvas(a.position.X, a.position.Z), Size = UDim2.new(0, 10, 0, 10), BackgroundColor3 = if a.isBase then C.accent else Color3.new(1, 1, 1), BorderSizePixel = 0, Parent = self.canvas })
			UI.corner(5).Parent = dot
			UI.label(a.name, 12, { Position = UDim2.new(0, 8, 0, -6), Size = UDim2.new(0, 200, 0, 16), TextStrokeTransparency = 0.6, Parent = dot })
		end
	end
	for _, t in targets do
		local m = UI.new("Frame", { AnchorPoint = Vector2.new(0.5, 0.5), Position = self:toCanvas(t.position.X, t.position.Z), Size = UDim2.new(0, 22, 0, 22), BackgroundTransparency = 1, Parent = self.canvas })
		UI.new("UIStroke", { Color = Color3.fromRGB(255, 220, 80), Thickness = 3, Parent = m })
		UI.corner(11).Parent = m
	end
	if campaign then
		for _, s in campaign.scrapSpots do
			if campaign:isDiscovered(s.x, s.z) then
				local d = UI.new("Frame", { AnchorPoint = Vector2.new(0.5, 0.5), Position = self:toCanvas(s.x, s.z), Size = UDim2.new(0, 6, 0, 6), BackgroundColor3 = Color3.fromRGB(255, 170, 60), BorderSizePixel = 0, Parent = self.canvas })
				UI.corner(3).Parent = d
			end
		end
	end
	local ux, uz = -planePosition.X, planePosition.Z
	local arrow = UI.label("▲", 22, { AnchorPoint = Vector2.new(0.5, 0.5), Position = self:toCanvas(ux, uz), Size = UDim2.new(0, 24, 0, 24), TextXAlignment = Enum.TextXAlignment.Center, TextColor3 = Color3.fromRGB(255, 80, 60), Parent = self.canvas })
	arrow.Rotation = math.deg(math.atan2(-heading.X, heading.Z))
end

function MapView:isOpen(): boolean
	return self.frame.Visible
end

-- ------------------------------------------------------------------------------------------- crash / game over
function UI.gameOver(text: string, options: { { string | () -> () } }): ScreenGui
	local gui = UI.screen("GameOver", 30)
	local back = UI.new("Frame", { BackgroundColor3 = Color3.new(0, 0, 0), BackgroundTransparency = 0.35, Size = UDim2.new(1, 0, 1, 0), Parent = gui })
	local box = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(0, 500, 0, 200), AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = back })
	UI.list(nil, 16, Enum.HorizontalAlignment.Center).Parent = box
	UI.label(text, 42, { Font = UI.FontBold, TextXAlignment = Enum.TextXAlignment.Center, Size = UDim2.new(1, 0, 0, 50), Parent = box })
	local row = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 44), Parent = box })
	UI.list(Enum.FillDirection.Horizontal, 12, Enum.HorizontalAlignment.Center).Parent = row
	for _, o in options do
		UI.button(o[1] :: string, function()
			gui:Destroy();
			(o[2] :: () -> ())()
		end, { Size = UDim2.new(0, 200, 0, 44), Parent = row })
	end
	return gui
end

-- ------------------------------------------------------------------------------------------- simple modal with text
function UI.modal(title: string, body: string): ScreenGui
	local gui = UI.screen("Modal", 40)
	local frame = UI.panel({ Size = UDim2.new(0, 520, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = gui })
	UI.padding(18).Parent = frame
	UI.list(nil, 10).Parent = frame
	UI.label(title, 22, { Font = UI.FontBold, LayoutOrder = 1, Parent = frame })
	UI.label(body, 16, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), LayoutOrder = 2, Parent = frame })
	UI.button("Close", function()
		gui:Destroy()
	end, { LayoutOrder = 3, Parent = frame })
	return gui
end

UI.ControlsText = table.concat({
	"<b>Flying</b>",
	"W / S — pitch down / up      A / D — roll      Q / E — yaw (rudder)",
	"Space — throttle up (hold near the ground to help take-off)      Ctrl or Shift — throttle down / brake / reverse",
	"G — retract landing gear      F — decouple      M — map      Enter — airport menu when landed",
	"Right mouse drag — look around      Mouse wheel — zoom      C — cockpit camera",
	"",
	"<b>Building</b>",
	"Click a part, then click on the plane to attach it      R / T — rotate      X — mirror mode",
	"Click a placed part to select it      Delete — remove it      Ctrl+Z — undo",
	"Right mouse drag — orbit      Mouse wheel — zoom",
}, "\n")

return UI
