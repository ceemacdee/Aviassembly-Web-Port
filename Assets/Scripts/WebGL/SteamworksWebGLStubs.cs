// WebGL port: Facepunch.Steamworks is a native Win64 plugin and cannot run in a browser.
// The plugin is excluded from WebGL builds via a define constraint (!UNITY_WEBGL) on its .meta,
// and these minimal stand-ins keep the game scripts that reference Steam types compiling.
// Steam features (Workshop, achievements, overlay) simply report "unavailable" on the web.
#if UNITY_WEBGL
using System;

namespace Steamworks
{
	public enum Result
	{
		None = 0,
		OK = 1,
		Fail = 2,
		LimitExceeded = 25
	}

	public static class SteamUtils
	{
		public static bool IsOverlayEnabled => false;
	}

	public static class SteamFriends
	{
		public static void OpenWebOverlay(string url, bool modal = false)
		{
			UnityEngine.Application.OpenURL(url);
		}
	}
}

namespace Steamworks.Data
{
	public struct PublishedFileId
	{
		public ulong Value;

		public override string ToString()
		{
			return Value.ToString();
		}
	}

	public struct Achievement
	{
		private const string PrefsKeyPrefix = "achievement_";

		public string Identifier { get; }

		public Achievement(string name)
		{
			Identifier = name;
		}

		// No Steam on the web: remember the unlock locally so it is only "triggered" once per browser.
		public bool Trigger(bool apply = true)
		{
			UnityEngine.PlayerPrefs.SetInt(PrefsKeyPrefix + Identifier, 1);
			UnityEngine.PlayerPrefs.Save();
			UnityEngine.Debug.Log("Achievement unlocked (web): " + Identifier);
			return true;
		}
	}
}

namespace Steamworks.Ugc
{
	public struct PublishResult
	{
		public Steamworks.Result Result;

		public Steamworks.Data.PublishedFileId FileId;

		public bool NeedsWorkshopAgreement;

		public bool Success => Result == Steamworks.Result.OK;
	}
}
#endif
