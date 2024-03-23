// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Headjack.Settings;
using SimpleJSON;
using System.Runtime.InteropServices;

namespace Headjack
{
    internal class Startup : MonoBehaviour
    {
        private static uint neverSleepFlag = 0;
        internal static uint NeverSleepFlag
        {
            get
            {
                return neverSleepFlag;
            }
            set
            {
                neverSleepFlag = value;
                if (neverSleepFlag > 0) {
                    // Debug.Log("[NeverSleepFlag] Never sleep screen");
                    Screen.sleepTimeout = SleepTimeout.NeverSleep;
                } else {
                    // Debug.Log("[NeverSleepFlag] Default sleep screen");
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                }
            }
        }

        private long ImageQueueTotal = 0;
        private long ImageQueueCurrent = 0;
        private int ImagesDownloading = 0;
        private bool ImageDecodeBusy = false;
        private UnityWebRequest www;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void iOSAudio_setupAudioSession();
#endif

        internal void Initialize(OnEnd OnReady, string appId, string authKey, 
            string platform = null, string customHost = null)
        {
            if (App.VRCamera == null) {
                App.VRCamera = new GameObject("Headjack Camera");
                App.VRCamera.AddComponent<VRCamera>();
                // App.VRCamera.GetComponent<VRCamera>().hideFlags = HideFlags.HideInInspector;
            }            

#if UNITY_IOS && !UNITY_EDITOR
            // enable audio of this app regardless of mute button (enable audio mixing)
            iOSAudio_setupAudioSession();
#endif

            PlatformSettings settings = Resources.Load<PlatformSettings>("Headjack Settings");
            if (settings == null)
            {
                Debug.LogError("No App information found");
                OnReady(false, "No App information found");
                return;
            }

            App.BasePath = Application.persistentDataPath + "/";

#if UNITY_ANDROID && !UNITY_EDITOR
            if (App.AndroidInternalStorage) {
                // get Android's protected internal app data path to use for downloads
                AndroidJavaObject activity = GetActivity();
                if (activity != null) {
                    AndroidJavaObject context = GetApplicationContext(activity);
                    activity.Dispose();
                    if (context != null) {
                        // get "File" class Android object containing internal files directory
                        AndroidJavaObject internalFilesDir = context.Call<AndroidJavaObject>("getFilesDir");
                        context.Dispose();
                        if (internalFilesDir == null) {
                            Debug.LogError("[Startup] could not get Android internal files directory");
                        } else {
                            string internalFilesDirPath = internalFilesDir.Call<string>("getCanonicalPath");
                            internalFilesDir.Dispose();
                            if (string.IsNullOrEmpty(internalFilesDirPath)) {
                                Debug.LogError("[Startup] Android internal files directory is empty path");
                            } else {
                                Debug.LogWarning($"[Startup] Android internal files directory: {internalFilesDirPath}");
                                App.BasePath = internalFilesDirPath + "/";
                            }
                        }
                    }
                }
            }
#endif //UNITY_ANDROID && !UNITY_EDITOR

#if (USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK) && HJ_STORE_BUILD
            // check oculus entitlement on oculus platforms
            try
            {
                Oculus.Platform.Core.AsyncInitialize();
            }
            catch (System.Exception e)
            {
                string oculusInitFailed = "Failed to initialize Oculus platform: " + e.Message;
                Debug.LogError(oculusInitFailed);
                OnReady(false, oculusInitFailed);
                return;
            }
            Oculus.Platform.Entitlements.IsUserEntitledToApplication().OnComplete(
                (Oculus.Platform.Message entitlementMsg) =>
                {
                    if (entitlementMsg.IsError)
                    {
                        Debug.LogError("Oculus entitlement check failed: " + entitlementMsg.GetError().Message);
                        CreateOculusErrorPopup(entitlementMsg.GetError().Message);
                        //OnReady(false, "Oculus entitlement check failed: " + entitlementMsg.GetError().Message);
                    }
                    else
                    {
                        StartCoroutine(Init(OnReady, platform));
                    }
                }
            );
#else
            StartCoroutine(Init(OnReady, platform));
#endif
        }

        internal void LoadAllTextures(bool GenerateMipmaps, OnEnd OnLoaded = null, OnImageProgress listener = null)
        {
            StartCoroutine(ImportAllTextures(GenerateMipmaps, OnLoaded, listener));
        }

        internal void LoadTexture(string MediaId, bool GenerateMipmaps, OnEnd OnLoaded = null)
        {
            StartCoroutine(ImportSingleTexture(MediaId, GenerateMipmaps, OnLoaded));
        }

        IEnumerator Init(OnEnd OnReady, string platform = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (App.AndroidInternalStorage) {
                App.Downloader = App.DataObject.AddComponent<PPDownloader>();
            } else {
                App.Downloader = App.DataObject.AddComponent<BgDownloader>();
            }
#else
			App.Downloader = App.DataObject.AddComponent<PPDownloader>();
#endif
            App.Downloader.Initialize();
            App.DataObject.AddComponent<DoNotDestroy>();
            DontDestroyOnLoad(App.DataObject);
 
            if (platform == null) {
                platform = App.GetAppPlatform();
            }

            bool wasLocal = false;

            using (AppRequest appRequest = new AppRequest(
                App.AppId, 
                App.Authkey, 
                platform, 
                App.BasePath
            )) {
                yield return appRequest.LoadAppData();

                if (appRequest.status != AppRequest.Status.Success || appRequest.appData == null) {
                    App.Reset();
                    switch (appRequest.status) {
                        case AppRequest.Status.AuthKeyInvalid:
                            if (OnReady != null) OnReady(false, appRequest.error);
                            yield break;
                        case AppRequest.Status.DataProcessingError:
                        case AppRequest.Status.ProtocolError:
                        case AppRequest.Status.ConnectionError:
                        case AppRequest.Status.LocalUnavailable:
                            if (OnReady != null) OnReady(false, "No Connection");
                            yield break;
                        case AppRequest.Status.AppUnpublished:
                            if (OnReady != null) OnReady(false, "Unpublished");
                            yield break;
                        case AppRequest.Status.BandwidthLimitReached:
                            if (OnReady != null) OnReady(false, "Bandwidth Limit");
                            yield break;
                        case AppRequest.Status.ViewsLimitReached:
                            if (OnReady != null) OnReady(false, "Views Limit");
                            yield break;
                    }
                    if (OnReady != null) OnReady(false, "Unknown Error");
                    yield break;
                }
                wasLocal = appRequest.usedLocal;
                App.Data = appRequest.appData;
                App.Data.Packaged = LoadPackagedInfo();
                // fill LocalVersion
                LocalVersionTracking localVersionTracking = new LocalVersionTracking();
                yield return localVersionTracking.Initialize(App.BasePath, App.AppId, appRequest.appData);
                App.LocalVersionTracking = localVersionTracking;
                
            }

            // delete any old (v2) local data (Media, Video) once (migration) to avoid stale media in local storage
            MigrateOutdatedLocalFiles();

#if UNITY_ANDROID && !UNITY_EDITOR
			if (Application.dataPath.Contains(".obb"))
			{
				yield return CopyTbeInObb();
			}

            if (!App.AndroidInternalStorage) {
                ((BgDownloader)App.Downloader).LoadExistingDownloads();
            }
            yield return null;
#endif
            
            //Ready to go to app
            Debug.Log("App Initialized");
#if !UNITY_EDITOR
            App.DataObject.AddComponent<Analytics>();
#endif
            LoadPushServer();

            if (OnReady != null) {
                OnReady(true, (wasLocal ? "Offline" : null));
            }
        }

        // deletes files downloaded before v3 local data format to avoid stale files on local storage
        private void MigrateOutdatedLocalFiles()
        {
            if (File.Exists(Headjack.App.BasePath + "(╯°□°）╯︵ ┻━┻")) {
                try {
                    Directory.Delete(Headjack.App.BasePath + "Media/", true);
                } catch (DirectoryNotFoundException) {

                } catch (System.Exception e) {
                    Debug.LogError($"Failed to delete Media directory in v2 migration: {e.Message}");
                }
                try {
                    Directory.Delete(Headjack.App.BasePath + "Video/", true);
                } catch (DirectoryNotFoundException) {

                } catch (System.Exception e) {
                    Debug.LogError($"Failed to delete Video directory in v2 migration: {e.Message}");
                }
                try {
                    File.Delete(Headjack.App.BasePath + "(╯°□°）╯︵ ┻━┻");
                } catch (FileNotFoundException) {

                } catch (System.Exception e) {
                    Debug.LogError($"Failed to delete v2 local data file in v2 migration: {e.Message}");
                }
            }
        }

        // checks if template supports cinema events, and loads server handler if so
        private void LoadPushServer()
        {
            TextAsset templateSettingsJson = Resources.Load<TextAsset>("Template Settings");
            if (templateSettingsJson != null)
            {
                try
                {
                    JSONNode templateVar = JSON.Parse(templateSettingsJson.text);
                    if (templateVar != null)
                    {
                        bool cinemaSupported = templateVar["CinemaSupported"];
                        if (cinemaSupported)
                        {
                            App.cinemaSupported = cinemaSupported;
                            App.DataObject.AddComponent<PushServerHandler>();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Loading Template Settings Json failed");
                    }
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("Loading Template Cinema setting failed");
                }
            }
            else
            {
                Debug.Log("No Template Settings file found in Resources");
            }
        }

        IEnumerator ImportSingleTexture(string MediaId, bool GenerateMipmaps, OnEnd OnLoaded = null)
        {

            if (MediaId == null)
            {
                if (OnLoaded != null)
                {
                    OnLoaded(false, "null");
                }
                yield break;
            }
            if (App.Data.Project.ContainsKey(MediaId))
            {
                MediaId = App.Data.Project[MediaId].Thumbnail;
            }
            if (MediaId == null)
            {
                if (OnLoaded != null)
                {
                    OnLoaded(false, "null");
                }

                yield break;
            }

            if (!App.Data.Media.ContainsKey(MediaId))
            {
                Debug.Log("No media key");
                if (OnLoaded != null)
                {
                    OnLoaded(false, "Media id " + MediaId + " not found");
                }
                yield break;
            }
            bool isDownloaded = false;
            bool downloadSuccess = true;
            string downloadError = null;
            if (App.UpdateAvailableMedia(MediaId) || !App.GotMediaFile(MediaId))
            {
                if (App.Data.Media[MediaId].MimeType == "image/png" || App.Data.Media[MediaId].MimeType == "image/jpg" || App.Data.Media[MediaId].MimeType == "image/jpeg" || App.Data.Media[MediaId].MimeType == "image/x-f2d")
                {
                    long queuePlace = ImageQueueTotal;
                    ImageQueueTotal++;
                    while (queuePlace > ImageQueueCurrent || ImagesDownloading > 4) {
                        yield return null;
                    }

                    ImageQueueCurrent = queuePlace + 1;
                    ImagesDownloading++;
                    App.Downloader.DownloadMedia(MediaId, false, (bool dlSuccess, string dlError) => {
                        downloadSuccess = dlSuccess;
                        downloadError = dlError;
                        ImagesDownloading--;
                        isDownloaded = true;
                    });
                }
                else
                {
                    Debug.LogWarning($"Media with ID {MediaId} is not a supported image format");
                    if (OnLoaded != null)
                    {
                        OnLoaded(false, "Incompatible filetype");
                    }
                    yield break;
                }
            } else {
                isDownloaded = true;
            }

            while (!isDownloaded) {
                yield return null;
            }

            if (!downloadSuccess) {
                Debug.LogError($"Image download error: {downloadError}");
                if (OnLoaded != null)
                {
                    OnLoaded(false, downloadError);
                }
                yield break;
            }

            if (App.Data.Textures.ContainsKey(MediaId))
            {
                if (OnLoaded != null)
                {
                    OnLoaded(true, "Texture already imported");
                }
                yield break;
            }
            if (App.PackageHasLatest(MediaId))
            {
                App.Data.Textures.Add(MediaId, (Texture2D)Resources.Load<Texture>(App.Data.Packaged[MediaId].Path));
                Debug.Log("Used packaged texture");
                if (OnLoaded != null)
                {
                    OnLoaded(true, null);
                }
                yield break;
            }

            if (App.Data.Media[MediaId].MimeType == "image/png" || App.Data.Media[MediaId].MimeType == "image/jpg" || App.Data.Media[MediaId].MimeType == "image/jpeg")
            {
                App.Data.Textures.Add(MediaId, new Texture2D(4, 4, TextureFormat.ARGB32, GenerateMipmaps));
                yield return null;
				try
				{
					App.Data.Textures[MediaId].LoadImage(File.ReadAllBytes(App.BasePath + "Media/" + MediaId + "/" + App.Data.Media[MediaId].Filename));
				}
				catch (System.Exception)
				{
					Debug.LogWarning("Error opening image file");
				}
                yield return null;
                App.Data.Textures[MediaId].Apply(true, true);
                yield return null;

            }
            if (App.Data.Media[MediaId].MimeType == "image/x-f2d")
            {
                if (!App.Data.Textures.ContainsKey(MediaId))
                {
                    while (ImageDecodeBusy)
                    {
                        yield return null;
                    }


                    ImageDecodeBusy = true;
                    bool isDecoded = false;
#if UNITY_STANDALONE || UNITY_EDITOR
                    FastTexture2D.decode(App.BasePath + "Media/" + MediaId + "/" + App.Data.Media[MediaId].Filename, (Texture2D imgTex) => {
                        App.Data.Textures[MediaId] = imgTex;
                        ImageDecodeBusy = false;
                        isDecoded = true;
                    }, GenerateMipmaps, 8, null);
#else
                    FastTexture2D.decode(App.BasePath + "Media/" + MediaId + "/" + App.Data.Media[MediaId].Filename, (Texture2D imgTex) => {
                        App.Data.Textures[MediaId] = imgTex;
                        ImageDecodeBusy = false;
                        isDecoded = true;
                    }, GenerateMipmaps, 2, null);
#endif
                    while (!isDecoded)
                    {
                        yield return null;
                    }
                }
            }

            if (OnLoaded != null)
            {
                OnLoaded(true, null);
            }

        }
        IEnumerator ImportAllTextures(bool GenerateMipmaps, OnEnd OnLoaded = null, OnImageProgress listener = null)
        {
            if (App.Data.Media == null) {
                if (OnLoaded != null)
                {
                    OnLoaded(false, "No Media block found");
                }
                yield break;
            }

            yield return null;
            int loadedTextures = 0;
            int totalTextures = 0;
            string texError = null;
            foreach (KeyValuePair<string, AppDataStruct.MediaBlock> k in App.Data.Media) {
                if (k.Key.EndsWith("_UNSCALED")) {
                    continue;
                }
                if (k.Value.MimeType != "image/png" &&
                    k.Value.MimeType != "image/jpg" &&
                    k.Value.MimeType != "image/jpeg" &&
                    k.Value.MimeType != "image/x-f2d"
                ) {
                    continue;        
                }

                totalTextures++;
                StartCoroutine(ImportSingleTexture(
                    k.Key, 
                    GenerateMipmaps, 
                    (bool sTexSuccess, string sTexError) => {
                        if (!sTexSuccess && texError == null) {
                            texError = sTexError;
                        }
                        loadedTextures++;
                    }
                ));
                yield return null;
            }

            int prevLoadedTextures = loadedTextures;
            while (loadedTextures < totalTextures) {
                if (prevLoadedTextures != loadedTextures && listener != null) {
                    prevLoadedTextures = loadedTextures;
                    listener(ImageLoadingState.Decoding, loadedTextures, totalTextures);
                }
                yield return null;
            }
            listener(ImageLoadingState.Decoding, totalTextures, totalTextures);

            if (texError != null) {
                Debug.LogError($"Failed to (down)load at least one image texture: {texError}");
                if (OnLoaded != null) {
                    OnLoaded(false, texError);
                }
                yield break;
            }

            OnLoaded(true, null);
        }

        internal void CreateOculusErrorPopup(string error)
        {
            GameObject messageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            messageQuad.name = "OculusError";
            Destroy(messageQuad.GetComponent<MeshCollider>());
            messageQuad.transform.position = new Vector3(0, 0, 1.5f);
            if (App.camera != null) {
                messageQuad.transform.position += App.camera.position;
            }
            messageQuad.transform.localScale = new Vector3(0.65f, 0.6f, 1f);
            messageQuad.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/Message"));
            messageQuad.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Textures/MessageBack");

            GameObject textObject = new GameObject();
            textObject.name = "ErrorText";
            textObject.transform.parent = messageQuad.transform;
            textObject.transform.localPosition = new Vector3(0, 0.15f, 0);
            textObject.transform.localEulerAngles = Vector3.zero;

            TextMesh tm = textObject.AddComponent<TextMesh>();
            tm.font = Resources.Load<Font>("Fonts/Rubik");
            textObject.GetComponent<MeshRenderer>().material = tm.font.material;
            tm.text = "You are not authorized to use this app";
            tm.characterSize = 0.015f;
            tm.fontSize = 32;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Left;

            Texture temp = textObject.GetComponent<MeshRenderer>().material.mainTexture;
            textObject.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/MessageText"));
            textObject.GetComponent<MeshRenderer>().material.mainTexture = temp;

            Tools.FitTextToBounds(
                tm,
                "You are not authorized to use this app.\nerror: " + error,
                new Vector2(0.55f, 0.3f),
                24, 32);

            // OK button
            GameObject buttonQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            buttonQuad.name = "OKButton";
            buttonQuad.transform.parent = messageQuad.transform;
            buttonQuad.transform.localPosition = new Vector3(0, -.3f, -.02f);
            buttonQuad.transform.localScale = new Vector3(0.3f, 0.15f, 1f);
            buttonQuad.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/Message"));
            buttonQuad.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Textures/MessageBack");

            GameObject buttonText = new GameObject();
            buttonText.name = "ButtonText";
            buttonText.transform.parent = buttonQuad.transform;
            buttonText.transform.localPosition = new Vector3(0, 0, -0.02f);
            buttonText.transform.localEulerAngles = Vector3.zero;

            TextMesh buttonTextMesh = buttonText.AddComponent<TextMesh>();
            buttonTextMesh.font = Resources.Load<Font>("Fonts/Rubik");
            buttonText.GetComponent<MeshRenderer>().material = buttonTextMesh.font.material;
            buttonTextMesh.text = "OK";
            buttonTextMesh.characterSize = 0.015f;
            buttonTextMesh.fontSize = 32;
            buttonTextMesh.anchor = TextAnchor.MiddleCenter;
            buttonTextMesh.alignment = TextAlignment.Center;

            temp = buttonText.GetComponent<MeshRenderer>().material.mainTexture;
            buttonText.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/MessageText"));
            buttonText.GetComponent<MeshRenderer>().material.mainTexture = temp;

            buttonQuad.AddComponent<OculusErrorButton>();

            App.ShowCrosshair = true;
            App.Fade(false, 0);
        }

        internal class OculusErrorButton : MonoBehaviour
        {
            private bool selected = false;
            private Texture2D buttonTex;

            private void Update()
            {
                if (App.IsCrosshairHit(gameObject, App.RaycastSource.MotionController)
                    || App.IsCrosshairHit(gameObject, App.RaycastSource.Gaze))
                {
                    if (!selected)
                    {
                        if (buttonTex == null)
                        {
                            buttonTex = (Texture2D)gameObject.GetComponent<MeshRenderer>().material.mainTexture;
                        }
                        gameObject.GetComponent<MeshRenderer>().material.mainTexture = null;
                        transform.GetChild(0).GetComponent<TextMesh>().color = Color.gray;
                    }
                    selected = true;
                    if (VRInput.Confirm.Pressed)
                    {
                        Debug.Log("Quitting Unity");
                        Application.Quit();
                    }
                }
                else
                {
                    if (selected)
                    {
                        gameObject.GetComponent<MeshRenderer>().material.mainTexture = buttonTex;
                        transform.GetChild(0).GetComponent<TextMesh>().color = Color.white;
                    }
                    selected = false;
                }
            }
        }

		private IEnumerator CopyTbeInObb()
		{
			List<string> packaged = new List<string>();
			foreach (KeyValuePair<string, AppDataStruct.MediaBlock> kv in App.Data.Media)
			{
				if (App.Data.Media[kv.Key].Url.ToLower().EndsWith("tbe"))
				{
					if (App.PackageHasLatest(kv.Key))
					{
						string path = App.GetMediaPath(kv.Key);
						packaged.Add(path);
						Debug.Log("Adding packaged obb to list: " + path);
					}
				}
			}
			foreach (string s in packaged)
			{
				string newPath = TBE.ObbExtractor.getStreamingAssetsPath() + "/" + s;
				Debug.Log("New path : " + newPath);
				System.IO.Directory.CreateDirectory(new System.IO.FileInfo(newPath).Directory.FullName);
			}
			if (TBE.ObbExtractor.streamingAssetsAreInObb())
			{
				yield return TBE.ObbExtractor.extractFromObb(packaged.ToArray(), false, App.BasePath);
			}
		}

        private Dictionary<string, AppDataStruct.PackagedInfo> LoadPackagedInfo()
        {
            Dictionary<string, AppDataStruct.PackagedInfo> ReturnPackaged = 
                    new Dictionary<string, AppDataStruct.PackagedInfo>();
            TextAsset packagedJson = Resources.Load<TextAsset>("Packaged");
            if (packagedJson != null)
            {
                // TODO: remove
                Debug.Log("Packaged information found");

				JSONNode PackagedClass = JSON.Parse(packagedJson.text);
                JSONArray idArray, pathArray;
                if (PackagedClass["id"]!=null && PackagedClass["path"] != null)
                {
                    idArray = (JSONArray)PackagedClass["id"];
                    pathArray = (JSONArray)PackagedClass["path"];
                    for (int j = 0; j < idArray.Count; ++j)
                    {
                        AppDataStruct.PackagedInfo packagedInfo = new AppDataStruct.PackagedInfo() {
                            Path = pathArray[j]
                        };
                        ReturnPackaged[idArray[j]] = packagedInfo;
                    }
                }
            }
            return ReturnPackaged;
        }

        public void SaveLocalData() {
            if (App.LocalVersionTracking != null) {
                StartCoroutine(App.LocalVersionTracking.SaveData());
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// Returns the Android Activity used by the Unity device player. The caller is
        /// responsible for memory-managing the returned AndroidJavaObject.
        private static AndroidJavaObject GetActivity() {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (jc == null) {
                Debug.LogErrorFormat("Failed to get class {0}", "com.unity3d.player.UnityPlayer");
                return null;
            }
            AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null) {
                Debug.LogError("Failed to obtain current Android activity.");
                jc.Dispose();
                return null;
            }
            jc.Dispose();
            return activity;
        }

        /// Returns the application context of the current Android Activity.
        private static AndroidJavaObject GetApplicationContext(AndroidJavaObject activity) {
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            if (context == null) {
                Debug.LogError("Failed to get application context from Activity.");
                return null;
            }
            return context;
        }
#endif //UNITY_ANDROID && !UNITY_EDITOR
    }
}
