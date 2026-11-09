using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;

#if UNITY_WEBGL
// WebGL port: there is no Steam client in the browser. This keeps the same public surface the
// rest of the game uses, with Workshop support reported as unavailable (steamworksInitialized == false),
// which the existing UI already handles by disabling the "Upload to Workshop" button.
public class SteamworksManager : Singleton<SteamworksManager>
{
	public Action PlanesUpdated;

	public Action<PublishResult> ResultAvailable;

	public bool steamworksInitialized { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		steamworksInitialized = false;
	}

	public void UploadPlane(string planeFilePath, string previewPath, string name, string description, string tag, ProgressClass progress)
	{
		ResultAvailable?.Invoke(new PublishResult
		{
			Result = Result.Fail
		});
	}

	public void GetSubscibedPlanes()
	{
		PlanesUpdated?.Invoke();
	}

	public void UnsubscribeFromItem(string directoryPath)
	{
	}

	public List<string> GetPlaneDirectories()
	{
		return new List<string>();
	}
}
#else
public class SteamworksManager : Singleton<SteamworksManager>
{
	public Action PlanesUpdated;

	public Action<PublishResult> ResultAvailable;

	public List<Item> items = new List<Item>();

	private bool isRefreshing;

	private bool refreshQueued;

	public bool steamworksInitialized { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		uint appid = 2660460u;
		if (SteamClient.RestartAppIfNecessary(appid))
		{
			Application.Quit();
			return;
		}
		try
		{
			SteamClient.Init(appid, asyncCallbacks: false);
			steamworksInitialized = true;
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			steamworksInitialized = false;
		}
		if (steamworksInitialized)
		{
			SteamUser.OnSteamServersConnected += OnDisconnect;
			SteamUGC.OnItemSubscribed += HandleLiveSubscription;
			GetSubscibedPlanes();
		}
	}

	public async void UploadPlane(string planeFilePath, string previewPath, string name, string description, string tag, ProgressClass progress)
	{
		string staging = Path.Combine(Application.temporaryCachePath, "ws_upload_" + Guid.NewGuid().ToString("N"));
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(planeFilePath);
		long num = 1048576L;
		try
		{
			Directory.CreateDirectory(staging);
			File.Copy(planeFilePath, Path.Combine(staging, fileNameWithoutExtension + ".planedesign"), overwrite: true);
			if (new DirectoryInfo(staging).EnumerateFiles("*", SearchOption.AllDirectories).Sum((FileInfo f) => f.Length) > num)
			{
				Debug.LogError("file size too big");
				PublishResult obj = new PublishResult
				{
					Result = Result.LimitExceeded
				};
				ResultAvailable?.Invoke(obj);
			}
			else
			{
				PublishResult obj2 = await Editor.NewCommunityFile.WithTitle(name).WithTag(tag).WithDescription(description)
					.WithContent(staging)
					.WithPreviewFile(previewPath)
					.WithPublicVisibility()
					.SubmitAsync(progress);
				ResultAvailable?.Invoke(obj2);
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		finally
		{
			try
			{
				if (Directory.Exists(staging))
				{
					Directory.Delete(staging, recursive: true);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Couldn't clean staging dir: " + ex.Message);
			}
		}
	}

	public async void GetSubscibedPlanes()
	{
		if (isRefreshing)
		{
			refreshQueued = true;
			return;
		}
		isRefreshing = true;
		try
		{
			do
			{
				refreshQueued = false;
				List<Item> newItems = new List<Item>();
				Query query = Query.All.WhereUserSubscribed(SteamClient.SteamId);
				int i = 1;
				while (true)
				{
					ResultPage? resultPage = await query.GetPageAsync(i);
					if (!resultPage.HasValue || resultPage.Value.ResultCount == 0 || i > 1000)
					{
						break;
					}
					foreach (Item entry in resultPage.Value.Entries)
					{
						newItems.Add(entry);
						await SteamUGC.DownloadAsync(entry.Id);
					}
					i++;
				}
				items = newItems;
				if (PlanesUpdated != null)
				{
					PlanesUpdated();
				}
			}
			while (refreshQueued);
		}
		finally
		{
			isRefreshing = false;
		}
	}

	private async void HandleLiveSubscription(AppId appId, PublishedFileId publishedFileId)
	{
		GetSubscibedPlanes();
	}

	public void UnsubscribeFromItem(string directoryPath)
	{
		directoryPath = Path.GetDirectoryName(directoryPath);
		for (int i = 0; i < items.Count; i++)
		{
			if (directoryPath == items[i].Directory)
			{
				items[i].Unsubscribe();
			}
		}
	}

	public List<string> GetPlaneDirectories()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].IsInstalled && !items[i].IsDownloading && !items[i].NeedsUpdate && items[i].IsSubscribed)
			{
				list.Add(items[i].Directory);
			}
		}
		return list;
	}

	private void OnDisconnect()
	{
		steamworksInitialized = false;
	}

	private void Update()
	{
		if (steamworksInitialized)
		{
			SteamClient.RunCallbacks();
		}
	}

	private void OnDestroy()
	{
		if (!(Singleton<SteamworksManager>.Instance != this))
		{
			SteamClient.Shutdown();
		}
	}
}
#endif
