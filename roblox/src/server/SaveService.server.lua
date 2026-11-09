-- Saves each player's campaign, current plane and saved designs with DataStoreService.
-- In Studio, enable Game Settings > Security > "Enable Studio Access to API Services" to test saving.
local DataStoreService = game:GetService("DataStoreService")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local HttpService = game:GetService("HttpService")

local remotes = Instance.new("Folder")
remotes.Name = "Remotes"
remotes.Parent = ReplicatedStorage
local loadFn = Instance.new("RemoteFunction")
loadFn.Name = "LoadGame"
loadFn.Parent = remotes
local saveFn = Instance.new("RemoteFunction")
saveFn.Name = "SaveGame"
saveFn.Parent = remotes

local ok, store = pcall(function()
	return DataStoreService:GetDataStore("AviassemblySave_v1")
end)
if not ok then
	warn("[Aviassembly] DataStore unavailable (" .. tostring(store) .. "). Progress will not be saved in this session.")
	store = nil
end

local cache = {}
local MAX_BYTES = 3_900_000

local function key(player)
	return "player_" .. player.UserId
end

loadFn.OnServerInvoke = function(player)
	if cache[player] then
		return cache[player]
	end
	if not store then
		return nil
	end
	local success, data = pcall(function()
		return store:GetAsync(key(player))
	end)
	if not success then
		warn("[Aviassembly] load failed:", data)
		return nil
	end
	cache[player] = data
	return data
end

local lastWrite = {}
saveFn.OnServerInvoke = function(player, data)
	if type(data) ~= "table" then
		return false, "bad data"
	end
	local encoded = HttpService:JSONEncode(data)
	if #encoded > MAX_BYTES then
		return false, "save too large"
	end
	cache[player] = data
	if not store then
		return false, "DataStore unavailable"
	end
	-- DataStore write budget: at most one write every 6 seconds per player.
	local now = os.clock()
	if lastWrite[player] and now - lastWrite[player] < 6 then
		return true, "queued"
	end
	lastWrite[player] = now
	local success, err = pcall(function()
		store:SetAsync(key(player), data)
	end)
	return success, err
end

Players.PlayerRemoving:Connect(function(player)
	local data = cache[player]
	if data and store then
		pcall(function()
			store:SetAsync(key(player), data)
		end)
	end
	cache[player] = nil
	lastWrite[player] = nil
end)

game:BindToClose(function()
	if not store then
		return
	end
	for player, data in cache do
		pcall(function()
			store:SetAsync(key(player), data)
		end)
	end
end)
