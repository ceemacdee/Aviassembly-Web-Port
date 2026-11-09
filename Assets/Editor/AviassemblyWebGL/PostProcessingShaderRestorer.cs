// WebGL port helper.
//
// The AssetRipper export only contains placeholder ("//DummyShaderTextExporter") versions of the Post-Processing
// Stack v2 shaders, because the shipped build contains compiled shaders only. Those shaders are Unity's own,
// publicly available in the com.unity.postprocessing package, so this tool downloads that package from Unity's
// package registry and writes the real shader sources into the existing Hidden_PostProcessing_*.shader files.
// Their .meta GUIDs are untouched, so PostProcessResources.asset keeps referencing them.
//
// The package's include files (.hlsl/.cginc) are extracted to Assets/PostProcessingShaderSources and the shaders'
// relative #include paths are rewritten to point there. Package .shader files are NOT copied as separate assets,
// which would create duplicate shader names.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class PostProcessingShaderRestorer
{
	public const string PackageName = "com.unity.postprocessing";

	public const string PreferredVersion = "3.4.0";

	private const string RegistryUrl = "https://packages.unity.com/";

	private const string FallbackTarballUrl = "https://download.packages.unity.com/com.unity.postprocessing/-/com.unity.postprocessing-3.4.0.tgz";

	private const string ShaderFolder = "Assets/Shader";

	private const string PlaceholderShaderPattern = "Hidden_PostProcessing_*.shader";

	public const string IncludeRoot = "Assets/PostProcessingShaderSources";

	private const string PackageShaderPrefix = "package/PostProcessing/Shaders/";

	private const string DummyMarker = "//DummyShaderTextExporter";

	[MenuItem("Aviassembly/WebGL/Restore Post-Processing Shaders (download)", priority = 10)]
	public static void RestoreFromMenu()
	{
		if (Restore(out string message))
		{
			EditorUtility.DisplayDialog("Post-processing shaders", message, "OK");
		}
		else
		{
			EditorUtility.DisplayDialog("Post-processing shaders", message + "\n\nYou can also use 'Restore Post-Processing Shaders (from .tgz)' with a manually downloaded com.unity.postprocessing package.", "OK");
		}
	}

	[MenuItem("Aviassembly/WebGL/Restore Post-Processing Shaders (from .tgz)", priority = 11)]
	public static void RestoreFromLocalFile()
	{
		string path = EditorUtility.OpenFilePanel("Select com.unity.postprocessing .tgz", "", "tgz");
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		bool ok = RestoreFromTarball(File.ReadAllBytes(path), out string message);
		EditorUtility.DisplayDialog("Post-processing shaders", message, "OK");
		if (!ok)
		{
			Debug.LogError(message);
		}
	}

	public static bool HasPlaceholderShaders()
	{
		if (!Directory.Exists(ShaderFolder))
		{
			return false;
		}
		foreach (string file in Directory.GetFiles(ShaderFolder, PlaceholderShaderPattern))
		{
			if (File.ReadAllText(file).Contains(DummyMarker))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Restore(out string message)
	{
		byte[] tarball;
		try
		{
			tarball = DownloadPackageTarball();
		}
		catch (Exception ex)
		{
			message = "Could not download " + PackageName + ": " + ex.Message;
			Debug.LogError(message);
			return false;
		}
		return RestoreFromTarball(tarball, out message);
	}

	public static bool RestoreFromTarball(byte[] tarball, out string message)
	{
		Dictionary<string, byte[]> entries;
		try
		{
			entries = ReadTarGz(tarball);
		}
		catch (Exception ex)
		{
			message = "Could not read the package archive: " + ex.Message;
			return false;
		}

		var packageShaders = new Dictionary<string, KeyValuePair<string, string>>();
		int includeCount = 0;
		foreach (KeyValuePair<string, byte[]> entry in entries)
		{
			if (!entry.Key.StartsWith(PackageShaderPrefix, StringComparison.Ordinal))
			{
				continue;
			}
			string relative = entry.Key.Substring(PackageShaderPrefix.Length);
			string extension = Path.GetExtension(relative).ToLowerInvariant();
			if (extension == ".hlsl" || extension == ".cginc")
			{
				string destination = Path.Combine(IncludeRoot, relative).Replace('\\', '/');
				Directory.CreateDirectory(Path.GetDirectoryName(destination));
				string text = RewriteAbsolutePackageIncludes(Encoding.UTF8.GetString(entry.Value));
				File.WriteAllText(destination, text);
				includeCount++;
			}
			else if (extension == ".shader")
			{
				string text = Encoding.UTF8.GetString(entry.Value);
				Match match = Regex.Match(text, "Shader\\s+\"([^\"]+)\"");
				if (match.Success)
				{
					packageShaders[match.Groups[1].Value] = new KeyValuePair<string, string>(entry.Key, text);
				}
			}
		}
		if (packageShaders.Count == 0)
		{
			message = "The archive does not look like com.unity.postprocessing (no shaders under " + PackageShaderPrefix + ").";
			return false;
		}

		int replaced = 0;
		var missing = new List<string>();
		foreach (string file in Directory.GetFiles(ShaderFolder, PlaceholderShaderPattern))
		{
			string current = File.ReadAllText(file);
			Match match = Regex.Match(current, "Shader\\s+\"([^\"]+)\"");
			if (!match.Success)
			{
				continue;
			}
			if (!packageShaders.TryGetValue(match.Groups[1].Value, out KeyValuePair<string, string> source))
			{
				missing.Add(match.Groups[1].Value);
				continue;
			}
			string sourceDirectory = source.Key.Substring(0, source.Key.LastIndexOf('/') + 1);
			string rewritten = RewriteRelativeIncludes(source.Value, sourceDirectory, entries);
			File.WriteAllText(file, "// Restored from " + PackageName + " by PostProcessingShaderRestorer (WebGL port).\n" + rewritten);
			replaced++;
		}

		AssetDatabase.Refresh();
		message = "Restored " + replaced + " post-processing shaders and " + includeCount + " include files.";
		if (missing.Count > 0)
		{
			message += "\nNot found in the package: " + string.Join(", ", missing);
		}
		Debug.Log(message);
		return replaced > 0;
	}

	private static string RewriteRelativeIncludes(string text, string sourceDirectory, Dictionary<string, byte[]> entries)
	{
		text = RewriteAbsolutePackageIncludes(text);
		return Regex.Replace(text, "#include\\s+\"([^\"]+)\"", match =>
		{
			string path = match.Groups[1].Value;
			if (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal))
			{
				return match.Value;
			}
			string resolved = NormalizePath(sourceDirectory + path);
			if (entries.ContainsKey(resolved) && resolved.StartsWith(PackageShaderPrefix, StringComparison.Ordinal))
			{
				return "#include \"" + IncludeRoot + "/" + resolved.Substring(PackageShaderPrefix.Length) + "\"";
			}
			return match.Value;
		});
	}

	private static string RewriteAbsolutePackageIncludes(string text)
	{
		return text.Replace("Packages/" + PackageName + "/PostProcessing/Shaders/", IncludeRoot + "/");
	}

	private static string NormalizePath(string path)
	{
		var parts = new List<string>();
		foreach (string part in path.Replace('\\', '/').Split('/'))
		{
			if (part == "..")
			{
				if (parts.Count > 0)
				{
					parts.RemoveAt(parts.Count - 1);
				}
			}
			else if (part != "." && part.Length > 0)
			{
				parts.Add(part);
			}
		}
		return string.Join("/", parts);
	}

	private static byte[] DownloadPackageTarball()
	{
		string tarballUrl = FallbackTarballUrl;
		try
		{
			string metadata = Encoding.UTF8.GetString(Download(RegistryUrl + PackageName));
			Match preferred = Regex.Match(metadata, "\"tarball\"\\s*:\\s*\"([^\"]*" + Regex.Escape(PackageName + "-" + PreferredVersion + ".tgz") + ")\"");
			if (preferred.Success)
			{
				tarballUrl = preferred.Groups[1].Value;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Package metadata lookup failed (" + ex.Message + "), trying " + FallbackTarballUrl);
		}
		return Download(tarballUrl);
	}

	private static byte[] Download(string url)
	{
		using (UnityWebRequest request = UnityWebRequest.Get(url))
		{
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();
			while (!operation.isDone)
			{
				if (EditorUtility.DisplayCancelableProgressBar("Downloading", url, request.downloadProgress))
				{
					request.Abort();
					EditorUtility.ClearProgressBar();
					throw new OperationCanceledException("cancelled");
				}
				System.Threading.Thread.Sleep(20);
			}
			EditorUtility.ClearProgressBar();
			if (request.result != UnityWebRequest.Result.Success)
			{
				throw new IOException(url + ": " + request.error);
			}
			return request.downloadHandler.data;
		}
	}

	// Minimal ustar/pax reader for npm-style .tgz packages.
	public static Dictionary<string, byte[]> ReadTarGz(byte[] tgz)
	{
		var result = new Dictionary<string, byte[]>();
		byte[] tar;
		using (var input = new GZipStream(new MemoryStream(tgz), CompressionMode.Decompress))
		using (var output = new MemoryStream())
		{
			input.CopyTo(output);
			tar = output.ToArray();
		}
		int offset = 0;
		string paxPath = null;
		while (offset + 512 <= tar.Length)
		{
			if (IsZeroBlock(tar, offset))
			{
				break;
			}
			string name = ReadString(tar, offset, 100);
			long size = Convert.ToInt64(ReadString(tar, offset + 124, 12).Trim().Trim('\0').PadLeft(1, '0'), 8);
			char type = (char)tar[offset + 156];
			string prefix = ReadString(tar, offset + 345, 155);
			if (prefix.Length > 0)
			{
				name = prefix + "/" + name;
			}
			int dataOffset = offset + 512;
			if (type == 'x')
			{
				string pax = Encoding.UTF8.GetString(tar, dataOffset, (int)size);
				Match path = Regex.Match(pax, "\\d+ path=([^\\n]*)\\n");
				paxPath = path.Success ? path.Groups[1].Value : null;
			}
			else if (type == '0' || type == '\0')
			{
				byte[] data = new byte[size];
				Buffer.BlockCopy(tar, dataOffset, data, 0, (int)size);
				result[NormalizePath(paxPath ?? name)] = data;
				paxPath = null;
			}
			else if (type != 'g')
			{
				paxPath = null;
			}
			offset = dataOffset + (int)((size + 511) / 512 * 512);
		}
		return result;
	}

	private static bool IsZeroBlock(byte[] data, int offset)
	{
		for (int i = 0; i < 512; i++)
		{
			if (data[offset + i] != 0)
			{
				return false;
			}
		}
		return true;
	}

	private static string ReadString(byte[] data, int offset, int length)
	{
		int end = offset;
		while (end < offset + length && data[end] != 0)
		{
			end++;
		}
		return Encoding.UTF8.GetString(data, offset, end - offset);
	}
}
