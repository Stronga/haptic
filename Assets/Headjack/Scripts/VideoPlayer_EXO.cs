// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using Headjack.Utils;

namespace Headjack
{
	internal class VideoPlayer_EXO : VideoPlayerBase
	{
		public const string PACKAGE_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";

        private AndroidJavaObject context = null;
        private AndroidJavaObject applicationStateBridge = null;
        private Coroutine nativeTexCheck = null;
        private float _volume = 1f;
        private bool _loop = false;
        private bool _buffering = true;
        private string url;
        private MeshRenderer RenderTo;
        private bool stream = false;
        private long jsonDuration = -2;
        // holds previous playback paused status to restore status on app resume
        private bool _wasPlaying = false;
        private bool _wasPlayingBeforePaused = false;
        private float _picoHeadsetOffTime = 0f;
        private bool _wasPlayingBeforePicoHeadsetOff = false;
        private bool _wavePresenceTriggered = false;
        private bool _waveWasPlaying = false;

		private bool videoPaused = false;
		private bool startedVideo = false;
		private AndroidJavaObject exoPlayer = null;
		private AndroidJavaObject exoPlayerHelper = null;
        private AndroidJavaObject player = null;
        private IntPtr setSurfaceMethodId = IntPtr.Zero;
		private OnEnd onEndEvent;
		private ExoPlayerState state = ExoPlayerState.STATE_IDLE;
		private Texture2D nativeTexture = null;
		private IntPtr nativeTexId = IntPtr.Zero;
		private int textureWidth = 512;
		private int textureHeight = 512;
        // keep track of state of MediaSurface
        private bool isSurfaceInit = false;
        private bool isSurfaceSet = false;
        private bool isSurfacePaused = false;
        private bool triggerSurfaceDestroy = false;
        // keep reference to AudioListener
        private AudioListener currentAudioListener = null;
        // delay setting looping property in case player is still initializing
        private bool needLoopingSet = false;

		private static void IssuePluginEvent(MediaSurfaceEventType eventType)
		{
#pragma warning disable 618
			GL.IssuePluginEvent((int)eventType);
#pragma warning restore 618
		}

		public class EventListener : AndroidJavaProxy
		{
			private static int lastId = 0;
			private OnEnd _onEnd;
			private int id, TYPE_RENDERER, TYPE_SOURCE, TYPE_UNEXPECTED;
			public EventListener(OnEnd onend) : base("com.google.android.exoplayer2.Player$EventListener")
			{
				_onEnd = onend;
				Debug.Log("Listener created");
				id = ++lastId;
				AndroidJavaClass ExoPlaybackException = new AndroidJavaClass("com.google.android.exoplayer2.ExoPlaybackException");
				TYPE_RENDERER = ExoPlaybackException.GetStatic<int>("TYPE_RENDERER");
				TYPE_SOURCE = ExoPlaybackException.GetStatic<int>("TYPE_SOURCE");
				TYPE_UNEXPECTED = ExoPlaybackException.GetStatic<int>("TYPE_UNEXPECTED");
			}
			public override bool equals(AndroidJavaObject o)
			{
				return hashCode() == o.Call<int>("hashCode");
			}
			public override int hashCode()
			{
				return this.id;
			}
			void onLoadingChanged(bool isLoading)
			{
				return;
			}
			void onPlaybackParametersChanged(AndroidJavaObject playbackParameters)
			{
				return;
			}
			void onPlayerError(AndroidJavaObject error)
			{
				string errorMessage = "Unexpected Exception";
				try
				{
					int errorType = error.Get<int>("type");
					if (errorType == TYPE_RENDERER)
					{
						AndroidJavaObject Exception = error.Call<AndroidJavaObject>("getRendererException");
						errorMessage = Exception.Call<string>("getMessage");
					}
					if (errorType == TYPE_SOURCE)
					{
						AndroidJavaObject IOException = error.Call<AndroidJavaObject>("getSourceException");
						errorMessage = IOException.Call<string>("getMessage");
					}
					if (errorType == TYPE_UNEXPECTED)
					{
						AndroidJavaObject RuntimeException = error.Call<AndroidJavaObject>("getUnexpectedException");
						errorMessage = RuntimeException.Call<string>("getMessage");
					}
				}
				catch (Exception e)
				{
					errorMessage = e.Message;
				}
				Debug.LogError(errorMessage);
				if (_onEnd != null) _onEnd(false, errorMessage);
			}
			void onPlayerStateChanged(bool playWhenReady, int playbackState)
			{
				return;
			}
			void onPositionDiscontinuity(int reason)
			{
				return;
			}
			void onRepeatModeChanged(int repeatMode)
			{
				return;
			}
			void onSeekProcessed()
			{
				return;
			}
			void onShuffleModeEnabledChanged(bool shuffleModeEnabled)
			{
				return;
			}
			void onTimelineChanged(AndroidJavaObject timeline, AndroidJavaObject manifest, int reason)
			{
				return;
			}
			void onTracksChanged(AndroidJavaObject trackGroups, AndroidJavaObject trackSelections)
			{
				return;
			}
		}

        public delegate void ApplicationStateChanged();

        /**
         * Workaround for Oculus headsets killing XR display subsystem (which resets render thread on resume) 
         * before being able to properly dispose of native video texture. So hooking into native PrePaused event.
         */
        public class ApplicationStateCallback : AndroidJavaProxy {
            public ApplicationStateChanged createdCallback;
            public ApplicationStateChanged startedCallback;
            public ApplicationStateChanged resumedCallback;
            public ApplicationStateChanged pausedCallback;
            public ApplicationStateChanged stoppedCallback;
            public ApplicationStateChanged saveInstanceStateCallback;
            public ApplicationStateChanged destroyedCallback;

            public ApplicationStateCallback() : base("io.purplepill.exoplayerlib.ApplicationStateCallback") {}

            public void onActivityCreated(AndroidJavaObject activity, AndroidJavaObject savedInstanceState) {
                createdCallback?.Invoke();
            }
            public void onActivityStarted(AndroidJavaObject activity) {
                startedCallback?.Invoke();
            }
            public void onActivityResumed(AndroidJavaObject activity) {
                resumedCallback?.Invoke();
            }
            public void onActivityPaused(AndroidJavaObject activity) {
                pausedCallback?.Invoke();
            }
            public void onActivityStopped(AndroidJavaObject activity) {
                stoppedCallback?.Invoke();
            }
            public void onActivitySaveInstanceState(AndroidJavaObject activity, AndroidJavaObject outState) {
                saveInstanceStateCallback?.Invoke();
            }
            public void onActivityDestroyed(AndroidJavaObject activity) {
                destroyedCallback?.Invoke();
            }
        }
	
		private enum MediaSurfaceEventType
        {
            Initialize = 0,
            Shutdown = 1,
            Update = 2,
            Max_EventType
        };

        void DestroyNativeVideoTexture() {
            if (!isSurfacePaused && isSurfaceSet && player != null && setSurfaceMethodId != IntPtr.Zero) {
                Debug.Log("[VideoPlayer_EXO] Emptying ExoPlayer target Surface");
                jvalue[] parms = new jvalue[1];
                parms[0] = new jvalue();
                parms[0].l = IntPtr.Zero;
                AndroidJNI.CallVoidMethod(player.GetRawObject(), setSurfaceMethodId, parms);
            }
            if (!isSurfacePaused && isSurfaceInit) {
                Debug.Log("[VideoPlayer_EXO] Shutting down MediaSurface");
                IssuePluginEvent(MediaSurfaceEventType.Shutdown);
            }
            isSurfacePaused = true;
            Debug.Log("[VideoPlayer_EXO] DestroyNativeVideoTexture finished");
        }

        IEnumerator OnApplicationPause(bool paused) {
            if (!paused && isSurfacePaused) {
                if (isSurfaceInit) {
                    // re-init surface
                    yield return null; 
                    Debug.Log("[VideoPlayer_EXO] Re-init MediaSurface");
                    OVR_Media_Surface_Init();
                    IssuePluginEvent(MediaSurfaceEventType.Initialize);
                }
                if (isSurfaceSet && player != null && setSurfaceMethodId != IntPtr.Zero) {
                    // re-set surface
                    yield return null;
                    Debug.Log("[VideoPlayer_EXO] Re-set ExoPlayer target Surface");
                    OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);
                    IntPtr androidSurface = OVR_Media_Surface_GetObject();
                    jvalue[] parms = new jvalue[1];
                    parms[0] = new jvalue();
                    parms[0].l = androidSurface;
                    AndroidJNI.CallVoidMethod(player.GetRawObject(), setSurfaceMethodId, parms);
                }
                yield return null;
                isSurfacePaused = false;
            }

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

        // call destroy native video texture as quickly as possible to beat the destruction of the XR Display subsystem
        IEnumerator EndOfFrameChecks() {
            while (context != null) {
                yield return new WaitForEndOfFrame();
                if (triggerSurfaceDestroy) {
                    Debug.Log($"[VideoPlayer_EXO] Surface destroy triggered");
                    triggerSurfaceDestroy = false;
                    DestroyNativeVideoTexture();
                }
            }
        }

        void Start() {
            // because ApplicationStateCallback does not call on main thread, start a coroutine to monitor
            // the paused callback
            if (nativeTexCheck == null) {
                nativeTexCheck = StartCoroutine(EndOfFrameChecks());
            }
        }

        internal override void Initialize()
        {
            if (context == null) {
                AndroidJavaObject activity = GetActivity();
                context = GetApplicationContext(activity);

                // register callback with Android Activity lifecycle events
                ApplicationStateCallback stateCallback = new ApplicationStateCallback();
                stateCallback.pausedCallback += () => {triggerSurfaceDestroy = true;};
                applicationStateBridge = new AndroidJavaObject("io.purplepill.exoplayerlib.ApplicationStateBridge", 
                    activity, 
                    stateCallback);
            }

            App.VideoMetadata meta = App.GetVideoMetadata(App.CurrentVideo);

			if (meta != null && meta.Width != 0) // no stream
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

            nativeTexture = Texture2D.CreateExternalTexture(
                Texture2D.blackTexture.width, 
                Texture2D.blackTexture.height, 
                Texture2D.blackTexture.format, 
                Texture2D.blackTexture.mipmapCount > 1, 
                false, 
                Texture2D.blackTexture.GetNativeTexturePtr()
            );

            if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                Shader.EnableKeyword("GAMMA_TO_LINEAR");
            } else {
                Shader.DisableKeyword("GAMMA_TO_LINEAR");
            }

            RenderTo.material.mainTexture = nativeTexture;
            Debug.Log("Native texture set");

			IssuePluginEvent(MediaSurfaceEventType.Initialize);
            isSurfaceInit = true;
        }

        internal override Status GetStatus()
        {
            if (exoPlayer != null)
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
            _volume = vol;
            if (exoPlayer != null)
            {
				Debug.Log("internal audio enabled: " + audioConf.Internal);
				if (audioConf.Internal && !Mute)
                {
					Debug.Log("setting exoplayer volume to " + vol);
                    exoPlayer.Call("setVolume", vol);
                }
                else
                {
					Debug.Log("setting exoplayer volume to 0");
					exoPlayer.Call("setVolume", 0f);
                }
            }
        }

        protected override float GetVolume()
        {
            return _volume;
        }

        private void SetParams(string FullPath, bool Stream, OnEnd onEnd = null) {
            if (FullPath.Contains("STREAMINGASSET:"))
            {
                Debug.Log("Playing packaged video");

                if (Application.dataPath.ToLower().Contains("/obb/"))
                {
                    Debug.Log("OBB detected, streaming video from obb");
                    FullPath = FullPath.Replace("STREAMINGASSET:", "OBB:");
                } else {
                    Debug.Log("No obb detected, streaming video from StreamingAssets");
                    FullPath = FullPath.Replace("STREAMINGASSET:", "asset:///");
                }
                Stream = false;
            }
            url = FullPath;

            if (App.Data != null)
            {
                if (App.Data.Video.ContainsKey(App.CurrentVideo))
                {
                    jsonDuration = App.Data.Video[App.CurrentVideo].Duration;
                }
            }

            stream = Stream;
            gameObject.SetActive(true);
            onEndEvent = onEnd;
            _buffering = false;
        }

        public override void Prepare(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null, long VideoTime = 0)
        {
            Debug.Log("Preparing media using ExoPlayer!");

            // TODO: if this video (with these params) is already loaded, reuse and do not reload

            SetParams(FullPath, Stream, onEnd);

            StartCoroutine(DelayedStartVideo(url, stream, false, VideoTime));

            base.Prepare(FullPath, stream, onEnd, FullPathFallback, VideoTime);
        }

        public override void Play(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null)
        {
            Debug.Log("Playing using ExoPlayer!");
		
            // TODO: if this video (with these params) is already loaded, reuse and do not reload

            SetParams(FullPath, Stream, onEnd);

			StartCoroutine(DelayedStartVideo(url, stream));

            base.Play(FullPath, stream);
        }

        IEnumerator DelayedStartVideo(string FullPath, bool Stream, bool PlayWhenReady = true, long VideoTime = 0)
        {
            // while loading video, mute volume in case of audio artifacts during buffering
            float previousVolume = Volume;
            Volume = 0f;

            Pause();

            yield return new WaitForSecondsRealtime(0.05f);

			if (exoPlayer == null)
			{
				exoPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, FullPath, Stream);
			}

            yield return null;

            Volume = 0f;

            // seek to video start time
            if (VideoTime > 0) {
                SeekMs = VideoTime;
            }

            Resume();

            startedVideo = true;

            if (!PlayWhenReady) {
                while (SeekMs <= VideoTime) {
                    yield return null;
                }
                Pause();
            }

            Volume = previousVolume;
        }

        public override void SeekTo(long seekTime, long videoTime)
        {
            base.SeekTo(seekTime, videoTime);
            StartCoroutine(SyncedSeek(seekTime, videoTime));
        }

        // Waits until seekTime (realtime timestamp) to start seeking to videoTime.
        // Equivalent of Prepare if media is already loaded (ends in paused state on correct timecode)
        private IEnumerator SyncedSeek(long seekTime, long videoTime) 
        {
            // we cannot seek to an invalid video time
            if (videoTime < 0) {
                yield break;
            }

            // wait until seek timestamp to start seeking
            long timeDiff = seekTime - TimeStamp.GetMsLong();
            if (timeDiff > SYNCED_DELAY_THRESHOLD_MS) {
                yield return new WaitForSecondsRealtime(0.001f * timeDiff);
            }

            Pause();
            long seekDiff = videoTime - SeekMs;
            if (seekDiff > MAX_SYNC_DIFF_MS || seekDiff < -MAX_DELAYED_SYNC_MS) {
                SeekMs = videoTime;
            }
        }

        // Seeks far enough ahead of video timecode to take into account if the video
        // is already playing from the syncing context
        // returns boolean on whether seeking operation was started        
        private bool CorrectedSync(long playTime, long videoTime) 
        {
            // negative videoTime means seeking to the current timestamp, so we have to do nothing
            if (videoTime < 0) {
                return false;
            }

            // how long has the video been playing (if it was synced properly)
            long timeRunning = TimeStamp.GetMsLong() - playTime;
            // the current video timecode target (taking into account play time)
            long nowVideoSynced = videoTime + timeRunning;
            // Seek target is some time ahead of current synced videotime to allow for time taken by seeking itself
            long seekTarget = nowVideoSynced + MAX_SYNC_DIFF_MS;
            // What is the difference between are target seek time and the current seek time
            long seekDiff = seekTarget - SeekMs;
            // If we are within a threshold value, then we do not seek but simply wait for a little while before starting the video
            if (seekDiff >= 0 && seekDiff < MAX_SYNC_DIFF_MS) {
                Debug.Log($"[VideoPlayer_EXO] No need for seek correction, target ({seekTarget}ms) is within {MAX_SYNC_DIFF_MS}ms of current seek time ({SeekMs}ms)");
                return false;
            }

            // seek to target
            SeekMs = seekTarget;
            return true;
        }

        public override void PlayAt(long playTime, long videoTime = -1)
        {
            // Debug.Log($"[VideoPlayer_EXO DEBUG] PlayAt called: playTime {playTime} - currentTime {TimeStamp.GetMsLong()} ({playTime - TimeStamp.GetMsLong()}) videoTime {videoTime} - currentVideoTime {SeekMs} ({videoTime - SeekMs})");

            base.PlayAt(playTime, videoTime);

            Status currentStatus = GetStatus();
            if ((int)currentStatus < (int)Status.VP_READY) {
                Debug.LogError("[VideoPlayer_EXO] Attempting to resume playback on the video player without a video loaded");
                onEndEvent?.Invoke(false, "Attempting to resume playback on the video player without a video loaded");
                onEndEvent = null;
                return;
            }
            
            Pause();

            StartCoroutine(PlaySynced(playTime, videoTime));
        }

        private IEnumerator PlaySynced(long playTime, long videoTime)
        {
            // correct for systematic bias in ExoPlayer play time
            playTime += 30;

            long availableTime = playTime - TimeStamp.GetMsLong();
            long seekDiff = 0;
            if (videoTime >= 0) {
                // only correct for video timecode if videoTime is valid timecode
                seekDiff = videoTime - SeekMs;
            }
 
            long playDelay = availableTime - seekDiff;

            // Debug.Log($"[VideoPlayer_EXO DEBUG] availableTime {availableTime} - seekDiff {seekDiff} (playDelay {playDelay})");

            // seek if it is impossible to start playback in sync
            if (seekDiff > availableTime || seekDiff < -MAX_DELAYED_SYNC_MS) {
                Debug.Log($"[VideoPlayer_EXO] Seeking to ensure synced playback.");

                bool startedSeek = CorrectedSync(playTime, videoTime);
                if (startedSeek) {
                    
                    // Debug.Log($"[VideoPlayer_EXO DEBUG] SeekMs before correctedsync resume: {SeekMs}");

                    // Start playback so it can be paused on first new frame
                    Resume();

                    // wait until corrected seek is finished
                    yield return null;

                    float timeout = MAX_SYNC_DIFF_MS * 0.001f;
                    while (GetStatus() != Status.VP_PLAYING && timeout > 0) {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }
                    long currentSeek = SeekMs;

                    // Debug.Log($"[VideoPlayer_EXO DEBUG] SeekMs after seeking: {SeekMs} (currentSeek: {currentSeek})");

                    while (System.Math.Abs(SeekMs - currentSeek) < 16 && timeout > 0) {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }
                    
                    Pause();

                    // Debug.Log($"[VideoPlayer_EXO DEBUG] SeekMs after pause: {SeekMs}");

                    if (timeout <= 0) {
                        Debug.LogWarning("[VideoPlayer_EXO] Seeking took longer than the sync logic allowed");
                    }
                }

                availableTime = playTime - TimeStamp.GetMsLong();
                if (videoTime >= 0) {
                    // only correct for video timecode if videoTime is valid timecode
                    seekDiff = videoTime - SeekMs;
                } else {
                    seekDiff = 0;
                }
                playDelay = availableTime - seekDiff;

                Debug.Log($"[VideoPlayer_EXO] result of seek: playTime {playTime} timeStamp {TimeStamp.GetMsLong()} videoTime {videoTime} SeekMs {SeekMs} availableTime {availableTime} seekDiff {seekDiff} playDelay {playDelay}");

                if (playDelay < 0) {
                    Debug.LogWarning("[VideoPlayer_EXO] Seeking took too long and current video timestamp is already out-of-date");
                }
            }

            // Debug.Log($"[VideoPlayer_EXO DEBUG] Waiting playDelay {playDelay}");

            bool flipFlop = false;
            while (playDelay >= SYNCED_DELAY_THRESHOLD_MS) {
                flipFlop = !flipFlop;
                if (flipFlop) {
                    yield return new WaitForEndOfFrame();
                } else {
                    yield return null;
                }
                availableTime = playTime - TimeStamp.GetMsLong();
                if (videoTime >= 0) {
                    // only correct for video timecode if videoTime is valid timecode
                    seekDiff = videoTime - SeekMs;
                } else {
                    seekDiff = 0;
                }
                playDelay = availableTime - seekDiff;

                // Debug.Log($"[VideoPlayer_EXO DEBUG] new playDelay {playDelay}, realtime: {TimeStamp.GetMsLong()}");
            }

            Debug.Log($"[VideoPlayer_EXO] Playing at {TimeStamp.GetMsLong()}, target {playTime} ({playTime - TimeStamp.GetMsLong()}. At video time {SeekMs}, target {videoTime} ({videoTime - SeekMs})\nState before playing: {CurrentStatus}");

            Resume();
            yield break;
        }

        public override void PauseAt(long pauseTime) 
        {
            // Debug.Log($"[VideoPlayer_EXO DEBUG] PauseAt called: pauseTime {pauseTime}, currentTime {TimeStamp.GetMsLong()} ({pauseTime - TimeStamp.GetMsLong()})");

            base.PauseAt(pauseTime);

            if (GetStatus() != Status.VP_PLAYING) {
                Debug.LogWarning($"[VideoPlayer_EXO] Attempting to pause a video player that is not playing");
            }

            StartCoroutine(PauseSynced(pauseTime));
        }

        private IEnumerator PauseSynced(long pauseTime) {
            long pauseDelay = pauseTime - TimeStamp.GetMsLong();

            if (pauseDelay < 0) {
                Debug.LogWarning($"[VideoPlayer_EXO] Cannot pause in sync as pause timestamp is in the past");
            }

            pauseDelay = Math.Max(0L, pauseDelay);
            // only wait to pause if delay exceeds roughly one frame time
            if (pauseDelay > SYNCED_DELAY_THRESHOLD_MS) {
                yield return new WaitForSecondsRealtime(0.001f * pauseDelay);
            }

            // Debug.Log($"[VideoPlayer_EXO DEBUG] Pausing at {TimeStamp.GetMsLong()} after {pauseDelay} delay, target {pauseTime} ({pauseTime - TimeStamp.GetMsLong()}). Current videoTime {SeekMs}");

            Pause();
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
            SetSeekMs(Mathf.RoundToInt((GetDuration() * 1.0f) * seek));
        }

        protected override long GetSeekMs()
        {
            if (exoPlayer != null)
            {
                return exoPlayer.Call<long>("getCurrentPosition");
            }
            else
            {
                return -1;
            }
        }
        protected override void SetSeekMs(long seek)
        {
            base.SetSeekMs(seek);
            if (exoPlayer != null)
            {
                _buffering = true;
                exoPlayer.Call("seekTo", seek);
            }
        }
        protected override bool GetLoop()
        {
            return _loop;
        }
        protected override void SetLoop(bool loop)
        {
            _loop = loop;
            if (exoPlayer != null)
            {
                ExoPlayerRepeatMode repeatMode = ExoPlayerRepeatMode.REPEAT_MODE_OFF;
                if (loop) {
                    repeatMode = ExoPlayerRepeatMode.REPEAT_MODE_ONE;
                }
                exoPlayer.Call("setRepeatMode", (int)repeatMode);
            } else {
                needLoopingSet = true;
            }
        }
        protected override long GetDuration()
        {
            if (jsonDuration < -1)
            {
                if (App.Data != null)
                {
                    if (App.Data.Video.ContainsKey(App.CurrentVideo))
                    {
                        jsonDuration = App.Data.Video[App.CurrentVideo].Duration;
                    }
                }
            }

            if (jsonDuration >= 0 && exoPlayer != null)
			{
				long duration= exoPlayer.Call<long>("getDuration");
                if (duration > 0)
                {
                    return duration;
                }
			}
            
            return Math.Max(jsonDuration, -1);
        }
        protected override bool GetIsPlaying()
        {
            if (exoPlayer != null && startedVideo)
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
			if (internalAudio && !Mute || !externalAudio)
			{
				if (exoPlayer != null)
				{
					Debug.Log("set internal audio to " + _volume);
					exoPlayer.Call("setVolume", _volume);
				}
			}
			else
			{
				if (exoPlayer != null)
				{
					exoPlayer.Call("setVolume", 0f);
					Debug.Log("set internal audio to " + 0);
				}
			}

            base.SetAudioConfig(internalAudio, externalAudio);
        }


        protected override void SetMute(bool mute)
        {
            base.SetMute(mute);
            if (exoPlayer != null)
            {
				Debug.Log("setting mute " + mute);
                if (mute || !audioConf.Internal)
                {
                    exoPlayer.Call("setVolume", 0f);
                }
                else
                {
                    exoPlayer.Call("setVolume", _volume);
                }
            }
        }

        public override void Pause()
        {
            base.Pause();
            if (exoPlayer != null)
            {
                videoPaused = true;
                try
                {
                    if (exoPlayer != null)
                    {
						Debug.Log("Set Play When Ready to false");
                        exoPlayer.Call("setPlayWhenReady", false);
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
            if (exoPlayer != null)
            {
                videoPaused = false;
                try
                {
					Debug.Log("Set Play When Ready to true");
					exoPlayer.Call("setPlayWhenReady", true);
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
            Pause();
            base.Clean();
        }

        protected override void OnDestroy()
        {
            IssuePluginEvent(MediaSurfaceEventType.Shutdown);

            player.Dispose();
            player = null;

            setSurfaceMethodId = IntPtr.Zero;
            isSurfaceSet = false;
            isSurfaceInit = false;
            isSurfacePaused = false;

            if (applicationStateBridge != null) {
                applicationStateBridge.Call("destroy");
                applicationStateBridge.Dispose();
                applicationStateBridge = null;
            }

            base.OnDestroy();

            exoPlayerHelper.Call("destroyInstance");
            exoPlayerHelper.Dispose();
            exoPlayerHelper = null;

            exoPlayer.Dispose();
            exoPlayer = null;

            context.Dispose();
            context = null;

            if (nativeTexCheck != null) {
                StopCoroutine(nativeTexCheck);
                nativeTexCheck = null;
            }
        }

        public override void Stop()
        {
            base.Stop();
        }

        internal override void Update()
        {
            base.Update();
			if (exoPlayer != null)
			{
                //state checking
				state = (ExoPlayerState)exoPlayer.Call<int>("getPlaybackState");
                _buffering = (state == ExoPlayerState.STATE_BUFFERING);

                if (isSurfaceInit && isSurfaceSet && !isSurfacePaused) {
                    IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
                    if (currTexId != nativeTexId)
                    {
                        nativeTexId = currTexId;
                        nativeTexture.UpdateExternalTexture(currTexId);
                        Debug.Log("Native texture has changed!");
                    }
                    IssuePluginEvent(MediaSurfaceEventType.Update);
                }

				if (onEndEvent != null)
				{
					if (state == ExoPlayerState.STATE_ENDED
                        || (Duration > 1 && SeekMs > Duration + 5))
					{

                        if (!_loop) {
                            Debug.Log("End of video");
                            onEndEvent.Invoke(true,null);
                            onEndEvent = null;
                        } else if (stream) {
                            // looping streaming video
                            SeekMs = 0;
                        }

					}
				}

                if (needLoopingSet) {
                    SetLoop(_loop);
                    needLoopingSet = false;
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

			if (exoPlayerHelper != null)
			{
                if (currentAudioListener == null) {
                    currentAudioListener = FindFirstAudioListener();
                }
                if (currentAudioListener != null) {
                    Quaternion camRot = currentAudioListener.transform.rotation;
				    exoPlayerHelper.Call("setSpatialOrientation", camRot.x, camRot.y, camRot.z, camRot.w);   
                }
			}
        }

		public enum ExoPlayerState
		{
			STATE_IDLE = 1,
			STATE_BUFFERING = 2,
			STATE_READY = 3,
			STATE_ENDED = 4
		}

        public enum ExoPlayerRepeatMode
        {
            REPEAT_MODE_OFF = 0,
            REPEAT_MODE_ONE = 1,
            REPEAT_MODE_ALL = 2
        }

        AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath, bool streaming)
        {
			EventListener listener = new EventListener(onEndEvent);
            OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);
            IntPtr androidSurface = OVR_Media_Surface_GetObject();
            exoPlayerHelper = new AndroidJavaObject("io/purplepill/exoplayerlib/ExoPlayerHelper");
            if (exoPlayerHelper == null) {
                throw new Exception("Failed to create ExoPlayerHelper instance");
            }
			if (App.CurrentVideo != null)
			{
                AppDataStruct.AudioBlock a = App.Data.Video[App.CurrentVideo].Audio;
                if (ExternalAudio)
                {
                    Debug.Log("TBE audio enabled, force exoplayer to handle audio");
                    a.Channels = 2;
                    a.Mapping = "STEREO";
                }
                player = exoPlayerHelper.Call<AndroidJavaObject>("createInstance", context, listener, a.Channels, a.SampleRate, a.Mapping);
            } else
            {
                player = exoPlayerHelper.Call<AndroidJavaObject>("createInstance",context, listener);
            }
			
            if (player == null) {
                throw new Exception("Failed to create ExoPlayer instance");
            }
			Debug.Log("Exoplayer instance created: " + player.ToString());
            setSurfaceMethodId = AndroidJNI.GetMethodID(player.GetRawClass(), "setVideoSurface", "(Landroid/view/Surface;)V");
            jvalue[] parms = new jvalue[1];
            parms[0] = new jvalue();
            parms[0].l = androidSurface;
            AndroidJNI.CallVoidMethod(player.GetRawObject(), setSurfaceMethodId, parms);
            isSurfaceSet = true;

            try
            {
				if (mediaPath.StartsWith("OBB"))
				{
					Debug.Log("Prepare OBB Stream");
					mediaPath = "assets/"+mediaPath.Replace("OBB:", "");
					string obbpath = Application.dataPath;
					Debug.Log("obb path: " + obbpath);
					Debug.Log("media path: " + mediaPath);
					string[] obbSplitted = obbpath.Split(new string[] { "/main." }, StringSplitOptions.None);
					int obbVersion = int.Parse(obbSplitted[obbSplitted.Length - 1].Split( "."[0])[0]);
					Debug.Log("Bundle version code: " + obbVersion);
					exoPlayerHelper.Call("prepareOBB", obbpath, mediaPath, obbVersion, context);
				}
				else
				{
					if (streaming)
					{
						if (mediaPath.EndsWith(".mpd"))
						{
							Debug.Log("Prepare Dash Stream");
							exoPlayerHelper.Call("prepareDashStream", mediaPath);
						}
						else
						{
							Debug.Log("Prepare HLS Stream");
							exoPlayerHelper.Call("prepareHlsStream", mediaPath);
						}
					}
					else
					{
						Debug.Log("playing local media file: " + mediaPath);

						exoPlayerHelper.Call("prepareLocalMedia", mediaPath);
					}
				}
            }
            catch (Exception e)
            {
                Debug.Log("Failed to start mediaPlayer with message " + e.Message);
            }
            _buffering = true;
            return player;
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

        internal override void SetFormatData(AppDataStruct.FormatBlock formatData)
        {

        }

        /// Returns the Android Activity used by the Unity device player. The caller is
        /// responsible for memory-managing the returned AndroidJavaObject.
        public static AndroidJavaObject GetActivity() {
            AndroidJavaClass jc = new AndroidJavaClass(PACKAGE_UNITY_PLAYER);
            if (jc == null) {
            Debug.LogErrorFormat("Failed to get class {0}", PACKAGE_UNITY_PLAYER);
            return null;
            }
            AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null) {
            Debug.LogError("Failed to obtain current Android activity.");
            return null;
            }
            return activity;
        }

        /// Returns the application context of the current Android Activity.
        public static AndroidJavaObject GetApplicationContext(AndroidJavaObject activity) {
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            if (context == null) {
            Debug.LogError("Failed to get application context from Activity.");
            return null;
            }
            return context;
        }

        private static AudioListener FindFirstAudioListener()
        {
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners == null || listeners.Length < 1)
            {
                return null;
            }

            return listeners[0];
        }
    }
}
