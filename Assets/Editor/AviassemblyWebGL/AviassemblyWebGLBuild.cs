// WebGL port helper: one-click configuration and build.
//   Aviassembly > WebGL > Configure Player Settings
//   Aviassembly > WebGL > Build WebGL
// Command line:
//   Unity -batchmode -quit -projectPath <project> -executeMethod AviassemblyWebGLBuild.BuildFromCommandLine [-buildPath <dir>]
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class AviassemblyWebGLBuild
{
	public const string DefaultBuildPath = "Builds/WebGL";

	private const string TemplateName = "PROJECT:Aviassembly";

	[MenuItem("Aviassembly/WebGL/Configure Player Settings", priority = 0)]
	public static void ConfigurePlayerSettings()
	{
		// WebGL 2 only: the project renders in linear colour space, which WebGL 1 cannot do.
		PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
		PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });

		// Gzip with a JS decompression fallback works on any static web host without server configuration.
		PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
		PlayerSettings.WebGL.decompressionFallback = true;
		PlayerSettings.WebGL.dataCaching = true;
		PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace; // full stack traces while the port is being debugged
		PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
		PlayerSettings.WebGL.threadsSupport = false;
		PlayerSettings.WebGL.nameFilesAsHashes = false;
		PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
		PlayerSettings.WebGL.initialMemorySize = 256;
		PlayerSettings.WebGL.maximumMemorySize = 4096;
		PlayerSettings.WebGL.template = TemplateName;
		PlayerSettings.runInBackground = true;

		// Managed code stripping can remove types only reached through serialized data in this decompiled
		// project, so keep it at the minimum level.
		PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
		PlayerSettings.stripEngineCode = false;

		AssetDatabase.SaveAssets();
		Debug.Log("Aviassembly: WebGL player settings configured.");
	}

	[MenuItem("Aviassembly/WebGL/Build WebGL", priority = 1)]
	public static void BuildFromMenu()
	{
		string path = EditorUtility.SaveFolderPanel("WebGL build folder", Path.GetFullPath(DefaultBuildPath), "");
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		Build(path, interactive: true);
	}

	public static void BuildFromCommandLine()
	{
		string[] args = Environment.GetCommandLineArgs();
		string path = DefaultBuildPath;
		int index = Array.IndexOf(args, "-buildPath");
		if (index >= 0 && index + 1 < args.Length)
		{
			path = args[index + 1];
		}
		bool ok = Build(path, interactive: false);
		EditorApplication.Exit(ok ? 0 : 1);
	}

	// For Unity Build Automation: set "Pre-Export Method Name" to AviassemblyWebGLBuild.PreExport
	// so cloud builds restore the post-processing shaders and use the WebGL settings.
	public static void PreExport()
	{
		if (PostProcessingShaderRestorer.HasPlaceholderShaders() && !PostProcessingShaderRestorer.Restore(out string message))
		{
			Debug.LogError("Aviassembly: " + message);
		}
		ConfigurePlayerSettings();
	}

	public static bool Build(string outputPath, bool interactive)
	{
		if (PostProcessingShaderRestorer.HasPlaceholderShaders())
		{
			Debug.Log("Aviassembly: post-processing shaders are still placeholders, restoring them from " + PostProcessingShaderRestorer.PackageName + "...");
			if (!PostProcessingShaderRestorer.Restore(out string message))
			{
				Debug.LogError("Aviassembly: " + message + " Building anyway; post-processing will not render correctly.");
				if (interactive && !EditorUtility.DisplayDialog("Post-processing shaders", message + "\n\nBuild anyway? Post-processing (vignette, colour grading, AO) will be wrong.", "Build anyway", "Cancel"))
				{
					return false;
				}
			}
		}

		if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
		{
			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
		}
		ConfigurePlayerSettings();

		string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
		var options = new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = outputPath,
			target = BuildTarget.WebGL,
			targetGroup = BuildTargetGroup.WebGL,
			options = BuildOptions.None
		};
		BuildReport report = BuildPipeline.BuildPlayer(options);
		bool success = report.summary.result == BuildResult.Succeeded;
		string summary = "Aviassembly WebGL build " + report.summary.result + ": " + outputPath + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB, " + report.summary.totalErrors + " errors)";
		if (success)
		{
			Debug.Log(summary);
			if (interactive)
			{
				EditorUtility.RevealInFinder(outputPath);
			}
		}
		else
		{
			Debug.LogError(summary);
		}
		return success;
	}
}
