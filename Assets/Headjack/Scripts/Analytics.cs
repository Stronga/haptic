// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine.Networking;
using System.ComponentModel;

namespace Headjack
{
    internal class Analytics : MonoBehaviour 
    {
        // the number of look direction records that are uploaded together as an Analytics package
        private const int MAX_DATA_COUNT = 200;
        private const int MAX_CACHED_ANALYTICS = 16384; // roughly equates to max 120MB analytics data and >150 hours of analytics collecting
        private const string ANALYTICS_DIRECTORY = "Analytics";
        private static DateTime EPOCH = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
        // buffers of look direction records
        private NewData[] dataBuffers;
        private uint currentDataBuffer = 0;
        // a reference to the analytics cache directory
        private DirectoryInfo analyticsDirectory = null;
        private int cachedAnalyticsCount = 0;
        private bool lastUploadSuccessful = false;

        private class BackgroundJsonReturns
        {
            public string json;
            public string key;
            public string cacheFile;
        }

        internal static string CurrentSessionId;
        internal static string CurrentVideoSessionId;
        internal static string CurrentVideoId;
        internal static string CurrentProjectId;

        [System.Serializable]
        internal class StartApp
        {
            public string EventType, SessionId, AppId, Os, Platform, Device, DeviceId;
            public int TimeStamp;
        }
        [System.Serializable]
        internal class EndApp
        {
            public string EventType, SessionId, AppId;
            public int TimeStamp;
        }
        [System.Serializable]
        internal class StartVideo
        {
            public string EventType, VideoSessionId, SessionId, ProjectId, VideoId, AppId, DeviceId;
            public int TimeStamp;
            public bool VrMode;
        }
        [System.Serializable]
        internal class EndVideo
        {
            public string EventType, VideoSessionId, SessionId, ProjectId, VideoId, AppId;
            public int TimeStamp;
        }
        [System.Serializable]
        internal class NewData
        {
            public string EventType, VideoSessionId, SessionId, ProjectId, VideoId, AppId;
            public int TimeStamp;
            public List<Coordinates> C;
        }
        [System.Serializable]
        internal class Coordinates
        {
            public int t, x, y;
        }

        private void UploadImmediately(object data)
        {
            if (App.Data?.App.Analytics != null && App.Data.App.Analytics.Length > 0)
            {
                // cache analytics data
                string cacheFile = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                bool newAnalyticsCached = false;
                if (analyticsDirectory != null && cachedAnalyticsCount < MAX_CACHED_ANALYTICS) {
                    FileStream cacheStream = null;
                    try {
                        cacheStream = File.Create(analyticsDirectory.FullName + "/" + cacheFile);
                        BinaryFormatter cacheSerializer = new BinaryFormatter();
                        cacheSerializer.Serialize(cacheStream, data);
                        newAnalyticsCached = true;
                    } catch (Exception exception) {
                        Debug.LogError($"[Analytics] Error caching analytics data, dould not serialize data to disk: {exception.Message}");
                        cacheFile = null;
                    } finally {
                        cacheStream?.Close();
                    }
                } else {
                    cacheFile = null;
                }

                string json = JsonUtility.ToJson(data);
                using (UnityWebRequest analyticsPut = UnityWebRequest.Post(App.Data.App.Analytics, json))
                {
                    analyticsPut.SetRequestHeader("Content-Type", "application/json");
                    analyticsPut.SetRequestHeader("X-PPVR-Signature", AnalyticsSignature.GenerateSignature(json));
                    float TimeOut = 0;
                    analyticsPut.SendWebRequest();
                    while (!analyticsPut.isDone && TimeOut <= 10)
                    {
                        //Debug.Log("Wait"+TimeOut);
                        System.Threading.Thread.Sleep(200);
                        TimeOut += 0.2f;
                    }
                    if (analyticsPut.isNetworkError)
                    {
                        lastUploadSuccessful = false;
                        Debug.LogWarning($"Uploading analytics failed, network error: {analyticsPut.error}");
                    } 
                    else if (analyticsPut.isHttpError)
                    {
                        lastUploadSuccessful = false;
                        Debug.LogWarning($"Uploading analytics failed, server returned failure code ({analyticsPut.responseCode}): {analyticsPut.error}");
                    }
                    else if (TimeOut > 10)
                    {
                        lastUploadSuccessful = false;
                        Debug.LogWarning($"Analytics upload timed out: {analyticsPut.responseCode}");
                    }
                    else
                    {
                        lastUploadSuccessful = true;
                        if (cacheFile != null) {
                            try {
                                File.Delete(analyticsDirectory.FullName + "/" + cacheFile);
                                newAnalyticsCached = false;
                            } catch (Exception exception) {
                                Debug.LogError($"[Analytics] Error deleting cached version of successfully uploaded analytics data: {exception.Message}");
                            }
                        }
                    }
                }

                if (newAnalyticsCached) {
                    ++cachedAnalyticsCount;
                }
            }
            else
            {
                Debug.LogWarning("No Analytics URL found");
            }
        }

        private IEnumerator Upload(object data, string cacheFile = null)
        {
            if (App.Data.App.Analytics != null && App.Data.App.Analytics.Length > 0)
            {
                // run json conversion in separate thread
                bool bwFinished = false;
                BackgroundJsonReturns bjr = null;
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = false;
                bw.WorkerSupportsCancellation = false;
                yield return null;
                bw.DoWork += new DoWorkEventHandler(
                    delegate (object o, DoWorkEventArgs args)
                    {
                        object analyticsData = args.Argument;
                        if (analyticsData == null && !string.IsNullOrEmpty(cacheFile) && analyticsDirectory != null) {
                            // this is a cached data resend, so retrieve cached data first
                            FileStream cacheOpenStream = null;
                            try {
                                cacheOpenStream = File.OpenRead(analyticsDirectory.FullName + "/" + cacheFile);
                                BinaryFormatter cacheDeserializer = new BinaryFormatter();
                                analyticsData = cacheDeserializer.Deserialize(cacheOpenStream);
                            } catch (Exception exception) {
                                Debug.LogError($"[Analytics] Error reading cached analytics data, dould not deserialize data from disk: {exception.Message}");
                                analyticsData = null;
                            } finally {
                                cacheOpenStream?.Close();
                            }
                        }

                        if (analyticsData == null) {
                            // no data to json-ify or cache, returning nothing
                            args.Result = null;
                            return;
                        }

                        BackgroundJsonReturns bgr = new BackgroundJsonReturns();
                        try {
                            bgr.json = JsonUtility.ToJson(analyticsData);
                            bgr.key = AnalyticsSignature.GenerateSignature(bgr.json);
                        } catch (Exception e) {
                            Debug.LogError("Error generating json");
                            Debug.LogError(e.Message);
                        }

                        // cache analytics data
                        if (!string.IsNullOrEmpty(cacheFile)) {
                            bgr.cacheFile = cacheFile;
                        } else {
                            bgr.cacheFile = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                            if (analyticsDirectory != null && cachedAnalyticsCount < MAX_CACHED_ANALYTICS) {
                                FileStream cacheStream = null;
                                try {
                                    cacheStream = File.Create(analyticsDirectory.FullName + "/" + bgr.cacheFile);
                                    BinaryFormatter cacheSerializer = new BinaryFormatter();
                                    cacheSerializer.Serialize(cacheStream, analyticsData);
                                } catch (Exception exception) {
                                    Debug.LogError($"[Analytics] Error caching analytics data, dould not serialize data to disk: {exception.Message}");
                                    bgr.cacheFile = null;
                                } finally {
                                    cacheStream?.Close();
                                }
                            } else {
                                bgr.cacheFile = null;
                            }
                        }

                        args.Result = bgr;
                    }
                );

                yield return null;

                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                    delegate (object o, RunWorkerCompletedEventArgs args)
                    {
                        bjr = args.Result as BackgroundJsonReturns;
                        bwFinished = true;
                    });

                yield return null;

                bw.RunWorkerAsync(data);

                int timeout = 0;
                while (!bwFinished && timeout < 200)
                {
                    ++timeout;
                    yield return null;
                }

                bool newAnalyticsCached = false;
                bool uploadSuccess = false;
                if (bjr != null && bjr.cacheFile != null && string.IsNullOrEmpty(cacheFile)) {
                    newAnalyticsCached = true;
                }

                if (bjr != null && bjr.json != null && bjr.key != null && bjr.json.Length > 2 && bjr.key.Length > 0)
                {
                    using (UnityWebRequest analyticsPut = UnityWebRequest.Post(App.Data.App.Analytics, bjr.json))
                    {
                        analyticsPut.SetRequestHeader("Content-Type", "application/json");
                        analyticsPut.SetRequestHeader("X-PPVR-Signature", bjr.key);
                        float TimeOut = 0;
                        analyticsPut.SendWebRequest();
                        while (!analyticsPut.isDone && TimeOut <= 15)
                        {
                            //Debug.Log("Wait"+TimeOut);
                            yield return new WaitForSeconds(0.2f);
                            TimeOut += 0.2f;
                        }

                        if (analyticsPut.isNetworkError)
                        {
                            lastUploadSuccessful = false;
                            Debug.LogWarning($"Uploading analytics failed, network error: {analyticsPut.error}");
                        } 
                        else if (analyticsPut.isHttpError)
                        {
                            lastUploadSuccessful = false;
                            Debug.LogWarning($"Uploading analytics failed, server returned failure code ({analyticsPut.responseCode}): {analyticsPut.error}");
                        }
                        else if (TimeOut > 10)
                        {
                            lastUploadSuccessful = false;
                            Debug.LogWarning($"Analytics upload timed out: {analyticsPut.responseCode}");
                        } 
                        else
                        {
                            lastUploadSuccessful = true;
                            uploadSuccess = true;

                            if (bjr.cacheFile != null) {
                                try {
                                    File.Delete(analyticsDirectory.FullName + "/" + bjr.cacheFile);

                                    // reduce cached analytics count if this was a successful resent
                                    if (!newAnalyticsCached) {
                                        --cachedAnalyticsCount;
                                    }
                                    newAnalyticsCached = false;
                                } catch (Exception exception) {
                                    Debug.LogError($"[Analytics] Error deleting cached version of successfully uploaded analytics data: {exception.Message}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Incorrect json or json key");
                }

                if (newAnalyticsCached) {
                    ++cachedAnalyticsCount;
                }

                if (uploadSuccess && string.IsNullOrEmpty(cacheFile) && cachedAnalyticsCount > 0) {
                    // try re-uploading cached analytics
                    StartCoroutine(ResendCached());
                }
            } else
            {
                Debug.LogWarning("No Analytics URL found");
            }
        }

        /// <summary>
        /// loop through all files stores in analytics cache folder and try to resend each of them
        /// <summary>
        private IEnumerator ResendCached() {
            if (analyticsDirectory == null) {
                yield break;
            }

            int counter = 0;
            foreach (FileInfo cachedFile in analyticsDirectory.EnumerateFiles("*", SearchOption.TopDirectoryOnly)) {
                ++counter;
                // start uploading multiple cached analytics packets simultaneously to speed up uploading a large
                // backlog of cached analytics
                if ((counter % 4) != 0) {
                    StartCoroutine(Upload(null, cachedFile.Name));
                } else {
                    yield return StartCoroutine(Upload(null, cachedFile.Name));
                    // retry in case of failure
                    int retries = 2;
                    while (!lastUploadSuccessful && retries > 0) {
                        --retries;
                        yield return StartCoroutine(Upload(null, cachedFile.Name));
                    }
                    // uploading failed repeatedly, stop trying to upload rest of cached analytics
                    if (!lastUploadSuccessful) {
                        break;
                    }
                }
            }

            yield break;
        }

        internal IEnumerator Record()
        {
            //Debug.Log("Record started");
            yield return null;
			while  (App.Player == null || dataBuffers == null) {
				yield return new WaitForSeconds(0.1f);
			}
            while (!App.Player.IsPlaying)
            {
                yield return new WaitForSeconds(0.1f);
            }
            //NewData DATA = new NewData();
            for (int i = 0; i < dataBuffers.Length; ++i)
            {
                dataBuffers[i].EventType = "NewData";
                dataBuffers[i].VideoSessionId = CurrentVideoSessionId;
                dataBuffers[i].SessionId = CurrentSessionId;
                dataBuffers[i].VideoId = CurrentVideoId;
                dataBuffers[i].ProjectId = CurrentProjectId;
                dataBuffers[i].AppId = App.AppId;
                dataBuffers[i].C = new List<Coordinates>(MAX_DATA_COUNT);
            }

            //DATA.C = coordBuffers[currentCoordBuff];// new List<Coordinates>();

            float Last = 0;
            int Correction = 0;
            float lastTotalTime = Time.time;

            while (CurrentVideoId == dataBuffers[currentDataBuffer].VideoId
                && CurrentProjectId == dataBuffers[currentDataBuffer].ProjectId
                && App.Player != null 
                && (int)App.Player.GetStatus() > 3)
                //&& (App.Player.GetStatus() == VideoPlayer.Status.VP_PAUSED
                //|| App.Player.GetStatus() == VideoPlayer.Status.VP_PLAYING
                //|| App.Player.GetStatus() == VideoPlayer.Status.VP_READY
                //|| App.Player.GetStatus() == VideoPlayer.Status.VP_BUFFERING))
            {

                if (App.Player.Seek == Last)
                {
                    Correction = Mathf.RoundToInt(1000 * (Time.time - lastTotalTime));
                }
                else
                {
                    Correction = 0;
                    lastTotalTime = Time.time;
                }
                Last = App.Player.Seek;

                if (App.Player.IsPlaying)
                {
                    Coordinates R = new Coordinates();

                    int Add = 0;
                    if (App.camera.eulerAngles.x > 90 && App.camera.eulerAngles.x < 270)
                    {
                        Add = 180;
                    }
                    R.x = (int)Mathf.RoundToInt(((App.camera.eulerAngles.y + 180f + Add) % 360f) * 11.375f);
                    R.y = (int)Mathf.RoundToInt(((App.camera.eulerAngles.x + 90f) % 360f) * 22.75f);
                    R.t = Mathf.RoundToInt((App.Data.Video[CurrentVideoId].Duration * 1f) * App.Player.Seek * 1f) + Correction;
                    //Debug.Log(R.t);
                    dataBuffers[currentDataBuffer].C.Add(R);
                    //DATA.C.Add(R);
                    if (dataBuffers[currentDataBuffer].C.Count >= MAX_DATA_COUNT)
                    {
                        //Debug.Log(DATA.C.Count);
                        dataBuffers[currentDataBuffer].TimeStamp = GetUtcNow();
                       
                        StartCoroutine(Upload(dataBuffers[currentDataBuffer]));

                        yield return null;
                        currentDataBuffer = (currentDataBuffer + 1) % 2;
                        dataBuffers[currentDataBuffer].C.Clear();// dataBuffers[currentDataBuffer].C =  new List<Coordinates>(MAX_DATA_COUNT);
                        //DATA.C = new List<Coordinates>();
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }

            if (dataBuffers[currentDataBuffer].C.Count > 0)
            {
                dataBuffers[currentDataBuffer].TimeStamp = GetUtcNow();

                StartCoroutine(Upload(dataBuffers[currentDataBuffer]));
            }

            Debug.Log("Stopped Recording Analytics");
            EndVideo endDATA = new EndVideo();
            endDATA.EventType = "EndVideo";
            endDATA.VideoSessionId = dataBuffers[currentDataBuffer].VideoSessionId;
            endDATA.SessionId = dataBuffers[currentDataBuffer].SessionId;
            endDATA.VideoId = dataBuffers[currentDataBuffer].VideoId;
            endDATA.ProjectId = dataBuffers[currentDataBuffer].ProjectId;
            endDATA.AppId = dataBuffers[currentDataBuffer].AppId;
            endDATA.TimeStamp = GetUtcNow();

            StartCoroutine(Upload(endDATA));

            CurrentVideoSessionId = null;
        }


        void Start () 
        {
            // create analytics directory on local storage to cache analytics
            try {
                analyticsDirectory = new DirectoryInfo(App.BasePath + ANALYTICS_DIRECTORY);
            } catch (System.Security.SecurityException securityException) {
                analyticsDirectory = null;
                Debug.LogError($"[Analytics] Security error constructing analytics cache directory reference: {securityException.Message}");
            } catch (ArgumentException argumentException) {
                analyticsDirectory = null;
                Debug.LogError($"[Analytics] Path to analytics cache directory contains invalid character(s), cannot cache analytics: {argumentException.Message}");
            } catch (PathTooLongException pathTooLongException) {
                analyticsDirectory = null;
                Debug.LogError($"[Analytics] Path to analytics cache directory is too long, cannot cache analytics: {pathTooLongException.Message}");
            } catch (Exception exception) {
                analyticsDirectory = null;
                Debug.LogError($"[Analytics] Unknown error constructing analytics cache directory reference: {exception.Message}");
            }

            if (analyticsDirectory != null && !analyticsDirectory.Exists) {
                try {
                    analyticsDirectory.Create();
                } catch (IOException ioException) {
                    analyticsDirectory = null;
                    Debug.LogError($"[Analytics] Could not create analytics cache directory: {ioException.Message}");
                } catch (Exception exception) {
                    analyticsDirectory = null;
                    Debug.LogError($"[Analytics] Could not create analytics cache directory: {exception.Message}");
                }
            }

            if (analyticsDirectory != null) {
                try {
                    cachedAnalyticsCount = analyticsDirectory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Count();
                } catch (Exception exception) {
                    Debug.LogError($"[Analytics] Error counting files in analytics cache, cannot cache analytics: {exception.Message}");
                    analyticsDirectory = null;
                    cachedAnalyticsCount = 0;
                }
            }

            // create buffers for coordinate saving and uploading
            dataBuffers = new NewData[2];
            for (int i = 0; i < dataBuffers.Length; ++i)
            {
                dataBuffers[i] = new NewData();
                dataBuffers[i].C = new List<Coordinates>(MAX_DATA_COUNT);
            }

            App.AnalyticsManager = this;
            CurrentSessionId=GetRndSessionID();
            StartApp DATA = new StartApp();
            DATA.EventType = "StartApp";
            DATA.SessionId = CurrentSessionId;
            DATA.AppId = App.AppId;
            DATA.Os = SystemInfo.operatingSystem;
#if UNITY_ANDROID
			if (App.CurrentPlatform == App.VRPlatform.Oculus) {
#if USE_OCULUS_SDK
				OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
                if (headset == OVRPlugin.SystemHeadset.Oculus_Quest) {
                    DATA.Platform = "Oculus Quest";
                } else if (headset == OVRPlugin.SystemHeadset.Oculus_Quest_2) {
					DATA.Platform = "Oculus Quest 2";
				} else if (headset == OVRPlugin.SystemHeadset.Meta_Quest_Pro) {
					DATA.Platform = "Meta Quest Pro";
				} else {
                    DATA.Platform = "Oculus";
                }
#endif
			} else if (App.CurrentPlatform == App.VRPlatform.Daydream) {
				DATA.Platform = "Daydream";
			} else if (App.CurrentPlatform == App.VRPlatform.ViveWave) {
				DATA.Platform = "Vive Wave";
            } else if (App.CurrentPlatform == App.VRPlatform.Pico) {
                DATA.Platform = "Pico G2";
			} else if (App.CurrentPlatform == App.VRPlatform.PicoPlatform) {
#if USE_PICO_PLATFORM_SDK
                switch (Unity.XR.PXR.PXR_Input.GetControllerDeviceType()) {
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.G2:
                        DATA.Platform = "Pico G2";
                        break;
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.Neo2:
                        DATA.Platform = "Pico Neo2";
                        break;
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.Neo3:
                        DATA.Platform = "Pico Neo3";
                        break;
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.PICO_4:
                        DATA.Platform = "Pico 4";
                        break;
                    case Unity.XR.PXR.PXR_Input.ControllerDevice.NewController:
                    default:
                        DATA.Platform = "Pico";
                        break;
                }
#endif
			} else if (App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
#if USE_OCULUS_LEGACY_SDK
				OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
                if ((int)headset < (int)OVRPlugin.SystemHeadset.Oculus_Go) {
                    DATA.Platform = "Samsung Gear VR";
                } else if (headset == OVRPlugin.SystemHeadset.Oculus_Go) {
					DATA.Platform = "Oculus Go";
				} else if (headset == OVRPlugin.SystemHeadset.Oculus_Quest) {
                    DATA.Platform = "Oculus Quest";
                } else {
                    DATA.Platform = "Oculus";
                }
#endif
            } else {
				DATA.Platform = "Cardboard Android";
			}
#endif
#if UNITY_STANDALONE
            if (App.CurrentPlatform == App.VRPlatform.Oculus ||
                App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
                DATA.Platform = "Oculus PC";
            } else if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
                DATA.Platform = "SteamVR";
            } else if (App.CurrentPlatform == App.VRPlatform.WindowsMixedReality) {
				DATA.Platform = "Windows Mixed Reality";
			} else {
                DATA.Platform = "Standalone PC";
            }
#endif
#if UNITY_IOS
            DATA.Platform = "Cardboard iOS";
#endif
			DATA.Device = SystemInfo.deviceModel;
            DATA.DeviceId = App.Guid;
            DATA.TimeStamp = GetUtcNow();

            StartCoroutine(Upload(DATA));
        }

        void OnDestroy ()
        {
            Debug.Log($"[Analytics] cached analytics count: {cachedAnalyticsCount}");

            EndApp endDATA = new EndApp();
            endDATA.EventType = "EndApp";
            endDATA.SessionId = CurrentSessionId;
            endDATA.AppId = App.AppId;
            endDATA.TimeStamp = GetUtcNow();
            UploadImmediately(endDATA);
        }

        internal void StartingVideo(string projectId, string videoId)
        {
            CurrentVideoId = videoId;
            CurrentProjectId = projectId;
            StartVideo DATA = new StartVideo();
            CurrentVideoSessionId = GetRndSessionID();
            DATA.EventType="StartVideo";
            DATA.VideoSessionId = CurrentVideoSessionId;
            DATA.SessionId = CurrentSessionId;
            DATA.VideoId = CurrentVideoId;
            DATA.ProjectId = CurrentProjectId;
            DATA.AppId = App.AppId;
            DATA.DeviceId = App.Guid;
            DATA.TimeStamp = GetUtcNow();
            if (App.CurrentPlatform == App.VRPlatform.Cardboard)
            {
                DATA.VrMode = false;

                // TODO: update to Unity 2021 (removed for testing)
                // if (UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.isInitializationComplete) {
                //     DATA.VrMode = true;
                // } else {
                //     DATA.VrMode = false;
                // }
            }
            else
            {
                DATA.VrMode = true;
            }
            
            StartCoroutine(Upload(DATA));
            StartCoroutine(Record());
        }

        internal void SendStartVideoEvent(string projectId, string videoId)
        {
            if (CurrentVideoSessionId != null && CurrentProjectId != null && CurrentVideoId != null) {
                SendStopVideoEvent(CurrentProjectId, CurrentVideoId);
            }

            CurrentProjectId = projectId;
            CurrentVideoId = videoId;
            StartVideo DATA = new StartVideo();
            CurrentVideoSessionId = GetRndSessionID();
            DATA.EventType = "StartVideo";
            DATA.VideoSessionId = CurrentVideoSessionId;
            DATA.SessionId = CurrentSessionId;
            DATA.VideoId = CurrentVideoId;
            DATA.ProjectId = CurrentProjectId;
            DATA.AppId = App.AppId;
            DATA.DeviceId = App.Guid;
            DATA.TimeStamp = GetUtcNow();
            DATA.VrMode = true;
            
            Debug.Log($"[Analytics] sending manual StartVideo event for project: {projectId}");
            StartCoroutine(Upload(DATA));
        }

        internal void SendStopVideoEvent(string projectId, string videoId)
        {
            if (CurrentVideoSessionId == null) {
                Debug.LogWarning("[Analytics] No current video session exists to send stop event about");
                return;
            }

            EndVideo DATA = new EndVideo();
            DATA.EventType = "EndVideo";
            DATA.VideoSessionId = CurrentVideoSessionId;
            DATA.SessionId = CurrentSessionId;
            DATA.VideoId = videoId;
            DATA.ProjectId = projectId;
            DATA.AppId = App.AppId;
            DATA.TimeStamp = GetUtcNow();

            Debug.Log($"[Analytics] sending manual EndVideo event for project: {projectId}");
            StartCoroutine(Upload(DATA));

            CurrentVideoSessionId = null;
        }

        private static string GetRndSessionID()
        {
            byte[] byteArr = new byte[9];
            for (int i = 0; i < byteArr.Length; ++i)
            {
                byteArr[i] = (byte)Mathf.FloorToInt(UnityEngine.Random.value * 255);
            }
            string rndID = Convert.ToBase64String(byteArr);
            rndID = rndID.Replace('/', '_');
            rndID = rndID.Replace('+', '-');
            return rndID;
        }

        private static int GetUtcNow()
        {
            return (int)(DateTime.UtcNow - EPOCH).TotalSeconds;
        }
    }
}