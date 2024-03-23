// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Headjack
{
    /// <summary>
    /// This static class is the starting point for all the functionality of the Headjack SDK
    /// </summary>
    public class App
    {
        #region PRIVATE VARIABLES
        /// <summary>
        /// false if on Cardboard and VrMode is not active
        /// </summary>
        private static bool VrMode = true;

        /// <summary>
        /// Whether to pause the video on app focus loss, see <see cref="App.PauseVideoOnFocusLoss">.
        /// </summary>
        private static bool _pauseVideoOnFocusLoss = true;

        /// <summary>
        /// Android internal storage can only be enabled before the app is initialized, so this
        /// bool keeps track of whether that option is still available
        /// </summary>
        private static bool canChangeAndroidInternalStorage = true;

        /// <summary>
        /// The radius (in meters) of the virtual sphere where monoscopic equirectangular video content is projected onto.
        /// </summary>
        private static float _monoSphereRadius = 10f;

        /// <summary>
        /// The number of attempts for setting tracking origin on Oculus with position tracking disabled.
        /// Oculus headsets with position tracking enabled sometimes report no position tracking for the first
        /// few attempts at setting tracking origin, so we need to retry a couple of times to be sure.
        /// </summary>
        private static int _oculusSetTrackingNoPositionCount = 0;

        #endregion
        #region PRIVATE PROPERTIES
        /// <summary>
        /// false if on Cardboard and VrMode is not active
        /// </summary>
        private static int CodecPriority(string codec)
        {
            if (codec == null) return 0;
            switch (codec.ToLower())
            {
                case "h265": return 2;
                case "hevc": return 2;
                case "vp9": return 1;
                default: return 0;
            }
        }
        #endregion
        #region INTERNAL VARIABLES
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
        /// <summary>
        /// Oculus platform menu
        /// </summary>
        internal static OVRPlatformMenu OculusMenu;
#endif

        /// <summary>
        /// Filepath to local folders
        /// </summary>
        internal static string BasePath;
        /// <summary>
        /// Message object instance
        /// </summary>
        internal static MessageQueue PopUp;
        /// <summary>
        /// Returns the currently loaded platform, the name says it all
        /// </summary>
        internal static VRPlatform CurrentlyLoadedPlatform;
        /// <summary>
        /// true if Cinema feature is supported and allowed
        /// </summary>
        internal static bool cinemaSupported = false;
        /// <summary>
        /// App ID
        /// </summary>
        internal static string AppId = null;
        /// <summary>
        /// Auth key
        /// </summary>
        internal static string Authkey = null;
        /// <summary>
        /// Analytics Object
        /// </summary>
        internal static Analytics AnalyticsManager;
        /// <summary>
        /// Data Object
        /// </summary>
        internal static GameObject DataObject;
        /// <summary>
        /// Reference to top-level VR Camera gameobject
        /// </summary>
        internal static GameObject VRCamera;
        /// <summary>
        /// The Mesh for the video player
        /// </summary>
        internal static Mesh Videosphere;
        /// <summary>
        /// The download manager
        /// </summary>
        internal static PPDownloader Downloader;
        /// <summary>
        /// Whether to use internal storage on Android
        /// </summary>
        internal static bool AndroidInternalStorage = false;
        /// <summary>
        /// App metadata
        /// </summary>
        public static AppDataStruct Data;
        /// <summary>
        /// Locally stored files/media version data
        /// </summary>
        public static LocalVersionTracking LocalVersionTracking;
        /// <summary>
        /// The currently selected tracking origin (e.g. floor level or eye level)
        /// </summary>
        internal static TrackingOrigin CurrentTrackingOrigin = TrackingOrigin.Legacy;

        #endregion
        #region INTERNAL FUNCTIONS
        internal static void SetPlatform()
        {
            Settings.PlatformSettings settings = Resources.Load<Settings.PlatformSettings>("Headjack Settings");
            if (settings == null)
            {
                Debug.LogWarning("No App information found");
                return;
            }
#if USE_OCULUS_SDK
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.Oculus)
            {

                GameObject ovrManagerObj = new GameObject("OVRManager");
                ovrManagerObj.AddComponent<OVRManager>();
                ovrManagerObj.AddComponent<DoNotDestroy>();
                CurrentlyLoadedPlatform = VRPlatform.Oculus;
            }
#endif
#if USE_OCULUS_LEGACY_SDK
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.OculusLegacy)
            {

                GameObject ovrManagerObj = new GameObject("OVRManager");
                ovrManagerObj.AddComponent<OVRManager>();
                ovrManagerObj.AddComponent<DoNotDestroy>();
                CurrentlyLoadedPlatform = VRPlatform.OculusLegacy;
            }
#endif
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.OpenVR)
            {
                CurrentlyLoadedPlatform = VRPlatform.OpenVR;
            }
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.Cardboard)
            {
                CurrentlyLoadedPlatform = VRPlatform.Cardboard;
            }
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.ViveWave)
            {
                CurrentlyLoadedPlatform = VRPlatform.ViveWave;
            }
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.Pico)
            {
                CurrentlyLoadedPlatform = VRPlatform.Pico;
            }
            if (settings.Platform == Settings.PlatformSettings.BuildPlatform.PicoPlatform)
            {
                CurrentlyLoadedPlatform = VRPlatform.PicoPlatform;
            }
            // force 60fps on iOS at startup
#if UNITY_IOS
            Application.targetFrameRate = 60;
#endif
        }

        /// <summary>
        /// Url for media file "Id"
        /// </summary>
        public static string GetMediaUrl(string Id)
        {
            return Data.Media[Id].Url;
        }
        /// <summary>
        /// Local path for media file "Id"
        /// </summary>
        public static string GetMediaPath(string Id)
        {
            return "Media/" + Id + "/" + Data.Media[Id].Filename;
        }

        /// <summary>
        /// stream url for video file "Id"
        /// </summary>
        internal static string GetVideoStream(string Id, Headjack.AppDataStruct.FormatBlock startAfter=null)
        {
            Debug.Log("Getting video stream url for id: "+Id);
            Headjack.AppDataStruct.FormatBlock formatData = GetVideoFormatData(Id, true, startAfter);
            //Debug.Log("BASE URL: " + Data.Video[Id].BaseUrl);
            if (formatData != null)
            {
                return formatData.StreamUrl;
            }
            else
            {
                Debug.Log("No format data found");
                return null;
            }
        }
        /// <summary>
        /// Local path for video file "Id"
        /// </summary>
        public static string GetVideoLocalSubPath(string Id, bool dl = false)
        {
            if (dl) {
                Headjack.AppDataStruct.FormatBlock VideoData = GetVideoFormatData(Id, false, null);
                if (VideoData != null) return "Video/" + Id + "/" + VideoData.Filename;
                return null;
            } else {
                if (!LocalVersionTracking.LocalVersion.ContainsKey(Id) || LocalVersionTracking.LocalVersion[Id] == null ||
                    LocalVersionTracking.LocalVersion[Id] == Headjack.AppDataStruct.PACKAGED_LABEL) {
                    return null;
                }
                return "Video/" + Id + "/" + LocalVersionTracking.LocalVersion[Id];
            }
        }
        /// <summary>
        /// Local path for video file "Id"
        /// </summary>
        public static string GetVideoPath(string Id)
        {
            return "Video/" + Id + "/";
        }
        /// <summary>
        /// Url for video file "Id"
        /// </summary>
        public static string GetVideoUrl(string Id)
        {
            AppDataStruct.FormatBlock VideoData = GetVideoFormatData(Id, false, null);
            if (VideoData != null) return VideoData.DownloadUrl;
            return null;
        }

        /// <summary>
        /// Local filename of video
        /// </summary>
        public static string GetVideoFilename(string Id)
        {
            AppDataStruct.FormatBlock VideoData = GetVideoFormatData(Id, false, null);
            if (VideoData != null) return VideoData.Filename;
            return null;
        }

        /// <summary>
        /// Size for video file "Id"
        /// </summary>
        internal static long GetVideoSize(string Id)
        {
            AppDataStruct.FormatBlock VideoData = GetVideoFormatData(Id, false, null);
            return (VideoData != null) ? VideoData.Filesize : -1;
        }

        internal static string GetAppPlatform() {
#if UNITY_EDITOR || UNITY_STANDALONE
            Debug.Log("App Data Platform: pcvr");
            return AppRequest.PLATFORM_PCVR;
#elif UNITY_IOS
            Debug.Log("App Data Platform: mobile");
            return AppRequest.PLATFORM_MOBILE;
#else // UNITY_ANDROID
            if (CurrentPlatform == VRPlatform.Cardboard) {
                Debug.Log("App Data Platform: mobile");
                return AppRequest.PLATFORM_MOBILE;
            } else if (CurrentPlatform == VRPlatform.Oculus) {
#if USE_OCULUS_SDK
                OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
                if (headset == OVRPlugin.SystemHeadset.Oculus_Quest) {
                    return AppRequest.PLATFORM_LEGACYVR;
                }
                return AppRequest.PLATFORM_HIGHENDVR;
#endif
            } else if (CurrentPlatform == VRPlatform.OculusLegacy) {
#if USE_OCULUS_LEGACY_SDK
                OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
                if ((int)headset < 9) {
                    AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build");
                    string product = buildClass.GetStatic<string>("PRODUCT").ToLower();
                    if (product == "hollywood" || 
                        product == "quest 2" || 
                        product == "quest_2" ||
                        product == "cambria" ||
                        product == "quest pro" ||
                        product == "quest_pro") {
                        Debug.Log("App Data Platform: q2");
                        return AppRequest.PLATFORM_HIGHENDVR;
                    }

                    Debug.Log("App Data Platform: lvr");
                    return AppRequest.PLATFORM_LEGACYVR;
                }
                Debug.Log("App Data Platform: q2");
                return AppRequest.PLATFORM_HIGHENDVR;
#endif
            } else if (CurrentPlatform == VRPlatform.PicoPlatform) {
#if USE_PICO_PLATFORM_SDK
                switch (Unity.XR.PXR.PXR_Input.GetControllerDeviceType()) {
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.G2:
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.Neo2:
                        Debug.Log("App Data Platform: lvr");
                        return AppRequest.PLATFORM_LEGACYVR;
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.Neo3:
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.PICO_4:
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.NewController:
                    default:
                        Debug.Log("App Data Platform: q2");
                        return AppRequest.PLATFORM_HIGHENDVR;
                }
#endif
                return AppRequest.PLATFORM_HIGHENDVR;
            } else if (CurrentPlatform == VRPlatform.ViveWave) {
#if USE_WAVE_SDK
                AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build");
                string model = buildClass.GetStatic<string>("MODEL").ToLower();
                if (model.Contains("focus 3") || model.Contains("focus_3")) {
                    Debug.Log("App Data Platform: q2");
                    return AppRequest.PLATFORM_HIGHENDVR;
                }

                Debug.Log("App Data Platform: mobile");
                return AppRequest.PLATFORM_MOBILE;
#endif
            }
#endif // UNITY_ANDROID
            Debug.Log("App Data Platform: lvr");
            return AppRequest.PLATFORM_LEGACYVR;
        }
       
        /// <summary>
        /// Check if a Video file is already downloaded
        /// </summary>
        internal static bool GotVideoFile(string VideoId)
        {
            if (LocalVersionTracking.LocalVersion[VideoId] == null) {
                return false;
            }
            if (LocalVersionTracking.LocalVersion[VideoId] == AppDataStruct.PACKAGED_LABEL) {
                return true;
            }
            return System.IO.File.Exists(BasePath + GetVideoPath(VideoId));
        }
        
        /// <summary>
        /// Check if an update is available for video with given id
        /// </summary>
        internal static bool UpdateAvailableVideo(string VideoId)
        {
            if (LocalVersionTracking.LocalVersion[VideoId] == null) {
                return true;
            }
            if (LocalVersionTracking.LocalVersion[VideoId] == AppDataStruct.PACKAGED_LABEL) {
                return false;
            }

            AppDataStruct.FormatBlock videoFormat = GetVideoFormatData(VideoId, false);
            if (videoFormat.Filename != null && LocalVersionTracking.LocalVersion[VideoId] != videoFormat.Filename) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Returns a string[] of all videos in a project
        /// </summary>
        internal static string GetProjectVideoId(string ProjectId)
        {
            if (ProjectId == null) return null;

            if (!Data.Project.ContainsKey(ProjectId))
            {
                Debug.LogWarning("ProjectId does not exist: " + ProjectId);
                return null;
            }

            return Data.Project[ProjectId].Video;
        }
        /// <summary>
        /// Returns the media ID of the spatial audio in a project
        /// </summary>
        internal static string GetProjectAudioId(string ProjectId)
        {
            if (ProjectId == null) return null;

            if (Data.Project[ProjectId].Audio != null)
            {
                return Data.Project[ProjectId].Audio;
            }

            return null;
        }
        /// <summary>
        /// Create video sphere mesh
        /// </summary>
        internal static void CreateVideoSphere()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            Videosphere = Generate.SortedSphere(16);
#else
            Videosphere = Generate.Sphere(new Vector2(1, 1), new Vector2(48, 48));
#endif
        }
        /// <summary>
        /// Create Videoplayer
        /// </summary>
        internal static void CreateVideoplayer(bool useLegacy = false, bool isHlsStream = false)
        {
            if (Player == null)
            {
                if (Videosphere == null)
                {
                    CreateVideoSphere();
                }
                GameObject P = new GameObject("Video Player");
                P.SetActive(false);
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_IOS
#if USE_AVPROVIDEO
                if (isHlsStream) {
                    Player = P.AddComponent<VideoPlayer_AVP>();
                } else {
                    Player = P.AddComponent<VideoPlayer_Unity>();
                }
#else // USE_AVPROVIDEO
                if (isHlsStream) {
                    Debug.LogError("Unity VideoPlayer does not support HLS (live)stream, please use Headjack Cloud Build to include proprietary video player with HLS support");
                }
                Player = P.AddComponent<VideoPlayer_Unity>();
#endif // USE_AVPROVIDEO
#else // UNITY_STANDALONE || UNITY_EDITOR || UNITY_IOS
                if (useLegacy) {
                     Player = P.AddComponent<VideoPlayer_OMS>();
                } else {
                     Player = P.AddComponent<VideoPlayer_EXO>();
                }
#endif // UNITY_STANDALONE || UNITY_EDITOR || UNITY_IOS

                Player.Initialize();
            }
            else
            {
                Debug.Log("Video player already exists");
            }
        }

        /// <summary>
        /// Returns the correct video format block for this device, and all the metadata with it
        /// </summary>
        internal static AppDataStruct.FormatBlock GetVideoFormatData(string Id, bool isStream, AppDataStruct.FormatBlock startAfter=null)
        {
            AppDataStruct.VideoBlock videoBlock = Data.Video[Id];
            if (videoBlock == null) {
                return null;
            }

            if (isStream) {
                if (videoBlock.StreamProfileCurrent < 1) {
                    return null;
                }

                for (int i = 0; i < videoBlock.Format.Count; ++i)
                {
                    AppDataStruct.FormatBlock f = videoBlock.Format[i];
                    if (f.StreamUrl != null) return f;
                }
            } else {
                if (videoBlock.DownloadProfileCurrent < 1) {
                    return null;
                }

                bool useCheck = UseDeviceCapabilityCheck();
                if (videoBlock.DownloadProfileCurrent == (int)VideoProfile.Original) {
                    useCheck = false;
                }

                for (int i = 0; i < videoBlock.Format.Count; ++i)
                {
                    AppDataStruct.FormatBlock f = videoBlock.Format[i];
                    if (useCheck && !UHDSupportInfo.Check(f)) continue; //skip if not supported
                    if (f.DownloadUrl == null) continue; //skip if not stream and no files are available
                    return f; //If not skipped, return immediately
                }
            }

            return null;
        }

        private static bool UseDeviceCapabilityCheck()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (CurrentlyLoadedPlatform == VRPlatform.Cardboard) {
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// The unique identifier of this app on this device.
        /// This UID is reset when the app is deleted from the phone
        /// </summary>
        internal static string Guid
        {
            get {
                // generate Guid for app analytics if it does not exist
                if (!PlayerPrefs.HasKey("PPGUID"))
                {
                    Debug.Log("Generating new Guid");
                    PlayerPrefs.SetString("PPGUID", System.Guid.NewGuid().ToString());
                }
                return PlayerPrefs.GetString("PPGUID");
            }
        }
        #endregion
        #region PUBLIC PROPERTIES
        /// <summary>
        /// Pass this string to <see cref="App.GetProjects(string, int, int)"/> to get all projects with explicitly no category set
        /// </summary>
        public const string NO_CATEGORY = "NO_CATEGORY";

        /// <summary>
        /// If true, the back button on the Oculus Mobile platforms will open the Oculus Platform Menu. You can disable this when the user goes out of the menu and in a video. 
        /// </summary>
        /// <returns>If the platform menu is enabled</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Oculus Mobile platforms only
        ///
        /// &gt; [!WARNING] 
        /// &gt; If disabled, make sure that the back button will always bring you back to the "upper menu" where it is enabled. The user must always be able to reach the Oculus platform menu by keep pressing the back button.
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Disable when playing a video
        /// void PlayVideo(string id)
        /// {
        ///     App.EnableOVRPlatformMenu=false;
        ///     App.Play(id, true, true, null);
        /// }
        /// </code>
        /// </example>
        public static bool EnableOVRPlatformMenu
        {
            get
            {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
                return (OculusMenu != null) ? OculusMenu.OnShortPress() : false;
#else
                Debug.LogError("EnableOVRPlatformMenu only applicable on Oculus platform");
                return false;
#endif
            }
            set
            {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
                if (OculusMenu != null) {
                    if (value) {
                        OculusMenu.OnShortPress = () => {return true;};
                    } else {
                        OculusMenu.OnShortPress = () => {return false;};
                    }
                }
#else
                Debug.LogError("EnableOVRPlatformMenu only applicable on Oculus platform");
#endif
            }
        }

        /// <summary>
        /// If true, downloaded data (including media) is stored on less accessible internal app storage on Android.
        /// By default, the app data is stored in app-specific storage that is still accessible through e.g. USB file access.
        /// </summary>
        /// <returns>Whether app internal storage is used on Android</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Can only be set before the app is initialized with <see cref="App.Initialize(OnEnd, bool, bool, string, string)"/>!
        ///
        /// &gt; [!NOTE] 
        /// &gt; Enabling internal app storage on Android disables the background downloading functionality on that platform, due to file permission limitations.
        /// </remarks>
        /// <example> 
        /// <code>
        /// // use app internal storage on Android
        /// App.UseAndroidInternalStorage = true;
        /// App.Initialize(OnInitialized);
        /// </code>
        /// </example>
        public static bool UseAndroidInternalStorage
        {
            get {
                return AndroidInternalStorage;
            }

            set {
                if (!canChangeAndroidInternalStorage) {
                    Debug.LogError("Cannot change Android Storage location, app is already initialized");
                    return;
                }
                AndroidInternalStorage = value;
            }
        }

        /// <summary>
        /// Returns whether app has published state set in Headjack
        /// </summary>
        /// <returns>True if the app is set to published in Headjack (and is therefore available to use)</returns>
        /// <remarks>
        /// &gt; [!WARNING] 
        /// &gt; If an app is set to unpublished in Headjack, the app will receive no media information, like projects, videos or thumbnails.
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Show message to the user when app is unpublished
        /// public void MessageUserUnpublished() 
        /// {
        ///     if (!Headjack.App.IsPublished)
        ///     {
        ///         Headjack.App.ShowMessage("This app is not published and is currently unavailable!", 5);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool IsPublished
        {
            get
            {
                return (Data != null) ? Data.App.Published : true;
            }
        }

        /// <summary>
        /// Headjack's crosshair
        /// </summary>
        /// <returns>True if the crosshair is currently visible</returns>
        /// <example> 
        /// <code>
        /// public void HideCrosshair() 
        /// {
        ///     App.ShowCrosshair=false;
        /// }
        /// </code>
        /// </example>
        public static bool ShowCrosshair
        {
            get
            {
                return (Crosshair.crosshair != null) ? Crosshair.crosshair.Show : false;
            }
            set
            {
                if (Crosshair.crosshair != null) Crosshair.crosshair.Show = value;
            }
        }

        /// <summary>
        /// Checks the current platform
        /// </summary>
        /// <returns>The current platform Headjack is running on</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; For testing: You can change this in the Headjack settings window
        /// </remarks>
        /// <example> 
        /// <code>
        /// void LogCurrentPlatform()
        /// {
        /// #if UNITY_ANDROID
        /// if (App.CurrentPlatform == App.VRPlatform.Oculus)
        /// {
        ///     Debug.Log("This app is running on Oculus Go/Quest");
        /// }
        /// if (App.CurrentPlatform == App.VRPlatform.Cardboard)
        /// {
        ///     Debug.Log("This app is running on Android Cardboard");
        /// }
        /// #endif
        /// #if UNITY_IOS
        /// if (App.CurrentPlatform == App.VRPlatform.Cardboard)
        /// {
        ///     Debug.Log("This app is running on iOS Cardboard");
        /// }
        /// #endif
        /// #if UNITY_STANDALONE
        /// if (App.CurrentPlatform == App.VRPlatform.Oculus)
        /// {
        ///     Debug.Log("This app is running on Oculus Rift");
        /// }
        /// if (App.CurrentPlatform == App.VRPlatform.OpenVR)
        /// {
        ///     Debug.Log("This app is running on HTC Vive");
        /// }
        /// #endif
        /// }
        /// </code>
        /// </example>
        public static VRPlatform CurrentPlatform
        {
            get
            {
                if (CurrentlyLoadedPlatform == VRPlatform.NotYetInitialized)
                {
                    SetPlatform();
                }
                return CurrentlyLoadedPlatform;
            }
        }

        /// <summary>
        /// Force conservative video downloads/streams, where the maximum resolution
        /// is capped at UHD (not recommended).
        /// </summary>
        public static bool ForceConservativeVideoSupport {
            get {
                return UHDSupportInfo.useConservativeSupport;
            } set {
                UHDSupportInfo.useConservativeSupport = value;
            }
        }

        /// <summary>
        /// Check if an update is available for media with given id
        /// </summary>
        public static bool UpdateAvailableMedia(string MediaId)
        {
            if (LocalVersionTracking.LocalVersion[MediaId] == null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a Media file is already downloaded
        /// </summary>
        public static bool GotMediaFile(string MediaId)
        {
            if (MediaId == null)
            {
                Debug.LogWarning("MediaId is null");
                return false;
            }
            if (LocalVersionTracking.LocalVersion[MediaId] == null) {
                return false;
            }
            if (LocalVersionTracking.LocalVersion[MediaId] == AppDataStruct.PACKAGED_LABEL) {
                return true;
            }
            return System.IO.File.Exists(BasePath + GetMediaPath(MediaId));
        }


        /// <summary>
        /// Returns whether the current template supports Cinema, and if this app
        /// is allowed to use it
        /// </summary>
        /// <returns>True if Cinema is supported and allowed</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Only valid after initialization
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Enable a gameobject only if Cinema is allowed
        /// public void ToggleGameObjectOnCinemaSupported(GameObject toggleThis) {
        ///     toggleThis.SetActive(App.CinemaSupported);
        /// }
        /// </code>
        /// </example>
        public static bool CinemaSupported
        {
            get
            {
                return cinemaSupported;
            }
        }

        /// <summary>
        /// Returns whether the Cinema server connection is live/online
        /// </summary>
        /// <returns>True if Cinema server connection is live, otherwise false</returns>
        /// <example> 
        /// <code>
        /// // Enable a gameobject only if Cinema server connection is online
        /// public void ToggleGameObjectOnCinemaConnect(GameObject toggleThis) {
        ///     toggleThis.SetActive(App.CinemaConnected);
        /// }
        /// </code>
        /// </example>
        public static bool CinemaConnected
        {
            get
            {
                if (PushServerHandler.instance != null)
                {
                    return PushServerHandler.instance.Connected;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns whether app is currently running in VR mode (e.g. stereo display), or not (e.g. fullscreen cardboard menu)
        /// </summary>
        /// <returns>True if the app is currently running in VR mode</returns>
        /// <example> 
        /// <code>
        /// // Show message to the user when not in VR mode
        /// public void MessageUserNotVR() 
        /// {
        ///     if (!Headjack.App.IsVRMode)
        ///     {
        ///         Headjack.App.ShowMessage("You are currently not in VR mode", 5);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool IsVRMode
        {
            get
            {
                if (CameraParent.activeSelf && VrMode)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Changing the camera scale will Shrink or Enlarge the world around you. Useful if an interface needs to get just a little bigger or smaller
        /// </summary>
        /// <returns>Returns the current camera scale</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will clamp between 0.01 and 100
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Shrink or grow with the arrow keys
        /// void Update()
        /// {
        ///     if (Input.GetKeyDown(KeyCode.UpArrow))
        ///     {
        ///         App.CameraScale+=1;
        ///     }
        ///     if (Input.GetKeyDown(KeyCode.DownArrow))
        ///     {
        ///         App.CameraScale-=1;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static float CameraScale
        {
            get
            {
                return (App.camera != null) ? App.camera.parent.localScale.x : -1;
            }
            set
            {
                if (App.camera != null)
                {
                    value = Mathf.Clamp(value, 0.01f, 100f);
                    App.camera.parent.localScale = new Vector3(value, value, value);
                }
            }

        }

        /// <summary>
        /// The radius (in meters) of the virtual sphere where monoscopic equirectangular video content is projected onto.
        /// </summary>
        /// <returns>Returns the current mono sphere radius</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; The default radius of 10 meters is comfortable as it matches the focal length of most current VR headset optics
        /// </remarks>
        /// <example> 
        /// <code>
        /// void SetMonoSphere(bool indoorVideo)
        /// {
        ///     if (indoorVideo) {
        ///         App.MonoSphereRadius = 2f;
        ///     } else {
        ///         App.MonoSphereRadius = 10f;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static float MonoSphereRadius
        {
            get
            {
                return _monoSphereRadius;
            }
            set
            {
                _monoSphereRadius = value;
                if (App.Player != null) {
                    App.Player.UpdateEyeScale();
                }
            }

        }

        /// <summary>
        /// Whether to automatically pause the video when the app loses focus, for instance on 
        /// Android when the on-screen keyboard is shown. Default is to pause video on focus loss (i.e. true).
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Changing this does not affect video pausing on Oculus platforms when OS menu is overlayed.
        /// </remarks>
        /// </summary>
        public static bool PauseVideoOnFocusLoss
        {
            get
            {
                return _pauseVideoOnFocusLoss;
            }
            set
            {
                _pauseVideoOnFocusLoss = value;
            }
        }

        /// <summary>
        /// Container class for app metadata, used by <see cref="App.GetAppMetadata()"/>.
        /// </summary>
        public class AppMetadata
        {

            /// <summary>
            /// True if the loaded application is published
            /// </summary>
            public bool Published;

            /// <summary>
            /// The application's name
            /// </summary>
            public string ProductName;

            /// <summary>
            /// Company name
            /// </summary>
            public string CompanyName;
        }

        /// <summary>
        /// Container class for video metadata, used by <see cref="App.GetVideoMetadata(string)"/>.
        /// </summary>
        public class VideoMetadata
        {

            /// <summary>
            /// Unique ID of the video, differs from projectId
            /// </summary>
            public string id;

            /// <summary>
            /// Dimensions tag
            /// </summary>
            public string ProjectionType;
            public string StereoMap;
            public bool Stereo;

            /// <summary>
            /// Duration of the video in milliseconds
            /// </summary>
            public long Duration;

            /// <summary>
            /// The video's dimensions
            /// </summary>
            public int Width, Height, AudioChannels;

            /// <summary>
            /// Formatted string of video duration
            /// </summary>
            public string DurationMMSS, DurationHHMMSS;

            /// <summary>
            /// True when this video can be played back as an adaptive (HLS) VOD or livestream
            /// </summary>
            public bool HasAdaptiveStream;
        }

        /// <summary>
        /// Container class with metadata about a media item (project thumbnail, custom media variable item)
        /// </summary>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Get media metadata using function <see cref="App.GetMediaMetadata(string)"/>
        ///
        /// &gt; [!NOTE] 
        /// &gt; Some metadata fields are only relevant to particular media items (e.g. <see cref="App.MediaMetadata.Width"/> for images)
        /// </remarks>
        public class MediaMetadata
        {

            /// <summary>
            /// Media ID of the item that this metadata describes
            /// </summary>
            public string Id;

            /// <summary>
            /// MIME type of this media item (e.g. "image/png")
            /// </summary>
            /// <remarks>
            /// &gt; [!NOTE]
            /// &gt; certain media items do not have an official MIME type, so they get an unofficial type (e.g. "audio/x-tbe" for a TBE spatial audio file)
            /// </remarks>
            public string MimeType;

            /// <summary>
            /// Filename of the media item when downloaded (e.g. using <see cref="App.DownloadSingleMedia(string, bool, OnEnd)"/>)
            /// </summary>
            /// <remarks>
            /// &gt; [!NOTE] 
            /// &gt; This is a randomly generated filename and not the original name shown on the Headjack website
            /// </remarks>
            public string Filename;

            /// <summary>
            /// Width (in pixels) of media item, for image files
            /// </summary>
            public int Width;

            /// <summary>
            /// Height (in pixels) of media item, for image files
            /// </summary>
            public int Height;

            /// <summary>
            /// Filesize (in bytes) of media item
            /// </summary>
            public long FileSize;
        }

        /// <summary>
        /// Container class for Category metadata, used by <see cref="App.GetCategoryMetadata(string)"/>.
        /// </summary>
        public class CategoryMetadata
        {

            /// <summary>
            /// Id of this category
            /// </summary>
            public string Id;

            /// <summary>
            /// Name of the Category
            /// </summary>
            public string Name;

            /// <summary>
            /// Additional description of the category
            /// </summary>
            public string Description;

            /// <summary>
            /// Media ID of the thumbnail associated with this category
            /// </summary>
            public string ThumbnailId;

            /// <summary>
            /// This category's parent category (null if this is not a sub-category)
            /// </summary>
            public string ParentId;

        }

        /// <summary>
        /// Container class with metadata about a Project
        /// </summary>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Get Project metadata using function <see cref="App.GetProjectMetadata(string, ByteConversionType)"/>
        ///
        /// &gt; [!NOTE] 
        /// &gt; Certain fields can be null when they arent uploaded on the server
        /// </remarks>
        public class ProjectMetadata
        {

            /// <summary>
            /// Id of the project
            /// </summary>
            public string Id;

            /// <summary>
            /// The project's title
            /// </summary>
            public string Title;

            /// <summary>
            /// The project's description (can be null)
            /// </summary>
            public string Description;

            /// <summary>
            /// The project's creation date (in ISO 9075 format)
            /// </summary>
            public string CreationDate;

            /// <summary>
            /// The project's total view count (if explicitly enabled for this project in the Headjack CMS)
            /// </summary>
            public int Views;

            /// <summary>   
            /// The category where the project can be found (can be null)
            /// </summary>
            public string Category;

            /// <summary>
            /// The media id of the thumbnail that belongs to this project (can be null)
            /// </summary>
            public string ThumbnailId;

            /// <summary>
            /// The video that is included in the project (can be null)
            /// </summary>
            public string VideoId;

            /// <summary>
            /// The external audio that is included in the project (can be null)
            /// </summary>
            public string AudioId;

            /// <summary>
            /// Filesize (in <see cref="App.ByteConversionType"/>) of all files in this project, when downloaded on current device
            /// </summary>
            public float TotalSize;
        }
#endregion
#region PUBLIC VARIABLES

         /// <summary>
         /// The video player instance
         /// </summary>
         /// <returns>The video player instance with it's own functions and variables, see <see cref="VideoPlayerBase"/> </returns>
        public static VideoPlayerBase Player;

        /// <summary>
        /// Transform of headjack's camera
        /// </summary>
        /// <returns>Transform in the middle of both eyes</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; GameObject associated with this Transform does not necessarily have a Camera component
        /// </remarks>
        public static Transform camera;

        /// <summary>
        /// Headjack's default crosshiar
        /// </summary>
        /// <returns>Crosshair class with all settings</returns>
        public static Crosshair Crosshair;

        /// <summary>
        /// All raycast information about what the user is looking at
        /// </summary>
        /// <returns>RaycastHit information</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.IsCrosshairHit(Collider, RaycastSource)"/> to quickly check if an certain object is being looked at
        /// </remarks>
        /// <example> 
        /// <code>
        /// public float DistanceToObjectLookingAt
        /// {
        ///     return App.CrosshairHit.distance;
        /// }
        /// </code>
        /// </example>
        public static RaycastHit CrosshairHit;

        /// <summary>
        /// All raycast information about what the user is pointing at with the motion controller
        /// </summary>
        /// <returns>RaycastHit information</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.IsCrosshairHit(Collider, RaycastSource)"/> to quickly check if an certain object is being aimed at
        /// </remarks>
        /// <example> 
        /// <code>
        /// public Vector3 AimingLocation
        /// {
        ///     return App.LaserHit.point;
        /// }
        /// </code>
        /// </example>
        public static RaycastHit LaserHit;

        /// <summary>
        /// All raycast information about what the user is touching on screen (Cardboard)
        /// </summary>
        /// <returns>RaycastHit information</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.IsCrosshairHit(Collider, RaycastSource)"/> to quickly check if an certain object is being touched
        /// </remarks>
        /// <example> 
        /// <code>
        /// public GameObject ObjectTouching
        /// {
        ///     return App.TouchHit.collider.gameObject;
        /// }
        /// </code>
        /// </example>
        public static RaycastHit TouchHit;

        /// <summary>
        /// The id of the video that is currently being played
        /// </summary>
        /// <returns>The id of the video that is currently being played</returns>
        public static string CurrentVideo;

        /// <summary>
        /// The id of the project that is currently being played
        /// </summary>
        /// <returns>The id of the project that is currently being played</returns>
        public static string CurrentProject;

        /// <summary>
        /// VR Camera GameObject
        /// </summary>
        /// <returns>GameObject of the active VR Camera</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; For Camera Position or Rotation, please use <see cref="App.camera"/>. For Disabling or Enabling the Camera, please use <see cref="App.SetCamera(bool, bool)"/>
        /// </remarks>
        public static GameObject CameraParent;
#endregion
#region PUBLIC FUNCTIONS

        /// <summary> 
        /// Initialize the app and download necessary data
        /// </summary>
        /// <param name="OnReady">Will be called when done</param>
        /// <param name="autoCreateCamera">Also activate the VR camera</param>
        /// <param name="cardboardStereoMode">(cardboard) VR Mode enabled for cardboard</param>
        /// <param name="platform">The encoding profile platform identifier to use (null means automatic)</param>
        /// <param name="customHost">Use a custom hosted app metadata file (contact support@headjack.io for more information)</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Must be called before all other Headjack operations
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void Initialize()
        /// {
        ///     App.Initialize(OnReady);
        /// }
        /// public void OnReady(bool Succes)
        /// {
        ///     if (Succes)
        ///     {
        ///         print("Initialized!");
        ///     } else
        ///     {
        ///         print("No internet connection");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void Initialize(OnEnd OnReady, bool autoCreateCamera = true, 
            bool cardboardStereoMode = true, string platform = null, string customHost = null)
        {
            Settings.PlatformSettings settings = Resources.Load<Settings.PlatformSettings>("Headjack Settings");
            if (settings == null)
            {
                Debug.LogError("No App information found");
                OnReady(false, "No App information found");
                return;
            }
            Initialize(OnReady, autoCreateCamera, cardboardStereoMode, settings.appID, 
                settings.AUTHKey, platform, customHost);
        }

        public static void Initialize(OnEnd OnReady, bool autoCreateCamera, bool cardboardStereoMode, 
            string appId, string authKey, string platform = null, string customHost = null)
        {
            if (string.IsNullOrEmpty(appId) || appId == "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")
            {
                Debug.LogError("No App ID found");
                OnReady(false, "No App ID found");
                return;
            }
            

            AppId = appId;
            Authkey = authKey;
            if (CurrentPlatform == VRPlatform.NotYetInitialized) {
                Debug.LogWarning("VR platform initialization failed");
            }

            Resources.Load<Material>("Materials/Unlit");

            // TODO(olaf): remove
            // if (!Analytics.ValidateAppID(appId, authKey))
            // {
            //     Debug.LogError("AUTH Key incorrect for this appID");
            //     OnReady(false, "AUTH Key incorrect for this appID");
            //     return;
            // }

            // initialize new XR loader (does not start VR/AR just yet)
            // UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.automaticLoading = false;
            // UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.automaticRunning = false;
            // UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoaderSync();

            canChangeAndroidInternalStorage = false;

            if (DataObject == null)
            {
                DataObject = new GameObject()
                {
                    name = "DATA",
                    // hideFlags = HideFlags.HideInHierarchy
                };
                DataObject.AddComponent<Startup>().Initialize(OnReady, appId, authKey, platform, 
                    customHost);
            }
            SetCamera(autoCreateCamera, cardboardStereoMode);
        }


        /// <summary> 
        /// Resets the initialization and configuration to allow for a different app initialization
        /// </summary>
        public static void Reset() {
            AppId = null;
            Authkey = null;

            if (DataObject != null) {
                GameObject.Destroy(DataObject);
            }

            App.Data = null;

            canChangeAndroidInternalStorage = true;
        }


        /// <summary>
        /// Download and import all textures
        /// </summary>
        /// <param name="OnLoaded">Will be called when done</param>
        /// <param name="GenerateMipmaps">Automatically generate mipmaps after importing</param>
        /// <param name="progressListener">Will be called with current progress information</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will only download textures once, or when they are updated on the server
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void LoadTextures()
        /// {
        ///     App.LoadAllTextures(OnReady);
        /// }
        /// public void OnReady(bool Succes)
        /// {
        ///     if (Succes)
        ///     {
        ///         print("Got all textures!");
        ///     } else
        ///     {
        ///         print("Could not load the textures");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void DownloadAllTextures(OnEnd OnLoaded = null, bool GenerateMipmaps = true, OnImageProgress progressListener = null)
        {
            if (DataObject != null)
            {
                DataObject.GetComponent<Startup>().LoadAllTextures(GenerateMipmaps, OnLoaded, progressListener);
            }
        }

        /// <summary>
        /// Download and import a texture
        /// </summary>
        /// <param name="Id">The media id or project of the texture/thumbnail</param>
        /// <param name="OnLoaded">Will be called when done</param>
        /// <param name="UnscaledSlow">Get the raw image from the server</param>
        /// <param name="GenerateMipmaps">Automatically generate mipmaps after importing</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will only download the texture once, or when it is updated on the server
        ///
        /// &gt; [!WARNING] 
        /// &gt; Headjack automatically converts images, these have a maximum resolution of 1024x1024. 
        /// &gt; You can download and import the original file by setting "UnscaledSlow" to true. However, 
        /// &gt; keep in mind that this can generate CPU spikes and some devices do not support textures higher then 2048x2048. 
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void DownLoadSingleTexture()
        /// {
        ///     App.LoadAllTextures("12345678",OnReady);
        /// }
        /// public void OnReady(bool Succes)
        /// {
        ///     if (Succes)
        ///     {
        ///         print("Got the texture!");
        ///     } else
        ///     {
        ///         print("Could not download the texture");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void DownLoadSingleTexture(string Id, OnEnd OnLoaded = null, bool UnscaledSlow = false, bool GenerateMipmaps = true)
        {
            if (Id == null)
            {
                Debug.LogWarning("Download Single Texture: Media ID is null");
                OnLoaded(false, "Media id not found");
                return;
            }
            if (DataObject != null)
            {
                if (Data.Project.ContainsKey(Id))
                {
                    Id = Data.Project[Id].Thumbnail;
                }
                if (Id == null)
                {
                    Debug.LogWarning("Download Single Texture: Media ID is null");
                    OnLoaded(false, "Media id not found");
                    return;
                }
                if (UnscaledSlow)
                {
                    if (Data.Media.ContainsKey(Id + "_UNSCALED"))
                    {
                        Id = Id + "_UNSCALED";
                    }
                }
                if (Data.Media.ContainsKey(Id))
                {
                    DataObject.GetComponent<Startup>().LoadTexture(Id, GenerateMipmaps, OnLoaded);
                }
                else
                {
                    if (OnLoaded != null)
                    {
                        OnLoaded(false, "Media id not found");
                    }
                }
            }
            else
            {
                if (OnLoaded != null)
                {
                    OnLoaded(false, "No Data Object");
                }
            }
        }

        public static void Prepare(string ProjectId, bool Stream, bool WifiOnly, OnEnd onEnd = null, long VideoTime = 0)
        {
            if (ProjectId == null || ProjectId.Length == 0)
            {
                onEnd(false, "Incorrect project ID");
                Debug.LogWarning("Incorrect project ID");
                return;
            }

            string VideoId = GetProjectVideoId(ProjectId);
            VideoAvailability vidAvailable = GetVideoAvailability(VideoId, Stream);
            if ((int)vidAvailable < 1) {
                switch (vidAvailable) {
                    case VideoAvailability.NoVideoInProject:
                        onEnd(false, "Oops, It looks like the project doesn't contain any video\nPlease return to the Headjack CMS and add a video to your project and restart this app.");
                    break;
                    case VideoAvailability.Disabled:
                        string verb = Stream ? "streaming" : "playback";
                        onEnd(false, $"Oops, It looks like video {verb} is explicitly disabled for this platform\nPlease return to the Headjack CMS to enable video {verb} for this device.");
                    break;
                    case VideoAvailability.FormatNotSelected:
                        onEnd(false, "Oops, you did not configure this video to be compatible with this platform. Please return to the Headjack CMS and set up your video for this device.\nIn the Headjack CMS, navigate to Content > \"Video Name\" > Device Settings to enable this platform for your video.");
                    break;
                    case VideoAvailability.UnavailableWillUpdate:
                        onEnd(false, "Oops, The video is still being processed for this platform\nPlease restart the app and try again when the video transcoding is completed.");
                    break;
                    case VideoAvailability.Incompatible:
                        onEnd(false, "Oops, None of the available video formats work on this device\nPlease try viewing this app on a more powerful device.");
                    break;
                    case VideoAvailability.IncompatibleWillUpdate:
                        onEnd(false, "Oops, The video is still being processed for this platform\nPlease restart the app and try again when the video transcoding is completed.");
                    break;
                }
                return;
            }

            if (!Stream && !GotFiles(ProjectId)) {
                onEnd(false, "Oops, The video has not been downloaded yet\nPlease download the video or stream the video");
                return;
            }

            CurrentVideo = VideoId;
            Debug.Log("Video ID: " + VideoId);

            //set shader projection
            Shader.DisableKeyword("GAMMA_TO_LINEAR");
            Shader.DisableKeyword("FORCE_TEXTURE_FLIP");
            Shader.DisableKeyword("MESH");
            Shader.DisableKeyword("LATLONG");
            Shader.DisableKeyword("FISHEYE");
            Shader.EnableKeyword("PROJECT");
            string projection = "EQUIRECT";
            try
            {
                if (App.Data.Video.ContainsKey(CurrentVideo)) {
                    projection = App.Data.Video[CurrentVideo].Projection.Type;
                    switch (projection) {
                    case "FLAT":
                        Shader.DisableKeyword("PROJECT");
                        Shader.EnableKeyword("MESH");
                        break;
                    case "EQUIRECT":
                    default:
                        int fovx = App.Data.Video[CurrentVideo].Projection.Params["FovX"];
                        int fovy = App.Data.Video[CurrentVideo].Projection.Params["FovY"];
                        if (fovx < 360 || fovy < 180)
                        {
                            Shader.DisableKeyword("PROJECT");
                            Shader.EnableKeyword("LATLONG");
                            Shader.SetGlobalFloat("_PROJ_LAT", fovx);
                            Shader.SetGlobalFloat("_PROJ_LONG", fovy);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(" Could not set projects - " + e.Message + " - " + e.StackTrace);
            }

            //legacy videoplayer for streaming mp4 and webm on android
            bool useLegacyVideoPlayer = false;
#if UNITY_ANDROID
            string url = GetVideoStream(VideoId);
            useLegacyVideoPlayer = (Stream && (url.EndsWith(".mp4") || url.EndsWith(".webm")));
#endif

            bool isHlsStream = false;
#if UNITY_IOS || UNITY_STANDALONE
            string url = GetVideoStream(VideoId);
            isHlsStream = Stream && url.EndsWith(".m3u8");
#endif

            CreateVideoplayer(useLegacyVideoPlayer, isHlsStream);
            CurrentProject = ProjectId;
            if (AnalyticsManager != null)
            {
                AnalyticsManager.StartingVideo(ProjectId, VideoId);
            }
            string stereoMap = Data.Video[VideoId].Projection.StereoMap.Trim();
            bool isStereo = false;
            switch (stereoMap)
            {
                case "TB":
                    isStereo = true;
                    Player.mapping = VideoPlayerBase.Mapping.TB;
                    break;
                case "LR":
                    isStereo = true;
                    Player.mapping = VideoPlayerBase.Mapping.LR;
                    break;
                default:
                    Player.mapping = VideoPlayerBase.Mapping.MONO;
                    break;
            }
            Player.SetEyes(isStereo, isStereo, projection);

            // get video metadata
            AppDataStruct.FormatBlock videoFormat = GetVideoFormatData(VideoId, false, null);

            // set higher render resolution on Oculus for maximum video quality in VR
#if UNITY_ANDROID && !UNITY_EDITOR && USE_OCULUS_SDK
            if (CurrentPlatform == VRPlatform.Oculus) {
                int pixelCount = 4096 * 4096;
                // double frameRate = 60;
                if (videoFormat != null) {
                    pixelCount = videoFormat.Width * videoFormat.Height;
                    // frameRate = videoFormat.Framerate;
                }

                // streaming can display higher quality, so use safe multiplier
                // high resolutions or framerates allow less render resolution

                // Oculus Quest (and above)
                if (Stream || pixelCount >= (2560 * 1440)) {
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                } else {
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                }

                Debug.Log("Setting render resolution multiplier to " + UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR && USE_OCULUS_LEGACY_SDK
            if (CurrentPlatform == VRPlatform.OculusLegacy) {
                int pixelCount = 4096 * 4096;
                // double frameRate = 60;
                if (videoFormat != null) {
                    pixelCount = videoFormat.Width * videoFormat.Height;
                    // frameRate = videoFormat.Framerate;
                }

                // streaming can display higher quality, so use safe multiplier
                // high resolutions or framerates allow less render resolution
                if ((int)OVRPlugin.GetSystemHeadsetType() <= (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                    // Oculus Go (and below)
                    if (pixelCount > (3840 * 2160)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.2f;
                    } else if (Stream || pixelCount >= (2560 * 1440)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                    } else {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                    }
                } else {
                    // Oculus Quest (and above)
                    if (Stream || pixelCount >= (2560 * 1440)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                    } else {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                    }
                }

                Debug.Log("Setting render resolution multiplier to " + UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }
#endif

            if (Stream || Data.Project[ProjectId].Audio == null)
            {
                Player.SetAudioConfig(true, false);
            }
            else
            {
                // Load spatial audio plugin (TBE)
                if (Player.ExternalAudio == null)
                {
                    Player.ExternalAudio = Player.gameObject.AddComponent<AudioPlayer_TBE>();
                    if (PackageHasLatest(Data.Project[ProjectId].Audio)&&!Application.dataPath.Contains("obb"))
                    {
                        Player.ExternalAudio.Load("STREAMINGASSET:" + Data.Packaged[Data.Project[ProjectId].Audio].Path);
                    }
                    else
                    {
                        Player.ExternalAudio.Load(BasePath + GetMediaPath(Data.Project[ProjectId].Audio));
                    }
                    // some players need to correct spatial audio timing based on video codec
                    Player.SetFormatData(videoFormat);
                }
                Player.SetAudioConfig(false, true);
            }

            if (Stream)
            {
                Player.isStream = true;
                if (WifiOnly && Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork)
                {
                    if (onEnd != null)
                    {
                        onEnd(false, "NoWifi");
                    }
                    return;
                }
                else
                {
                    string videoUrl = GetVideoStream(VideoId); //BODHI
                    string fallbackUrl = null;
                    if (videoUrl == null)
                    {
                        if (onEnd != null)
                        {
                            onEnd(false, "the video url is null");
                        }
                        return;
                    }

                    Player.Prepare(videoUrl, Stream, onEnd, fallbackUrl, VideoTime);
                }
            }
            else
            {
                Player.isStream = false;
                Debug.Log("Playing: " + VideoId);
                if (PackageHasLatest(VideoId))
                {
                    Debug.Log("Playing Video from streaming assets");
                    Player.Prepare("STREAMINGASSET:" + Data.Packaged[VideoId].Path, false, onEnd, null, VideoTime);
                }
                else
                {

                    string path = BasePath + GetVideoLocalSubPath(VideoId);
                    Debug.Log("VIDEO PATH: " + path);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                        Debug.Log("Playing on Mobile");
                    Player.Prepare("file://" + path, false, onEnd, null, VideoTime);
#endif
#if UNITY_STANDALONE || UNITY_EDITOR

                    Player.Prepare(path, false, onEnd, null, VideoTime);
#endif
                }
            }
        }


        /// <summary>
        /// Play a project
        /// </summary>
        /// <param name="ProjectId">The project you want to play</param>
        /// <param name="Stream">Directly stream the video from the server</param>
        /// <param name="WifiOnly">(Streaming) only stream when connected to a wifi network</param>
        /// <param name="onEnd">Will be called when the video is finished</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will automatically create and initialize the video player. If stream is false, the video won't play if it isn't downloaded first
        ///
        /// &gt; [!WARNING] 
        /// &gt; Enabling WifiOnly is strongly recommended when using Stream to avoid high mobile data costs
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void StreamFirstVideo()
        /// {
        ///     string id = App.GetProjects()[0];
        ///     App.Play(id, true, true, onEnd);
        /// }
        /// public void onEnd(bool succes, string error)
        /// {
        ///     print("Video has finished playing!");
        /// }
        /// </code>
        /// </example>
        public static void Play(string ProjectId, bool Stream, bool WifiOnly, OnEnd onEnd = null)
        {
            if (ProjectId == null || ProjectId.Length == 0)
            {
                onEnd(false, "Incorrect project ID");
                Debug.LogWarning("Incorrect project ID");
                return;
            }
            string VideoId = GetProjectVideoId(ProjectId);
            VideoAvailability vidAvailable = GetVideoAvailability(VideoId, Stream);
            if ((int)vidAvailable < 1) {
                switch (vidAvailable) {
                    case VideoAvailability.NoVideoInProject:
                        onEnd(false, "Oops, It looks like the project doesn't contain any video\nPlease return to the Headjack CMS and add a video to your project and restart this app.");
                    break;
                    case VideoAvailability.Disabled:
                        string verb = Stream ? "streaming" : "playback";
                        onEnd(false, $"Oops, It looks like video {verb} is explicitly disabled for this platform\nPlease return to the Headjack CMS to enable video {verb} for this device.");
                    break;
                    case VideoAvailability.FormatNotSelected:
                        onEnd(false, "Oops, you did not configure this video to be compatible with this platform. Please return to the Headjack CMS and set up your video for this device.\nIn the Headjack CMS, navigate to Content > \"Video Name\" > Device Settings to enable this platform for your video.");
                    break;
                    case VideoAvailability.UnavailableWillUpdate:
                        onEnd(false, "Oops, The video is still being processed for this platform\nPlease restart the app and try again when the video transcoding is completed.");
                    break;
                    case VideoAvailability.Incompatible:
                        onEnd(false, "Oops, None of the available video formats work on this device\nPlease try viewing this app on a more powerful device.");
                    break;
                    case VideoAvailability.IncompatibleWillUpdate:
                        onEnd(false, "Oops, The video is still being processed for this platform\nPlease restart the app and try again when the video transcoding is completed.");
                    break;
                }
                return;
            }

            if (!Stream && !GotFiles(ProjectId)) {
                onEnd(false, "Oops, The video has not been downloaded yet\nPlease download the video or stream the video");
                return;
            }

            CurrentVideo = VideoId;
            Debug.Log("Video ID: " + VideoId);

            //set shader projection
            Shader.DisableKeyword("GAMMA_TO_LINEAR");
            Shader.DisableKeyword("FORCE_TEXTURE_FLIP");
            Shader.DisableKeyword("MESH");
            Shader.DisableKeyword("LATLONG");
            Shader.DisableKeyword("FISHEYE");
            Shader.EnableKeyword("PROJECT");
            string projection = "EQUIRECT";
            try
            {
                if (App.Data.Video.ContainsKey(CurrentVideo)) {
                    projection = App.Data.Video[CurrentVideo].Projection.Type;
                    switch (projection) {
                    case "FLAT":
                        Shader.DisableKeyword("PROJECT");
                        Shader.EnableKeyword("MESH");
                        break;
                    case "EQUIRECT":
                    default:
                        int fovx = App.Data.Video[CurrentVideo].Projection.Params["FovX"];
                        int fovy = App.Data.Video[CurrentVideo].Projection.Params["FovY"];
                        if (fovx < 360 || fovy < 180)
                        {
                            Shader.DisableKeyword("PROJECT");
                            Shader.EnableKeyword("LATLONG");
                            Shader.SetGlobalFloat("_PROJ_LAT", fovx);
                            Shader.SetGlobalFloat("_PROJ_LONG", fovy);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(" Could not set projects - " + e.Message + " - " + e.StackTrace);
            }

            //legacy videoplayer for streaming mp4 and webm on android
            bool useLegacyVideoPlayer = false;
#if UNITY_ANDROID
            string url = GetVideoStream(VideoId);
            useLegacyVideoPlayer = (Stream && (url.EndsWith(".mp4") || url.EndsWith(".webm")));
#endif

            bool isHlsStream = false;
#if UNITY_IOS || UNITY_STANDALONE
            string url = GetVideoStream(VideoId);
            isHlsStream = Stream && url.EndsWith(".m3u8");
#endif

            CreateVideoplayer(useLegacyVideoPlayer, isHlsStream);
            CurrentProject = ProjectId;
            if (AnalyticsManager != null)
            {
                AnalyticsManager.StartingVideo(ProjectId, VideoId);
            }
            string stereoMap = Data.Video[VideoId].Projection.StereoMap.Trim();
            bool isStereo = false;
            switch (stereoMap)
            {
                case "TB":
                    isStereo = true;
                    Player.mapping = VideoPlayerBase.Mapping.TB;
                    break;
                case "LR":
                    isStereo = true;
                    Player.mapping = VideoPlayerBase.Mapping.LR;
                    break;
                default:
                    Player.mapping = VideoPlayerBase.Mapping.MONO;
                    break;
            }
            Player.SetEyes(isStereo, isStereo, projection);

            // get video metadata
            AppDataStruct.FormatBlock videoFormat = GetVideoFormatData(VideoId, false, null);

            // set higher render resolution on Oculus for maximum video quality in VR
#if UNITY_ANDROID && !UNITY_EDITOR && USE_OCULUS_SDK
            if (CurrentPlatform == VRPlatform.Oculus) {
                int pixelCount = 4096 * 4096;
                // double frameRate = 60;
                if (videoFormat != null) {
                    pixelCount = videoFormat.Width * videoFormat.Height;
                    // frameRate = videoFormat.Framerate;
                }

                // streaming can display higher quality, so use safe multiplier
                // high resolutions or framerates allow less render resolution

                // Oculus Quest (and above)
                if (Stream || pixelCount >= (2560 * 1440)) {
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                } else {
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                }

                Debug.Log("Setting render resolution multiplier to " + UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR && USE_OCULUS_LEGACY_SDK
            if (CurrentPlatform == VRPlatform.OculusLegacy) {
                int pixelCount = 4096 * 4096;
                // double frameRate = 60;
                if (videoFormat != null) {
                    pixelCount = videoFormat.Width * videoFormat.Height;
                    // frameRate = videoFormat.Framerate;
                }

                // streaming can display higher quality, so use safe multiplier
                // high resolutions or framerates allow less render resolution
                if ((int)OVRPlugin.GetSystemHeadsetType() <= (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                    // Oculus Go (and below)
                    if (pixelCount > (3840 * 2160)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.2f;
                    } else if (Stream || pixelCount >= (2560 * 1440)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                    } else {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                    }
                } else {
                    // Oculus Quest (and above)
                    if (Stream || pixelCount >= (2560 * 1440)) {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
                    } else {
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.8f;
                    }
                }

                Debug.Log("Setting render resolution multiplier to " + UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }
#endif

            if (Stream || Data.Project[ProjectId].Audio == null)
            {
                Player.SetAudioConfig(true, false);
            }
            else
            {
                // Load spatial audio plugin (TBE)
                if (Player.ExternalAudio == null)
                {
                    Player.ExternalAudio = Player.gameObject.AddComponent<AudioPlayer_TBE>();
                    if (PackageHasLatest(Data.Project[ProjectId].Audio)&&!Application.dataPath.Contains("obb"))
                    {
                        Player.ExternalAudio.Load("STREAMINGASSET:" + Data.Packaged[Data.Project[ProjectId].Audio].Path);
                    }
                    else
                    {
                        Player.ExternalAudio.Load(BasePath + GetMediaPath(Data.Project[ProjectId].Audio));
                    }
                    // some players need to correct spatial audio timing based on video codec
                    Player.SetFormatData(videoFormat);
                }
                Player.SetAudioConfig(false, true);
            }

            if (Stream)
            {
                Player.isStream = true;
                if (WifiOnly && Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork)
                {
                    if (onEnd != null)
                    {
                        onEnd(false, "NoWifi");
                    }
                    return;
                }
                else
                {
                    string videoUrl = GetVideoStream(VideoId);//BODHI
                    //Debug.Log(videoUrl);
                    string fallbackUrl = null;
                    if (videoUrl == null)
                    {
                        if (onEnd != null)
                        {
                            onEnd(false, "the video url is null");
                        }
                        return;
                    }
                    //Debug.Log("Playing " + videoUrl + " with fallback " + fallbackUrl);

                    Player.Play(videoUrl, Stream, onEnd, fallbackUrl);
                }
            }
            else
            {
                Player.isStream = false;
                Debug.Log("Playing: " + VideoId);
                if (PackageHasLatest(VideoId))
                {
                    Debug.Log("Playing Video from streaming assets");
                    Player.Play("STREAMINGASSET:" + Data.Packaged[VideoId].Path, false, onEnd);
                }
                else
                {

                    string path = BasePath + GetVideoLocalSubPath(VideoId);
                    Debug.Log("VIDEO PATH: "+path);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                        Debug.Log("Playing on Mobile");
                    Player.Play("file://" + path, false, onEnd);
#endif
#if UNITY_STANDALONE || UNITY_EDITOR
                    
                    Player.Play(path, false, onEnd);
#endif
                }
            }
        }

        public static void Play(int projectNr, bool Stream, bool WifiOnly, OnEnd onEnd = null)
        {
            try
            {
                Play(GetProjects()[projectNr], Stream, WifiOnly, onEnd);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message + " - "+e.StackTrace);
                if(onEnd != null) onEnd(false, e.Message);
            }
        }

        /// <summary>
        /// Download all files in a project
        /// </summary>
        /// <param name="ProjectId">The project you want to download</param>
        /// <param name="WifiOnly">Only download when connected to Wifi</param>
        /// <param name="onEnd">Will be called when the download is finished</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will call onEnd with (false,"NoWifi") if WifiOnly is true and there is no wifi connection
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string id;
        /// public void DownloadAndPlayFirstVideo()
        /// {
        ///     id = App.GetProjects()[0];
        ///     App.Download(id, true, OnDownloaded);
        /// }
        /// public void OnDownloaded(bool succes, string error)
        /// {
        ///     if (succes)
        ///     {
        ///         App.Play(id, false, false, null);
        ///     }else{
        ///     Debug.Log(error);
        /// }
        /// }
        /// </code>
        /// </example>
        public static void Download(string ProjectId, bool WifiOnly = false, OnEnd onEnd = null)
        {
            if (GotFiles(ProjectId) && !UpdateAvailable(ProjectId))
            {
                Debug.Log("Already got files for project " + ProjectId);
                if (onEnd != null) onEnd(true, "Already got files for project");
                return;
            }
            if (WifiOnly && Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                onEnd(false, "NoWifi");
                Debug.Log("Wifi/Local Connection Required");
            }
            else
            {
                switch (GetVideoAvailability(ProjectId, false)) {
                    case VideoAvailability.Disabled:
                         if (onEnd != null) onEnd(false, "Downloading has been disabled for this video");
                        return;
                    case VideoAvailability.FormatNotSelected:
                         if (onEnd != null) onEnd(false, "This video has not been set up for this platform");
                        return;
                    case VideoAvailability.Incompatible:
                         if (onEnd != null) onEnd(false, "Oops, you did not configure this video to be compatible with this platform. Please return to the Headjack CMS and set up your video for this device.\nIn the Headjack CMS, navigate to Content > \"Video Name\" > Device Settings to enable this platform for your video.");
                        return;
                    case VideoAvailability.NoVideoInProject:
                         if (onEnd != null) onEnd(false, "This project has no video attached; add a video in Headjack");
                        return;
                    case VideoAvailability.IncompatibleWillUpdate:
                         if (onEnd != null) onEnd(false, "Video is still processing; please restart the app when transcoding is finished");
                        return;
                    case VideoAvailability.UnavailableWillUpdate:
                         if (onEnd != null) onEnd(false, "Video is still processing; please restart the app when transcoding is finished");
                        return;
                } 

                if (CanBeDownloaded(ProjectId))
                {
                    Downloader.DownloadProject(ProjectId, WifiOnly, onEnd);
                }
                else
                {
                    Debug.LogWarning("One or more file url's are null");
                    if (onEnd != null) onEnd(false, "One or more file url's are null");
                }
            }
        }

        /// <summary>
        /// Check if a project's contents can be downloaded.
        /// </summary>
        /// <param name="projectId">Project id</param>
        /// <returns>true if the project can be downloaded</returns>
        /// <example> 
        /// <code>
        /// public bool DownloadIfPossible(ProjectId)
        /// {
        ///     if(App.CanBeDownloaded(ProjectId))
        ///     {
        ///         App.Download(ProjectId);
        ///         return true;
        ///     }else{
        ///         return false;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool CanBeDownloaded(string projectId)
        {
            if (!Data.Project.ContainsKey(projectId)) return false;

            string AudioId = GetProjectAudioId(projectId);
            string VideoId = GetProjectVideoId(projectId);

            if (AudioId != null && LocalVersionTracking.LocalVersion[AudioId] != AppDataStruct.PACKAGED_LABEL &&
                GetMediaUrl(AudioId) != null) {
                return true;
            }
            if (VideoId != null && LocalVersionTracking.LocalVersion[VideoId] != AppDataStruct.PACKAGED_LABEL &&
                GetVideoUrl(VideoId) != null) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancel an active download
        /// </summary>
        /// <param name="ProjectId">Project download to cancel</param>
        /// <example> 
        /// <code>
        /// public void CancelAllDownloads()
        /// {
        ///     string[] projects = App.GetProjects();
        ///     foreach (string project in projects)
        ///     {
        ///         App.Cancel(project);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void Cancel(string ProjectId)
        {
            if (Downloader.Active.ContainsKey(ProjectId))
            {
                Downloader.Active[ProjectId] = false;
            }
        }

        /// <summary>
        /// Delete all local files of a project
        /// </summary>
        /// <param name="ProjectId">The project id</param>
        /// <example> 
        /// <code>
        /// public void DeleteAllProjects()
        /// {
        ///     string[] projects = App.GetProjects();
        ///     foreach (string project in projects)
        ///     {
        ///         App.Delete(project);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void Delete(string ProjectId)
        {
            Debug.Log("deleting project " + ProjectId);

            string videoId = GetProjectVideoId(ProjectId);
            if (videoId != null) {
                try {
                    System.IO.Directory.Delete(BasePath + GetVideoPath(videoId), true);
                } catch (System.IO.DirectoryNotFoundException) {

                } catch (System.Exception e) {
                    Debug.LogError("Failed to delete video " + videoId + " : " + e.Message);
                }

                if (LocalVersionTracking.LocalVersion[videoId] != AppDataStruct.PACKAGED_LABEL) {
                    LocalVersionTracking.LocalVersion[videoId] = null;
                }
            }

            string audioId = GetProjectAudioId(ProjectId);
            if (audioId != null) {
                try {
                    System.IO.Directory.Delete(BasePath + "Media/" + audioId + "/", true);
                } catch (System.IO.DirectoryNotFoundException) {

                } catch (System.Exception e) {
                    Debug.LogError("Failed to delete audio " + audioId + " : " + e.Message);
                }

                if (LocalVersionTracking.LocalVersion[audioId] != AppDataStruct.PACKAGED_LABEL) {
                    LocalVersionTracking.LocalVersion[audioId] = null;
                }
            }

            DataObject.GetComponent<Startup>().SaveLocalData();
        }

        /// <summary>
        /// Check if you have all the files of a project
        /// </summary>
        /// <param name="ProjectId">Project to check</param>
        /// <returns>True if you have all the files</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Does not check if you have the latest version of the files, for that see <see cref="App.UpdateAvailable"/>
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void StreamOnlyWhenFilesAreNotPresent(string projectid)
        /// {
        ///     if (App.GotFiles(projectid))
        ///     {
        ///         App.Play(projectid, false, false, null);
        ///     }
        ///     else
        ///     {
        ///         App.Play(projectid, true, true, null);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool GotFiles(string ProjectId)
        {
            if (ProjectId == null) return true;

            string videoId = GetProjectVideoId(ProjectId);
            if (videoId != null && LocalVersionTracking.LocalVersion[videoId] == null) {
                return false;
            }

            string audioId = GetProjectAudioId(ProjectId);
            if (audioId != null && LocalVersionTracking.LocalVersion[audioId] == null) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a project or media file has an update available
        /// </summary>
        /// <param name="Id">Id to check</param>
        /// <returns>True if there is an update available</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Will also return True if the files are not yet downloaded
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void CheckForUpdates()
        /// {
        ///     string[] projects = App.GetProjects();
        ///     foreach (string project in projects)
        ///     {
        ///         if(App.UpdateAvailable(project))
        ///         {
        ///             print(project + " has an update available!");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool UpdateAvailable(string Id)
        {
            if (Data.Project.ContainsKey(Id))
            {
                string videoId = GetProjectVideoId(Id);
                if (videoId != null && UpdateAvailableVideo(videoId)) {
                    return true;
                }
                string audioId = GetProjectAudioId(Id);
                if (audioId != null && UpdateAvailableMedia(audioId)) {
                    return true;
                }
                return false;
            }
            if (Data.Media.ContainsKey(Id))
            {
                return UpdateAvailableMedia(Id);
            }
            if (Data.Video.ContainsKey(Id))
            {
                return UpdateAvailableVideo(Id);
            }
            Debug.Log("Given id not found");
            return false;
        }

        internal static FadeEffect fade;

        /// <summary>
        /// Fade screen to or from black
        /// </summary>
        /// <param name="ToBlack">True to fade to black (fade out), false to fade from black (fade in)</param>
        /// <param name="Time">Duration of the fade effect in seconds</param>
        /// <param name="OnFade">Event function that gets executed when fade is completed</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use this function to hide stutter, by fading to black before heavy loading
        ///
        /// &gt; [!NOTE] 
        /// &gt; The parameters of OnFade (bool success, string error) are unused
        /// </remarks>
        /// <example> 
        /// <code>
        /// UnityEngine.GameObject interface;
        /// // This function fades the screen to black for 1.5 seconds and then disables the 'interface' GameObject
        /// // Another function has to fade the screen back in, or the screen will remain black
        /// void FadeOutInterface()
        /// {
        ///     App.Fade(true, 1.5f, delegate (bool success, string error) {interface.SetActive(false);});
        /// }
        /// </code>
        /// </example>
        public static void Fade(bool ToBlack, float Time = 1, OnEnd OnFade = null)
        {
            fade.Fade(ToBlack, Time, OnFade);
        }

        /// <summary>
        /// Returns true if the user is aiming at the target
        /// </summary>
        /// <param name="target">The object to check</param>
        /// <param name="hitInfo">RaycastHit containing all information about the raycast</param>
        /// <param name="raycastSource">Where the raycast is coming from</param>
        /// <returns>True if the user is looking at the object</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Object must have a collider (2d colliders won't work)
        /// </remarks>
        /// <example> 
        /// <code>
        /// bool IsTheCameraLookingAtMe
        /// {
        ///     return App.IsCrosshairHit(gameobject);
        /// }
        /// </code>
        /// </example>
        public static bool IsCrosshairHit(GameObject target, out RaycastHit hitInfo, RaycastSource raycastSource = RaycastSource.Gaze)
        {
            return IsCrosshairHit(target.GetComponent<Collider>(), out hitInfo, raycastSource);
        }

        /// <summary>
        /// Returns true if the user is aiming at the target
        /// </summary>
        /// <param name="target">The collider to check</param>
        /// <param name="hitInfo">RaycastHit containing all information about the raycast</param>
        /// <param name="raycastSource">Where the raycast is coming from</param>
        /// <returns>True if the user is looking at the collider</returns>
        /// <example> 
        /// <code>
        /// bool IsTheCameraLookingAtMe
        /// {
        ///     return App.IsCrosshairHit(gameobject);
        /// }
        /// </code>
        /// </example>
        public static bool IsCrosshairHit(Collider target, out RaycastHit hitInfo, RaycastSource raycastSource = RaycastSource.Gaze)
        {
            if (raycastSource == RaycastSource.MotionController && !VRInput.MotionControllerAvailable)
            {
                raycastSource = RaycastSource.Gaze;
            }
            if (raycastSource == RaycastSource.Gaze && CrosshairHit.collider != null && CrosshairHit.collider == target)
            {
                hitInfo = CrosshairHit;
                return true;
            }
            if (raycastSource == RaycastSource.MotionController && LaserHit.collider != null && LaserHit.collider == target)
            {
                hitInfo = LaserHit;
                return true;
            }
            if (raycastSource == RaycastSource.Touch && Input.GetMouseButton(0) && TouchHit.collider != null && TouchHit.collider == target)
            {
                hitInfo = TouchHit;
                return true;
            }
            RaycastHit dummy = new RaycastHit();
            hitInfo = dummy;
            return false;
        }

        /// <summary>
        /// Returns true if the user is aiming at the target
        /// </summary>
        /// <param name="target">The object to check</param>
        /// <param name="raycastSource">Where the raycast is coming from</param>
        /// <returns>True if the user is looking at the object</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Object must have a collider (2d colliders won't work)
        /// </remarks>
        /// <example> 
        /// <code>
        /// bool IsTheCameraLookingAtMe
        /// {
        ///     return App.IsCrosshairHit(gameobject);
        /// }
        /// </code>
        /// </example>
        public static bool IsCrosshairHit(GameObject target, RaycastSource raycastSource = RaycastSource.Gaze)
        {
            RaycastHit dummy = new RaycastHit();
            return IsCrosshairHit(target.GetComponent<Collider>(), out dummy, raycastSource);
        }

        /// <summary>
        /// Returns true if the user is aiming at the target
        /// </summary>
        /// <param name="target">The collider to check</param>
        /// <param name="raycastSource">Where the raycast is coming from</param>
        /// <returns>True if the user is looking at the collider</returns>
        /// <example> 
        /// <code>
        /// bool IsTheCameraLookingAtMe
        /// {
        ///     return App.IsCrosshairHit(gameobject);
        /// }
        /// </code>
        /// </example>
        public static bool IsCrosshairHit(Collider target, RaycastSource raycastSource = RaycastSource.Gaze)
        {
            RaycastHit dummy = new RaycastHit();
            return IsCrosshairHit(target, out dummy, raycastSource);
        }

        /// <summary>
        /// Get metadata of project
        /// </summary>
        /// <param name="projectId">Project ID whose video metadata to retrieve</param>
        /// <param name="FileSizeFormat">Filesize format of <see cref="App.ProjectMetadata.TotalSize"/> in return class</param>
        /// <returns><see cref="App.ProjectMetadata"/> class containing metadata for project, null when projectId is invalid</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.GetProjects(string, int, int)"/> to get a list of Project IDs for this App
        ///
        /// &gt; [!NOTE] 
        /// &gt; Some variables in the returned <see cref="App.ProjectMetadata"/> class can be null if the project is missing data (e.g. does not have a thumbnail)
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Set text of 3D Text Mesh to filesize (in MB) of project with projectId
        /// void SetProjectSize(string projectId, UnityEngine.TextMesh projectSizeText)
        /// {
        ///     float projectSize = Headjack.App.GetProjectMetadata(projectId, Headjack.App.ByteConversionType.Megabytes).TotalSize;
        ///     // set 3D text to &lt;projectSize&gt; MB (projectSize is rounded to 2 decimal points)
        ///     projectSizeText.text = projectSize.ToString("2n") + " MB";
        /// }
        /// </code>
        /// </example>
        static public ProjectMetadata GetProjectMetadata(string projectId, ByteConversionType FileSizeFormat = ByteConversionType.Megabytes)
        {
            //BODHI
            if (projectId != null && Data != null && Data.Project != null && Data.Project.ContainsKey(projectId))
            {
                ProjectMetadata ReturnData = new ProjectMetadata();
                ReturnData.Id = projectId;
                ReturnData.Title = Data.Project[projectId].Title;
                ReturnData.Description = Data.Project[projectId].Description;
                ReturnData.Category = Data.Project[projectId].Category;
                ReturnData.ThumbnailId = Data.Project[projectId].Thumbnail;
                ReturnData.AudioId = Data.Project[projectId].Audio;
                ReturnData.VideoId = Data.Project[projectId].Video;
                ReturnData.CreationDate = Data.Project[projectId].CreationDate;
                ReturnData.Views = Data.Project[projectId].Views;
                
                long Bytes = 0;
                if (Data.Project[projectId].Video != null && Data.Video[Data.Project[projectId].Video].Format.Count > 0)
                {
                    AppDataStruct.FormatBlock vidFormat = null;

                    vidFormat = GetVideoFormatData(Data.Project[projectId].Video, false, null);
                    Bytes += (vidFormat != null) ? vidFormat.Filesize : 0;
                    string audioid = Data.Project[projectId].Audio;
                    if (audioid != null)
                    {
                        Bytes += Data.Media[audioid].FileSize;
                    }

                    float ReturnSize = Bytes * 1f;
                    if (FileSizeFormat == ByteConversionType.Kilobytes)
                    {
                        ReturnSize = (ReturnSize * 1f) / 1024f;
                    }
                    if (FileSizeFormat == ByteConversionType.Megabytes)
                    {
                        ReturnSize = (ReturnSize * 1f) / 1024f / 1024f;
                    }
                    if (FileSizeFormat == ByteConversionType.Gigabytes)
                    {
                        ReturnSize = (ReturnSize * 1f) / 1024f / 1024f / 1024f;
                    }
                    ReturnData.TotalSize = ReturnSize;

                }
                else
                {
                    ReturnData.TotalSize = 0;
                }
                return ReturnData;
            }
            return null;
        }
        //[System.Obsolete("Use Headjack.App.UpdateAvailable(string id) instead.")] // will be internal
        internal static bool PackageHasLatest(string id)
        {
            if (Data.Packaged.ContainsKey(id))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get all (sub)category IDs used in this app.
        /// </summary>
        /// <returns>a string array containing all category IDs</returns>
        public static string[] GetCategories()
        {
            if (Data != null && Data.Category != null)
            {
                string[] ReturnData = new string[Data.Category.Count];
                int i = 0;
                foreach (KeyValuePair<string, AppDataStruct.CategoryBlock> KV in Data.Category)
                {
                    ReturnData[i] = KV.Key;
                    i += 1;
                }
                return ReturnData;
            }
            return null;
        }

        /// <summary>
        /// Destroys the active videoplayer
        /// </summary>
        /// <example> 
        /// <code>
        /// GameObject menuObject;
        /// public void BackToMenu()
        /// {
           /// App.DestroyVideoPlayer();
           /// menuObject.SetActive(true);
        /// }
        /// </code>
        /// </example>
        public static void DestroyVideoPlayer()
        {
            if (Player != null)
            {
                Player.Clean();
                if (Player.ExternalAudio != null)
                {
                    Player.ExternalAudio.Destroy();
                }
                UnityEngine.Object.Destroy(Player.gameObject);
                Player = null;
            }
        }

        /// <summary>
        /// Check if a project contains a live stream
        /// </summary>
        /// <returns>true if the project contains a live stream</returns>
        /// <example> 
        /// <code>
        /// public string CheckIfLiveStream(ProjectId)
        /// {
        ///     if(App.ProjectIsLiveStream(ProjectId))
        ///     {
        ///         return "Livestream!";
        ///     } else {
        ///         return "No Livestream";
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool ProjectIsLiveStream(string ProjectId)
        {
            return IsLiveStream(ProjectId);
        }

        /// <summary>
        /// Check if a project contains a live stream
        /// </summary>
        /// <returns>true if the project contains a live stream</returns>
        /// <example> 
        /// <code>
        /// public string CheckIfLiveStream(ProjectId)
        /// {
        ///     if(App.IsLiveStream(ProjectId))
        ///     {
        ///         return "Livestream!";
        ///     } else {
        ///         return "No Livestream";
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool IsLiveStream(string id)
        {
            StreamProfile profile = GetStreamProfile(id);
            return profile == StreamProfile.Live;
        }

        public static bool IsPackaged(string id)
        {
            if(Data!=null&&Data.Packaged!=null)	return Data.Packaged.ContainsKey(id);
            return false;
        }


        /// <summary>
        /// Access custom template variables, downloaded from Headjack
        /// </summary>
        /// <returns>Returns the string value corresponding to the custom variable key, or null of key does not exist</returns>
        /// <param name="key">String key/name identifying the custom variable</param>
        /// <example> 
        /// <code>
        /// // Get a Texture object of an additional media item, whose id is passed to this
        /// // template using a custom variable called "menu_background_image_id"
        /// public Texture2D getBackgroundTexture()
        /// {
        ///     return App.GetImage(App.GetCustomVariable("menu_background_image_id"));
        /// }
        /// </code>
        /// </example>
        [System.Obsolete("Please use CustomVariables.GetVariable<string>() instead.")]
        public static string GetCustomVariable(string key)
        {
            if (Data.Custom != null && Data.Custom.ContainsKey(key))
            {
                return Data.Custom[key];
            }
            return null;
        }

        /// <summary>
        /// Get the app colors, from the server
        /// </summary>
        /// <returns>Returns the color given in Key</returns>
        /// <example> 
        /// <code>
        /// public Color GetBackgroundColor(string key)
        /// {
        ///     return App.GetColor(key,Color.black);
        /// }
        /// </code>
        /// </example>
        [System.Obsolete("Please use CustomVariables.GetVariable<Color>() instead.")]
        public static Color GetColor(string Key, Color DefaultValue)
        {
            
            string hex = GetCustomVariable(Key);
            if (hex != null)
            {
                hex = hex.Replace("0x", "").Replace("x", "").Replace("#", "");
                System.Globalization.NumberStyles s = System.Globalization.NumberStyles.HexNumber;
                return new Color32(
                    byte.Parse(hex.Substring(0, 2), s),
                    byte.Parse(hex.Substring(2, 2), s),
                    byte.Parse(hex.Substring(4, 2), s),
                    (hex.Length == 8) ? byte.Parse(hex.Substring(4, 2), s) : (byte)255);
            }
            return DefaultValue;
        }

        /// <summary>
        /// Check if a project is currently downloading
        /// </summary>
        /// <param name="ProjectId">Project to check</param>
        /// <returns>True if a project is currently downloading</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; To get the progress, use <see cref="App.GetProjectProgress"/>
        /// </remarks>
        /// <example> 
        /// <code>
        /// public float GetProgress(string projectid)
        /// {
        ///     if (App.ProjectIsDownloading(projectid))
        ///     {
        ///         return App.GetProjectProgress(projectid);
        ///     }
        ///     else
        ///     {
        ///         return -1;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool ProjectIsDownloading(string ProjectId)
        {
            if (Downloader != null && Downloader.Active.ContainsKey(ProjectId))
            {
                return (Downloader.Active[ProjectId]);
            }
            return false;
        }

        /// <summary>
        /// Get the download progress of a project (0 to 100)
        /// </summary>
        /// <param name="ProjectId">The project to check</param>
        /// <returns>The download progress from 0 to 100, -1 on fail</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; The returned value is not rounded, for displaying smooth loading bars
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string GetProgressText(string projectid)
        /// {
        ///     if (!App.ProjectIsDownloading(projectid))
        ///     {
        ///         return null;
        ///     }
        ///     float progress = App.GetProjectProgress(projectid);
        ///     return Mathf.FloorToInt(progress).ToString() + "%";
        /// }
        /// </code>
        /// </example>
        public static float GetProjectProgress(string ProjectId)
        {
            if (Downloader != null)
            {
                return Downloader.getProjectProgress(ProjectId);
            }
            return -1f;
        }


        /// <summary>
        /// Get the download progress of an individual media item in percentage (0 to 100)
        /// </summary>
        /// <param name="MediaId">The media (by ID) to check</param>
        /// <returns>The download progress from 0 to 100, -1 when not currently downloading or total size unknown</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; The returned value is not rounded, for displaying smooth loading bars
        ///
        /// &gt; [!NOTE] 
        /// &gt; Video IDs are not supported, use <see cref="App.GetProjectProgress(string)"/> instead
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string GetProgressText(string mediaId)
        /// {
        ///     float progress = App.GetMediaProgress(mediaId);
        ///     if (progress &lt; 0) {
        ///         return "-%";
        ///     }
        ///     return Mathf.FloorToInt(progress).ToString() + "%";
        /// }
        /// </code>
        /// </example>
        public static float GetMediaProgress(string MediaId)
        {
            if (Downloader != null)
            {
                return Downloader.getMediaProgress(MediaId);
            }
            return -1f;
        }


        /// <summary>
        /// Get the progress of an individual media download in bytes
        /// </summary>
        /// <param name="MediaId">The media (by ID) to check</param>
        /// <returns>The download progress in bytes, -1 when not currently downloading</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.GetMediaMetadata(string)"/> to get media item's total size in bytes
        ///
        /// &gt; [!NOTE] 
        /// &gt; Video IDs are not supported, use <see cref="App.GetProjectProgress(string)"/> instead
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string GetDetailedDownloadProgress(string mediaId)
        /// {
        ///     long progress = App.GetMediaDownloadedBytes(mediaId);
        ///     if (progress &lt; 0) {
        ///         return "not downloading";
        ///     }
        ///     return progress + "/" + App.GetMediaMetadata(mediaId).FileSize;
        /// }
        /// </code>
        /// </example>
        public static long GetMediaDownloadedBytes(string MediaId)
        {
            if (Downloader != null)
            {
                return Downloader.getMediaDownloadedBytes(MediaId);
            }
            return -1;
        }


        /// <summary>
        /// Register listener for a named event from the live Cinema feature
        /// </summary>
        /// <param name="eventName">Event name listener will be associated with</param>
        /// <param name="listener">This delegate will be invoked when event with eventName is activated</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; The listener takes a <see cref="SimpleJSON.JSONArray"/> parameter, which contains an array of event arguments
        ///
        /// Cinema default events:
        ///     "changeAlias": string parameter is new device name/alias
        ///     "download": download project, string parameter is project ID
        ///     "play": play project, string parameter is project ID
        ///     "delete": delete project, string parameter is project ID
        ///     "stop": stop playback, no parameters
        ///     "pause": pause playback, no parameters
        ///     "resume": resume playback, no parameters
        ///     "view": show project details, string parameter is project ID
        ///     "stream": stream from url, string parameter is stream url
        ///     "message": show message to user, string parameter is message
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void Start() {
        ///     App.RegisterCinemaListener("pause", HandleCinemaPause);
        /// }
        /// // pause video playback
        /// public void HandleCinemaPause() {
        ///     if (App.Player != null) {
        ///         App.Player.Pause();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void RegisterCinemaListener(string eventName, PushEvent listener)
        {
            if (PushServerHandler.instance != null)
            {

                PushEvent thisEvent = null;
                if (!PushServerHandler.eventDict.TryGetValue(eventName, out thisEvent))
                {
                    thisEvent = delegate { };
                    PushServerHandler.eventDict.Add(eventName, thisEvent);
                }
                thisEvent += listener;
                PushServerHandler.eventDict[eventName] = thisEvent;
            }
        }


        /// <summary>
        /// Remove a listener for a named Cinema event
        /// </summary>
        /// <param name="eventName">Event name listener is associated with</param>
        /// <param name="listener">This delegate will be removed from the event's listeners</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; See <see cref="App.RegisterCinemaListener(string, PushEvent)"/> for more information.
        /// </remarks>
        /// <example> 
        /// <code>
        /// PushEvent previouslyRegisteredListener;
        /// // remove previouslyRegisteredListener from "pause" event listeners
        /// public void RemoveListener() {
        ///     App.RemoveCinemaListener("pause", previouslyRegisteredListener);
        /// }
        /// </code>
        /// </example>
        public static void RemoveCinemaListener(string eventName, PushEvent listener)
        {
            if (PushServerHandler.instance != null)
            {
                PushEvent thisEvent = null;
                if (PushServerHandler.eventDict.TryGetValue(eventName, out thisEvent))
                {
                    thisEvent -= listener;
                }
            }
        }

        /// <summary>
        /// Send an updated status to the Cinema server
        /// </summary>
        /// <param name="statusType">One of several pre-defined status type names</param>
        /// <param name="message">A message further detailing the status</param>
        /// <param name="progressCurrent">Generic current progress (e.g. download or playback progress)</param>
        /// <param name="progressTotal">Max value of <paramref name="progressCurrent"/></param>
        /// <param name="progressUnit">String indicating which unit <paramref name="progressCurrent"/> should be displayed in</param>
        /// <remarks>
        /// Pre-defined status types:
        ///     "error": error occurred, message contains details
        ///     "playing": currently playing a project
        ///     "paused": playback of a project is paused
        ///     "downloading": downloading project/media
        ///     "idle": nothing active, waiting for command
        /// </remarks>
        /// <example> 
        /// <code>
        /// // register listener to "pause" event that sends "paused" status back
        /// public void RegisterPauseListener() {
        ///     App.RegisterCinemaListener("pause", delegate {
        ///         if (App.Player != null) {
        ///             App.Player.Pause();
        ///             App.SendCinemaStatus("paused", "Paused playback of " +
        ///                 App.GetProjectMetadata(App.CurrentProject).Title);
        ///         } else {
        ///             App.SendCinemaStatus("idle", "Nothing to pause");
        ///         }
        ///     });
        /// }
        /// </code>
        /// </example>
        public static void SendCinemaStatus(string statusType, string message, float progressCurrent = -1, float progressTotal = -1, string progressUnit = "%")
        {
            if (PushServerHandler.instance != null)
            {
                PushServerHandler.instance.SendDeviceStatus(statusType, message, progressCurrent, progressTotal, progressUnit);
            }
        }

        /// <summary>
        /// Enable or Disable the default Headjack VR Camera and switch VR mode on Cardboard
        /// </summary>
        /// <param name="visible">Enable or Disable the VR Camera</param>
        /// <param name="vrMode">(Cardboard) Enable or Disable VR mode</param>
        /// <example> 
        /// <code>
        /// public void GoFullScreen()
        /// {
        ///     App.SetCamera(true,false);
        /// }
        /// </code>
        /// </example>
        public static void SetCamera(bool visible = true, bool vrMode = true)
        {
            if (VRCamera == null) {
                VRCamera = new GameObject("Headjack Camera");
                VRCamera.AddComponent<VRCamera>();
                // VRCamera.GetComponent<VRCamera>().hideFlags = HideFlags.HideInInspector;
            }

            if (CameraParent != null)
            {
                CameraParent.SetActive(visible);
#if UNITY_IOS
                CameraParent.transform.rotation = Quaternion.identity;
#endif
            }

            if (!visible)
            {
                Debug.Log("SetCamera not visible set, disabling (VR) camera entirely");
                VrMode = false;
#if USE_CARDBOARD_SDK
                if (CurrentPlatform == VRPlatform.Cardboard)
                {
                    MobfishCardboard.CardboardManager.SetVRViewEnable(false);
                    CameraParent?.SetActive(false);
                }
#endif
                return;
            }
            Debug.Log("Camera set: Visible " + visible + " / VR " + VrMode);
            if (CurrentPlatform == VRPlatform.Cardboard)
            {
                if (camera != null)
                {
                    camera.localEulerAngles = Vector3.zero;
                }

                CameraParent?.SetActive(true);
                if (!visible || !vrMode)
                {
                    VrMode = false;
#if USE_CARDBOARD_SDK
                    MobfishCardboard.CardboardManager.SetVRViewEnable(false);
#endif

                    if (camera != null)
                    {
                        CenterEye ce = camera.GetComponent<CenterEye>();
                        ce.FullscreenMode = true;
                        Camera actualCam = camera.GetComponent<Camera>();
                        actualCam.stereoTargetEye = StereoTargetEyeMask.None;                       

                        TouchDrag touchDrag = ce.touchHelper.GetComponent<TouchDrag>();
                        if (touchDrag == null)
                        {
                            actualCam.fieldOfView = 40;
                            touchDrag =  ce.touchHelper.AddComponent<TouchDrag>();
                            touchDrag.CamObject = CameraParent;
                            touchDrag.Cam = Headjack.VRCamera.FindChildRecursive(
                                    CameraParent.transform, 
                                    "NoVRCamGroup"
                                ).GetComponent<Camera>();
                        }
                        touchDrag.enabled = true;
                    }
                }
                else
                {
                    VrMode = true;
#if USE_CARDBOARD_SDK
                    MobfishCardboard.CardboardManager.SetVRViewEnable(true);
#endif
                    if (camera != null)
                    {
                        CenterEye ce = camera.GetComponent<CenterEye>();
                        ce.FullscreenMode = false;
                        Camera actualCam = camera.GetComponent<Camera>();
                        actualCam.stereoTargetEye = StereoTargetEyeMask.Both;

                        TouchDrag touchDrag = ce.touchHelper.GetComponent<TouchDrag>();
                        if (touchDrag != null)
                        {
                            touchDrag.enabled = false;
                        }
                    }

                }
            }
            else
            {
                VrMode = true;
            }
        }

        /// <summary>
        /// Set background color
        /// </summary>
        /// <param name="backgroundColor"><see href="https://docs.unity3d.com/ScriptReference/Color.html">UnityEngine.Color</see> object containing new background color</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; This sets the background color of the active VR camera, and persists between Unity scenes.
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Set backgound color to red
        /// public void SetRedBackground()
        /// {
        ///     Headjack.App.SetCameraBackground(Color.red);
        /// }
        /// </code>
        /// </example>
        public static void SetCameraBackground(Color backgroundColor)
        {
            if (camera == null) return;

            // TODO: fix (also in VideoPlayerBase)
            CameraInformation Info = camera.GetComponent<CameraInformation>();
            if (Info.gameObject.GetComponent<Camera>())
            {
                Info.gameObject.GetComponent<Camera>().clearFlags = CameraClearFlags.Color;
                Info.gameObject.GetComponent<Camera>().backgroundColor = backgroundColor;
            }
        }

        /// <summary>
        /// Set background skybox
        /// </summary>
        /// <param name="skybox"><see href="https://docs.unity3d.com/ScriptReference/Material.html">UnityEngine.Material</see> object containing new skybox</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; This sets the background skybox of the active VR camera, and persists between Unity scenes.
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Set the default Unity Skybox to the headjack camera
        /// public void SetDefaultSkybox()
        /// {
        ///     Headjack.App.SetCameraBackground(RenderSettings.skybox);
        /// }
        /// </code>
        /// </example>
        public static void SetCameraBackground(Material skybox)
        {
            if (camera == null) return;
            CameraInformation Info = camera.GetComponent<CameraInformation>();

            // Info.Left.clearFlags = CameraClearFlags.Skybox;
            // Info.Right.clearFlags = CameraClearFlags.Skybox;
            
            RenderSettings.skybox = skybox;
            if (Info.gameObject.GetComponent<Camera>())
            {
                Info.gameObject.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            }
        }

        /// <summary>
        /// Recenter orientation and position on your device
        /// </summary>
        /// <param name="allowRoomScaleHeight">When true, (0,0,0) will be at floor level</param>
        /// <example> 
        /// <code>
        /// // Recenter when playing a video
        /// void PlayVideo(string id)
        /// {
        ///     App.Recenter();
        ///     App.Play(id, true, true, null);
        /// }
        /// </code>
        /// </example>
        public static void Recenter(bool allowRoomScaleHeight = true)
        {   
            if (CurrentPlatform == VRPlatform.Cardboard) {
#if USE_CARDBOARD_SDK
                MobfishCardboard.CardboardManager.RecenterCamera();
#endif
            } else {
                XRGeneralSettings.Instance.Manager.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>()?.TryRecenter();
            }

            if (!allowRoomScaleHeight && CameraParent != null && camera != null)
            {
#if UNITY_STANDALONE || UNITY_EDITOR
                CameraParent.transform.localPosition = new Vector3(0, -camera.localPosition.y, 0);
#endif
            }

            if (!App.IsVRMode && camera != null)
            {
                CenterEye ce = camera.GetComponent<CenterEye>();
                TouchDrag touchDrag = ce?.touchHelper?.GetComponent<TouchDrag>();
                if (touchDrag != null)
                {
                    touchDrag.Recenter();
                }
            }
        }


        /// <summary>
        /// Sets whether to Unity scene is centered on the floor level or the eye level
        /// </summary>
        /// <returns>True if Tracking origin was successfully set</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; For VR platforms that do not have an internal floor height, using 1.6m below current eye height
        /// </remarks>
        public static bool SetTrackingOrigin(TrackingOrigin origin) {
            bool setTrackingOrigin = true;

            if (CameraParent == null || camera == null) {
                Debug.LogWarning("CameraParent and/or camera gameobjects not populated yet, might not be able to set tracking origin");
            }

            if (origin == TrackingOrigin.EyeLevel) {
                switch (App.CurrentPlatform) {
                    case VRPlatform.OpenVR:
                        if (CameraParent != null && camera != null) {
                            CameraParent.transform.localPosition = new Vector3(0, -camera.localPosition.y, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                    case VRPlatform.Oculus:
#if USE_OCULUS_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel) {
                                setTrackingOrigin = false;
                            }

                            if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = Vector3.zero;
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.OculusLegacy:
#if USE_OCULUS_LEGACY_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel) {
                                setTrackingOrigin = false;
                            }

                            if ((int)OVRPlugin.GetSystemHeadsetType() <= (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                                if (CameraParent != null) {
                                    CameraParent.transform.localPosition = Vector3.zero;
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = Vector3.zero;
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.ViveWave:
#if USE_WAVE_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
#else // UNITY_EDITOR
                        if (Wave.Native.Interop.WVR_Base.Instance.GetDegreeOfFreedom(Wave.Native.WVR_DeviceType.WVR_DeviceType_HMD) == Wave.Native.WVR_NumDoF.WVR_NumDoF_3DoF) {
                            if (CameraParent != null) {
                                Debug.Log($"[SetTrackingOrigin ViveWave] setting to EyeLevel with 3DoF");
                                CameraParent.transform.localPosition = Vector3.zero;
                            } else {
                                setTrackingOrigin = false;
                            }
                        } else {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin ViveWave] setting to EyeLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin ViveWave] No XRInputSubsystems found when trying to set to EyeLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device)) {
                                    Debug.Log($"[SetTrackingOrigin ViveWave] Set input {i} Tracking Origin to EyeLevel/Device");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin ViveWave] Failed to set input {i} Tracking Origin to EyeLevel/Device");
                                }
                            }
                        }
#endif // UNITY_EDITOR
#endif // USE_WAVE_SDK
                        break;
                    case VRPlatform.Pico:
#if USE_PICO_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
#endif // UNITY_EDITOR
                        if (Pvr_UnitySDKManager.SDK != null) {
                            Pvr_UnitySDKManager.SDK.TrackingOrigin = Pvr_UnitySDKAPI.TrackingOrigin.EyeLevel;
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.PicoPlatform:
#if USE_PICO_PLATFORM_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
#else
                        int is6Dof = Unity.XR.PXR.PXR_Plugin.System.UPxr_GetConfigInt(Unity.XR.PXR.ConfigType.Ability6Dof);
                        if (is6Dof > 0) {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin Pico] setting to EyeLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin Pico] No XRInputSubsystems found when trying to set to EyeLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device)) {
                                    Debug.Log($"[SetTrackingOrigin Pico] Set input {i} Tracking Origin to Device/EyeLevel");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin Pico] Failed to set input {i} Tracking Origin to Device/EyeLevel");
                                }
                            }
                        } else {
                            if (CameraParent != null) {
                                CameraParent.transform.localPosition = Vector3.zero;
                            } else {
                                setTrackingOrigin = false;
                            }
                        }
#endif
#endif
                        break;
                    case VRPlatform.Cardboard:
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                    case VRPlatform.Daydream:
                    case VRPlatform.WindowsMixedReality:
                    case VRPlatform.NotYetInitialized:
                    default:
                        if (CameraParent != null) {
                            CameraParent.transform.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                }
            } else if (origin == TrackingOrigin.FloorLevel) {
                switch (App.CurrentPlatform) {
                    case VRPlatform.OpenVR:
                        if (CameraParent != null) {
                            CameraParent.transform.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                    case VRPlatform.Oculus:
#if USE_OCULUS_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.FloorLevel) {
                                setTrackingOrigin = false;
                            }

                            if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = new Vector3(0, OVRPlugin.eyeHeight, 0);
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.OculusLegacy:
#if USE_OCULUS_LEGACY_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.FloorLevel) {
                                setTrackingOrigin = false;
                            }

                            if ((int)OVRPlugin.GetSystemHeadsetType() <= (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                                if (CameraParent != null) {
                                    CameraParent.transform.localPosition = new Vector3(0, OVRPlugin.eyeHeight, 0);
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = new Vector3(0, OVRPlugin.eyeHeight, 0);
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.ViveWave:
#if USE_WAVE_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = new Vector3(0, 1.6f, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
#else // UNITY_EDITOR
                        if (Wave.Native.Interop.WVR_Base.Instance.GetDegreeOfFreedom(Wave.Native.WVR_DeviceType.WVR_DeviceType_HMD) == Wave.Native.WVR_NumDoF.WVR_NumDoF_3DoF) {
                            if (CameraParent != null) {
                                Debug.Log($"[SetTrackingOrigin ViveWave] setting to FloorLevel with 3DoF, use fixed 1.6m height");
                                CameraParent.transform.localPosition = new Vector3(0, 1.6f, 0);
                            } else {
                                setTrackingOrigin = false;
                            }
                        } else {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin ViveWave] setting to FloorLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin ViveWave] No XRInputSubsystems found when trying to set to FloorLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)) {
                                    Debug.Log($"[SetTrackingOrigin ViveWave] Set input {i} Tracking Origin to FloorLevel");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin ViveWave] Failed to set input {i} Tracking Origin to FloorLevel");
                                }
                            }
                        }
#endif // UNITY_EDITOR
#endif // USE_WAVE_SDK
                        break;
                    case VRPlatform.Pico:
#if USE_PICO_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = new Vector3(0, 1.6f, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
#endif // UNITY_EDITOR
                        if (Pvr_UnitySDKManager.SDK != null) {
                            Pvr_UnitySDKManager.SDK.TrackingOrigin = Pvr_UnitySDKAPI.TrackingOrigin.FloorLevel;
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.PicoPlatform:
#if USE_PICO_PLATFORM_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = new Vector3(0, 1.6f, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
#else
                        int is6Dof = Unity.XR.PXR.PXR_Plugin.System.UPxr_GetConfigInt(Unity.XR.PXR.ConfigType.Ability6Dof);
                        if (is6Dof > 0) {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin Pico] setting to FloorLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin Pico] No XRInputSubsystems found when trying to set to FloorLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)) {
                                    Debug.Log($"[SetTrackingOrigin Pico] Set input {i} Tracking Origin to Floor/FloorLevel");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin Pico] Failed to set input {i} Tracking Origin to Floor/FloorLevel");
                                }
                            }
                        } else {
                            if (CameraParent != null) {
                                CameraParent.transform.localPosition = new Vector3(0, 1.6f, 0);
                            } else {
                                setTrackingOrigin = false;
                            }
                        }
#endif
#endif
                        break;
                    case VRPlatform.Cardboard:
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = new Vector3(0, 1.6f, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                    case VRPlatform.Daydream:
                    case VRPlatform.WindowsMixedReality:
                    case VRPlatform.NotYetInitialized:
                    default:
                        if (CameraParent != null) {
                            CameraParent.transform.localPosition = new Vector3(0, 1.6f, 0);
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                }
            } else {
                switch (App.CurrentPlatform) {
                    case VRPlatform.Oculus:
#if USE_OCULUS_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel) {
                                setTrackingOrigin = false;
                            }

                            if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = Vector3.zero;
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.OculusLegacy:
#if USE_OCULUS_LEGACY_SDK
                        if (OVRManager.instance != null) {
                            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
                            if (OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel) {
                                setTrackingOrigin = false;
                            }

                            if ((int)OVRPlugin.GetSystemHeadsetType() <= (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                                if (CameraParent != null) {
                                    CameraParent.transform.localPosition = Vector3.zero;
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else if (OVRManager.tracker != null && !OVRManager.tracker.isPositionTracked) {
                                _oculusSetTrackingNoPositionCount++;
                                if (_oculusSetTrackingNoPositionCount > 5) {
                                    _oculusSetTrackingNoPositionCount = 0;
                                    if (CameraParent != null) {
                                        CameraParent.transform.localPosition = Vector3.zero;
                                    } else {
                                        setTrackingOrigin = false;
                                    }
                                } else {
                                    setTrackingOrigin = false;
                                }
                            } else {
                                _oculusSetTrackingNoPositionCount = 0;
                            }
                        } else {
                            setTrackingOrigin = false;
                        }
#endif
                        break;
                    case VRPlatform.ViveWave:
#if USE_WAVE_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
#else // UNITY_EDITOR
                        if (Wave.Native.Interop.WVR_Base.Instance.GetDegreeOfFreedom(Wave.Native.WVR_DeviceType.WVR_DeviceType_HMD) == Wave.Native.WVR_NumDoF.WVR_NumDoF_3DoF) {
                            if (CameraParent != null) {
                                Debug.Log($"[SetTrackingOrigin ViveWave] setting to EyeLevel with 3DoF");
                                CameraParent.transform.localPosition = Vector3.zero;
                            } else {
                                setTrackingOrigin = false;
                            }
                        } else {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin ViveWave] setting to EyeLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin ViveWave] No XRInputSubsystems found when trying to set to EyeLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device)) {
                                    Debug.Log($"[SetTrackingOrigin ViveWave] Set input {i} Tracking Origin to EyeLevel/Device");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin ViveWave] Failed to set input {i} Tracking Origin to EyeLevel/Device");
                                }
                            }
                        }
#endif // UNITY_EDITOR
#endif // USE_WAVE_SDK
                        break;
                    case VRPlatform.PicoPlatform:
#if USE_PICO_PLATFORM_SDK
#if UNITY_EDITOR
                        // Editor does not have proper floor height, so fake it
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
#else
                        int is6Dof = Unity.XR.PXR.PXR_Plugin.System.UPxr_GetConfigInt(Unity.XR.PXR.ConfigType.Ability6Dof);
                        if (is6Dof > 0) {
                            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
                            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubsystems);
                            Debug.Log($"[SetTrackingOrigin Pico] setting to EyeLevel, number of XRInputSubsystems: {inputSubsystems.Count}");
                            if (inputSubsystems.Count < 1) {
                                setTrackingOrigin = false;
                                Debug.LogError("[SetTrackingOrigin Pico] No XRInputSubsystems found when trying to set to EyeLevel!");
                            }
                            for (int i = 0; i < inputSubsystems.Count; ++i) {
                                if (inputSubsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device)) {
                                    Debug.Log($"[SetTrackingOrigin Pico] Set input {i} Tracking Origin to Device/EyeLevel");
                                } else {
                                    setTrackingOrigin = false;
                                    Debug.LogError($"[SetTrackingOrigin Pico] Failed to set input {i} Tracking Origin to Device/EyeLevel");
                                }
                            }
                        } else {
                            if (CameraParent != null) {
                                CameraParent.transform.localPosition = Vector3.zero;
                            } else {
                                setTrackingOrigin = false;
                            }
                        }
#endif
#endif
                        break;
                    case VRPlatform.Cardboard:
                        if (CameraParent != null) {
                            CameraParent.transform.parent.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                    case VRPlatform.Daydream:
                    case VRPlatform.WindowsMixedReality:
                    case VRPlatform.OpenVR:
                    case VRPlatform.NotYetInitialized:
                    default:
                        if (CameraParent != null) {
                            CameraParent.transform.localPosition = Vector3.zero;
                        } else {
                            setTrackingOrigin = false;
                        }
                        break;
                }
            }

            if (setTrackingOrigin) {
                CurrentTrackingOrigin = origin;
            }

            return setTrackingOrigin;
        }


        /// <summary>
        /// Gets whether to Unity scene is centered on the floor level or the eye level
        /// </summary>
        public static TrackingOrigin GetTrackingOrigin() {
            return CurrentTrackingOrigin;
        }


        /// <summary>
        /// Get the title of the given project
        /// </summary>
        /// <param name="Id">The project id</param>
        /// <returns>The title of the project with ProjectId</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; To get a list with available projects, use <see cref="App.GetProjects(string, int, int)"/>
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string GetFirstTitle()
        /// {
        ///     string[] ids = App.GetProjects();
        ///     return App.GetTitle(ids[0]);
        /// }
        /// </code>
        /// </example>
        public static string GetTitle(string Id)
        {
            if (Data.Project.ContainsKey(Id)) return Data.Project[Id].Title;
            if (Data.Category.ContainsKey(Id)) return Data.Category[Id].Name;
            return null;
        }

        /// <summary>
        /// Get the Description of the given project
        /// </summary>
        /// <param name="ProjectId">The project id</param>
        /// <returns>The description of the project with ProjectId, null if the project has no description</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; To get a list with available projects, use <see cref="App.GetProjects"/>
        ///
        /// &gt; [!WARNING] 
        /// &gt; When using this, make sure to check if the string is not null before using to avoid NullReference exceptions
        /// </remarks>
        /// <example> 
        /// <code>
        /// public string GetFirstDescription()
        /// {
        ///     string[] ids = App.GetProjects();
        ///     return App.GetDescription(ids[0]);
        /// }
        /// </code>
        /// </example>
        public static string GetDescription(string ProjectId)
        {
            return Data.Project[ProjectId].Description;
        }


        /// <summary>
        /// Contains all available projects from the server
        /// </summary>
        /// <param name="CategoryId">Only return projects that fit in this category filter</param>
        /// <param name="MaxPageSize">Only return a part from the list</param>
        /// <param name="CurrentPage">The part of the list to return. Starting at 0</param>
        /// <returns>A list with project ids</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.GetCategories"/> To get all available categories.
        /// </remarks>
        /// <example> 
        /// <code>
        /// //In this case, 5 projects per page
        /// public string[] WhatsOnTheFirstPage
        /// {
        ///     return App.GetProjects(null,5,0);
        /// }
        /// public string[] WhatsOnTheLastPage
        /// {
        ///     return App.GetProjects(null,5,int.MaxValue);
        /// }
        /// </code>
        /// </example>
        public static string[] GetProjects(string CategoryId = null, int MaxPageSize = -1, int CurrentPage = 0)
        {
            if (App.Data != null)
            {
                List<string> FullList = new List<string>();
                if (CategoryId == null)
                {
                    for (int i = 0; i < Data.ProjectOrder.Count; ++i)
                    {
                        FullList.Add(Data.ProjectOrder[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < Data.ProjectOrder.Count; ++i)
                    {
                        if (Data.Project[Data.ProjectOrder[i]].Category == CategoryId)
                        {
                            FullList.Add(Data.ProjectOrder[i]);
                        }
                        if (Data.Project[Data.ProjectOrder[i]].Category == null && CategoryId == NO_CATEGORY)
                        {
                            FullList.Add(Data.ProjectOrder[i]);
                        }
                    }
                    if (FullList.Count == 0)
                    {
                        Debug.Log("No videos found with Category id " + CategoryId);
                        return null;
                    }
                }
                if (MaxPageSize < 1)
                {
                    string[] ReturnString = new string[FullList.Count];
                    for (int i = 0; i < FullList.Count; ++i)
                    {
                        ReturnString[i] = FullList[i];
                    }
                    return ReturnString;
                }
                else
                {
                    string[] TempReturnString = new string[MaxPageSize];
                    int StartAt = CurrentPage * MaxPageSize;
                    if (StartAt > FullList.Count - 1)
                    {
                        StartAt = 0;
                    }
                    int Total = 0;
                    int j = 0;
                    for (int i = StartAt; i < StartAt + MaxPageSize; ++i)
                    {
                        if (i < FullList.Count)
                        {
                            TempReturnString[j] = FullList[i];
                            Total += 1;
                        }
                        else
                        {
                            break;
                        }
                        j += 1;
                    }
                    string[] ReturnString = new string[Total];
                    for (int i = 0; i < Total; ++i)
                    {
                        ReturnString[i] = TempReturnString[i];
                    }
                    return ReturnString;
                }
            }
            Debug.Log("App is not initialized");
            return null;
        }

        /// <summary>
        /// Get a downloaded texture from te server
        /// </summary>
        /// <param name="Id">Project id or media id</param>
        /// <param name="UnscaledSlow">Get the raw texture</param>
        /// <returns>Texture 2d of given project/media id. Returns null if the media file doesn exists. Returns null if the given project doesn't have a thumbnail attached</returns>
        /// <remarks>
        /// &gt; [!WARNING] 
        /// &gt; When using this, make sure to check if the Texture is not null before using to avoid NullReference errors
        /// </remarks>
        /// <example> 
        /// <code>
        /// public void ApplyTexture(string MediaId)
        /// {
        ///     GetComponent&lt;MeshRenderer&gt;().material.mainTexture = App.GetImage(MediaId);
        /// }
        /// </code>
        /// </example>
        public static Texture2D GetImage(string Id, bool UnscaledSlow = false)
        {
            if (Id == null)
            {
                // Debug.Log("ID was null");
                return null;
            }
            if (Data.Project.ContainsKey(Id))
            {
                Id = Data.Project[Id].Thumbnail;
            }
            else if (Data.Category.ContainsKey(Id))
            {
                Id = Data.Category[Id].Thumbnail;
            }
            if (Id == null)
            {
                // Debug.Log("ID was null");
                return null;
            }
            Id += (UnscaledSlow && Data.Textures.ContainsKey(Id + "_UNSCALED")) ? "_UNSCALED" : "";
            if (Data.Textures.ContainsKey(Id))
            {
                return Data.Textures[Id];
            }
            // Debug.Log("Project/Image ID not found");
            return null;
        }
        internal static bool UnloadSingleTexture(string id)
        {
            if (Data.Project.ContainsKey(id))
            {
                id = Data.Project[id].Thumbnail;
            }
            else if (Data.Category.ContainsKey(id))
            {
                id = Data.Category[id].Thumbnail;
            }
            if (id == null) return false;
            if (Data.Textures.ContainsKey(id + "_UNSCALED"))
            {
                id = id + "_UNSCALED";
            }
            if (Data.Textures.ContainsKey(id))
            {
                Object.Destroy(Data.Textures[id]);
                return Data.Textures.Remove(id);
            }
            return false;
        }

        /// <summary>
        /// Get IDs of extra media items from server
        /// </summary>
        /// <returns>List of ID strings of extra media items as entered on Headjack "Edit app" page</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Download a media item from this list using <see cref="App.DownloadSingleMedia(string, bool, OnEnd)"/>
        ///
        /// &gt; [!NOTE] 
        /// &gt; Load the downloaded media as texture using <see cref="App.GetImage(string, bool)"/>
        ///
        /// &gt; [!NOTE]
        /// &gt; Get metadata for media item using <see cref="App.GetMediaMetadata(string)"/>
        /// </remarks>
        /// <example> 
        /// <code>
        /// // return number of extra media items
        /// public int GetNumberOfExtraMedia()
        /// {
        ///     return App.GetAdditionalMedia().Count;
        /// }
        /// </code>
        /// </example>
        [System.Obsolete("Additional media has been superseded by template variables. Please use Headjack.CustomVariables.GetVariable<string>(string variable) instead.")]
        public static List<string> GetAdditionalMedia()
        {
            return (Data != null && Data.ExtraMedia != null) ? Data.ExtraMedia : new List<string>();
        }

        /// <summary>
        /// Download a single media item (e.g. media from a custom variable)
        /// </summary>
        /// <param name="mediaId">ID of media item that will be downloaded</param>
        /// <param name="wifiOnly">Set to true to only download over wifi and not use mobile data</param>
        /// <param name="onEnd">This delegate is called when the download has finished (or failed)</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; mediaId must be a media item either from a custom variable or a thumbnail of a project or category used in the app
        ///
        /// &gt; [!NOTE] 
        /// &gt; Videos in projects are special entries on the server and do not have a media ID
        /// 
        /// &gt; [!NOTE]
        /// &gt; onEnd returns bool indicating success of download and a string containing error message if unsuccessful
        /// </remarks>
        /// <example> 
        /// <code>
        /// // download a specific custom media variable
        /// public void DownloadFirstExtraMedia()
        /// {
        ///     App.DownloadSingleMedia(Headjack.CustomVariables.GetVariable&lt;string&gt;("a_media_variable"), false,
        ///         delegate (bool success, string error)
        ///         {
        ///             if (success)
        ///             {
        ///                 Debug.Log("Downloaded a_media_variable media");
        ///             }
        ///             else
        ///             {
        ///                 Debug.LogError("Downloading a_media_variable media failed!");
        ///             }
        ///         });
        /// }
        /// </code>
        /// </example>
        public static void DownloadSingleMedia(string mediaId, bool wifiOnly, OnEnd onEnd = null)
        {
            if (Downloader != null)
            {
                Downloader.DownloadMedia(mediaId, wifiOnly, onEnd);
            }
            else if (onEnd != null)
            {
                onEnd(false, "No downloader found to download media file with");
            }
        }

        /// <summary>
        /// PopUp Messages for displaying a text
        /// </summary>
        /// <example> 
        /// <code>
        /// public void SayHello()
        /// {
        ///     App.ShowMessage("Hello world", 1f);
        /// }
        /// </code>
        /// </example>
        public static void ShowMessage(string Message, float TimeInSeconds = 3, OnEnd onEnd = null)
        {
            if (PopUp != null)
            {
                PopUp.Add(Message, TimeInSeconds, onEnd);
            }
        }

        /// <summary>
        /// Get metadata for the app
        /// </summary>
        /// <returns><see cref="App.AppMetadata"/> class containing metadata for app</returns>
        /// <example> 
        /// <code>
        /// bool IsMyAppPublished()
        /// {
        ///     return Headjack.App.GetAppMetadata().Published;               
        /// }
        /// </code>
        /// </example>
        public static AppMetadata GetAppMetadata()
        {
            if (Data != null)
            {
                AppMetadata ReturnData = new AppMetadata();
                ReturnData.Published = Data.App.Published;
                ReturnData.ProductName = Data.App.ProductName;
                ReturnData.CompanyName = Data.App.CompanyName;
                return ReturnData;
            }
            return null;
        }

        /// <summary>
        /// Get metadata for video associated with project ID
        /// </summary>
        /// <param name="Id">Project ID whose video metadata to retrieve</param>
        /// <returns><see cref="App.VideoMetadata"/>class containing metadata for video item, or null when invalid Project ID or project without video</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.GetProjects(string, int, int)"/> to get a list of Project IDs for this App
        ///
        /// &gt; [!NOTE] 
        /// &gt; To get filesize and other information about a project, use <see cref="App.GetProjectMetadata(string, ByteConversionType)"/>
        /// </remarks>
        /// <example> 
        /// <code>
        /// // Returns string with duration of video in n-th project (first = 0, second = 1, etc. project in App)
        /// // in format MM:SS (M = minutes, S = seconds)
        /// string GetFormattedVideoDuration(int projectIndex)
        /// {
        ///     string[] projects = Headjack.App.GetProjects();
        ///     // if projectIndex is invalid (negative or larger than number of projects), return empty string
        ///     if (projectIndex &lt; 0 || projectIndex &gt;= projects.Length) 
        ///     {
        ///         return "";
        ///     }
        ///     return Headjack.App.GetVideoMetadata(projects[projectIndex]).DurationMMSS;               
        /// }
        /// </code>
        /// </example>
        static public VideoMetadata GetVideoMetadata(string Id)
        {
            if (Id == null || Data == null)
            {
                //Debug.LogWarning("Id or Data is null");
                return null;
            }
            if (Data.Project.ContainsKey(Id))
            {
                if (Data.Project[Id].Video != null)
                {
                    Id = Data.Project[Id].Video;
                }
                else
                {
                    Debug.LogWarning("Project id has no videos");
                    return null;
                }
            }
            if (Data.Video != null && Data.Video.ContainsKey(Id))
            {
                VideoMetadata ReturnData = new VideoMetadata();
                ReturnData.id = Data.Video[Id].Id;
                ReturnData.ProjectionType = Data.Video[Id].Projection.Type;
                ReturnData.Duration = Data.Video[Id].Duration;
                ReturnData.StereoMap = Data.Video[Id].Projection.StereoMap;
                ReturnData.Stereo = (Data.Video[Id].Projection.StereoMap == "TB" ||
                                    Data.Video[Id].Projection.StereoMap == "LR");
                AppDataStruct.FormatBlock F = GetVideoFormatData(Id, false);
                if (F != null)
                {
                    ReturnData.Width = F.Width;
                    ReturnData.Height = F.Height;
                }
                else
                {
                    ReturnData.Width = ReturnData.Height = 0;
                }
                System.TimeSpan t = System.TimeSpan.FromMilliseconds(ReturnData.Duration);
                ReturnData.DurationHHMMSS = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
                if (t.Hours < 1)
                {
                    ReturnData.DurationMMSS = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
                }
                else
                {
                    int Minutes = 60 * t.Hours + t.Minutes;
                    if (Minutes < 100)
                    {
                        ReturnData.DurationMMSS = string.Format("{0:D2}:{1:D2}", Minutes, t.Seconds);
                    }
                    else
                    {
                        ReturnData.DurationMMSS = string.Format("{0:D3}:{1:D2}", Minutes, t.Seconds);
                    }
                }
                // check whether video streaming URL is HLS adaptive stream
                ReturnData.HasAdaptiveStream = false;
                F = GetVideoFormatData(Id, true);
                if (F != null) {
                    if (F.StreamUrl.EndsWith("m3u8")) {
                        ReturnData.HasAdaptiveStream = true;
                    }
                }

                return ReturnData;
            }
            Debug.LogWarning("video contains key = null");
            return null;
        }

        /// <summary>
        /// Get metadata for video associated with project ID
        /// </summary>
        /// <param name="id">Project ID or Video ID to retrieve custom parameter from</param>
        /// <param name="paramName">Custom video parameter identifier</param>
        /// <param name="paramVal">Value of custom video parameter</param>
        /// <returns>Whether parameter with paramName could be retrieved</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; Available video params depends on VideoMetadata.ProjectionType
        ///
        /// &gt; [!NOTE] 
        /// &gt; EQUIRECT projection has "FovX" and "FovY" int params for horizontal and vertical field of view
        ///
        /// &gt; [!NOTE]
        /// &gt; FLAT projection has "OrgX" and "OrgY" int params for original video width and height in pixels
        /// </remarks>
        static public bool GetCustomVideoParam(string id, string paramName, out int paramVal)
        {
            VideoMetadata videoMeta = GetVideoMetadata(id);
            if (videoMeta == null) {
                paramVal = -1;
                return false;
            }

            if (!App.Data.Video[videoMeta.id].Projection.Params.ContainsKey(paramName)) {
                paramVal = -1;
                return false;
            }

            try {
                paramVal = App.Data.Video[videoMeta.id].Projection.Params[paramName];
                return true;
            } catch (System.Exception e) {
                Debug.LogError($"Failed to get custom video parameter ({paramName}): {e.Message}");
                paramVal = -1;
                return false;
            }
        }

        /// <summary>
        /// Get metadata for media item
        /// </summary>
        /// <param name="mediaId">ID of media item</param>
        /// <returns><see cref="App.MediaMetadata"/> class containing metadata for media item, or null when invalid mediaId</returns>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; mediaId must be a media item either from a custom variable or a thumbnail of a project or category used in the app 
        ///
        /// &gt; [!NOTE] 
        /// &gt; Videos in projects are special entries on the server and do not have a media ID
        /// </remarks>
        /// <example> 
        /// <code>
        /// // return filesize of specific custom media variable (or -1 if custom variable fails to return media)
        /// public long GetCustomFileSize()
        /// {
        ///     // try to get media id from custom variable named a_media_variable
        ///     string mediaId;
        ///     if (Headjack.CustomVariables.TryGetVariable&lt;string&gt;("a_media_variable", out mediaId))
        ///     {
        ///         App.MediaMetadata mediaMetaData = Headjack.App.GetMediaMetadata(mediaId);
        ///         if (mediaMetaData != null)
        ///         {
        ///             return mediaMetaData.FileSize;
        ///         }
        ///     }
        ///     return -1;
        /// }
        /// </code>
        /// </example>
        public static MediaMetadata GetMediaMetadata(string mediaId)
        {
            if (mediaId == null) return null;
            if (Data != null && Data.Media != null && Data.Media.ContainsKey(mediaId))
            {
                MediaMetadata ReturnData = new MediaMetadata();
                ReturnData.Filename = Data.Media[mediaId].Filename;
                ReturnData.FileSize = Data.Media[mediaId].FileSize;
                ReturnData.Height = Data.Media[mediaId].Height;
                ReturnData.Id = Data.Media[mediaId].Id;
                ReturnData.MimeType = Data.Media[mediaId].MimeType;
                ReturnData.Width = Data.Media[mediaId].Width;
                return ReturnData;
            }
            return null;
        }

        /// <summary>
        /// Get metadata for category
        /// </summary>
        /// <param name="categoryId">ID of category</param>
        /// <returns> <see cref="App.CategoryMetadata"/> class containing metadata for category, or null when invalid category ID</returns>
        /// <example> 
        /// <code>
        /// // return a list of all category names
        /// public List&lt;String&gt; AllCategoryNames
        /// {
        ///     List&lt;String&gt; names = new List&lt;String&gt;();
        ///     foreach(string id in App.GetCategories())
        ///     {
        ///         names.Add(App.GetCategoryMetadata(id).Name);
        ///     }
        ///     return names;
        /// }
        /// </code>
        /// </example>
        public static CategoryMetadata GetCategoryMetadata(string categoryId)
        {
            if (categoryId == null) return null;
            if (Data != null && Data.Category != null && Data.Category.ContainsKey(categoryId))
            {
                CategoryMetadata ReturnData = new CategoryMetadata();
                ReturnData.Id = Data.Category[categoryId].Id;
                ReturnData.Name = Data.Category[categoryId].Name;
                ReturnData.Description = Data.Category[categoryId].Description;
                ReturnData.ThumbnailId = Data.Category[categoryId].Thumbnail;
                ReturnData.ParentId = Data.Category[categoryId].Parent;
                return ReturnData;
            }
            return null;
        }

        /// <summary>
        /// Get the full local path to a downloaded media file
        /// </summary>
        /// <param name="Id">ID of the media item</param>
        /// <returns>full local path to media item with Id (file may not exist if not downloaded), or null if Id is incorrect</returns>
        /// <example> 
        /// <code>
        /// // Download and load a video from a specific custom variable into the Unity VideoPlayer
        /// public void LoadMenuVideo(UnityEngine.Video.VideoPlayer player)
        /// {
        ///     // get the video ID by getting it from a specific named custom variable
        ///     string videoId = Headjack.CustomVariables.GetVariable&lt;string&gt;("menu_video");
        ///     // download the video file
        ///     Headjack.App.DownloadSingleMedia(videoId, false, delegate (bool success, string error)
        ///     {
        ///         // if the video successfully downloaded, load that video into the Unity VideoPlayer
        ///         if (success)
        ///         {
        ///             player.url = Headjack.App.GetMediaFullPath(videoId);
        ///         }
        ///     });
        /// }
        /// </code>
        /// </example>
        public static string GetMediaFullPath(string Id)
        {
            if (Id == null || !Data.Media.ContainsKey(Id)) return null;
            return App.BasePath + "Media/" + Id + "/" + Data.Media[Id].Filename;
        }

        /// <summary>
        /// Get the URL to a video when streaming
        /// </summary>
        /// <param name="VideoId">ID of the video, see <see cref="ProjectMetadata.VideoId"/></param>
        /// <returns>URL to the video stream (usually to .m3u8 HLS playlist or direct .mp4 video file). Returns null if VideoId could not be found or if video cannot be streamed.</returns>
        public static string GetVideoStreamUrl(string VideoId)
        {
            return GetVideoStream(VideoId);
        }

        /// <summary>
        /// Get the availability of a video based on the video or project ID
        /// </summary>
        /// <param name="Id">ID of the video or project (containing the video)</param>
        /// <param name="Stream">Whether to return availability of the stream of this video or the download</param>
        /// <returns><see cref="VideoAvailability"/> whether the video can be downloaded or streamed (and why)</returns>
        public static VideoAvailability GetVideoAvailability(string Id, bool Stream)
        {
            if (Id == null) {
                return VideoAvailability.NoVideoInProject;
            }

            if (Data.Project.ContainsKey(Id)) {
                Id = Data.Project[Id].Video;
            }
            if (Id == null || !Data.Video.ContainsKey(Id)) {
                return VideoAvailability.NoVideoInProject;
            }
            int current = -1;
            int next = -1;
            if (Stream) {
                current = Data.Video[Id].StreamProfileCurrent;
                next = Data.Video[Id].StreamProfileNext;
            } else {
                current = Data.Video[Id].DownloadProfileCurrent;
                next = Data.Video[Id].DownloadProfileNext;
            }

            if (current < 1 && next >= 1) {
                return VideoAvailability.UnavailableWillUpdate;
            }
            if (current == -1) {
                return VideoAvailability.FormatNotSelected;
            }
            if (current == 0) {
                return VideoAvailability.Disabled;
            }
            if (current >= 1 && current != next) {
                if (App.GetVideoFormatData(Id, Stream) == null) {
                    return VideoAvailability.IncompatibleWillUpdate;
                }
                return VideoAvailability.AvailableWillUpdate;
            }
            if (current >= 1) {
                if (App.GetVideoFormatData(Id, Stream) == null) {
                    return VideoAvailability.Incompatible;
                }
                return VideoAvailability.Available;
            }
            return VideoAvailability.Disabled;
        }

        /// <summary>
        /// Get the video encoding settings for downloaded video
        /// </summary>
        /// <param name="Id">ID of the video or project (containing the video)</param>
        /// <returns><see cref="VideoProfile"/> video encoding profile and settings</returns>
        public static VideoProfile GetVideoProfile(string Id)
        {
            if (Id == null) {
                return VideoProfile.None;
            }
            if (Data.Project.ContainsKey(Id)) {
                Id = Data.Project[Id].Video;
            }
            if (Id == null || !Data.Video.ContainsKey(Id)) {
                return VideoProfile.None;
            }
            return (VideoProfile)Data.Video[Id].DownloadProfileCurrent;
        }

        /// <summary>
        /// Get the video encoding settings for streaming video
        /// </summary>
        /// <param name="Id">ID of the video or project (containing the video)</param>
        /// <returns><see cref="StreamProfile"/> stream encoding profile and settings</returns>
        public static StreamProfile GetStreamProfile(string Id)
        {
            if (Id == null) {
                return StreamProfile.None;
            }
            if (Data.Project.ContainsKey(Id)) {
                Id = Data.Project[Id].Video;
            }
            if (Id == null || !Data.Video.ContainsKey(Id)) {
                return StreamProfile.None;
            }
            return (StreamProfile)Data.Video[Id].StreamProfileCurrent;
        }

        /// <summary>
        /// Send manual video playback analytics event
        /// </summary>
        /// <param name="eventType">The type of analytics event to record</param>
        /// <param name="projectId">The project ID to attribute the analytics event to</param>
        /// <remarks>
        /// &gt; [!WARNING] 
        /// &gt; Do not use this function when playing video with the regular <see cref="Play(string, bool, bool, OnEnd)"/> or <see cref="Prepare(string, bool, bool, OnEnd, long)"/> functions as these already automatically record video playback analytics. Only use this function to record analytics when using your own custom video player logic and automatic analytics recording is not working. 
        ///
        /// &gt; [!NOTE] 
        /// &gt; Sending the <see cref="VideoEventType.EndVideo"/> event at the end of video playback is optional, but not sending it will result in the view duration analytics data being inaccurate.
        /// </remarks>
        public static void SendVideoEvent(VideoEventType eventType, string projectId)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[Analytics] analytics events are disabled in Unity Editor");
            return;
#endif
            if (string.IsNullOrEmpty(projectId)) {
                Debug.LogError("[Analytics] project ID is required to send analytics video event");
                return;
            }

            if (AnalyticsManager == null) {
                Debug.LogError("[Analytics] Analytics (or app) have not been initialized yet, so cannot send analytics event");
                return;
            }

            string videoId = GetProjectMetadata(projectId).VideoId;
            if (videoId == null) {
                // allow analytics video event for project without video, as it is displayed in CMS
                // as a project view, not related to the exact video in the project.
                // Analytics event will fail to send if videoId == null, so use empty string
                videoId = "";
            }

            if (eventType == VideoEventType.StartVideo) {
                AnalyticsManager.SendStartVideoEvent(projectId, videoId);
            } else if (eventType == VideoEventType.EndVideo) {
                AnalyticsManager.SendStopVideoEvent(projectId, videoId);
            }
        }

#endregion
#region PUBLIC TYPES

        /// <summary>
        /// Order of magnitude of filesizes, used by <see cref="App.GetProjectMetadata(string, ByteConversionType)"/>.
        /// </summary>
        /// <example> 
        /// <code>
        /// // Set text of 3D Text Mesh to filesize (in MB) of project with projectId
        /// void SetProjectSize(string projectId, UnityEngine.TextMesh projectSizeText)
        /// {
        ///     float projectSize = Headjack.App.GetProjectMetadata(projectId, Headjack.App.ByteConversionType.Megabytes).TotalSize;
        ///     // set 3D text to &lt;projectSize&gt; MB (projectSize is rounded to 2 decimal points)
        ///     projectSizeText.text = projectSize.ToString("2n") + " MB";
        /// }
        /// </code>
        /// </example>
        public enum ByteConversionType
        {
            bytes,
            Kilobytes,
            Megabytes,
            Gigabytes
        }

       /// <summary>
       /// Where the raycast is coming from
       /// </summary>
        public enum RaycastSource
        {
            Gaze,
            MotionController,
            Touch
        }

        /// <summary>
        /// VR platform that Headjack is running on
        /// </summary>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; For testing: You can change this in the Headjack settings window
        ///
        /// &gt; [!NOTE] 
        /// &gt; Use <see cref="App.CurrentPlatform"/> to get the currently loaded platform
        /// </remarks>
        public enum VRPlatform
        {
            NotYetInitialized,
            OpenVR,
            Oculus,
            Cardboard,
            Daydream,
            WindowsMixedReality,
            ViveWave,
            Pico,
            OculusLegacy,
            PicoPlatform
        }


        /// <summary>
        /// Whether the Unity scene origin is set to VR eye level, floor level or the legacy behavior dependent on VR platform
        /// </summary>
        public enum TrackingOrigin
        {
            EyeLevel,
            FloorLevel,
            Legacy
        }

        public enum VideoAvailability
        {
            IncompatibleWillUpdate = -5,// Current formats not compatible with device but new profiles are processing
            Incompatible = -4,          // None of the available video formats is compatible with this device
            UnavailableWillUpdate = -3, // Video is currently unavailable while new encoding profile is processing
            FormatNotSelected = -2,     // This platform does not have an encoding profile selected
            Disabled = -1,              // Video (stream or download) is explicitly disabled
            NoVideoInProject = 0,       // This project has no video selected
            Available = 1,              // Video is available and up-to-date
            AvailableWillUpdate = 2     // Outdated video is available while new encoding profile is processing
        }

        public enum VideoProfile
        {
            None = -1,
            Disabled = 0,
            EncodedProfile1 = 1,
            EncodedProfile2 = 2,
            EncodedProfile3 = 3,
            EncodedFlatProfile3 = 103,
            Original = 1000
        }

        public enum StreamProfile
        {
            None = -1,
            Disabled = 0,
            AdaptiveProfile1 = 1,
            AdaptiveProfile2 = 2,
            AdaptiveProfile3 = 3,
            AdaptiveFlatProfile3 = 103,
            Live = 1000
        }

        public enum VideoEventType
        {
            StartVideo,
            EndVideo
        }
#endregion
    }
}
