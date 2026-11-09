--!strict
-- Sound effects, engine loops, ambience and music (AudioManager, EngineSound, FlyingAmbience,
-- BackgroundMusicManager, FlyingBackgroundMusic).
-- Roblox needs audio uploaded to your account, so the sounds are imported once in Studio (see README) as Sound
-- objects named after the original files, anywhere under ReplicatedStorage (e.g. ReplicatedStorage.AviassemblySounds).
-- Everything is silent until then.
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local SoundService = game:GetService("SoundService")
local Workspace = game:GetService("Workspace")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Sounds = require(Shared.Data.Sounds)

local Audio = {}
Audio.masterVolume = 1
Audio.musicVolume = 0.6

local index: { [string]: Sound }? = nil

local function normalize(name: string): string
	return (name:lower():gsub("%.ogg$", ""):gsub("[%s_%-]", ""))
end

function Audio.refresh()
	local map: { [string]: Sound } = {}
	for _, d in ReplicatedStorage:GetDescendants() do
		if d:IsA("Sound") then
			map[normalize(d.Name)] = d
		end
	end
	index = map
end

function Audio.find(name: string): Sound?
	if not index then
		Audio.refresh()
	end
	return (index :: any)[normalize(name)]
end

function Audio.available(): boolean
	if not index then
		Audio.refresh()
	end
	return next(index :: any) ~= nil
end

local holder = Instance.new("Folder")
holder.Name = "AviassemblyAudio"
holder.Parent = SoundService

-- One-shot UI / game sound by its key in Data/Sounds.ui (or a raw file name).
function Audio.play(key: string, volume: number?)
	local file = Sounds.ui[key] or key
	local source = Audio.find(file)
	if not source then
		return
	end
	local s = source:Clone()
	s.Volume = (volume or Sounds.volumes[key] or 1) * Audio.masterVolume
	s.Looped = false
	s.Parent = holder
	s:Play()
	s.Ended:Connect(function()
		s:Destroy()
	end)
	task.delay(30, function()
		if s.Parent then
			s:Destroy()
		end
	end)
end

-- A looping sound whose volume/pitch is driven every frame.
export type Loop = { sound: Sound?, set: (self: any, volume: number, pitch: number?) -> (), stop: (self: any) -> () }

function Audio.loop(file: string, parent: Instance?): Loop
	local source = Audio.find(file)
	local s: Sound? = nil
	if source then
		s = source:Clone()
		local snd = s :: Sound
		snd.Looped = true
		snd.Volume = 0
		snd.Parent = parent or holder
		snd:Play()
	end
	return {
		sound = s,
		set = function(self, volume: number, pitch: number?)
			if self.sound then
				self.sound.Volume = math.clamp(volume, 0, 3) * Audio.masterVolume
				if pitch then
					self.sound.PlaybackSpeed = math.max(pitch, 0.05)
				end
			end
		end,
		stop = function(self)
			if self.sound then
				self.sound:Destroy()
				self.sound = nil
			end
		end,
	}
end

-- ------------------------------------------------------------------------------------------- music
local current: Sound? = nil
local currentName: string? = nil
local fading: { Sound } = {}

function Audio.music(file: string?, volume: number?)
	if file == currentName then
		return
	end
	if current then
		table.insert(fading, current)
	end
	current, currentName = nil, file
	if not file then
		return
	end
	local source = Audio.find(file)
	if not source then
		return
	end
	local s = source:Clone()
	s.Looped = true
	s.Volume = 0
	s:SetAttribute("Target", volume or 1)
	s.Parent = holder
	s:Play()
	current = s
end

-- Building music cycles through the original playlist.
local buildingIndex = 0
function Audio.nextBuildingTrack()
	buildingIndex = buildingIndex % #Sounds.music.building + 1
	Audio.music(Sounds.music.building[buildingIndex], 0.8)
end

game:GetService("RunService").Heartbeat:Connect(function(dt)
	if current then
		local target = (current:GetAttribute("Target") or 1) * Audio.musicVolume * Audio.masterVolume
		current.Volume += (target - current.Volume) * math.clamp(dt * 0.8, 0, 1)
	end
	for i = #fading, 1, -1 do
		local s = fading[i]
		s.Volume -= dt * 0.5
		if s.Volume <= 0.01 then
			s:Destroy()
			table.remove(fading, i)
		end
	end
end)

local _ = Workspace
return Audio
