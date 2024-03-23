// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.PackageManager;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using System;
using System.IO;
using SimpleJSON;
using UnityEditor.Build;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Android;

namespace Headjack.Settings
{
	[InitializeOnLoad]
	public class HeadjackSettings : EditorWindow
	{
		// urls for version checking and download url of latest version
		private const string versionUrl = "https://data.headjack.io/Versions/latest.json";
		private const string updateUrl = "https://content.headjack.io/Versions/headjack_plugin_latest.zip";

		private const string shouldSetXRPref = "HJ_SHOULD_SET_XR";

		// holds results of version checking
		private static UnityWebRequest versionWR;
		//private static AsyncOperation versionAsyncOperation;

		private static string latestVersion;
		private static string latestChangelog;
		private static bool startDL = false;
		// string holds format for edit app link
		private const string editAppUrl = "https://app.headjack.io/apps/{0}/edit";

		private static string version;
		Vector2 scrollPos;
		public PlatformSettings HeadjackSet = null;
		public TemplateSettings templateSettings = null;

		static HeadjackSettings()
		{
			startDL = true;
			EditorApplication.update += Update;
		}

		[MenuItem("Headjack/Settings")]
		static void Init()
		{
			HeadjackSettings window = (HeadjackSettings)GetWindow(typeof(HeadjackSettings));
			window.titleContent.text = "Headjack";
			window.Show();
		}

		static void Update()
		{
			if (startDL)
			{
				startDL = false;
				// Check plugin version
				versionWR = UnityWebRequest.Get(versionUrl);
				versionWR.SendWebRequest();
			}

			// check on download status plugin version info
			if (versionWR != null)
			{
				if (versionWR.result == UnityWebRequest.Result.ConnectionError || 
					versionWR.result == UnityWebRequest.Result.DataProcessingError || 
					versionWR.result == UnityWebRequest.Result.ProtocolError || 
					versionWR.responseCode >= 400)
				{
					// version check failed with server error
					Debug.LogWarning("Check for new version of Headjack plugin failed: " + versionWR.error);
					versionWR = null;
					return;
				}

				if (versionWR.isDone)
				{
					// successfully downloaded version info json
					JSONNode versionJson = JSON.Parse(versionWR.downloadHandler.text);
					versionWR = null;
					if (versionJson == null)
					{
						Debug.LogWarning("Could not load Headjack version info as json");
						return;
					}
					latestVersion = versionJson["latestVersion"];
					latestChangelog = versionJson["changelog"];
				}
			}
		}

		void OnGUI()
		{
			float displayScaleFactor = (96f / Screen.dpi);

			if (version == null)
			{
				version = Resources.Load<TextAsset>("Version").text;
			}

			EditorGUI.BeginChangeCheck();
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Width(Screen.width * displayScaleFactor), GUILayout.Height(Screen.height * displayScaleFactor));
			GUILayout.Label("Plugin version: " + version);

			//version check
			if (latestVersion != null && latestVersion != version)
			{
				GUI.color = Color.red;
				GUILayout.Label("New plugin version available: " + latestVersion);
				GUI.color = Color.white;
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Download latest (" + latestVersion + ")", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(updateUrl);
				}
				if (GUILayout.Button("View Changelog", GUILayout.ExpandWidth(false)))
				{
					Changelog_Window changelogWin = EditorWindow.GetWindow<Changelog_Window>(true, "Changelog " + latestVersion, true);
					changelogWin.SetLogData(latestVersion, latestChangelog);
				}
				GUILayout.EndHorizontal();
			}
			else if (versionWR != null)
			{
				GUILayout.Label("Checking plugin version");
			}

			GUILayout.Space(10f);
			GUI.color = Color.white;
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Open Headjack CMS", GUILayout.ExpandWidth(false)))
			{
				Application.OpenURL("https://app.headjack.io/");
			}
			if (GUILayout.Button("View Documentation", GUILayout.ExpandWidth(false)))
			{
				Application.OpenURL("https://headjack.io/knowledge-base");
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width * displayScaleFactor - 20));

			BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
			if (activeTarget != BuildTarget.Android && activeTarget != BuildTarget.StandaloneWindows64 && activeTarget != BuildTarget.iOS)
			{
				GUI.color = Color.red;
				GUILayout.Label("This version of headjack does not support " + activeTarget.ToString(), EditorStyles.boldLabel);
				if (activeTarget == BuildTarget.StandaloneWindows)
				{
					GUILayout.Label("Switch to Windows 64-bit for Standalone build target", EditorStyles.boldLabel);
				}
				GUI.color = Color.white;
			}

			if (HeadjackSet == null)
			{
				HeadjackSet = Resources.Load<PlatformSettings>("Headjack Settings");
				if (HeadjackSet == null)
				{
					HeadjackSet = CreateInstance<PlatformSettings>();

					// Create destination if not existing folder
					if (!Directory.Exists(Application.dataPath + "/Resources"))
					{
						Directory.CreateDirectory(Application.dataPath + "/Resources");
					}
					AssetDatabase.CreateAsset(HeadjackSet, "Assets/Resources/Headjack Settings.asset");

					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				EditorUtility.SetDirty(HeadjackSet);
			}
			if (templateSettings == null)
			{
				templateSettings = Resources.Load<TemplateSettings>("Template Settings");
				if (templateSettings == null)
				{
					templateSettings = CreateInstance<TemplateSettings>();

					// Create destination if not existing folder
					if (!Directory.Exists(Application.dataPath + "/Template/Resources"))
					{
						Directory.CreateDirectory(Application.dataPath + "/Template/Resources");
					}
					AssetDatabase.CreateAsset(templateSettings, "Assets/Template/Resources/Template Settings.asset");
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				EditorUtility.SetDirty(templateSettings);
			}


			string appID = HeadjackSet.appID;
			bool validAppId = true;
			if (appID == null || appID == "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" || appID.Length != 32)
			{
				GUI.color = Color.red;
				validAppId = false;
				GUILayout.Label("Please fill in a valid Headjack App ID");
			}

			string keyID = HeadjackSet.AUTHKey;
			if (keyID == null || keyID == "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" || keyID.Length != 48)
			{
				GUI.color = Color.red;
				GUILayout.Label("Please fill in a valid authentication key");
			}

			GUI.color = Color.white;

			GUILayout.BeginHorizontal();
			GUILayout.Label("App ID", EditorStyles.boldLabel);
			if (validAppId)
			{
				if (GUILayout.Button("Open App in CMS", GUILayout.ExpandWidth(false)))
				{
					Application.OpenURL(string.Format(editAppUrl, appID));
				}
			}
			GUILayout.EndHorizontal();
			appID = EditorGUILayout.TextArea(appID);
			HeadjackSet.appID = appID;

			GUILayout.Label("AUTH Key", EditorStyles.boldLabel);
			keyID = EditorGUILayout.TextArea(keyID);
			HeadjackSet.AUTHKey = keyID;

			GUILayout.Space(10f);

			//Get current VRapi > Set new VRapi > put back in headjack settings
			PlatformSettings.BuildPlatform _VRApi = HeadjackSet.Platform;
			_VRApi = (PlatformSettings.BuildPlatform)EditorGUILayout.EnumPopup("Virtual Reality API", _VRApi);
			HeadjackSet.Platform = _VRApi;

			GUILayout.Space(10f);
			GUILayout.Label("Template Settings", EditorStyles.boldLabel);

			templateSettings.Orientation = (TemplateSettings.CBOrientation)EditorGUILayout.EnumPopup("Cardboard Orientation", templateSettings.Orientation);
			templateSettings.CinemaSupported = EditorGUILayout.Toggle("Cinema supported", templateSettings.CinemaSupported);

			GUILayout.Space(10f);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Apply Settings"))
			{
				EditorUtility.SetDirty(HeadjackSet);
				EditorUtility.SetDirty(templateSettings);

				// Create destination if not existing folder
				if (!Directory.Exists(Application.dataPath + "/Template/Resources"))
				{
					Directory.CreateDirectory(Application.dataPath + "/Template/Resources");
				}

				File.WriteAllText(Application.dataPath + "/Template/Resources/Template Settings.json", JsonUtility.ToJson(templateSettings));

				SetPlatformSettings(HeadjackSet, templateSettings);

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			//GUILayout.Label("Template Settings", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded() {
			if (EditorPrefs.GetBool(shouldSetXRPref, false)) {
				EditorPrefs.SetBool(shouldSetXRPref, false);
				PlatformSettings pSettings = Resources.Load<PlatformSettings>("Headjack Settings");
				SetVRSDK(pSettings);
			}
		}

		public static void SetVRSDK(PlatformSettings pSettings) {
			switch (pSettings.Platform) {
				case PlatformSettings.BuildPlatform.Oculus:
					SetVirtualRealitySDK(true, BuildTargetGroup.Android, new string[] { "Oculus Loader" });
					SetVirtualRealitySDK(true, BuildTargetGroup.Standalone, new string[] { "Oculus Loader" });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.OculusLegacy:
					SetVirtualRealitySDK(true, BuildTargetGroup.Android, new string[] { "Oculus Legacy Loader" });
					SetVirtualRealitySDK(true, BuildTargetGroup.Standalone, new string[] { "Oculus Legacy Loader" });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.OpenVR:
					SetVirtualRealitySDK(true, BuildTargetGroup.Standalone, new string[] { "Open VR Loader" });
					SetVirtualRealitySDK(false, BuildTargetGroup.Android, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.Pico:
					SetVirtualRealitySDK(false, BuildTargetGroup.Android, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.Standalone, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.PicoPlatform:
					SetVirtualRealitySDK(true, BuildTargetGroup.Android, new string[] { "PXR Loader" });
					SetVirtualRealitySDK(false, BuildTargetGroup.Standalone, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.Cardboard:
					SetVirtualRealitySDK(false, BuildTargetGroup.Android, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.Standalone, new string[] { });
					break;
				case PlatformSettings.BuildPlatform.ViveWave:
					SetVirtualRealitySDK(true, BuildTargetGroup.Android, new string[] { "Wave XR Loader" });
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, new string[] { });
					SetVirtualRealitySDK(false, BuildTargetGroup.Standalone, new string[] { });
					break;
				default:
					SetVirtualRealitySDK(false, BuildTargetGroup.Android, null);
					SetVirtualRealitySDK(false, BuildTargetGroup.Standalone, null);
					SetVirtualRealitySDK(false, BuildTargetGroup.iOS, null);
					break;
			}
		}

		public static void SetPlatformSettings(
			PlatformSettings pSettings, 
			TemplateSettings tSettings, 
			bool moveFolders = true
		) {
			if (moveFolders) {
				UseDirectories(
					pSettings.Platform == PlatformSettings.BuildPlatform.Oculus,
					oculusEnabled,
					oculusDisabled
				);
				UseDirectories(
					pSettings.Platform == PlatformSettings.BuildPlatform.OculusLegacy,
					oculusLegacyEnabled,
					oculusLegacyDisabled
				);
				UseDirectories(
					pSettings.Platform == PlatformSettings.BuildPlatform.OpenVR,
					steamEnabled,
					steamDisabled
				);
				UseDirectories(
					pSettings.Platform == PlatformSettings.BuildPlatform.Pico,
					picoLegacyEnabled,
					picoLegacyDisabled
				);
				UseDirectories(
					pSettings.Platform == PlatformSettings.BuildPlatform.Cardboard,
					cardboardEnabled,
					cardboardDisabled
				);

				UseDirectoriesFromDefine("USE_AVPROVIDEO", avProVideoEnabled, avProVideoDisabled);
				AssetDatabase.Refresh();
			}

			if (pSettings.Platform != PlatformSettings.BuildPlatform.Oculus) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_OCULUS_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_OCULUS_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_OCULUS_SDK");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.OculusLegacy) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_OCULUS_LEGACY_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_OCULUS_LEGACY_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_OCULUS_LEGACY_SDK");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.Oculus &&
				pSettings.Platform != PlatformSettings.BuildPlatform.OculusLegacy
			) {
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "AUDIO360_USE_OVR_AUDIO_OUT");

				RemovePackage("com.unity.xr.oculus");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.Cardboard) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_CARDBOARD_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_CARDBOARD_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_CARDBOARD_SDK");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.OpenVR) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_OPENVR_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_OPENVR_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_OPENVR_SDK");

				RemovePackage("com.valvesoftware.unity.openvr");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.Pico) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_PICO_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_PICO_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_PICO_SDK");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.PicoPlatform) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_PICO_PLATFORM_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_PICO_PLATFORM_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_PICO_PLATFORM_SDK");

				RemovePackage("com.unity.xr.picoxr");
			}
			if (pSettings.Platform != PlatformSettings.BuildPlatform.ViveWave) {
				RemoveScriptingDefine(BuildTargetGroup.Android, "USE_WAVE_SDK");
				RemoveScriptingDefine(BuildTargetGroup.iOS, "USE_WAVE_SDK");
				RemoveScriptingDefine(BuildTargetGroup.Standalone, "USE_WAVE_SDK");

				RemovePackage("com.htc.upm.wave.native");
				RemovePackage("com.htc.upm.wave.xrsdk");
			}
			
			PlayerSettings.colorSpace = ColorSpace.Gamma;
			// set quality defaults
			QualitySettings.vSyncCount = 0;
			QualitySettings.antiAliasing = 0;
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
			PlayerSettings.MTRendering = true;
			PlayerSettings.gpuSkinning = true;
			PlayerSettings.graphicsJobs = true;
			// set Android defaults
			PlayerSettings.Android.blitType = AndroidBlitType.Always;
			PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
			PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)21;
			PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)33;
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.SetScriptingBackend(UnityEditor.BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
			// external storage/SD card permission required for older API levels
			PlayerSettings.Android.forceSDCardPermission = false;
			if (PlayerSettings.Android.useAPKExpansionFiles && (int)PlayerSettings.Android.minSdkVersion <= 23) {
				PlayerSettings.Android.forceSDCardPermission = true;
			}
			// set iOS defaults
			PlayerSettings.iOS.targetOSVersionString = "12.1";
			// build system
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
			// set windows defaults
			PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 });

			// Virtual Reality Supported
			switch (pSettings.Platform)
			{
				case PlatformSettings.BuildPlatform.Oculus:
					PlayerSettings.colorSpace = ColorSpace.Linear;
					PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
					PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)32;
					AddScriptingDefines(BuildTargetGroup.Standalone, "AUDIO360_USE_OVR_AUDIO_OUT");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_OCULUS_SDK");
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_OCULUS_SDK");
					SetAndroidManifest(AndroidManifest.Oculus);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					AddCustomPackage("com.unity.xr.oculus@3.3.0c");
					break;
				case PlatformSettings.BuildPlatform.OculusLegacy:
					PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)23;
					PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)29;
					AddScriptingDefines(BuildTargetGroup.Standalone, "AUDIO360_USE_OVR_AUDIO_OUT");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_OCULUS_LEGACY_SDK");
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_OCULUS_LEGACY_SDK");
					SetAndroidManifest(AndroidManifest.GearVR);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					AddCustomPackage("com.unity.xr.oculus@1.5.1");
					break;
				case PlatformSettings.BuildPlatform.OpenVR:
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_OPENVR_SDK");
					AddCustomPackage("com.valvesoftware.unity.openvr@3ee6c452bc34");
					break;
				case PlatformSettings.BuildPlatform.ViveWave:
					PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
					PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)29;
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_WAVE_SDK");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_WAVE_SDK");
					SetAndroidManifest(AndroidManifest.Wave);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					PlayerSettings.gpuSkinning = false;
					AddCustomPackage("com.htc.upm.wave.native@5.0.3-r.5");
					AddCustomPackage("com.htc.upm.wave.xrsdk@5.0.3-r.5");
					break;
				case PlatformSettings.BuildPlatform.Pico:
					PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)29;
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_PICO_SDK");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_PICO_SDK");
					SetAndroidManifest(AndroidManifest.Pico);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					break;
				case PlatformSettings.BuildPlatform.PicoPlatform:
					PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_PICO_PLATFORM_SDK");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_PICO_PLATFORM_SDK");
					SetAndroidManifest(AndroidManifest.PicoPlatform);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					AddCustomPackage("com.unity.xr.picoxr@2.1.2");
					QualitySettings.antiAliasing = 4;
					break;
				case PlatformSettings.BuildPlatform.Cardboard:
					SetAndroidManifest(AndroidManifest.Default);
					SetAndroidMainGradle(AndroidMainGradle.Cardboard);
					AddScriptingDefines(BuildTargetGroup.Standalone, "USE_CARDBOARD_SDK");
					AddScriptingDefines(BuildTargetGroup.Android, "USE_CARDBOARD_SDK");
					AddScriptingDefines(BuildTargetGroup.iOS, "USE_CARDBOARD_SDK");
					PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
					QualitySettings.vSyncCount = 1;
					PlayerSettings.defaultInterfaceOrientation = (UIOrientation)tSettings.Orientation;
					break;
				default:
					SetAndroidManifest(AndroidManifest.Default);
					SetAndroidMainGradle(AndroidMainGradle.Default);
					break;
			}

			EditorPrefs.SetBool(shouldSetXRPref, true);

			AssetDatabase.Refresh();
			Client.Resolve();
		}

		private static void UseDirectoriesFromDefine(
			string scriptingDefineSymbol, 
			string[] enabledDirs,
			string[] disabledDirs,
			bool moveMeta = true
		) {
			string defines = PlayerSettings.GetScriptingDefineSymbols(
				NamedBuildTarget.FromBuildTargetGroup(
					BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)
				)
			);

			UseDirectories(defines.Contains(scriptingDefineSymbol), enabledDirs, disabledDirs, moveMeta);
		}

		public static void UseDirectories(
			bool enable, 
			string[] enabledDirs,
			string[] disabledDirs,
			bool moveMeta = true
		) {
			if (enable && 
				Directory.Exists(Application.dataPath + disabledDirs[0])
			) {
				for (int i = 0; i < disabledDirs.Length; i++) {
					try {
						Directory.Move(
							Application.dataPath + disabledDirs[i], 
							Application.dataPath + enabledDirs[i]
						);
						if (moveMeta) {
							File.Move(
								Application.dataPath + disabledDirs[i] + ".meta", 
								Application.dataPath + enabledDirs[i] + ".meta"
							);
						}
					} catch (Exception e) {
						Debug.LogError($"Failed to enable directory {disabledDirs[i]} : {e.Message}");
					}
				}
			} else if (!enable && 
				Directory.Exists(Application.dataPath + enabledDirs[0])
			) {
				for (int i = 0; i < enabledDirs.Length; i++) {
					try {
						Directory.Move(
							Application.dataPath + enabledDirs[i], 
							Application.dataPath + disabledDirs[i]
						);
						if (moveMeta) {
							File.Move(
								Application.dataPath + enabledDirs[i] + ".meta", 
								Application.dataPath + disabledDirs[i] + ".meta"
							);
						}
					} catch (Exception e) {
						Debug.LogError($"Failed to disable directory {enabledDirs[i]} : {e.Message}");
					}
				}
			}
		}

		private static void AddCustomPackage(
			string packageName
		) {
			string packageRelPath = "../CustomPackages/" + packageName;
			if (!Directory.Exists(Application.dataPath + "/" + packageRelPath)) {
				Debug.LogError($"Custom package could not be found: {packageName}");
				return;
			}
			AddRequest addReq = Client.Add("file:" + packageRelPath);
			int timeoutMs = 1000;
			while (timeoutMs > 0 && !addReq.IsCompleted) {
				System.Threading.Thread.Sleep(33);
				timeoutMs -= 33;
			}
			Debug.Log($"Added package {packageName}");
		}

		private static void RemovePackage(
			string packageName
		) {
			RemoveRequest removeRequest = Client.Remove(packageName);
			int timeoutMs = 1000;
			while (timeoutMs > 0 && !removeRequest.IsCompleted) {
				System.Threading.Thread.Sleep(33);
				timeoutMs -= 33;
			}
		}

		private static void SetVirtualRealitySDK(bool enabled, BuildTargetGroup buildTargetGroup, string[] devices)
		{
			XRGeneralSettings targetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
			targetSettings.InitManagerOnStart = enabled;
			targetSettings.AssignedSettings.automaticLoading = enabled;
			targetSettings.AssignedSettings.automaticRunning = enabled;
			List<XRLoader> loaderList = new List<XRLoader>(devices.Length);
			for (int i = 0; i < devices.Length; ++i) {
				XRLoader deviceLoader = AssetDatabase.LoadAssetAtPath<XRLoader>("Assets/XR/Loaders/" + devices[i] + ".asset");
				if (deviceLoader != null) {
					loaderList.Add(deviceLoader);
				}
			}
			if (!targetSettings.AssignedSettings.TrySetLoaders(loaderList)) {
				Debug.LogError("Failed to set XRLoader list");
			}

			AssetDatabase.Refresh();
		}

		public static void AddScriptingDefines(BuildTargetGroup buildGroup, string newString)
		{
			string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
			if (current == null)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, newString);
				return;
			}
			if (current.Length == 0)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, newString);
				return;
			}
			if (current.Contains(newString))
			{
				return;
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, current + ";" + newString);
		}

		public static void RemoveScriptingDefine(BuildTargetGroup buildGroup, string ToRemove)
		{
			string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
			if (current.Contains(";" + ToRemove))
			{
				string removed = ";" + ToRemove;
				string newstring = current.Replace(removed, "");
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, newstring);
				return;
			}
			if (current.Contains(ToRemove + ";"))
			{
				string removed = ToRemove + ";";
				string newstring = current.Replace(removed, "");
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, newstring);
				return;
			}
			if (current.Contains(ToRemove))
			{
				string removed = ToRemove;
				string newstring = current.Replace(removed, "");
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, newstring);
				return;
			}
		}

		public enum AndroidManifest
		{
			Default,
			GearVR,
			Wave,
			Pico,
			Oculus,
			PicoPlatform
		}

		private static void SetAndroidManifest(AndroidManifest manifestType)
		{
			string dirTarget = Application.dataPath + "/Plugins/Android/";
			string dirManifests = Application.dataPath + "/Headjack/Plugins/Android/";
			if (File.Exists(dirTarget + "AndroidManifest.xml")) File.Delete(dirTarget + "AndroidManifest.xml");
			switch (manifestType)
			{
				case AndroidManifest.GearVR:
					File.Copy(dirManifests + "AndroidManifest.gearvr.xml", dirTarget + "AndroidManifest.xml", false);
					break;
				case AndroidManifest.Wave:
					File.Copy(dirManifests + "AndroidManifest.wave.xml", dirTarget + "AndroidManifest.xml", false);
					break;
				case AndroidManifest.Pico:
					File.Copy(dirManifests + "AndroidManifest.pico.xml", dirTarget + "AndroidManifest.xml", false);
					break;
				case AndroidManifest.PicoPlatform:
					File.Copy(dirManifests + "AndroidManifest.picoplatform.xml", dirTarget + "AndroidManifest.xml", false);
					break;
				case AndroidManifest.Oculus:
					File.Copy(dirManifests + "AndroidManifest.oculus.xml", dirTarget + "AndroidManifest.xml", false);
					break;
				case AndroidManifest.Default:
				default:
					File.Copy(dirManifests + "AndroidManifest.default.xml", dirTarget + "AndroidManifest.xml", false);
					break;
			}
		}

		public enum AndroidMainGradle
		{
			Default,
			Cardboard
		}

		private static void SetAndroidMainGradle(AndroidMainGradle mainGradleType)
		{
			string dirTarget = Application.dataPath + "/Plugins/Android/";
			string dirManifests = Application.dataPath + "/Headjack/Plugins/Android/";
			if (File.Exists(dirTarget + "mainTemplate.gradle")) File.Delete(dirTarget + "mainTemplate.gradle");
			switch (mainGradleType)
			{
				case AndroidMainGradle.Cardboard:
					File.Copy(dirManifests + "mainTemplate.cardboard.gradle", dirTarget + "mainTemplate.gradle", false);
					break;
				case AndroidMainGradle.Default:
				default:
					File.Copy(dirManifests + "mainTemplate.default.gradle", dirTarget + "mainTemplate.gradle", false);
					break;
			}
		}
		
		private static readonly string[] avProVideoEnabled = new string[]
		{
			"/AVProVideo"
		};
		private static readonly string[] avProVideoDisabled = new string[]
		{
			"/.AVProVideo"
		};

		private static readonly string[] cardboardEnabled = new string[]
		{
			"/Cardboard"
		};
		private static readonly string[] cardboardDisabled = new string[]
		{
			"/.Cardboard"
		};

		private static readonly string[] oculusEnabled = new string[]
		{
			"/Oculus"
		};
		private static readonly string[] oculusDisabled = new string[]
		{
			"/.Oculus"
		};

		private static readonly string[] oculusLegacyEnabled = new string[]
		{
			"/OculusLegacy"
		};
		private static readonly string[] oculusLegacyDisabled = new string[]
		{
			"/.OculusLegacy"
		};

		private static readonly string[] picoLegacyEnabled = new string[]
		{
			"/PicoMobileSDK"
		};
		private static readonly string[] picoLegacyDisabled = new string[]
		{
			"/.PicoMobileSDK"
		};

		private static readonly string[] steamEnabled = new string[]
		{
			"/SteamVR",
			"/SteamVR_Resources"

		};
		private static readonly string[] steamDisabled = new string[]
		{
			"/.SteamVR",
			"/.SteamVR_Resources"
		};
	}

    public class Changelog_Window : EditorWindow
    {
        private string displayed_log;
        //private string latestVersion;
        private Vector2 scrollPosition;

        public void SetLogData(string version, string log)
        {
            displayed_log = log;
            //latestVersion = version;
            minSize = new Vector2(320, 400);
        }

        public void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (displayed_log != null)
            {
                EditorGUILayout.HelpBox(displayed_log, MessageType.Info);
            }

            GUILayout.EndScrollView();
        }
    }
}
