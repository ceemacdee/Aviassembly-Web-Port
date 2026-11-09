-- Aviassembly for Roblox: game flow (GameManager), flight loop, camera (CameraController), saving (AutoSave).
local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local UserInputService = game:GetService("UserInputService")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")
local Lighting = game:GetService("Lighting")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local WorldMap = require(Shared.WorldMap)
local Campaign = require(Shared.Campaign)
local Parts = require(Shared.Data.Parts)
local Game = script.Parent:WaitForChild("Game")
local UI = require(Game.UI)
local Frames = require(Game.Frames)
local PlaneDesign = require(Game.PlaneDesign)
local PartVisual = require(Game.PartVisual)
local Hangar = require(Game.Hangar)
local Flight = require(Game.Flight)
local WorldStreamer = require(Game.WorldStreamer)
local Audio = require(Game.Audio)
local Sounds = require(Shared.Data.Sounds)

UI.clickSound = function()
	Audio.play("click")
end

local player = Players.LocalPlayer
local camera = Workspace.CurrentCamera
local C = UI.Colors

-- ------------------------------------------------------------------------------------------- world settings
Workspace.Gravity = Flight.Gravity
Lighting.FogStart = 300
Lighting.FogEnd = 3500
Lighting.FogColor = Color3.new(0.448, 0.561, 0.638) -- DefaultFog profile
Lighting.ClockTime = 13
Lighting.Brightness = 2
Lighting.OutdoorAmbient = Color3.fromRGB(150, 150, 150)
Lighting.GlobalShadows = true
local sky = Lighting:FindFirstChildOfClass("Sky") or Instance.new("Sky")
sky.CelestialBodiesShown = true
sky.Parent = Lighting
local atmosphere = Lighting:FindFirstChildOfClass("Atmosphere") or Instance.new("Atmosphere")
atmosphere.Density = 0.25
atmosphere.Color = Color3.new(0.448, 0.561, 0.638)
atmosphere.Decay = Color3.new(0.01, 0.2, 0.333)
atmosphere.Haze = 1
atmosphere.Parent = Lighting
camera.CameraType = Enum.CameraType.Scriptable
camera.FieldOfView = 70

-- ------------------------------------------------------------------------------------------- state
local Remotes = ReplicatedStorage:WaitForChild("Remotes", 10)
local campaign = nil
local design = PlaneDesign.new()
local savedDesigns = {}
local messages = UI.Messages.new()
local streamer = nil
local hangar = nil
local flight = nil
local hud, airportPanel, mapView = nil, nil, nil
local flightConnections = {}
local mode = "menu"
local saveData = nil
local settings = { treeDensity = 1, music = 0.6, sound = 1 }
local cutscenePlaying = false

local function applySettings()
	WorldStreamer.TreeDensity = settings.treeDensity
	Audio.musicVolume = settings.music
	Audio.masterVolume = settings.sound
end

-- QuestAdditionUI sounds
do
	local push = messages.push
	messages.push = function(self, text, kind)
		if kind == "Mystery" then
			Audio.play("radioStatic", 0.6)
			Audio.play("mystery")
		elseif kind == "QuestCompleted" then
			Audio.play("reward")
		else
			Audio.play("radioClick")
		end
		push(self, text, kind)
	end
end

-- Ambience (FlyingAmbience): nature fades with height, earthquake rumble follows the shake.
local natureLoop = Audio.loop(Sounds.ambience.nature)
local quakeLoop = Audio.loop(Sounds.ambience.earthquake)
local birdsLoop = Audio.loop(Sounds.ambience.birds)

local function hookCampaign()
	campaign.onMessage = function(text, kind)
		messages:push(text, kind)
	end
	campaign.onReward = function(info)
		if info.money > 0 then
			Audio.play("cash")
		end
		if hud and (info.money ~= 0 or info.scrap ~= 0 or info.advancedScrap ~= 0) then
			local parts = {}
			if info.money ~= 0 then
				table.insert(parts, "💰 +" .. math.floor(info.money + 0.5))
			end
			if info.scrap ~= 0 then
				table.insert(parts, "⚙ +" .. info.scrap)
			end
			if info.advancedScrap ~= 0 then
				table.insert(parts, "✦ +" .. info.advancedScrap)
			end
			UI.toast(hud.gui, table.concat(parts, "   "), C.good)
		end
	end
end

local function save()
	if not campaign or not Remotes then
		return
	end
	local data = { campaign = campaign:serialize(), design = design:serialize(), designs = savedDesigns, settings = settings }
	task.spawn(function()
		local ok, result = pcall(function()
			return Remotes.SaveGame:InvokeServer(data)
		end)
		if not ok then
			warn("[Aviassembly] save failed:", result)
		end
	end)
end

local function load()
	if not Remotes then
		return nil
	end
	local ok, data = pcall(function()
		return Remotes.LoadGame:InvokeServer()
	end)
	if ok and type(data) == "table" and data.campaign then
		return data
	end
	return nil
end

-- ------------------------------------------------------------------------------------------- helpers
local function airportById(id)
	return id and WorldMap.airportById(id) or nil
end

local function homeBase()
	return WorldMap.Airports[1]
end

local function robloxPos(a)
	return Vector3.new(-a.position.X, 0, a.position.Z)
end

-- Plane forward in design space (cockpit forward or average engine thrust, like UpdateForwardDirection).
local function designForward(d)
	local forward = Vector3.zAxis
	local engines = Vector3.zero
	local n = 0
	for _, e in d.parts do
		if string.find(e.name, "Cock") then
			forward = e.cf.ZVector
		end
		for _, c in Parts[e.name].components do
			if c.type == "Engine" then
				local dir = if c.data.helicopterRotor == 1 then Frames.v(c.frame.up) else Frames.v(c.frame.right)
				engines += e.cf:VectorToWorldSpace(dir)
				n += 1
			end
		end
	end
	if n > 0 then
		local avg = engines / n
		if avg.Magnitude > 1e-3 and math.abs(avg.Unit.Y) <= 0.5 then
			forward = avg.Unit
		end
	end
	return forward
end

local function designLowest(d)
	local lowest = 0
	for _, e in d.parts do
		local data = Parts[e.name]
		for _, node in data.nodes do
			local c = e.cf * Frames.localPoint(Frames.v(node.center), e.flip)
			local h = node.size[2] / 2
			lowest = math.min(lowest, c.Y - h)
		end
		for _, w in data.components do
			if w.type == "Wheel" then
				local p = e.cf * Frames.localPoint(Frames.v(w.frame.pos), e.flip)
				lowest = math.min(lowest, p.Y - w.data.suspentionLength * 0.6)
			end
		end
		if data.procedural and e.fuselage then
			lowest = math.min(lowest, e.cf.Y - math.max(e.fuselage.side1.radius.Y, e.fuselage.side2.radius.Y))
		end
	end
	return lowest
end

-- GameManager.GetPlaneSpawnPoint / GetPlaneSpawnRotation
local function spawnCFrame(airport)
	local cf = WorldMap.airportCFrame(airport)
	local sp = airport.data.spawn
	local localPos = if sp then Vector3.new(sp.pos[1], sp.pos[2], sp.pos[3]) else Vector3.new(0, 0, -215)
	local localFwd = if sp then Vector3.new(sp.forward[1], sp.forward[2], sp.forward[3]) else Vector3.zAxis
	local pos = cf * localPos
	local fwd = cf:VectorToWorldSpace(localFwd)
	local ground = WorldMap.heightAt(pos)
	if airport.data.offshore then
		ground = pos.Y - 8 + 8
	end
	-- terrain might be below a runway deck; trust whichever is higher
	local rayHit = Workspace:Raycast(pos + Vector3.new(0, 200, 0), Vector3.new(0, -400, 0))
	if rayHit then
		ground = math.max(ground, rayHit.Position.Y)
	end
	-- rotate the design so its flight direction faces the runway direction
	local df = designForward(design)
	local yawDesign = math.atan2(df.X, df.Z)
	local yawSpawn = math.atan2(fwd.X, fwd.Z)
	local rot = CFrame.Angles(0, yawSpawn - yawDesign, 0)
	return CFrame.new(pos.X, ground - designLowest(design) + 0.3, pos.Z) * rot
end

-- ------------------------------------------------------------------------------------------- camera (CameraController)
local cam = { yaw = 0, pitch = 0.25, distance = nil, dragging = false, cockpit = false, smoothedForward = nil, shake = 0 }

local function updateFlightCamera(dt)
	if not flight or not flight.root.Parent then
		return
	end
	local root = flight.root
	local cf = root.CFrame
	local fwd = cf:VectorToWorldSpace(flight.localForward)
	local length = math.max(flight.planeLength, 2)
	cam.distance = cam.distance or (length * 3 + 6)
	if cam.cockpit then
		camera.CFrame = CFrame.lookAt(cf * (flight.boundsCenter + Vector3.new(0, flight.boundsHalf.Y * 0.8, 0)), cf * (flight.boundsCenter + Vector3.new(0, flight.boundsHalf.Y * 0.8, 0)) + fwd, cf.UpVector)
		return
	end
	local vel = root.AssemblyLinearVelocity
	local target = if vel.Magnitude > 8 then vel.Unit:Lerp(fwd, 0.5) else fwd
	cam.smoothedForward = if cam.smoothedForward then cam.smoothedForward:Lerp(target, math.clamp(dt * 3, 0, 1)) else target
	local flat = Vector3.new(cam.smoothedForward.X, 0, cam.smoothedForward.Z)
	if flat.Magnitude < 1e-3 then
		flat = Vector3.zAxis
	end
	local baseYaw = math.atan2(-flat.X, -flat.Z)
	local rot = CFrame.Angles(0, baseYaw + cam.yaw, 0) * CFrame.Angles(-cam.pitch, 0, 0)
	local focus = root.Position
	local pos = focus + rot * Vector3.new(0, 0, cam.distance)
	if cam.shake > 0 then
		pos += Vector3.new(math.random() - 0.5, math.random() - 0.5, math.random() - 0.5) * cam.shake * 2
	end
	camera.CFrame = CFrame.lookAt(pos, focus + Vector3.new(0, length * 0.2, 0))
	local speed = vel.Magnitude
	camera.FieldOfView = 65 + math.clamp(speed / 500, 0, 1) * 25
end

-- ------------------------------------------------------------------------------------------- input
local function readInput()
	local function key(k)
		return UserInputService:IsKeyDown(k)
	end
	local function axis(neg, pos)
		return (if key(pos) then 1 else 0) - (if key(neg) then 1 else 0)
	end
	local throttleUp = key(Enum.KeyCode.Space)
	local throttleDown = key(Enum.KeyCode.LeftControl) or key(Enum.KeyCode.RightControl) or key(Enum.KeyCode.LeftShift)
	return {
		pitch = axis(Enum.KeyCode.S, Enum.KeyCode.W),
		roll = axis(Enum.KeyCode.A, Enum.KeyCode.D),
		yaw = axis(Enum.KeyCode.Q, Enum.KeyCode.E),
		throttle = (if throttleUp then 1 else 0) - (if throttleDown then 1 else 0),
		space = throttleUp,
		retractPressed = false,
		decouplePressed = false,
		rotator = axis(Enum.KeyCode.H, Enum.KeyCode.J),
		autoSteer = true,
	}
end

-- BackgroundMusicManager / FlyingBackgroundMusic: continent music with story overrides.
local function flightMusic(position)
	local s = campaign.story
	if cutscenePlaying then
		return Sounds.music.finalSequence
	end
	if s.finalMessagePlayed and not s.playedEndCutscene then
		return Sounds.music.postFinalMessage
	end
	if s.postEarthquake and not s.finalMessagePlayed and not s.playedEndCutscene then
		return Sounds.music.postEarthquake
	end
	local continent = WorldMap.closestContinent(-position.X, position.Z)
	return Sounds.music.continents[continent.name] or Sounds.music.continents.Spawn
end

-- ------------------------------------------------------------------------------------------- modes
local enterHangar, startFlight, showMenu, playEndCutscene

local function clearFlight()
	for _, c in flightConnections do
		c:Disconnect()
	end
	flightConnections = {}
	if flight then
		flight:destroy()
		flight = nil
	end
	if hud then
		hud:destroy()
		hud = nil
	end
	if airportPanel then
		airportPanel.gui:Destroy()
		airportPanel = nil
	end
	if mapView then
		mapView.gui:Destroy()
		mapView = nil
	end
	cam.shake = 0
end

local function designsMenu()
	local gui = UI.screen("Designs", 25)
	local frame = UI.panel({ Size = UDim2.new(0, 460, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = gui })
	UI.padding(16).Parent = frame
	UI.list(nil, 8).Parent = frame
	UI.label("PLANE DESIGNS", 20, { Font = UI.FontBold, Parent = frame })
	UI.label("Saving keeps a copy of the current plane. Loading replaces the plane in the hangar; the price difference is charged or refunded.", 14, { TextColor3 = C.muted, AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), Parent = frame })
	for slot = 1, 5 do
		local row = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 36), Parent = frame })
		local d = savedDesigns[tostring(slot)]
		UI.label(string.format("Slot %d: %s", slot, if d then (#(d.parts or {}) .. " parts") else "empty"), 16, { Size = UDim2.new(1, -220, 1, 0), Parent = row })
		UI.button("Save", function()
			savedDesigns[tostring(slot)] = design:serialize()
			save()
			gui:Destroy()
			designsMenu()
		end, { Size = UDim2.new(0, 100, 0, 32), Position = UDim2.new(1, -210, 0, 2), Parent = row })
		UI.button("Load", function()
			if not d then
				return
			end
			local newDesign = PlaneDesign.deserialize(d)
			local diff = newDesign:cost() - design:cost()
			if diff > 0 and not campaign:hasMoney(diff) then
				UI.toast(gui, "Not enough money for this design", C.bad)
				return
			end
			campaign:changeMoney(-diff)
			design = newDesign
			gui:Destroy()
			enterHangar()
		end, { Size = UDim2.new(0, 100, 0, 32), Position = UDim2.new(1, -104, 0, 2), Parent = row })
	end
	UI.button("Close", function()
		gui:Destroy()
	end, { Parent = frame })
end

function enterHangar()
	clearFlight()
	if hangar then
		hangar:destroy()
		hangar = nil
	end
	mode = "hangar"
	Audio.nextBuildingTrack()
	campaign.cargo = {}
	campaign.cargoVolume = 0
	local base = homeBase()
	local origin = CFrame.new(-base.position.X, WorldMap.height(base.position.X, base.position.Z) + 3.5, base.position.Z)
	if streamer then
		streamer:setFocus(origin.Position)
	end
	hangar = Hangar.new(campaign, design, origin, {
		fly = function()
			if #design.parts == 0 then
				UI.toast(hangar.gui, "Build a plane first", C.bad)
				return
			end
			save()
			startFlight(base)
		end,
		menu = function()
			save()
			showMenu()
		end,
		designs = designsMenu,
		changed = save,
	})
end

local function gameOver(text)
	local options = {}
	local last = airportById(campaign.lastAirportId)
	if last and not last.isBase then
		table.insert(options, { "Last airport", function()
			startFlight(last)
		end })
	end
	table.insert(options, { "Hangar", function()
		enterHangar()
	end })
	UI.gameOver(text, options)
end

function startFlight(airport)
	if hangar then
		hangar:destroy()
		hangar = nil
	end
	clearFlight()
	mode = "flight"
	campaign.lastAirportId = airport.id
	local spawnPos = robloxPos(airport)
	streamer:ensureNear(spawnPos)
	local cf = spawnCFrame(airport)
	flight = Flight.new(design, cf, { campaign = campaign, infiniteFuel = campaign.creative })
	cam.distance = nil
	cam.yaw, cam.pitch = 0, 0.25

	local atAirport = nil
	local landedTimer = 0
	local outOfFuel = 0
	local autosave = 0
	local prevVelocity = Vector3.zero
	local pending = { retract = false, decouple = false }

	hud = UI.HUD.new({
		openAirport = function()
			if atAirport then
				airportPanel:open(atAirport, campaign, flight)
			end
		end,
		openMap = function() end,
		showControls = function()
			UI.modal("Controls", UI.ControlsText)
		end,
	})
	mapView = UI.MapView.new()
	airportPanel = UI.AirportPanel.new({
		refuel = function()
			flight:refuel()
		end,
		hangar = function()
			airportPanel:close()
			save()
			enterHangar()
		end,
		cargoChanged = function()
			flight:updateMass()
		end,
	})
	local function questTargets()
		local out = {}
		for _, item in campaign:activeQuests() do
			local _, target = campaign:questStage(item.quest, flight.cargoVolume)
			if target then
				table.insert(out, target)
			end
		end
		return out
	end
	hud.gui:FindFirstChild("Map (M)", true)
	local function toggleMap()
		mapView:toggle(campaign, flight.root.Position, flight.root.CFrame:VectorToWorldSpace(flight.localForward), questTargets())
	end
	for _, b in hud.gui:GetDescendants() do
		if b:IsA("TextButton") and b.Text == "Map (M)" then
			b.Activated:Connect(toggleMap)
		end
	end

	flight.onExploded = function()
		campaign.cargo = {}
		campaign.cargoVolume = 0
		task.delay(1.5, function()
			if mode == "flight" then
				gameOver("You Crashed")
			end
		end)
	end

	table.insert(flightConnections, UserInputService.InputBegan:Connect(function(input, processed)
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			cam.dragging = true
		end
		if processed then
			return
		end
		if input.KeyCode == Enum.KeyCode.G then
			pending.retract = true
		elseif input.KeyCode == Enum.KeyCode.F then
			pending.decouple = true
		elseif input.KeyCode == Enum.KeyCode.M then
			toggleMap()
		elseif input.KeyCode == Enum.KeyCode.C then
			cam.cockpit = not cam.cockpit
		elseif input.KeyCode == Enum.KeyCode.Return or input.KeyCode == Enum.KeyCode.KeypadEnter then
			if messages:isOpen() then
				messages:advance()
			elseif atAirport and not airportPanel:isOpen() then
				airportPanel:open(atAirport, campaign, flight)
			elseif airportPanel:isOpen() then
				airportPanel:close()
			end
		end
	end))
	table.insert(flightConnections, UserInputService.InputEnded:Connect(function(input)
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			cam.dragging = false
		end
	end))
	table.insert(flightConnections, UserInputService.InputChanged:Connect(function(input, processed)
		if input.UserInputType == Enum.UserInputType.MouseMovement and cam.dragging then
			cam.yaw -= input.Delta.X * 0.006
			cam.pitch = math.clamp(cam.pitch + input.Delta.Y * 0.006, -1.2, 1.3)
		elseif input.UserInputType == Enum.UserInputType.MouseWheel and not processed then
			cam.distance = math.clamp((cam.distance or 10) * (1 - input.Position.Z * 0.1), 2, 300)
		end
	end))

	table.insert(flightConnections, RunService.PreSimulation:Connect(function(dt)
		if not flight or flight.exploded or cutscenePlaying then
			return
		end
		local input = readInput()
		if airportPanel:isOpen() then
			input.throttle = math.min(input.throttle, 0)
		end
		input.retractPressed = pending.retract
		input.decouplePressed = pending.decouple
		pending.retract, pending.decouple = false, false
		flight:step(dt, input)
	end))

	table.insert(flightConnections, RunService.RenderStepped:Connect(function(dt)
		UserInputService.MouseBehavior = if cam.dragging then Enum.MouseBehavior.LockCurrentPosition else Enum.MouseBehavior.Default
		if not cutscenePlaying then
			updateFlightCamera(dt)
		end
	end))

	table.insert(flightConnections, RunService.Heartbeat:Connect(function(dt)
		if not flight or cutscenePlaying then
			return
		end
		local pos = flight.root.Position
		Audio.music(flightMusic(pos), 0.8)
		natureLoop:set(0.6 * (1 - math.clamp(pos.Y / 300, 0, 1)), 0.8)
		birdsLoop:set(0)
		streamer:setFocus(pos)
		local t = flight:telemetry()
		local ux, uz = -pos.X, pos.Z
		campaign:reveal(ux, uz, 1)

		-- airport detection (PlaneContainer.IsAtAirport)
		local closest = WorldMap.closestAirport(ux, uz)
		local horizontalSpeed = flight.root.AssemblyLinearVelocity
		horizontalSpeed = Vector3.new(horizontalSpeed.X, 0, horizontalSpeed.Z).Magnitude
		local vel = flight.root.AssemblyLinearVelocity
		local speedFactor = vel.Magnitude * (1 - (if vel.Magnitude > 1e-3 then vel.Unit.Y else 0))
		local grounded = t.grounded or (flight.hasBalloons and closest and closest.isBase)
		local nowAt = nil
		if closest and not flight.exploded and grounded and speedFactor < 25 and WorldMap.distanceToRect(closest.flatness, ux, uz) < 100 then
			nowAt = closest
		end
		atAirport = nowAt
		if atAirport then
			landedTimer += dt
			if landedTimer > 0.25 then
				landedTimer = 0
				campaign:landed(atAirport, { position = pos, speed = vel.Magnitude, airport = atAirport, maxCargoVolume = flight.cargoVolume })
				flight:updateMass()
			end
			autosave += dt
			if autosave > 120 then
				autosave = 0
				save()
			end
		elseif airportPanel:isOpen() then
			airportPanel:close()
		end

		-- cargo: perishable timers & fragile breakage
		local dv = (flight.root.AssemblyLinearVelocity - prevVelocity).Magnitude
		prevVelocity = flight.root.AssemblyLinearVelocity
		local cargoMsg = campaign:updateCargo(dt, dv, os.clock())
		if cargoMsg then
			UI.toast(hud.gui, cargoMsg, C.bad)
			flight:updateMass()
		end

		-- scrap piles
		local got = campaign:collectScrapNear(ux, uz)
		if got then
			UI.toast(hud.gui, "⚙ 5 Scrap added — research new parts at base", C.good)
		end

		-- story
		local story = campaign:updateStory({ dt = dt, position = pos, heightAboveGround = t.height, atAirport = atAirport ~= nil, exploded = flight.exploded, messageOpen = messages:isOpen() })
		cam.shake = story.shake
		quakeLoop:set(story.shake)
		if story.explode then
			flight:explodePlane()
		end
		if story.finished then
			save()
			task.spawn(playEndCutscene)
		end

		-- out of fuel (GameOver)
		local refuelHere = atAirport and (atAirport.data.refuel or atAirport.isBase)
		if vel.Magnitude < 25 and not refuelHere and t.fuel < 0.001 and t.fuelCapacity > 0 then
			outOfFuel += dt
		else
			outOfFuel = 0
		end
		if outOfFuel > 6 and not flight.exploded then
			outOfFuel = -math.huge
			gameOver("Out Of Fuel")
		end

		-- HUD
		local lines = {}
		for _, item in campaign:activeQuests() do
			local stage = campaign:questStage(item.quest, flight.cargoVolume)
			table.insert(lines, "<b>" .. item.airport.name .. "</b>: " .. (stage or item.quest.name))
		end
		hud:update(t, campaign, if #lines > 0 then table.concat(lines, "\n") else "Land at an airport to get a job.", atAirport)
	end))

	flight:refuel()
	messages:push(if campaign.creative then "Creative mode: unlimited money and every part unlocked." else "Fly to the airports, pick up cargo and deliver it. Land slowly on a runway, then press Enter for the airport menu.", "Misc")
end

-- ------------------------------------------------------------------------------------------- ending (EndCutscene)
local ENDING = {
	{ airport = "Explosion Site", text = "The radioactive materials are now securely stored in the bunker." },
	{ airport = "Waste Storage", text = "You've contained the dangers and the islands are safe again." },
	{ airport = "The Hospital", text = "Your mission is complete!" },
}

function playEndCutscene()
	if cutscenePlaying or not flight then
		return
	end
	cutscenePlaying = true
	while messages:isOpen() do
		task.wait(0.2)
	end
	flight.root.Anchored = true
	for _, e in flight.engines do
		if e.sound then
			e.sound:set(0)
		end
	end
	Audio.music(Sounds.music.finalSequence, 1)
	local gui = UI.screen("Ending", 60)
	local fader = UI.new("Frame", { BackgroundColor3 = Color3.new(0, 0, 0), BackgroundTransparency = 1, Size = UDim2.fromScale(1, 1), ZIndex = 5, Parent = gui })
	local caption = UI.label("", 34, { Font = UI.FontBold, TextXAlignment = Enum.TextXAlignment.Center, AnchorPoint = Vector2.new(0.5, 1), Position = UDim2.new(0.5, 0, 1, -120), Size = UDim2.new(0.8, 0, 0, 90), TextStrokeTransparency = 0.4, Visible = false, ZIndex = 3, Parent = gui })
	local band = UI.new("Frame", { BackgroundColor3 = Color3.new(0, 0, 0), BackgroundTransparency = 0.5, AnchorPoint = Vector2.new(0, 1), Position = UDim2.new(0, 0, 1, -100), Size = UDim2.new(1, 0, 0, 130), Visible = false, ZIndex = 2, Parent = gui })
	local continue = false
	local continueButton = UI.button("<b>Continue</b>", function()
		continue = true
	end, { AnchorPoint = Vector2.new(0.5, 1), Position = UDim2.new(0.5, 0, 1, -30), Size = UDim2.new(0, 220, 0, 46), BackgroundColor3 = C.accent, TextColor3 = C.text, Visible = false, ZIndex = 4, Parent = gui })
	local function fade(target)
		local t0 = fader.BackgroundTransparency
		for i = 1, 30 do
			fader.BackgroundTransparency = t0 + (target - t0) * (i / 30)
			task.wait(1 / 30)
		end
	end
	local drift = nil
	for i, shot in ENDING do
		fade(0)
		local a = WorldMap.findAirport(shot.airport)
		if a then
			local target = Vector3.new(-a.position.X, WorldMap.airportHeight(a), a.position.Z)
			-- airport.position + up * 40 + forward * 200, looking at the airport, then drifting back
			local start = target + Vector3.new(0, 40, 200)
			streamer:ensureNear(target)
			local camCF = CFrame.lookAt(start, target)
			camera.CFrame = camCF
			if drift then
				drift:Disconnect()
			end
			drift = RunService.RenderStepped:Connect(function(dt)
				camCF = camCF - camCF.LookVector * dt * 10
				camera.CFrame = camCF
			end)
		end
		caption.Text = shot.text
		caption.Visible, band.Visible = true, true
		continueButton.Visible = i == #ENDING
		task.wait(2)
		fade(1)
		if i < #ENDING then
			task.wait(4)
		end
	end
	while not continue do
		task.wait(0.1)
	end
	fade(0)
	if drift then
		drift:Disconnect()
	end
	caption.Visible, band.Visible, continueButton.Visible = false, false, false
	if flight then
		flight.root.Anchored = false
	end
	cutscenePlaying = false
	task.wait(2)
	fade(1)
	gui:Destroy()
	if hud then
		UI.toast(hud.gui, "Random quests unlocked — you can now get random quests at every airport", C.good)
	end
end

-- ------------------------------------------------------------------------------------------- settings
local function settingsMenu()
	local gui = UI.screen("Settings", 55)
	local frame = UI.panel({ Size = UDim2.new(0, 460, 0, 0), AutomaticSize = Enum.AutomaticSize.Y, AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = gui })
	UI.padding(16).Parent = frame
	UI.list(nil, 10).Parent = frame
	UI.label("SETTINGS", 20, { Font = UI.FontBold, Parent = frame })
	local function row(label, options, get, set)
		local r = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(1, 0, 0, 34), Parent = frame })
		UI.label(label, 15, { Size = UDim2.new(0, 150, 1, 0), Parent = r })
		for i, o in options do
			local b = UI.button(o[1], function()
				set(o[2])
				applySettings()
				gui:Destroy()
				settingsMenu()
			end, { Size = UDim2.new(0, 66, 0, 30), Position = UDim2.new(0, 150 + (i - 1) * 70, 0, 2), Parent = r })
			if get() == o[2] then
				b.BackgroundColor3 = C.accent
				b.TextColor3 = C.text
			end
		end
	end
	row("Trees & rocks", { { "Off", 0 }, { "Low", 0.25 }, { "Half", 0.5 }, { "Full", 1 } }, function()
		return settings.treeDensity
	end, function(v)
		settings.treeDensity = v
	end)
	row("Music", { { "Off", 0 }, { "Low", 0.3 }, { "Med", 0.6 }, { "High", 1 } }, function()
		return settings.music
	end, function(v)
		settings.music = v
	end)
	row("Sound", { { "Off", 0 }, { "Low", 0.4 }, { "Med", 0.7 }, { "High", 1 } }, function()
		return settings.sound
	end, function(v)
		settings.sound = v
	end)
	UI.label("Tree changes apply to newly loaded areas. \"Full\" matches the original density; lower it if the game runs slowly.", 13, { TextColor3 = C.muted, AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), Parent = frame })
	if not Audio.available() then
		UI.label('<font color="#E0A040">Sounds not imported yet — see the README (Importing sounds).</font>', 13, { AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), Parent = frame })
	end
	UI.button("Close", function()
		gui:Destroy()
		save()
	end, { Parent = frame })
end

-- ------------------------------------------------------------------------------------------- menu
function showMenu()
	if hangar then
		hangar:destroy()
		hangar = nil
	end
	clearFlight()
	mode = "menu"
	Audio.music(Sounds.music.menu, 0.65)
	birdsLoop:set(0.2)
	natureLoop:set(0)
	local gui = UI.screen("Menu", 50)
	local back = UI.new("Frame", { BackgroundColor3 = Color3.fromRGB(20, 40, 60), BackgroundTransparency = 0.25, Size = UDim2.new(1, 0, 1, 0), Parent = gui })
	local box = UI.new("Frame", { BackgroundTransparency = 1, Size = UDim2.new(0, 420, 0, 520), AnchorPoint = Vector2.new(0.5, 0.5), Position = UDim2.new(0.5, 0, 0.5, 0), Parent = back })
	UI.list(nil, 12, Enum.HorizontalAlignment.Center).Parent = box
	UI.label("AVIASSEMBLY", 56, { Font = UI.FontBold, TextXAlignment = Enum.TextXAlignment.Center, Size = UDim2.new(1, 0, 0, 70), Parent = box })
	UI.label("Build planes. Fly cargo. Save the islands.", 16, { TextXAlignment = Enum.TextXAlignment.Center, TextColor3 = C.muted, Parent = box })
	local function start(c)
		campaign = c
		hookCampaign()
		gui:Destroy()
		if not streamer then
			streamer = WorldStreamer.new(campaign)
		else
			streamer.campaign = campaign
		end
		enterHangar()
	end
	if saveData then
		UI.button("<b>Continue</b>", function()
			local c = Campaign.deserialize(saveData.campaign)
			design = PlaneDesign.deserialize(saveData.design)
			savedDesigns = saveData.designs or {}
			start(c)
		end, { Size = UDim2.new(0, 300, 0, 48), BackgroundColor3 = C.accent, TextColor3 = C.text, Parent = box })
	end
	for _, diff in { "Relaxed", "Normal", "Hard" } do
		UI.button("New campaign — " .. diff, function()
			design = PlaneDesign.new()
			start(Campaign.new(diff, false))
		end, { Size = UDim2.new(0, 300, 0, 44), Parent = box })
	end
	UI.button("Creative mode", function()
		design = PlaneDesign.new()
		start(Campaign.new("Normal", true))
	end, { Size = UDim2.new(0, 300, 0, 44), Parent = box })
	UI.button("Controls", function()
		UI.modal("Controls", UI.ControlsText)
	end, { Size = UDim2.new(0, 300, 0, 40), Parent = box })
	UI.button("Settings", settingsMenu, { Size = UDim2.new(0, 300, 0, 40), Parent = box })
	if not PartVisual.hasImportedMeshes() then
		UI.label('<font color="#E0A040">3D models not imported yet — parts show as coloured blocks. See the README to import the .glb files.</font>', 14, { TextXAlignment = Enum.TextXAlignment.Center, AutomaticSize = Enum.AutomaticSize.Y, Size = UDim2.new(1, 0, 0, 0), Parent = box })
	end
	camera.CFrame = CFrame.lookAt(Vector3.new(60, 40, -120), Vector3.new(0, 0, 0))
end

-- ------------------------------------------------------------------------------------------- boot
saveData = load()
if saveData and type(saveData.settings) == "table" then
	for k, v in saveData.settings do
		settings[k] = v
	end
end
applySettings()
showMenu()
