// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System;

namespace Headjack
{
    public abstract class VideoPlayerBase : MonoBehaviour
    {
        #region PRIVATE
        void LateUpdate()
        {
            if (currentProjection == "EQUIRECT") {
                ProjectionMesh.transform.position = App.camera.position;
            }
        }

        private int AA;
        private bool VideoIsStereoscopic, lastOutputStereo, HMDLost;
        private string currentProjection;
        private long lastSeekMs = 0;
        private bool ExternalAudioFirstSync = false;
        private bool startedBuffering = false;
        // private CameraClearFlags cameraClearCache = CameraClearFlags.SolidColor;
        // save current playing state to restore after resuming from pause
        private bool wasPlaying = false;

        #endregion
        #region PUBLIC
        /**
        <summary>
        Description of which audio tracks are playing, audio track in video file (internal) and/or external spatial audio file.
        Get the current audio configuration using <see cref="GetAudioConfig"/>, set the audio configuration using <see cref="SetAudioConfig(bool, bool)"/>.
        </summary>
        */
        public struct AudioConfig
        {
            /**
            <summary>
            Describes whether the audiotrack embedded in the video file is enabled
            </summary>
            */
            public bool Internal;
            /**
            <summary>
            Describes whether the audiotrack embedded in the video file is enabled
            </summary>
            */
            public bool External;
        }
        /**
		<summary>
		Status codes of videoplayer returned by GetStatus()
		</summary>
		*/
        public enum Status
        {
            VP_ERROR = 0,
            VP_NOT_READY,
            VP_STOPPED,
            VP_END,
            VP_READY,
            VP_BUFFERING,
            VP_PLAYING,
            VP_PAUSED
        }
        /**
        <summary>
        The mesh that displays the video. Mesh type depends on video projection
        </summary>
        */
        public MeshRenderer ProjectionMesh;
        /**
        <summary>
        True if the videoplayer is buffering
        </summary>
        <returns>If the videoplayer is buffering</returns>
        <example>
        <code>
        //Show buffering icon when buffering
        void Update()
        {
        if(App.Player.Buffering)
        {
	        BufferingObject.SetActive(true);
        }else{
	        BufferingObject.SetActive(false);
        }
        }   
        </code>
        </example>
        */
        public bool Buffering
        {
            get { return GetBuffering(); }
        }
        /**
        <summary>
        Videoplayer volume
        </summary>
        <returns>The current volume of the videoplayer</returns>
        <example>
        <code>
        //Change volume with Arrow Keys
        void Update()
        {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            App.Player.Volume += 0.1f;
            }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            App.Player.Volume -= 0.1f;
        }
        }   
        </code>
        </example>
        */
        public float Volume
        {
            get { return GetVolume(); }
            set { SetVolume(value); }
        }
        /**
        <summary>
        The video position (0.0 - 1.0)
        </summary>
        <returns>The current position of the video from 0.0 to 1.0</returns>
        <example>
        <code>
        //Change position with Arrow Keys
        void Update()
        {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            App.Player.Seek += 0.05f;
            }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            App.Player.Seek -= 0.05f;
        }
        }   
        </code>
        </example>
        */
        public float Seek
        {
            get { return GetSeek(); }
            set { SetSeek(value); }
        }
        /**
        <summary>
        The video position in milliseconds
        </summary>
        <returns>The current position of the video in milliseconds</returns>
        <example>
        <code>
        //Change position by 5 seconds with Arrow Keys
        void Update()
        {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            App.Player.SeekMs += 5000;
            }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            App.Player.SeekMs -= 5000;
        }
        }   
        </code>
        </example>
        */
        public long SeekMs
        {
            get { return GetSeekMs(); }
            set { SetSeekMs(value); }
        }
        /**
        <summary>
        If this is a looping video
        </summary>
        <returns>If the video is currently set to loop</returns>
        <example>
        <code>
        public void LoopButtonSwitch()
        {
        App.Player.Loop=!App.Player.Loop;
        }
        </code>
        </example>
        */
        public bool Loop
        {
            get { return GetLoop(); }
            set { SetLoop(value); }
        }
        /**
        <summary>
        If this video is muted
        </summary>
        <returns>If the video is currently muted</returns>
        <example>
        <code>
        public void MuteButtonSwitch()
        {
        App.Player.Mute=!App.Player.Mute;
        }
        </code>
        </example>
        */
        public bool Mute
        {
            get { return mute_; }
            set { SetMute(value); }
        }

		public abstract Texture GetVideoTexture();

		/**
        <summary>
        The total duration of the video
        </summary>
        <returns>The total duration of the video in milliseconds</returns>
        <example>
        <code>
        public float VideoLengthInSeconds()
        {
            return App.Player.Duration*0.001f;
        }
        </code>
        </example>
        */
		public long Duration
        {
            get { return GetDuration(); }
        }
        /**
        <summary>
        If the video is being played right now
        </summary>
        <returns>True if the video is being played right now</returns>
        <remarks>
        &gt; [!NOTE] 
        &gt; False on pause
        </remarks>
        <example>
        <code>
        void Update()
        {
        //Don't show the crosshair if the video is playing
        if(App.Player.IsPlaying)
        {
            App.ShowCrosshair=false;
        }
        }
        </code>
        </example>
        */
        public bool IsPlaying
        {
            get { return GetIsPlaying(); }
        }
        
        /// <summary> 
        /// The video timestamp of the latest command/function (e.g PauseAt or PlayAt)
        /// </summary>
        public long LastVideoTime { get; protected set; }

        /// <summary> 
        /// The realtime local timestamp of the latest command/function (e.g PauseAt or PlayAt)
        /// </summary>
        public long LastTimestamp { get; protected set; }

        /**
        <summary>
        Whether the current video is streaming or not
        </summary>
        <returns>True if the video is being streamed right now</returns>
        */
        public bool IsStream
        {
            get { return isStream; }
        }

        /**
        <summary>
        The current playback status
        </summary>
        <returns>True if the video is being streamed right now</returns>
        */
        public Status CurrentStatus
        {
            get { return GetStatus(); }
        }

        /**
		<summary>
		Sets audio configuration, whether to play audio of video file and whether to play external (spatial) audio file
		</summary>
		<param name="internalAudio">set to true to enable video audio track</param>
		<param name="externalAudio">set to true to enable external (spatial) audio track</param>
        <remarks>
        &gt; [!NOTE] 
        &gt; Enabling both internal and external audio causes both tracks to play simultaneously

        &gt; [!NOTE] 
        &gt; Enabling the external audio track without an external audio file being present in the project has no effect
        </remarks>
		*/
        public virtual void SetAudioConfig(bool internalAudio, bool externalAudio)
        {
            audioConf.Internal = internalAudio;
            if (ExternalAudio != null)
            {
                ExternalAudio.Volume = (!externalAudio || Mute) ? 0f : Volume;
            }
            audioConf.External = externalAudio;
        }
        /**
        <summary>
        Gets the audio playing configuration, whether playing audio of video file (internal) and/or playing external (spatial) audio file (external)
        </summary>
        <returns><see cref="AudioConfig"/> struct describing current audio configuration</returns>
        <remarks>
        &gt; [!NOTE] 
        &gt; Use <see cref="SetAudioConfig(bool, bool)"/> to change the audio configuration
        </remarks>
        */
        public AudioConfig GetAudioConfig()
        {
            return audioConf;
        }
        
        public virtual void Prepare(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null, long VideoTime = 0)
        {
            // turn on never screen sleep
            Startup.NeverSleepFlag |= (uint)2;
            LastVideoTime = VideoTime;
        }

        public virtual void SeekTo(long seekTime, long videoTime)
        {
            LastTimestamp = seekTime;
            LastVideoTime = videoTime;
        }

        /// <summary>
        /// Play a new video
        /// </summary>
        /// <param name="FullPath">Full path to the mp4 file</param>
        /// <param name="Stream">Whether to play local video or stream URL</param>
        /// <param name="onEnd">Will be called when the video is finished</param>
        /// <param name="FullPathFallback">deprecated fallback video path, now unused</param>
        /// <remarks>
        /// &gt; [!NOTE] 
        /// &gt; onEnd overwrites current onEnd
        /// 
        /// &gt; [!WARNING] 
        /// &gt; You need a full path to the mp4 file. To play videos from the server use <see cref="App.Play(string, bool, bool, OnEnd)"/>
        /// </remarks>
        public virtual void Play(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null)
        {
            // turn on never screen sleep
            Startup.NeverSleepFlag |= (uint)2;
        }

        /// <summary>
        /// Start playing the currently loaded video from a specific time, starting on a specific timestamp
        /// </summary>
        /// <param name="playTime">Realtime (local) timestamp to start playing</param>
        /// <param name="videoTime">Video timecode to start playing from</param>
        public virtual void PlayAt(long playTime, long videoTime = -1)
        {
            LastTimestamp = playTime;

            if (videoTime >= 0) {
                LastVideoTime = videoTime;
            } else {
                LastVideoTime = SeekMs;
            }
        }

        /// <summary>
        /// Pause the video at a specific timestamp
        /// NOTE: video is paused on a timecode derived from the realtime timestamp
        /// </summary>
        /// <param name="pauseTime">Realtime (local) timestamp to pause playback</param>
        public virtual void PauseAt(long pauseTime) 
        {
            LastVideoTime = pauseTime - LastTimestamp + LastVideoTime;
            LastTimestamp = pauseTime;
        }

        /**
        <summary>
        Pause the video
        </summary>
        <remarks>
        &gt; [!NOTE] 
        &gt; Will set VideoPlayer.IsPlaying to false
        </remarks>
        <example>
        <code>
        void Update()
        {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            App.Player.Pause();
        }
        }
        </code>
        </example>
        */
        public virtual void Pause()
        {
            if (ExternalAudio != null) ExternalAudio.Pause();
        }
        /**
        <summary>
        Resume the video
        </summary>
        <remarks>
        &gt; [!NOTE] 
        &gt; Will set VideoPlayer.IsPlaying to true
        </remarks>
        <example>
        <code>
        void Update()
        {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            App.Player.Resume();
        }
        }
        </code>
        </example>
        */
        public virtual void Resume()
        {
            if (ExternalAudio != null && ExternalAudioFirstSync) {
                ExternalAudio.Play();
            }
        }
        /**
        <summary>
        Switch between playing and paused
        </summary>
        <example>
        <code>
        void Update()
        {
        //Pause switch
        if (Input.GetKeyDown(KeyCode.Space))
        {
            App.Player.PauseResume();
        }
        }
        </code>
        </example>
        */
        public virtual void PauseResume()
        {
            //if (ExternalAudio != null)
            //{
            //    if (IsPlaying)
            //    {
            //        ExternalAudio.Pause();
            //    }
            //    else
            //    {
            //        Debug.Log("TBE pauseresume sync");
            //        ExternalAudio.Play();
            //        ExternalAudio.SetSeekMs(SeekMs);
            //    }
            //}
        }
        /**
        <summary>
        Clean up the video
        </summary>
        <example>
        <code>
        public void Release()
        {
			App.Player.Clean();
        }
        </code>
        </example>
        */
        public virtual void Clean()
        {
            // turn off never screen sleep
            Startup.NeverSleepFlag &= ~(uint)2;

			if (ProjectionMesh != null)
			{
				Debug.Log("clearing videoplayer");
				ProjectionMesh.material = null;
			}
            if (ExternalAudio != null)
            {
                ExternalAudio.Stop();
                ExternalAudioFirstSync = false;
                ExternalAudio.Close();
            }
        }
        /**
        <summary>
        Stop the video
        </summary>
        <example>
        <code>
        public void StopVideo()
        {
        App.Player.Stop();
        }
        </code>
        </example>
        */
        public virtual void Stop()
        {
            if (ExternalAudio != null)
            {
                ExternalAudio.Stop();
                ExternalAudioFirstSync = false;
            }
        }

		public enum Mapping
		{
			MONO,
			TB,
			LR
		}
		public Mapping mapping;

        /// <summary>
        /// Stereoscopic settings
        /// </summary>
        /// <param name="InputIsStereo">Is the video stereoscopic</param>
        /// <param name="OutputIsStereo">Display in stereoscopic</param>
        /// <param name="Projection">projection keyword (e.g. EQUIRECT or FLAT)</param>
        /// <remarks>
        /// &gt; [!WARNING] 
        /// &gt; This will be automatically set with <see cref="App.Play(string, bool, bool, OnEnd)"/>. Use <see cref="VideoPlayerBase.SetStereoscopicRendering(bool)"/> to switch enable/disable the stereoscopic rendering
        /// </remarks>
        public void SetEyes(bool InputIsStereo, bool OutputIsStereo, string Projection = "EQUIRECT")
        {
            VideoIsStereoscopic = InputIsStereo;
            currentProjection = Projection;
            EyeScale(OutputIsStereo);
            if (ProjectionMesh == null)
            {
                CreateEyes();
            }

            switch (mapping) {
                case Mapping.TB:
                    ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                        new Vector4(1f, 0.5f, 0f, 0.5f),
                        new Vector4(1f, 0.5f, 0f, 0f)
                    });
                    break;
                case Mapping.LR:
                    ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                        new Vector4(0.5f, 1f, 0f, 0f),
                        new Vector4(0.5f, 1f, 0.5f, 0f)
                    });
                    break;
                case Mapping.MONO:
                    ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                        new Vector4(1f, 1f, 0f, 0f),
                        new Vector4(1f, 1f, 0f, 0f)
                    });
                    break;
            }
        }

        /**
        <summary>
        Stereoscopic video rendering
        </summary>
        <param name="Stereoscopic">Render stereoscopic</param>
        <remarks>
        &gt; [!NOTE] 
        &gt; You can use this to disable the stereo effect if you want to display an interface
        </remarks>
        <example>
        <code>
        public void SwitchToMono()
        {
            App.Player.SetStereoscopicRendering(false);
        }
        </code>
        </example>
        */
        public void SetStereoscopicRendering(bool Stereoscopic)
        {
            if (App.CurrentPlatform == App.VRPlatform.Cardboard && !App.IsVRMode)
            {
                Stereoscopic = false;
            }
            if (VideoIsStereoscopic)
            {
                if (Stereoscopic) {
                    switch (mapping) {
                        case Mapping.TB:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(1f, 0.5f, 0f, 0.5f),
                                new Vector4(1f, 0.5f, 0f, 0f)
                            });
                            break;
                        case Mapping.LR:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(0.5f, 1f, 0f, 0f),
                                new Vector4(0.5f, 1f, 0.5f, 0f)
                            });
                            break;
                        case Mapping.MONO:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(1f, 1f, 0f, 0f),
                                new Vector4(1f, 1f, 0f, 0f)
                            });
                            break;
                    }
                } else {
                    switch (mapping) {
                        case Mapping.TB:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(1f, 0.5f, 0f, 0.5f),
                                new Vector4(1f, 0.5f, 0f, 0.5f)
                            });
                            break;
                        case Mapping.LR:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(0.5f, 1f, 0f, 0f),
                                new Vector4(0.5f, 1f, 0f, 0f)
                            });
                            break;
                        case Mapping.MONO:
                            ProjectionMesh?.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                                new Vector4(1f, 1f, 0f, 0f),
                                new Vector4(1f, 1f, 0f, 0f)
                            });
                            break;
                    }
                }
            }
            EyeScale(Stereoscopic);
        }

		/**
        <summary>
        Focus at a certain direction in the video
        </summary>
        <param name="Degrees">Direction in degrees</param>
        <remarks>
        &gt; [!NOTE] 
        &gt; You can use this to disable the stereo effect if you want to display an interface
        </remarks>
        */
		public void FocusAt(int Degrees)
        {
            App.Recenter();
            Vector3 euler = App.camera.parent.localEulerAngles;
            euler.y = Degrees;
            App.camera.parent.localEulerAngles = euler;
        }
        /// <summary>
        /// Subtitle class
        /// </summary>
        public Subtitles subtitles;

		#endregion
		#region INTERNAL
		internal bool isStream = false;
        internal abstract Status GetStatus();
        internal AudioPlayer ExternalAudio;
        internal virtual void Initialize()
        {
            AA = QualitySettings.antiAliasing;
            QualitySettings.antiAliasing = 0;
        }
        internal void DoNotDestroy()
        {
            if (!GetComponent<DoNotDestroy>())
            {
                gameObject.AddComponent<DoNotDestroy>();
            }
        }
        internal virtual void Update()
        {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
            if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
            {
                if (!HMDLost)
                {
                    if (!OVRPlugin.hasVrFocus || !OVRPlugin.userPresent)
                    {
                        Debug.Log("[VideoPlayerBase DEBUG] Oculus HMD Lost");

                        wasPlaying = IsPlaying;
                        Pause();
                        HMDLost = true;
                    }
                }
                else
                {
                    if (OVRPlugin.hasVrFocus && OVRPlugin.userPresent)
                    {
                        Debug.Log("[VideoPlayerBase DEBUG] Oculus HMD Found");

                        if (wasPlaying) {
                            Resume();
                        }
                        HMDLost = false;
                    }
                }
            }
#endif
            if (ExternalAudio != null)
            {
                // tbe audio sync
                long currentSeekMs = SeekMs;
                if (!ExternalAudioFirstSync && IsPlaying && !Buffering && currentSeekMs > 0)
                {
                    ExternalAudio.SetSeekMs(currentSeekMs);
                    ExternalAudio.Sync(currentSeekMs);
                    ExternalAudio.Play();
                    ExternalAudioFirstSync = true;
                }
                if (IsPlaying && ExternalAudioFirstSync && currentSeekMs != lastSeekMs)
                {
                    lastSeekMs = currentSeekMs;
                    ExternalAudio.Sync(currentSeekMs);
                }
                if (Buffering)
                {
                    if (!startedBuffering)
                    {
                        startedBuffering = true;
                        ExternalAudio.Volume = 0;
                    }
                }
                else
                {
                    if (startedBuffering)
                    {
                        startedBuffering = false;
                        ExternalAudio.SetSeekMs(SeekMs);
                        ExternalAudio.Volume = (mute_ || !audioConf.External) ? 0f : Volume;
                    }
                }

                
            }
        }
        internal abstract void SetFormatData(AppDataStruct.FormatBlock formatData);
        internal void EyeScale(bool outputStereo)
        {
            lastOutputStereo = outputStereo;
            UpdateEyeScale();
        }
        // Updates projection sphere scale (if App.MonoSphereRadius changed)
        internal void UpdateEyeScale()
        {
            switch (currentProjection) {
                case "FLAT":
                    break;
                case "EQUIRECT":
                default:
                    float monoScale = App.MonoSphereRadius;
                    ProjectionMesh.transform.localScale = (VideoIsStereoscopic && lastOutputStereo) ? 
                        new Vector3(512f, 512f, 512f) : new Vector3(monoScale, monoScale, monoScale);
                    break;
            }
        }

        #endregion
        #region PROTECTED
        protected const long MAX_DELAYED_SYNC_MS = 1000;
        protected const long MAX_SYNC_DIFF_MS = 2500;
        protected const long SYNCED_DELAY_THRESHOLD_MS = 8;
        protected const string TEXTURE_FLIP_SHADER_KEYWORD = "FORCE_TEXTURE_FLIP";

        protected AudioConfig audioConf;
        protected bool mute_;
        protected virtual void OnDestroy()
        {
            // turn off never screen sleep
            Startup.NeverSleepFlag &= ~(uint)2;

			if (App.camera == null) return;

			Vector3 euler = App.camera.parent.localEulerAngles;
            euler.y = 0;
            App.camera.parent.localEulerAngles = euler;

            // CameraInformation camInfo = App.camera.GetComponent<CameraInformation>();
            // if (camInfo.gameObject.GetComponent<Camera>())
            // {
            //     camInfo.gameObject.GetComponent<Camera>().clearFlags = cameraClearCache;
            // }
            // camInfo.Left.clearFlags = cameraClearCache;
            // camInfo.Right.clearFlags = cameraClearCache;

            QualitySettings.antiAliasing = AA;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
                // reset render resolution scale
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.0f;
                Debug.Log("Setting render resolution multiplier to " + UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }
#endif
        }

        protected void CreateEyes()
        {
            if (ProjectionMesh == null)
            {
                // for flat video, get aspect ratio to size screen mesh accordingly
                float aspect = 1.77777778f;
                App.VideoMetadata meta = App.GetVideoMetadata(App.CurrentVideo);
                if (meta != null) {
                    currentProjection = meta.ProjectionType;
                    // calculate intended aspect ratio of virtual screen
                    if (currentProjection == "FLAT") {
                        int videoWidth;
                        if (!App.GetCustomVideoParam(App.CurrentVideo, "OrgX", out videoWidth) || videoWidth <= 0) {
                            videoWidth = 1920;
                        }
                        int videoHeight;
                        if (!App.GetCustomVideoParam(App.CurrentVideo, "OrgY", out videoHeight) || videoHeight <= 0) {
                            videoHeight = 1080;
                        }
                        // correct video aspect ratio in case of stereoscopic flat video
                        switch (meta.StereoMap) {
                            case "TB":
                                videoHeight /= 2;
                                break;
                            case "LR":
                                videoWidth /= 2;
                                break;
                        }
                        aspect = (1f * videoWidth) / videoHeight;
                    }
                }

                GameObject projectionMeshObj = new GameObject("ProjectionMesh");
                switch (currentProjection) {
                    case "FLAT":
                        projectionMeshObj.AddComponent<MeshFilter>().mesh = Generate.Flat(aspect, 1f, 0f, 0f); // preferred values: 3f, 5.5f, 1.7f
                        // Set default size and position of virtual screen
                        projectionMeshObj.transform.localScale = new Vector3(3f, 3f, 3f);
                        float heightAdjust = 0f;
                        if (App.GetTrackingOrigin() == App.TrackingOrigin.FloorLevel) {
                            heightAdjust = 1.7f;
                        }
                        projectionMeshObj.transform.localPosition = new Vector3(0f, heightAdjust, 5.5f);
                        break;
                    case "EQUIRECT":
                    default:
                        projectionMeshObj.AddComponent<MeshFilter>().mesh = App.Videosphere;
                        projectionMeshObj.transform.localScale = new Vector3(512, 512, 512);
                        break;
                }

                ProjectionMesh = projectionMeshObj.AddComponent<MeshRenderer>();
                ProjectionMesh.material = new Material(VRInput.VideoShader);
                ProjectionMesh.transform.parent = gameObject.transform;
                // set default stereo-compatible projection texture tiling & offset (to monoscopic)
                ProjectionMesh.material.SetVectorArray("_MainTex_Stereo_ST", new Vector4[] {
                    new Vector4(1f, 1f, 0f, 0f),
                    new Vector4(1f, 1f, 0f, 0f)
                });
            }
        }

        protected void SetTextureFlip(bool requiresFlip) {
			if (requiresFlip) {
				Shader.EnableKeyword(TEXTURE_FLIP_SHADER_KEYWORD);
			} else {
                Shader.DisableKeyword(TEXTURE_FLIP_SHADER_KEYWORD);
            }
		}

        protected abstract float GetVolume();
        protected virtual void SetVolume(float vol)
        {

			if (ExternalAudio != null && !Mute && audioConf.External)
            {
                ExternalAudio.Volume = vol;
            }
        }
        protected virtual void SetMute(bool mute)
        {
            if (ExternalAudio != null)
            {
                ExternalAudio.Volume = (mute || !audioConf.External) ? 0f : Volume;
            }
            mute_ = mute;
        }
        protected abstract bool GetLoop();
        protected abstract void SetLoop(bool loop);
        protected abstract float GetSeek();
        protected abstract void SetSeek(float seek);
        protected abstract long GetSeekMs();
        protected virtual void SetSeekMs(long seek) { }
        protected abstract bool GetBuffering();
        protected abstract long GetDuration();
        protected abstract bool GetIsPlaying();
        #endregion
    }
}
