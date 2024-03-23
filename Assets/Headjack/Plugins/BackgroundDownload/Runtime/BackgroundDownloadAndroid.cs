#if UNITY_ANDROID

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.Networking
{

    class BackgroundDownloadAndroid : BackgroundDownload
    {
        private const string TEMP_FILE_SUFFIX = ".part";
        private const string FILE_PROTOCOL = "file://";
        private const string CACHE_FILENAME = "unity_background_downloads.dl";
        static AndroidJavaClass _playerClass;
        static AndroidJavaClass _backgroundDownloadClass;

        class Callback : AndroidJavaProxy
        {
            public Callback()
                : base("com.unity3d.backgrounddownload.CompletionReceiver$Callback")
            {}

            void downloadCompleted()
            {
                lock (typeof(BackgroundDownload))
                {
                    foreach (var download in _downloads.Values)
                        ((BackgroundDownloadAndroid) download).CheckFinished();
                }
            }
        }

        static Callback _finishedCallback;

        AndroidJavaObject _download;
        long _id = 0;
        string _tempFilePath;

        static void SetupBackendStatics()
        {
            if (_backgroundDownloadClass == null)
                _backgroundDownloadClass = new AndroidJavaClass("com.unity3d.backgrounddownload.BackgroundDownload");
            if (_finishedCallback == null)
            {
                _finishedCallback = new Callback();
                var receiver = new AndroidJavaClass("com.unity3d.backgrounddownload.CompletionReceiver");
                receiver.CallStatic("setCallback", _finishedCallback);
            }
            if (_playerClass == null)
                _playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        }

        internal BackgroundDownloadAndroid(BackgroundDownloadConfig config)
            : base(config)
        {
            SetupBackendStatics();
            string filePath = config.filePath;
            _tempFilePath = filePath + TEMP_FILE_SUFFIX;
            if (File.Exists(filePath))
                File.Delete(filePath);
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
            else
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            string fileUri = FILE_PROTOCOL + _tempFilePath;
            bool allowMetered = false;
            bool allowRoaming = false;
            switch (_config.policy)
            {
                case BackgroundDownloadPolicy.AllowMetered:
                    allowMetered = true;
                    break;
                case BackgroundDownloadPolicy.AlwaysAllow:
                    allowMetered = true;
                    allowRoaming = true;
                    break;
                default:
                    break;
            }
            _download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("create", config.url.AbsoluteUri, fileUri);

            _download.Call("setAllowMetered", allowMetered);
            _download.Call("setAllowRoaming", allowRoaming);
            _download.Call("setNotificationVisibility", _config.showNotification);
            if (_config.notificationTitle != null) {
                _download.Call("setTitle", _config.notificationTitle);
            }
            if (_config.notificationDescription != null) {
                _download.Call("setDescription", _config.notificationDescription);
            }
            if (config.requestHeaders != null)
                foreach (var header in config.requestHeaders)
                    if (header.Value != null)
                        foreach (var val in header.Value)
                            _download.Call("addRequestHeader", header.Key, val);
            var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            _id = _download.Call<long>("start", activity);
        }

        BackgroundDownloadAndroid(long id, AndroidJavaObject download, string uid)
        {
            _id = id;
            _download = download;
            _config.url = QueryDownloadUri();
            _config.filePath = QueryDestinationPath(out _tempFilePath);
            _config.uid = uid;
            CheckFinished();
        }

        static BackgroundDownloadAndroid Recreate(long id, string uid)
        {
            try
            {
                SetupBackendStatics();
                var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
                var download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("recreate", activity, id);
                if (download != null)
                    return new BackgroundDownloadAndroid(id, download, uid);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Failed to recreate background download with id {0}: {1}", id, e.Message));
            }

            return null;
        }

        Uri QueryDownloadUri()
        {
            return new Uri(_download.Call<string>("getDownloadUrl"));
        }

        string QueryDestinationPath(out string tempFilePath)
        {
            string uri = _download.Call<string>("getDestinationUri");
            if (uri.IndexOf(FILE_PROTOCOL) == 0) {
                tempFilePath = uri.Substring(FILE_PROTOCOL.Length);
            } else {
                tempFilePath = uri;
            }

            var suffixPos = tempFilePath.LastIndexOf(TEMP_FILE_SUFFIX);
            if (suffixPos > 0)
            {
                return tempFilePath.Substring(0, suffixPos);
            }

            return tempFilePath;
        }

        string GetError()
        {
            return _download.Call<string>("getError");
        }

        void CheckFinished()
        {
            if (_status == BackgroundDownloadStatus.Downloading)
            {
                int status = _download.Call<int>("checkFinished");
                if (status == 1)
                {
                    if (_tempFilePath.EndsWith(TEMP_FILE_SUFFIX))
                    {
                        string filePath = _tempFilePath.Substring(0, _tempFilePath.Length - TEMP_FILE_SUFFIX.Length);
                        if (File.Exists(_tempFilePath))
                        {
                            if (File.Exists(filePath))
                                File.Delete(filePath);
                            File.Move(_tempFilePath, filePath);
                        }
                    }

                    _status = BackgroundDownloadStatus.Done;
                }
                else if (status < 0)
                {
                    _status = BackgroundDownloadStatus.Failed;
                    _error = GetError();
                }
            }
        }

        void RemoveDownload()
        {
            _download.Call("remove");
        }

        public override bool keepWaiting { get { return _status == BackgroundDownloadStatus.Downloading; } }

        protected override float GetProgress()
        {
            return _download.Call<float>("getProgress");
        }

        protected override long GetDownloadedBytes()
        {
            long downloadedBytes = _download.Call<long>("getDownloadedBytes");
            if (downloadedBytes < 0) {
                CheckFinished();
                downloadedBytes = 0;
            }

            return downloadedBytes;
        }

        protected override long GetTotalBytes()
        {
            return _download.Call<long>("getTotalBytes");
        }

        public override void Dispose()
        {
            RemoveDownload();
            base.Dispose();
        }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();
            var file = Path.Combine(Application.persistentDataPath, CACHE_FILENAME);
            if (File.Exists(file))
            {
                foreach (var line in File.ReadAllLines(file))
                    if (!string.IsNullOrEmpty(line))
                    {
                        int pos = line.IndexOf(",");
                        long id = long.Parse(line.Substring(0, pos));
                        string uid = line.Substring(pos + 1);
                        var dl = Recreate(id, uid);
                        if (dl != null)
                            downloads[dl.config.filePath] = dl;
                    }
            }

            // some loads might have failed, save the actual state
            SaveDownloads(downloads);
            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
            var file = Path.Combine(Application.persistentDataPath, CACHE_FILENAME);
            if (downloads.Count > 0)
            {
                var ids = new string[downloads.Count];
                int i = 0;
                foreach (var dl in downloads)
                    ids[i++] = ((BackgroundDownloadAndroid)dl.Value)._id.ToString() +
                        "," + ((BackgroundDownloadAndroid)dl.Value)._config.uid;
                File.WriteAllLines(file, ids);
            }
            else if (File.Exists(file))
                File.Delete(file);
        }
    }

}

#endif
