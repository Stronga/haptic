// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

#if USE_AVPROVIDEO

using System.Collections;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using Headjack.Utils;

namespace Headjack
{
	/// <summary>
	/// AVPro integration for Headjack video player
	/// </summary>
	public class VideoPlayer_AVP : VideoPlayerBase
	{
		private MediaPlayer mediaPlayer;
		private OnEnd onEndEvent;
		private Status currentStatus;
		private bool currentTextureRequiresVerticalFlip = false;
		private AVProVideoSpatialAudioOutput spatialAudioOutput;
		private AudioOutput audioOutput;

		internal override void Initialize()
		{
			mediaPlayer = GetComponent<MediaPlayer>();
			if (mediaPlayer == null) {
				mediaPlayer = gameObject.AddComponent<MediaPlayer>();
				mediaPlayer.Events.AddListener(OnVideoEvent);
				// set main camera as spatial audio head transform
				mediaPlayer.AudioHeadTransform = Camera.main?.transform;
			}

			base.Initialize();
			CreateEyes();

			if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                Shader.EnableKeyword("GAMMA_TO_LINEAR");
            } else {
                Shader.DisableKeyword("GAMMA_TO_LINEAR");
            }
		}

		public void OnVideoEvent(MediaPlayer mp, RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType et, ErrorCode errorCode)
		{
			switch (et) {
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.Error:
					// execute callback with error
					string errorMessage = "Error loading video";
					if (errorCode == ErrorCode.DecodeFailed) {
						errorMessage = "Error decoding video during playback";
					}
					onEndEvent?.Invoke(false, errorMessage);
					onEndEvent = null;
					currentStatus = Status.VP_ERROR;
					break;
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.ReadyToPlay:
					currentStatus = Status.VP_READY;
					break;
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.FinishedPlaying:
					// Stop video player as end is reached (without looping)
					onEndEvent?.Invoke(true, null);
					onEndEvent = null;
					base.Stop();
					currentStatus = Status.VP_END;
					break;
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.Closing:
					currentStatus = Status.VP_NOT_READY;
					break;
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.MetaDataReady:
					currentStatus = Status.VP_BUFFERING;
					break;
			}
		}

		internal override void Update()
		{
			base.Update();

			ProjectionMesh.material.mainTexture = GetVideoTexture();

			bool newTextureRequiresVerticalFlip = (mediaPlayer?.TextureProducer?.RequiresVerticalFlip()).GetValueOrDefault(false);
			if (newTextureRequiresVerticalFlip != currentTextureRequiresVerticalFlip) {
				// Take into account vertically flipping textures
				currentTextureRequiresVerticalFlip = newTextureRequiresVerticalFlip;
				SetTextureFlip(newTextureRequiresVerticalFlip);
			}
		}

		public override void Prepare(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null, long VideoTime = 0)
        {
            Debug.Log("[VideoPlayer_AVP] Preparing media using AVPro!");

			// stop existing playback
			Stop();
			mediaPlayer.CloseVideo();

            if (FullPath.Contains("STREAMINGASSET:"))
            {
                FullPath = FullPath.Replace("STREAMINGASSET:", Application.streamingAssetsPath + "/");
            }

			// save callback
			gameObject.SetActive(true);
			onEndEvent += onEnd;

#if UNITY_IOS && !UNITY_EDITOR
			MediaPlayer.OptionsIOS iosOptions = mediaPlayer.PlatformOptionsIOS;
			iosOptions.useYpCbCr420Textures = false;
#endif // UNITY_IOS && !UNITY_EDITOR

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			MediaPlayer.OptionsWindows windowsOptions = mediaPlayer.PlatformOptionsWindows;
			if (App.CurrentVideo != null && 
				App.Data.Video[App.CurrentVideo].Audio != null &&
				App.Data.Video[App.CurrentVideo].Audio.Channels > 2) {
				Debug.Log($"[VideoPlayer_AVP] Enabling Audio360 spatial audio support with {App.Data.Video[App.CurrentVideo].Audio.Channels} channels");

				if (audioOutput != null) {
					Destroy(audioOutput);
					audioOutput = null;
				}

				AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				if (placeholderSource == null) {
					placeholderSource = gameObject.AddComponent<AudioSource>();
				}

				// initialize spatial audio passthrough
				if (spatialAudioOutput == null) {
					spatialAudioOutput = gameObject.AddComponent<AVProVideoSpatialAudioOutput>();
				}
				spatialAudioOutput._mediaPlayer = mediaPlayer;
				spatialAudioOutput._audioSource = placeholderSource;

				windowsOptions.useUnityAudio = true;
				windowsOptions.forceAudioResample = false;
				// set channel spatial mapping
				string channelMapString = App.Data.Video[App.CurrentVideo].Audio.Mapping;
				TBE.ChannelMap channelMap = AudioPlayer_TBE.GetChannelMapFromChannelCount(
					App.Data.Video[App.CurrentVideo].Audio.Channels,
					channelMapString);
				spatialAudioOutput.SetChannels(App.Data.Video[App.CurrentVideo].Audio.Channels, channelMap);
			} else {
				Debug.Log($"[VideoPlayer_AVP] No embedded spatial audio detected, using regular audio passthrough");

				// not using spatial audio passthrough, removing old components
				if (spatialAudioOutput != null) {
					Destroy(spatialAudioOutput);
					spatialAudioOutput = null;
				}

				AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				if (placeholderSource == null) {
					placeholderSource = gameObject.AddComponent<AudioSource>();
				}

				// initialize audio passthrough (to support custom audio device)
				if (audioOutput == null) {
					audioOutput = gameObject.AddComponent<AudioOutput>();
				}
				audioOutput.ChangeMediaPlayer(mediaPlayer);

				windowsOptions.useUnityAudio = true;
				windowsOptions.forceAudioResample = true;
			}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

			// load video
			// TODO: replace auto-playback and a pausing coroutine with non-auto playback and loading to a specific timestamp
			mediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, FullPath, true);
			SetVolume(1f);

			if (mediaPlayer.AudioHeadTransform == null) {
				// set main camera as spatial audio head transform
				mediaPlayer.AudioHeadTransform = Camera.main?.transform;
			}

			// pause playback as soon as it starts
			StartCoroutine(SeekPause(VideoTime));

            base.Prepare(FullPath, Stream, onEnd, FullPathFallback, VideoTime);
        }

        private IEnumerator SeekPause(long seekTime) {
            while ((int)GetStatus() < (int)Status.VP_PLAYING) {
                yield return null;
            }

            Pause();
            if (seekTime > 0) {
                SeekMs = seekTime;
            }
        }

		public override void Play(string FullPath, bool Stream, OnEnd onEnd = null,  string FullPathFallback=null)
        {
			// stop existing playback
			Stop();
			mediaPlayer.CloseVideo();

            if (FullPath.Contains("STREAMINGASSET:"))
            {
                FullPath = FullPath.Replace("STREAMINGASSET:", Application.streamingAssetsPath + "/");
            }

			// save callback
			gameObject.SetActive(true);
			onEndEvent += onEnd;

#if UNITY_IOS && !UNITY_EDITOR
			MediaPlayer.OptionsIOS iosOptions = mediaPlayer.PlatformOptionsIOS;
			iosOptions.useYpCbCr420Textures = false;
#endif // UNITY_IOS && !UNITY_EDITOR

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			MediaPlayer.OptionsWindows windowsOptions = mediaPlayer.PlatformOptionsWindows;
			if (App.CurrentVideo != null && 
				App.Data.Video[App.CurrentVideo].Audio != null &&
				App.Data.Video[App.CurrentVideo].Audio.Channels > 2) {
				Debug.Log($"[VideoPlayer_AVP] Enabling Audio360 spatial audio support with {App.Data.Video[App.CurrentVideo].Audio.Channels} channels");

				if (audioOutput != null) {
					Destroy(audioOutput);
					audioOutput = null;
				}

				AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				if (placeholderSource == null) {
					placeholderSource = gameObject.AddComponent<AudioSource>();
				}

				// initialize spatial audio passthrough
				if (spatialAudioOutput == null) {
					spatialAudioOutput = gameObject.AddComponent<AVProVideoSpatialAudioOutput>();
				}
				spatialAudioOutput._mediaPlayer = mediaPlayer;
				spatialAudioOutput._audioSource = placeholderSource;

				windowsOptions.useUnityAudio = true;
				windowsOptions.forceAudioResample = false;
				// set channel spatial mapping
				string channelMapString = App.Data.Video[App.CurrentVideo].Audio.Mapping;
				TBE.ChannelMap channelMap = AudioPlayer_TBE.GetChannelMapFromChannelCount(
					App.Data.Video[App.CurrentVideo].Audio.Channels,
					channelMapString);
				spatialAudioOutput.SetChannels(App.Data.Video[App.CurrentVideo].Audio.Channels, channelMap);
			} else {
				Debug.Log($"[VideoPlayer_AVP] No embedded spatial audio detected, using regular audio passthrough");

				// not using spatial audio passthrough, removing old components
				if (spatialAudioOutput != null) {
					Destroy(spatialAudioOutput);
					spatialAudioOutput = null;
				}

				AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				if (placeholderSource == null) {
					placeholderSource = gameObject.AddComponent<AudioSource>();
				}

				// initialize audio passthrough (to support custom audio device)
				if (audioOutput == null) {
					audioOutput = gameObject.AddComponent<AudioOutput>();
				}
				audioOutput.ChangeMediaPlayer(mediaPlayer);

				windowsOptions.useUnityAudio = true;
				windowsOptions.forceAudioResample = true;
			}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

			// load video and start playing immediately
			mediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, FullPath, true);
			SetVolume(1f);

			if (mediaPlayer.AudioHeadTransform == null) {
				// set main camera as spatial audio head transform
				mediaPlayer.AudioHeadTransform = Camera.main?.transform;
			}

            base.Play(FullPath, Stream);
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

            // force pause so seek times are not affected by playback
            Pause();

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
                Debug.Log($"[VideoPlayer_AVP] No need for seek correction, target ({seekTarget}ms) is within {MAX_SYNC_DIFF_MS}ms of current seek time ({SeekMs}ms)");
                return false;
            }

            // seek to target
            SeekMs = seekTarget;
            return true;
        }

        public override void PlayAt(long playTime, long videoTime = -1)
        {
            base.PlayAt(playTime, videoTime);

            Status currentStatus = GetStatus();
            if ((int)currentStatus < (int)Status.VP_READY) {
                Debug.LogError("[VideoPlayer_AVP] Attempting to resume playback on the video player without a video loaded");
                onEndEvent?.Invoke(false, "Attempting to resume playback on the video player without a video loaded");
                onEndEvent = null;
                return;
            }

            if (currentStatus == Status.VP_PLAYING) {
                Pause();
            }

            StartCoroutine(PlaySynced(playTime, videoTime));
        }

        private IEnumerator PlaySynced(long playTime, long videoTime)
        {
            long availableTime = playTime - TimeStamp.GetMsLong();
            long seekDiff = 0; 
            if (videoTime >= 0) {
                // only correct for video timecode if videoTime is valid timecode
                seekDiff = videoTime - SeekMs;
            }

            long playDelay = availableTime - seekDiff;
            // seek if it is impossible to start playback in sync
            if (seekDiff > availableTime || seekDiff < -MAX_DELAYED_SYNC_MS) {
                // TODO: seek to appropriate time first
                Debug.Log($"[VideoPlayer_AVP] Seeking to ensure synced playback.");

                bool startedSeek = CorrectedSync(playTime, videoTime);
                if (startedSeek) {
                    // wait until corrected seek is finished
                    yield return null;
                    float timeout = MAX_SYNC_DIFF_MS * 0.001f;
                    while (GetStatus() != Status.VP_PAUSED && timeout > 0) {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }
                    if (timeout <= 0) {
                        Debug.LogWarning("[VideoPlayer_AVP] Seeking took longer than the sync logic allowed");
                    }
                }

                long nowVideoSynced = videoTime + TimeStamp.GetMsLong() - playTime;
                playDelay = SeekMs - nowVideoSynced;
                if (playDelay < 0) {
                    Debug.LogWarning("[VideoPlayer_AVP] Seeking took too long and current video timestamp is already out-of-date");
                }
            }

            yield return new WaitForSecondsRealtime(0.001f * playDelay);

            Resume();
            yield break;
        }

        public override void PauseAt(long pauseTime) 
        {
            base.PauseAt(pauseTime);

            if (GetStatus() != Status.VP_PLAYING) {
                Debug.LogWarning($"[VideoPlayer_AVP] Attempting to pause a video player that is not playing");
            }

            StartCoroutine(PauseSynced(pauseTime));
        }

        private IEnumerator PauseSynced(long pauseTime) {
            long pauseDelay = pauseTime - TimeStamp.GetMsLong();

            if (pauseDelay < 0) {
                Debug.LogWarning($"[VideoPlayer_AVP] Cannot pause in sync as pause timestamp is in the past");
            }

            pauseDelay = System.Math.Max(0L, pauseDelay);
            yield return new WaitForSecondsRealtime(0.001f * pauseDelay);
            Pause();
        }

		public override void Pause()
		{
			mediaPlayer?.Control?.Pause();
			base.Pause();
		}
		public override void Resume()
		{
			mediaPlayer?.Control?.Play();
			base.Resume();
		}
		public override void PauseResume()
		{
			base.PauseResume();
			if (IsPlaying)
			{
				Pause();
			}
			else
			{
				Resume();
			}
		}
		public override Texture GetVideoTexture()
		{
			Texture t = mediaPlayer?.TextureProducer?.GetTexture();
			if (t != null) return t;
			return Texture2D.blackTexture;
		}

		protected override bool GetBuffering()
		{
			bool isPlaying = (mediaPlayer?.Control?.IsPlaying()).GetValueOrDefault(false);
			bool isBuffering = (mediaPlayer?.Control?.IsBuffering()).GetValueOrDefault(false);
			bool isSeeking = (mediaPlayer?.Control?.IsSeeking()).GetValueOrDefault(false);
			return  !isPlaying && (isBuffering || isSeeking);
		}
		protected override long GetDuration()
		{
			return (long)(mediaPlayer?.Info?.GetDurationMs()).GetValueOrDefault(0);
		}

		protected override bool GetIsPlaying()
		{
			return (mediaPlayer?.Control?.IsPlaying()).GetValueOrDefault(false);
		}

		protected override float GetSeek()
		{
			float duration = (mediaPlayer?.Info?.GetDurationMs()).GetValueOrDefault(1f);
			if (duration <= 0f) {
				return 0f;
			}
			return GetSeekMs() / duration;
		}

		protected override long GetSeekMs()
		{
			return (long)(mediaPlayer?.Control?.GetCurrentTimeMs()).GetValueOrDefault(0f);
		}

		protected override void SetSeek(float seek)
		{
			mediaPlayer?.Control?.SeekFast(seek * (mediaPlayer?.Info?.GetDurationMs()).GetValueOrDefault(1f));
		}

		protected override void SetSeekMs(long seek)
		{
			base.SetSeekMs(seek);
			mediaPlayer?.Control?.SeekFast(seek);
		}

		protected override float GetVolume()
		{
			return (mediaPlayer?.Control?.GetVolume()).GetValueOrDefault(1f);
		}

		protected override void SetVolume(float volume)
		{
			base.SetVolume(volume);
			
			mediaPlayer?.Control?.SetVolume(volume);

			// ensure mute status is updated
			SetMute(Mute);
		}

		protected override void SetMute(bool mute)
        {
            base.SetMute(mute);

            if (mute || !audioConf.Internal)
            {
				mediaPlayer?.Control?.MuteAudio(true);
            }
            else
            {
                mediaPlayer?.Control?.MuteAudio(false);
            }
		}

		protected override bool GetLoop()
		{
			return (mediaPlayer?.Control?.IsLooping()).GetValueOrDefault(false);
		}

		protected override void SetLoop(bool loop)
		{
			mediaPlayer?.Control?.SetLooping(loop);
		}

		public override void SetAudioConfig(bool internalAudio, bool externalAudio)
        {
            base.SetAudioConfig(internalAudio, externalAudio);
			SetMute(Mute);
        }

		internal override Status GetStatus()
		{
			if (mediaPlayer == null) {
				return Status.VP_NOT_READY;
			}
			if (currentStatus == Status.VP_ERROR) {
				return currentStatus;
			}
			if (mediaPlayer.Control.IsBuffering() || mediaPlayer.Control.IsSeeking()) {
				return Status.VP_BUFFERING;
			}
			if (mediaPlayer.Control.IsPlaying()) {
				return Status.VP_PLAYING;
			}
			if (mediaPlayer.Control.IsPaused()) {
				return Status.VP_PAUSED;
			}
			if (mediaPlayer.Control.IsFinished()) {
				return Status.VP_END;
			}
			if (mediaPlayer.Control.CanPlay()) {
				return Status.VP_READY;
			}
			return currentStatus;
		}

		public override void Clean()
        {
            Pause();
			Debug.Log("Destroying video player");
			mediaPlayer?.CloseVideo();
			mediaPlayer?.Events?.RemoveListener(OnVideoEvent);

			// reset shader vertical texture flip
			currentTextureRequiresVerticalFlip = false;
			SetTextureFlip(false);

			base.Clean();
        }

		protected override void OnDestroy() {
			Clean();

			base.OnDestroy();
		}

		public override void Stop()
		{
            base.Stop();
            
			mediaPlayer?.Control?.Stop();
        }

		internal override void SetFormatData(AppDataStruct.FormatBlock formatData)
		{
			
		}
	}
}

#endif