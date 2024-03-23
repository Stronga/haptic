// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System;
using System.Runtime.InteropServices;
namespace Headjack
{
    internal class onComplete : AndroidJavaProxy
    {
        OnEnd onDone;
        public onComplete( OnEnd onEnd=null ) : base("android.media.MediaPlayer$OnCompletionListener")
        {
            onDone = onEnd;
            //Debug.Log("created oncomplete with onend: " + onDone);
        }
        public void onCompletion(AndroidJavaObject mp)
        {
            if (onDone != null)
            {
                onDone(true, null);
            }
        }
    }

    internal class onReady : AndroidJavaProxy
    {
        OnEnd onDone;
        public onReady( OnEnd onEnd =null):base("android.media.MediaPlayer$OnPreparedListener")
        {
            onDone = onEnd;
            Debug.Log("Created onReady with onend: " + onDone);
        }
        public void onPrepared(AndroidJavaObject mp)
        {
            if(onDone!=null)
            {
                onDone(true, null);
            }
        }
    }

    internal class onSeekDone : AndroidJavaProxy
    {
        OnEnd onDone;
        public onSeekDone(OnEnd onEnd=null):base("android.media.MediaPlayer$OnSeekCompleteListener")
        {
            onDone = onEnd;
        }
        public void onSeekComplete(AndroidJavaObject mp)
        {
            if(onDone!=null)
            {
                onDone(true, null);
            }
        }
    }

	internal class OnError : AndroidJavaProxy
	{
		OnEnd onDone;
		public OnError(OnEnd onEnd = null) : base("android.media.MediaPlayer$OnErrorListener")
		{
			onDone = onEnd;
		}
		public bool onError(AndroidJavaObject mp,int what, int extra)
		{
			Debug.LogError("ERROR" + what + " " + extra);
			return true;
		}
	}

	internal class OnBufferingUpdate : AndroidJavaProxy
	{
		public OnBufferingUpdate() : base("android.media.MediaPlayer$OnBufferingUpdateListener")
		{
		}
		public void onBufferingUpdate(AndroidJavaObject mp, int percent)
		{
			//Debug.Log("buffer " + percent);
		}
	}

	internal class onInfoListener : AndroidJavaProxy
    {
        OnEnd onReceiveBuffer;
        OnEnd onStartBuffer;
        OnEnd onEndBuffer;
        public onInfoListener(OnEnd onReceive=null, OnEnd onStart=null, OnEnd onEndB=null):base("android.media.MediaPlayer$OnInfoListener")
        {
            onReceiveBuffer = onReceive;
            onStartBuffer = onStart;
            onEndBuffer = onEndB;
        }
        public bool onInfo(AndroidJavaObject mp, int what, int extra)
        {
            if (what == 701)
            {
                if (onStartBuffer != null)
                {
                    onStartBuffer(true, null);
                }
            }
            if (what == 702)
            {
                if (onEndBuffer != null)
                {
                    onEndBuffer(true, null);
                }
            }
            if (what == 703)
            {
                if (onReceiveBuffer != null)
                {
                    onReceiveBuffer(true, extra.ToString());
                }
            }
            Debug.Log("what: " + what + " - extra: " + extra);
            return true;
        }
    }

    internal class VideoPlayer_OMS : VideoPlayerBase
    {
        private float _volume = 1f;
		private bool _loop = false;
        private long StartSeek = -1;
        private bool _buffering=false;
        private int minBandwidth;
        private int LastUTC = 0;
		private string url, fallbackUrl;
        private MeshRenderer RenderTo;
        private long seekCorrection = 0;
        private float fallbackCountdown = 0;
        private float fallbackAfterSecs = 15;

        private bool                 videoPaused = false;
        private bool                startedVideo = false;
        private AndroidJavaObject   mediaPlayer = null;
        private AndroidJavaObject assetFileDescriptor = null;
        //private Renderer            mediaRenderer = null;
        private Texture2D           nativeTexture = null;
        private IntPtr              nativeTexId = IntPtr.Zero;
        private int                 textureWidth = 512;
        private int                 textureHeight = 512;
        // holds previous playback paused status to restore status on app resume
        private bool _wasPlaying = false;
        private bool _wasPlayingBeforePaused = false;
        private float _picoHeadsetOffTime = 0f;
        private bool _wasPlayingBeforePicoHeadsetOff = false;
        private bool _wavePresenceTriggered = false;
        private bool _waveWasPlaying = false;

        private static DateTime EPOCH = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        private enum MediaSurfaceEventType
        {
            Initialize = 0,
            Shutdown = 1,
            Update = 2,
            Max_EventType
        };

        private static void IssuePluginEvent(MediaSurfaceEventType eventType)
        {
#pragma warning disable 618
            GL.IssuePluginEvent((int)eventType);
#pragma warning restore 618
        }

        void OnApplicationFocus(bool hasFocus) {
            if (hasFocus)
            {
                if (_wasPlaying) {
                    Resume();
                }
            } else if (App.PauseVideoOnFocusLoss) {
                _wasPlaying = IsPlaying;
                Pause();
            } else {
                _wasPlaying = false;
            }
        }

        void OnApplicationPause(bool paused) {
            if (App.CurrentPlatform == App.VRPlatform.Cardboard ||
                App.CurrentPlatform == App.VRPlatform.Pico ||
                App.CurrentPlatform == App.VRPlatform.PicoPlatform ||
                App.CurrentPlatform == App.VRPlatform.ViveWave) {
                if (paused) {
                    _wasPlayingBeforePaused = IsPlaying;
                    Pause();
                } else if (_wasPlayingBeforePaused) {
                    Resume();
                    _wasPlayingBeforePaused = false;
                }
            }
        }

        internal override void Initialize()
        {
            App.VideoMetadata meta = App.GetVideoMetadata(App.CurrentVideo);

			if (meta.Width != 0) // no stream
			{
				textureWidth = meta.Width;
				textureHeight = meta.Height;
			}
			else
			{
				textureWidth = 3840;
				textureHeight = 2160;
			}
            base.Initialize();
            CreateEyes();
            OVR_Media_Surface_Init();

            RenderTo = ProjectionMesh;
			nativeTexture = Texture2D.blackTexture;
			IssuePluginEvent(MediaSurfaceEventType.Initialize);
        }

        internal override Status GetStatus()
        {
            if (mediaPlayer != null)
            {
                if (_buffering)
                {
                    return Status.VP_BUFFERING;
                }
                if (videoPaused)
                {
                    return Status.VP_PAUSED;
                }
                if (IsPlaying)
                {
                    return Status.VP_PLAYING;
                }
                return Status.VP_STOPPED;
            }
            return Status.VP_NOT_READY;
        }

		public override Texture GetVideoTexture()
		{
			return nativeTexture;
		}

		protected override void SetVolume(float vol)
        {
            base.SetVolume(vol);
            if (mediaPlayer != null)
            {
                
                _volume = vol;
                if (audioConf.Internal && !Mute)
                {
                    mediaPlayer.Call("setVolume", vol, vol);
                }
                else
                {
                    mediaPlayer.Call("setVolume", 0f, 0f);
                }
            }
        }

        protected override float GetVolume()
        {
            return _volume;
        }

        private int CalculateMinimalBandwith(string videoId)
        {
            App.VideoMetadata V = App.GetVideoMetadata(videoId);
            float size = (App.GetVideoSize(videoId) * 8f) / 1024f;
            float dur = V.Duration / 1000f;
            return Mathf.RoundToInt(((size * 1f) / (dur * 1f)) * 0.8f);
        }
            
		public override void Play(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback=null)
        {
			url = FullPath;
            fallbackUrl = FullPathFallback;
            if (FullPath.Contains("STREAMINGASSET:"))
            {
                FullPath = FullPath.Replace("STREAMINGASSET:", "");
                AndroidJavaObject StreamingAssetFileDescriptor = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                assetFileDescriptor = StreamingAssetFileDescriptor.Call<AndroidJavaObject>("getAssets").Call<AndroidJavaObject>("openFd", FullPath);
            }
            gameObject.SetActive(true);
            if (FullPathFallback != null) {
                minBandwidth = CalculateMinimalBandwith(App.CurrentVideo);
                Debug.Log("Min bandwidth calculated with magic: " + minBandwidth + " kbps");
            } else {
                minBandwidth = 0;
            }

            StartCoroutine(DelayedStartVideo(FullPath,onEnd));
            Volume = 0;
            base.Play(FullPath, Stream);
        }

        IEnumerator DelayedStartVideo(string FullPath, OnEnd onEnd)
        {

            _buffering = false;
            yield return new WaitForSeconds(0.5f);
            Volume = 0;
            yield return null;
            //if (!startedVideo)
            //{
            if (mediaPlayer == null)
            {
                mediaPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, FullPath);
            }

            yield return null;

            OnEnd setTextureInEyes = delegate (bool succes, string error)
            {
                RenderTo.material.mainTexture = nativeTexture;
                _buffering = false;
                mediaPlayer.Call("setLooping", _loop);
                if (StartSeek != -1)
                {
                    mediaPlayer.Call("seekTo", (int)StartSeek);
                }
                mediaPlayer.Call("start");

                Volume = 1;
                SetVolume(_volume);

            };
            OnEnd BufferToFalse = delegate (bool succes, string error)
            {
                _buffering = false;
            };
            OnEnd BufferToTrue = delegate (bool succes, string error)
            {

                _buffering = true;
            };
            onComplete _onDone = new onComplete(onEnd);
            onReady _onPrepaired = new onReady(setTextureInEyes);
            onSeekDone _onSeekDone = new onSeekDone(BufferToFalse);
			OnError _onError = new OnError(null);
			OnBufferingUpdate _onBufferUpdate = new OnBufferingUpdate();

            OnEnd receive = delegate (bool succes, string kbps)
            {
                if (url != fallbackUrl && fallbackUrl != null)
                {
                    int receivedKbps = int.Parse(kbps);
                    if (receivedKbps == 0)
                    {
                        Debug.Log("kbps == 0??");
                    }
                    else
                    {
                        int newUTC = GetUtcNow();
                        if (newUTC > LastUTC + 5)
                        {
                            Debug.Log("Very special numbers: " + receivedKbps + "/" + minBandwidth);
                            LastUTC = newUTC;
                            if (receivedKbps < minBandwidth)
                            {
                                Debug.Log("Starting Fallback");
                                url = fallbackUrl;
                                StartCoroutine(StartFallBack(fallbackUrl));
                            }
                        }
                    }
                }
            };
            onInfoListener _onInfoListener = new onInfoListener(receive, BufferToTrue, BufferToFalse);

            if (startedVideo)
            {
                Debug.Log("Video already started");
                mediaPlayer.Call("reset");
                yield return null;
                if (assetFileDescriptor != null)
                {
                    Debug.Log("Setting file descriptor");
                    mediaPlayer.Call("setDataSource",
                        assetFileDescriptor.Call<AndroidJavaObject>("getFileDescriptor"),
                        assetFileDescriptor.Call<long>("getStartOffset"),
                        assetFileDescriptor.Call<long>("getLength"));
                    assetFileDescriptor.Call("close");
                }
                else
                {
                    mediaPlayer.Call("setDataSource", FullPath);
                }
                _buffering = true;
                yield return new WaitForSeconds(0.5f);

            }


            if (mediaPlayer != null)
            {
                mediaPlayer.Call("setOnCompletionListener", _onDone);
                mediaPlayer.Call("setOnPreparedListener", _onPrepaired);
                mediaPlayer.Call("setOnSeekCompleteListener", _onSeekDone);
                mediaPlayer.Call("setOnInfoListener", _onInfoListener);
				mediaPlayer.Call("setOnErrorListener", _onError);
				mediaPlayer.Call("setOnBufferingUpdateListener", _onBufferUpdate);
				_buffering = true;
                mediaPlayer.Call("prepareAsync");

            }
            else
            {
                Debug.Log("no media player");
            }
            startedVideo = true;
            //}
        }

        IEnumerator StartFallBack(string newurl)
        {
            //long s = ;// Mathf.RoundToInt( _seek);
            mediaPlayer.Call("reset");
            yield return null;
            mediaPlayer.Call("setDataSource", newurl);
            yield return null;
            _buffering = true;
            StartSeek = GetSeekMs();
            mediaPlayer.Call("prepareAsync");
        }

        protected override bool GetBuffering()
        {
            return _buffering;
        }

        protected override float GetSeek()
        {
            return (GetSeekMs() * 1.0f) / (GetDuration() * 1.0f);
        }
        protected override void SetSeek(float seek)
        {
            SetSeekMs(Mathf.RoundToInt( (GetDuration() * 1.0f) * seek));
        }

        protected override long GetSeekMs()
        {
            if (mediaPlayer != null)
            {
                return (long)mediaPlayer.Call<int>("getCurrentPosition") + seekCorrection;
            }
            else
            {
                return -1;
            }
        }
        protected override void SetSeekMs(long seek)
        {
            base.SetSeekMs(seek);
            if (mediaPlayer != null)
            {
                _buffering = true;
                mediaPlayer.Call("seekTo", (int)seek);
            }
        }
		protected override bool GetLoop()
		{
			return _loop;
		}
		protected override void SetLoop(bool loop)
		{
			_loop = loop;
			if (mediaPlayer != null) {
				mediaPlayer.Call ("setLooping", _loop);
			}
		}
        protected override long GetDuration()
        {
            if (App.Data != null)
            {
                if (App.Data.Video.ContainsKey(App.CurrentVideo))
                {
                    return App.Data.Video[App.CurrentVideo].Duration;
                }
            }
            if (mediaPlayer != null)
            {
                return (long)mediaPlayer.Call<int>("getDuration");
            }
            else
            {
                return 1;
            }
        }
        protected override bool GetIsPlaying()
        {
            if (mediaPlayer != null&&startedVideo)
            {
                return !videoPaused;
            }
            else
            {
                return false;
            }
        }
        public override void SetAudioConfig(bool internalAudio, bool externalAudio)
        {
           
            if (internalAudio && !Mute||App.Player.ExternalAudio==null)
            {
                if (mediaPlayer != null)
                {
                    mediaPlayer.Call("setVolume", _volume, _volume);
                }
            }
            else
            {
                if (mediaPlayer != null)
                {
                    mediaPlayer.Call("setVolume", 0f, 0f);
                }
            }

            base.SetAudioConfig(internalAudio, externalAudio);
    }


        protected override void SetMute(bool mute)
        {
            base.SetMute(mute);
            if (mediaPlayer != null)
            {
                if (mute || !audioConf.Internal)
                {
                    mediaPlayer.Call("setVolume",0f,0f);
                }
                else
                {
                    mediaPlayer.Call("setVolume",_volume,_volume);
                }
            }
        }

        public override void Pause()
        {
            base.Pause();
            if (mediaPlayer != null)
            {
                videoPaused = true;
                try
                {
                    if (mediaPlayer != null)
                    {
                        mediaPlayer.Call("pause");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
                }
            }
        }

        public override void Resume()
        {
            base.Resume();
            if (mediaPlayer != null)
            {
                videoPaused = false;
                try
                {
                    if (mediaPlayer != null)
                    {
                        mediaPlayer.Call("start");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
                }
            }
        }

        public override void PauseResume()
        {
            base.PauseResume();
            if (videoPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public override void Clean()
        {
            base.Clean();
        }

        protected override void OnDestroy()
        {
            IssuePluginEvent(MediaSurfaceEventType.Shutdown);
            base.OnDestroy();
            mediaPlayer.Call("release");
           
            mediaPlayer = null;
        }

        public override void Stop()
        {
            base.Stop();
        }

        internal override void Update()
        {
            base.Update();
            if (mediaPlayer != null)
            {
                IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
                if (currTexId != nativeTexId)
                {
                    nativeTexId = currTexId;
                    nativeTexture.UpdateExternalTexture(currTexId);
                    Debug.Log("Native texture has changed!");
                }
                IssuePluginEvent(MediaSurfaceEventType.Update);

                if (_buffering && fallbackUrl != null && url != fallbackUrl)
                {
                    fallbackCountdown += Time.deltaTime;
                    if (fallbackCountdown > fallbackAfterSecs)
                    {
                        Debug.Log("Starting Fallback");
                        url = fallbackUrl;
                        StartCoroutine(StartFallBack(fallbackUrl));
                        fallbackCountdown = 0f;
                    }
                }
                else
                {
                    fallbackCountdown = 0f;
                }

                // pause video if user removes Pico headset
#if USE_PICO_SDK
                if (App.CurrentPlatform == App.VRPlatform.Pico) {
                    if (Pvr_UnitySDKAPI.Sensor.UPvr_GetPsensorState() > 0) {
                        if (_picoHeadsetOffTime >= 0f) {
                            _picoHeadsetOffTime += Time.deltaTime;
                            if (_picoHeadsetOffTime > 0.5f) {
                                _picoHeadsetOffTime = -1f;
                                _wasPlayingBeforePicoHeadsetOff = IsPlaying;
                                Pause();
                            }
                        }
                    } else {
                        if (_picoHeadsetOffTime < 0f && _wasPlayingBeforePicoHeadsetOff) {
                            Resume();
                            _wasPlayingBeforePicoHeadsetOff = false;
                        }
                        _picoHeadsetOffTime = 0f;
                    }
                }
#endif
#if USE_PICO_PLATFORM_SDK
                if (App.CurrentPlatform == App.VRPlatform.PicoPlatform) {
                    if (Unity.XR.PXR.PXR_Plugin.Sensor.UPxr_GetPSensorState() > 0) {
                        if (_picoHeadsetOffTime >= 0f) {
                            _picoHeadsetOffTime += Time.deltaTime;
                            if (_picoHeadsetOffTime > 0.5f) {
                                _picoHeadsetOffTime = -1f;
                                _wasPlayingBeforePicoHeadsetOff = IsPlaying;
                                Pause();
                            }
                        }
                    } else {
                        if (_picoHeadsetOffTime < 0f && _wasPlayingBeforePicoHeadsetOff) {
                            Resume();
                            _wasPlayingBeforePicoHeadsetOff = false;
                        }
                        _picoHeadsetOffTime = 0f;
                    }
                }
#endif
#if USE_WAVE_SDK
                if (App.CurrentPlatform == App.VRPlatform.ViveWave) {
                    bool isUserPresent = false;
                    if (!_wavePresenceTriggered &&
                        InputDevices.GetDeviceAtXRNode(XRNode.Head)
                            .TryGetFeatureValue(CommonUsages.userPresence, out isUserPresent) &&
                        !isUserPresent
                    ) {
                        _wavePresenceTriggered = true;
                        _waveWasPlaying = IsPlaying;
                        Pause();
                    } else if (_wavePresenceTriggered &&
                        InputDevices.GetDeviceAtXRNode(XRNode.Head)
                            .TryGetFeatureValue(CommonUsages.userPresence, out isUserPresent) &&
                        isUserPresent
                    ) {
                        _wavePresenceTriggered = false;
                        if (_waveWasPlaying) {
                            Resume();
                        }
                    }
                }
#endif
            }
        }

        AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath)
        {
            OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);
            IntPtr androidSurface = OVR_Media_Surface_GetObject();
            AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
            IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(),"setSurface","(Landroid/view/Surface;)V");
            jvalue[] parms = new jvalue[1];
            parms[0] = new jvalue();
            parms[0].l = androidSurface;
            AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parms);
            try
            {
                if (assetFileDescriptor != null)
                {
                    Debug.Log("Setting file descriptor");
                    mediaPlayer.Call("setDataSource", 
                        assetFileDescriptor.Call<AndroidJavaObject>("getFileDescriptor"), 
                        assetFileDescriptor.Call<long>("getStartOffset"),
                        assetFileDescriptor.Call<long>("getLength"));
                    assetFileDescriptor.Call("close");
                }
                else {
                    mediaPlayer.Call("setDataSource", mediaPath);
                }

            }
            catch (Exception e)
            {
                Debug.Log("Failed to start mediaPlayer with message " + e.Message);
            }
            _buffering = true;
            return mediaPlayer;
        }

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_Init();
        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_SetEventBase(int eventBase);
        [DllImport("OculusMediaSurface")]
        private static extern IntPtr OVR_Media_Surface_GetObject();
        [DllImport("OculusMediaSurface")]
        private static extern IntPtr OVR_Media_Surface_GetNativeTexture();
        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_SetTextureParms(int texWidth, int texHeight);

        internal override void SetFormatData(AppDataStruct.FormatBlock formatData) {
            if (formatData != null && formatData.Codec == "vp9") {
                seekCorrection = 217;
            } else {
                seekCorrection = 0;
            }

            Debug.Log("Seek correction (ms): " + seekCorrection);
        }

        private static int GetUtcNow()
        {
            return (int)(DateTime.UtcNow - EPOCH).TotalSeconds;
        }
    }
}
