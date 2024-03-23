// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace Headjack {
    internal class PushServerHandler : MonoBehaviour {
        public static PushServerHandler instance = null;
        public static Dictionary<string, PushEvent> eventDict = new Dictionary<string, PushEvent>();

        public bool Connected {
            get {
                return isConnected;
            }
        }

        private int ErrorCounter {
            get {
                return errorCounter;
            }
            set {
                errorCounter = value;
                if (errorCounter > 3) {
                    isConnected = false;
                } else {
                    isConnected = true;
                }
            }
        }

        private bool isConnected = false;
        private int errorCounter = 0;
        private string lastReceivedMsg = null;
        private string sessionKey = null;
        private string cachedAppId = null;
        Coroutine currentConnection = null;

        private string serverURL = null;

        // !!!!!
        // ToDo: handle server errors in json response

        private struct Status {
            public string status;
            public string message;
            public float progressTotal;
            public float progressCurrent;
            public string progressUnit;
        }

        private struct ConnectPost {
            public string authKey;
            public string vrSDK;
            public string os;
            public string deviceModel;
        }

		public PushServerHandler()
		{
			instance = this;

			// start remote server connect
			serverURL = App.Data.App.Cinema;
		}

        // Use this for initialization
        void Start() {
            
            currentConnection = StartCoroutine(ServerConnect());

        }

        UnityWebRequest GetJsonPostWebRequest(string url, string json = null) {
            UploadHandlerRaw uploadHandler = null;
            if (json != null) {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)) {
                    contentType = "application/json",

                };
            }
            UnityWebRequest jsonPostWR = new UnityWebRequest(url, "POST") {
                downloadHandler = new DownloadHandlerBuffer(),
                uploadHandler = uploadHandler,
                disposeDownloadHandlerOnDispose = true,
                disposeUploadHandlerOnDispose = true
            };

            return jsonPostWR;
        }

        IEnumerator ServerConnect() {
            if (serverURL == null)
            {
                Debug.Log("No cinema server address found, not connecting");
                yield break;
            }

            ConnectPost connectObj = new ConnectPost() {
                authKey = App.Authkey,
                vrSDK = App.CurrentPlatform.ToString(),
                os = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel
            };

            cachedAppId = App.AppId;

            // setup connection to push server
            using (UnityWebRequest pushServerConnection = GetJsonPostWebRequest(serverURL + "/connect/" + App.AppId + "/" + App.Guid,
                JsonUtility.ToJson(connectObj))) {
                // send device status along
                SetDeviceStatusHeaders(pushServerConnection);

				//AsyncOperation ao = pushServerConnection.SendWebRequest();

				UnityWebRequestAsyncOperation ao = pushServerConnection.SendWebRequest();
				while (ao != null && !ao.isDone)
				{
					//Debug.Log(ao);
					//Debug.Log(ao.isDone);
					//Debug.Log(ao.progress);
					yield return null;
				}

                if (pushServerConnection.isDone && !pushServerConnection.isNetworkError && pushServerConnection.responseCode < 400) {

                    // successfully connected to push server, get session id and alias
                    JSONNode connectedJson = null;
                    try {
                        connectedJson = JSON.Parse(pushServerConnection.downloadHandler.text);
                        if (connectedJson == null) {
                            throw new System.Exception("Json loading failed: " + pushServerConnection.downloadHandler.text);
                        }
                    }
                    catch (System.Exception e) {
                        Debug.LogError("Loading json from push server response failed: " + e.Message);
                        yield break;
                    }
                    // get session key and device alias
                    try {
                        sessionKey = connectedJson["sessionKey"];

                        // start coroutine that keeps long polling connection open
                        if (sessionKey == null) {
                            Debug.LogError("Session key not received");
                            yield break;
                        }

                        ErrorCounter = 0;
                        StartCoroutine(LongPollingConnect());
                    } catch (System.Exception e) {
                        Debug.LogError("connect json reading failed: " + e.Message);
                    }
                } else {
                    Debug.LogError("Push server connection failed (" + pushServerConnection.responseCode + "): " + pushServerConnection.error + " " + pushServerConnection.downloadHandler.text);
                }
            }
            yield break;
        }

        IEnumerator LongPollingConnect() {
            while (sessionKey != null) {
                // TODO: test error handling
                if (ErrorCounter > 15)
                {
                    ServerDisconnect();
                    break;
                }

                using (UnityWebRequest watchServer = UnityWebRequest.Get(serverURL + "/watch/" + App.AppId + "/" + App.Guid + "?session=" + sessionKey + (lastReceivedMsg != null ? "&lastmsg=" + lastReceivedMsg : ""))) {
                    // send device status along
                    SetDeviceStatusHeaders(watchServer);

                    watchServer.timeout = 120;

					UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
					UnityWebRequestAsyncOperation ao = watchServer.SendWebRequest();
					bool abortNetworkRequest = false;
					while (ao != null && !ao.isDone)
					{
						if (currentScene != UnityEngine.SceneManagement.SceneManager.GetActiveScene() || sessionKey == null)
						{
							Debug.LogWarning("Cinema detected Scene switch (or session disconnect), restarting WebRequest");
							abortNetworkRequest = true;
							break;
						}
						yield return null;
					}
					if (abortNetworkRequest) continue;

					if (watchServer.isDone && !watchServer.isNetworkError && watchServer.responseCode < 400) {
                        lastReceivedMsg = watchServer.GetResponseHeader("x-msgid");
                        // received message from push server
                        JSONNode msgJson = null;
                        try {
                            msgJson = JSON.Parse(watchServer.downloadHandler.text);
                            if (msgJson == null) {
                                throw new System.Exception("Json loading failed!");
                            }
							//Debug.Log(msgJson);
                        }
                        catch (System.Exception e) {
                            Debug.LogError("Loading json from push server response failed: " + e.Message);
                            ErrorCounter += 1;
                            continue;
                        }

                        // handle message from server
                        try {
                            if (msgJson["error"] != null)
                            {
                                Debug.LogError("error from server: " + msgJson["error"]);
                                ErrorCounter += 1;
                                continue;
                            }


                            ErrorCounter = 0;
                            foreach (JSONNode action in (JSONArray)msgJson["actions"]) {
                                PushEvent thisEvent = null;
                                if (eventDict.TryGetValue(action["type"], out thisEvent)) {
                                    thisEvent((JSONArray)action["message"]);
                                }
                            }
                        }
                        catch (System.Exception e) {
                            Debug.LogError("Reading json message failed: " + e.Message);
                            Debug.LogError(e.StackTrace);
                            ErrorCounter += 1;
                            continue;
                        }
                    }
                    else {
                        if (watchServer.responseCode != 0) {
                            // not a regular long polling timeout, actual error
                            Debug.LogError("Error long polling cinema server (" + watchServer.responseCode + "): " + watchServer.error + " " + watchServer.downloadHandler.text);
                            ErrorCounter += 1;
                            try {
                                JSONNode errorJson = JSON.Parse(watchServer.downloadHandler.text);
                                if (errorJson["error"] == "device not found") {
                                    sessionKey = null;
                                    isConnected = false;
                                    currentConnection = StartCoroutine(ServerConnect());
                                }
                            }
                            catch (System.Exception) {
                                // could not load error json
                            }

                        } else {
                            if (!watchServer.error.ToLower().Contains("timeout"))
                            {
                                ErrorCounter += 1;
                            } else
                            {
                                // regular long polling timeout
                                ErrorCounter = 0;
                            }
                        }
                    }
                }
            }
            Debug.Log("Cinema, Session key is null");
			yield break;
        }

        public void SendDeviceStatus(string status, string message, float progressCurrent, float progressTotal, string progressUnit) {
            if (sessionKey != null) {
                StartCoroutine(SendStatus(status, message, progressCurrent, progressTotal, progressUnit));
            }
        }

        // send current status to server
        IEnumerator SendStatus(string status, string message, float progressCurrent, float progressTotal, string progressUnit) {
            Status jsonObj = new Status() {
                status = status,
                message = message,
                progressCurrent = progressCurrent,
                progressTotal = progressTotal,
                progressUnit = progressUnit
            };
            using (UnityWebRequest pushServerConnection = GetJsonPostWebRequest(serverURL + "/status/" + App.AppId + "/" + App.Guid + "?session=" + sessionKey, JsonUtility.ToJson(jsonObj))) {
                // send device status along
                SetDeviceStatusHeaders(pushServerConnection);

                yield return pushServerConnection.SendWebRequest();

                if (pushServerConnection.isDone && !pushServerConnection.isNetworkError && pushServerConnection.responseCode < 400) {
                    // successfully sent status
                } else {
                    Debug.LogError("Sending status failed (" + pushServerConnection.responseCode + "): " + pushServerConnection.error);
                }
            }
            yield break;
        }

        private void ServerDisconnect(bool waitForDisconnect = true) {
			//Debug.Log("ServerDisconnect?");

            ErrorCounter = 0;
            isConnected = false;
            if (sessionKey != null) {
                // stop longpollingconnect coroutine
                StopCoroutine(LongPollingConnect());
                // send disconnect
                UnityWebRequest disconnectServer = GetJsonPostWebRequest(
                    serverURL + "/disconnect/" + cachedAppId + "/" + App.Guid + "?session=" + sessionKey);

                disconnectServer.SendWebRequest();
                Debug.Log("Disconnecting from cinema server");
                sessionKey = null;
                cachedAppId = null;

                if (waitForDisconnect)
                {
                    int timeout = 0;
                    do
                    {
                        timeout += 100;
                        System.Threading.Thread.Sleep(100);
                    } while (!disconnectServer.isDone && !disconnectServer.isNetworkError && timeout < 1000);
                    if (disconnectServer.isDone)
                    {
                        Debug.Log("disconnect message: " + disconnectServer.downloadHandler.text);
                    }
                }
            } else
            {
                Debug.Log("Current session key does not exist, probably already disconnected");
            }
        }

        private readonly static string hexGlyphs = "1234567890abcdef";

        private static string GetRandomHexString(int length) {
            string returnHex = "";
            for (int i = 0; i < length; ++i) {
                returnHex += hexGlyphs[UnityEngine.Random.Range(0, hexGlyphs.Length)];
            }
            return returnHex;
        }

        private void SetDeviceStatusHeaders(UnityWebRequest wr)
        {
            // send battery lvl
            wr.SetRequestHeader("x-batterylvl", (SystemInfo.batteryLevel * 100f).ToString("F0"));

            // send battery temperature and throttling level for Oculus builds
            if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
            {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
                wr.SetRequestHeader("x-batterytemp", (OVRManager.batteryTemperature).ToString("F0"));
                wr.SetRequestHeader("x-powersaving", OVRManager.isPowerSavingActive ? "1" : "0");
#endif
            }
            else
            {
                wr.SetRequestHeader("x-batterytemp", "0");
                wr.SetRequestHeader("x-powersaving", "0");
            }
        }

		private void OnDestroy() {
			ServerDisconnect();
        }

        private void OnApplicationPause(bool pause) {
            if (pause) {
                ServerDisconnect();
            } else {
                currentConnection = StartCoroutine(ServerConnect());
            }
        }
    }
}