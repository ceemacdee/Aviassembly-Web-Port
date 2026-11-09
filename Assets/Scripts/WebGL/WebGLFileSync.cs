using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// WebGL port: makes sure files written to Application.persistentDataPath (save games, plane designs,
// thumbnails, settings, keybinds) are persisted to the browser's IndexedDB. No-op on other platforms.
public static class WebGLFileSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern void WebGLFileSync_Flush();

	[DllImport("__Internal")]
	private static extern void WebGLFileSync_InstallUnloadHandlers();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Install()
	{
		WebGLFileSync_InstallUnloadHandlers();
	}
#endif

	public static void Flush()
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		WebGLFileSync_Flush();
#endif
	}
}
