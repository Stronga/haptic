// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Headjack.Utils;

namespace Headjack
{
	public class VideoPlayer_Unity : VideoPlayerBase
	{
		private VideoPlayer videoPlayer;
        private Status internalStatus = Status.VP_NOT_READY;
        private string lastError = null;
        private OnEnd onEndEvent = null;
        private UnityVideoSpatialAudioOutput spatialAudioOutput;

		internal override void Initialize()
		{
            internalStatus = Status.VP_NOT_READY;
			videoPlayer = GetComponent<VideoPlayer>();
			if (videoPlayer == null) {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                // register eventlisteners
                videoPlayer.errorReceived += (VideoPlayer source, string errorMessage) => {
                    internalStatus = Status.VP_ERROR;
                    lastError = errorMessage;
                    Stop();
                    onEndEvent?.Invoke(false, errorMessage);
                };
                videoPlayer.loopPointReached += (VideoPlayer source) => {
                    if (!source.isLooping) {
                        internalStatus = Status.VP_END;
                        Stop();
                        onEndEvent?.Invoke(true, null);
                    }
                };
            }
			videoPlayer.source = VideoSource.Url;
			videoPlayer.playOnAwake = false;
			videoPlayer.waitForFirstFrame = true;
			videoPlayer.renderMode = VideoRenderMode.APIOnly;
			videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            base.Initialize();
            CreateEyes();
		}

        public override void Prepare(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null, long VideoTime = 0)
		{
            if (gameObject.activeSelf) {
                Stop();
            } else {
                gameObject.SetActive(true);
            }

            if (FullPath.Contains("STREAMINGASSET:"))
            {
                FullPath = FullPath.Replace("STREAMINGASSET:", Application.streamingAssetsPath + "/");
            }

            // Handle spatial audio streaming
            bool spatialChannels = false;
            if (App.CurrentVideo != null && App.Data.Video[App.CurrentVideo].Audio != null)
            {
                spatialChannels = (App.Data.Video[App.CurrentVideo].Audio.Channels > 2);
            }

// #if UNITY_IOS && !UNITY_EDITOR
//             // Known Issue: iOS: [iOS 14] VideoPlayer crashes on EXC_BAD_ACCESS or signal SIGABRT 
//             // when audioOutputMode is set to APIOnly or Audio Source (1274837)
//             spatialChannels = false;
// #endif

			if (spatialChannels) {
				Debug.Log($"[VideoPlayer_Unity] Enabling Audio360 spatial audio support with {App.Data.Video[App.CurrentVideo].Audio.Channels} channels");

				// AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				// if (placeholderSource == null) {
				// 	placeholderSource = gameObject.AddComponent<AudioSource>();
				// }

				// initialize spatial audio passthrough
				if (spatialAudioOutput == null) {
					spatialAudioOutput = gameObject.AddComponent<UnityVideoSpatialAudioOutput>();
				}
				spatialAudioOutput._mediaPlayer = videoPlayer;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.APIOnly;

				// set channel spatial mapping
				string channelMapString = App.Data.Video[App.CurrentVideo].Audio.Mapping;
				TBE.ChannelMap channelMap = AudioPlayer_TBE.GetChannelMapFromChannelCount(
					App.Data.Video[App.CurrentVideo].Audio.Channels,
					channelMapString);
				spatialAudioOutput.SetChannels(App.Data.Video[App.CurrentVideo].Audio.Channels, channelMap);
			} else {
				Debug.Log($"[VideoPlayer_Unity] No embedded spatial audio detected, using direct audio output");

				// not using spatial audio passthrough, removing old components
				if (spatialAudioOutput != null) {
					Destroy(spatialAudioOutput);
					spatialAudioOutput = null;
				}

				// AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				// if (placeholderSource != null) {
                //     Destroy(placeholderSource);
				// }

				videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
			}

            StartCoroutine(SeekPause(VideoTime));

            onEndEvent = onEnd;
            videoPlayer.url = FullPath;
            videoPlayer.Play();

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

		public override void Play(string FullPath, bool Stream, OnEnd onEnd = null, string FullPathFallback = null)
		{
            if (gameObject.activeSelf) {
                Stop();
            } else {
                gameObject.SetActive(true);
            }

            if (FullPath.Contains("STREAMINGASSET:"))
            {
                FullPath = FullPath.Replace("STREAMINGASSET:", Application.streamingAssetsPath + "/");
            }

            // Handle spatial audio streaming
            bool spatialChannels = false;
            if (App.CurrentVideo != null && App.Data.Video[App.CurrentVideo].Audio != null)
            {
                spatialChannels = (App.Data.Video[App.CurrentVideo].Audio.Channels > 2);
            }

			if (spatialChannels) {
				Debug.Log($"[VideoPlayer_Unity] Enabling Audio360 spatial audio support with {App.Data.Video[App.CurrentVideo].Audio.Channels} channels");

				// AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				// if (placeholderSource == null) {
				// 	placeholderSource = gameObject.AddComponent<AudioSource>();
				// }

				// initialize spatial audio passthrough
				if (spatialAudioOutput == null) {
					spatialAudioOutput = gameObject.AddComponent<UnityVideoSpatialAudioOutput>();
				}
				spatialAudioOutput._mediaPlayer = videoPlayer;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.APIOnly;

				// set channel spatial mapping
				string channelMapString = App.Data.Video[App.CurrentVideo].Audio.Mapping;
				TBE.ChannelMap channelMap = AudioPlayer_TBE.GetChannelMapFromChannelCount(
					App.Data.Video[App.CurrentVideo].Audio.Channels,
					channelMapString);
				spatialAudioOutput.SetChannels(App.Data.Video[App.CurrentVideo].Audio.Channels, channelMap);
			} else {
				Debug.Log($"[VideoPlayer_Unity] No embedded spatial audio detected, using direct audio output");

				// not using spatial audio passthrough, removing old components
				if (spatialAudioOutput != null) {
					Destroy(spatialAudioOutput);
					spatialAudioOutput = null;
				}

				// AudioSource placeholderSource = gameObject.GetComponent<AudioSource>();
				// if (placeholderSource != null) {
                //     Destroy(placeholderSource);
				// }

				videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
			}

            onEndEvent = onEnd;
            videoPlayer.url = FullPath;
			videoPlayer.Play();

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
                Debug.Log($"[VideoPlayer_Unity] No need for seek correction, target ({seekTarget}ms) is within {MAX_SYNC_DIFF_MS}ms of current seek time ({SeekMs}ms)");
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
                Debug.LogError("[VideoPlayer_Unity] Attempting to resume playback on the video player without a video loaded");
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

            // Debug.Log($"[VideoPlayer_EXO DEBUG] availableTime {availableTime} - seekDiff {seekDiff} (playDelay {playDelay})");

            // seek if it is impossible to start playback in sync
            if (seekDiff > availableTime || seekDiff < -MAX_DELAYED_SYNC_MS) {
                // TODO: seek to appropriate time first
                Debug.Log($"[VideoPlayer_Unity] Seeking to ensure synced playback.");

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
                        Debug.LogWarning("[VideoPlayer_Unity] Seeking took longer than the sync logic allowed");
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
                if (playDelay < 0) {
                    Debug.LogWarning("[VideoPlayer_Unity] Seeking took too long and current video timestamp is already out-of-date");
                }
            }

            // Debug.Log($"[VideoPlayer_Unity DEBUG] Waiting playDelay {playDelay}");

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

                // Debug.Log($"[VideoPlayer_Unity DEBUG] new playDelay {playDelay}, realtime: {TimeStamp.GetMsLong()}, SeekMs: {SeekMs}");
            }

            Debug.Log($"[VideoPlayer_Unity] Playing at {TimeStamp.GetMsLong()}, target {playTime} ({playTime - TimeStamp.GetMsLong()}. At video time {SeekMs}, target {videoTime} ({videoTime - SeekMs})\nState before playing: {CurrentStatus}");

            Resume();
            yield break;
        }

        public override void PauseAt(long pauseTime) 
        {
            base.PauseAt(pauseTime);

            if (GetStatus() != Status.VP_PLAYING) {
                Debug.LogWarning($"[VideoPlayer_Unity] Attempting to pause a video player that is not playing");
            }

            StartCoroutine(PauseSynced(pauseTime));
        }

        private IEnumerator PauseSynced(long pauseTime) {
            long pauseDelay = pauseTime - TimeStamp.GetMsLong();

            if (pauseDelay < 0) {
                Debug.LogWarning($"[VideoPlayer_Unity] Cannot pause in sync as pause timestamp is in the past");
            }

            pauseDelay = System.Math.Max(0L, pauseDelay);
            yield return new WaitForSecondsRealtime(0.001f * pauseDelay);
            Pause();
        }

		internal override void Update()
		{
            base.Update();

            ProjectionMesh.material.mainTexture = GetVideoTexture();
		}

        protected override void OnDestroy()
        {
            Clean();
            base.OnDestroy();
        }

		public override Texture GetVideoTexture()
		{
			return videoPlayer.texture;
		}

		protected override bool GetBuffering()
		{
            return !videoPlayer.isPaused && !videoPlayer.isPrepared;
		}

		protected override long GetDuration()
		{
            return (long)System.Math.Floor(videoPlayer.length * 1000);
			// return (long)(videoPlayer.frameCount / videoPlayer.frameRate);
		}

		protected override bool GetIsPlaying()
		{
			return videoPlayer.isPlaying;
		}

		protected override bool GetLoop()
		{
			return videoPlayer.isLooping;
		}

		protected override float GetSeek()
		{
			if (videoPlayer.length <= Mathf.Epsilon) {
                return 0f;
            }
            return (float)(videoPlayer.time / videoPlayer.length);
		}

		protected override long GetSeekMs()
		{
            return (long)System.Math.Floor(videoPlayer.time * 1000);
		}

		protected override float GetVolume()
		{
			return videoPlayer.GetDirectAudioVolume(0);
		}
		protected override void SetVolume(float volume)
		{
            base.SetVolume(volume);

			videoPlayer.SetDirectAudioVolume(0,volume);
		}

        protected override void SetMute(bool mute)
        {
            base.SetMute(mute);
            if (mute || !audioConf.Internal)
            {
                videoPlayer.SetDirectAudioMute(0, mute);
            }
            else
            {
                videoPlayer.SetDirectAudioMute(0, mute);
            }
            // TODO: spatial audio
			// if (sTBE != null) sTBE.SetVolume(mute ? volume_ : 0);
		}

        public override void SetAudioConfig(bool internalAudio, bool externalAudio)
        {
            if (internalAudio)
            {
                videoPlayer.SetDirectAudioMute(0, false);
            }
            else
            {
                videoPlayer.SetDirectAudioMute(0, true);
            }
            base.SetAudioConfig(internalAudio, externalAudio);
        }

		protected override void SetLoop(bool loop)
		{
			videoPlayer.isLooping = loop;
		}

		protected override void SetSeek(float seek)
		{
            if (videoPlayer.length <= Mathf.Epsilon) {
                Debug.LogWarning($"Cannot seek ({seek}) proportional to video length: video length not known (is this a livestream?)");
                return;
            }
			videoPlayer.time = seek * videoPlayer.length;
		}

        protected override void SetSeekMs(long seekMs)
		{
            base.SetSeekMs(seekMs);

			videoPlayer.time = seekMs / 1000f;
		}

		internal override Status GetStatus()
		{
			if (videoPlayer.isPaused) {
                internalStatus = Status.VP_PAUSED;
                return Status.VP_PAUSED;
            }
            if (videoPlayer.isPlaying) {
                internalStatus = Status.VP_PLAYING;
                return Status.VP_PLAYING;
            }
            if (videoPlayer.isPrepared) {
                internalStatus = Status.VP_READY;
                return Status.VP_READY;
            }
            if (internalStatus == Status.VP_ERROR) {
                return internalStatus;
            }
            if (internalStatus == Status.VP_END) {
                return internalStatus;
            }
            if (GetBuffering()) {
                return Status.VP_BUFFERING;
            }
            return Status.VP_NOT_READY;
		}

        public override void Pause()
        {
            base.Pause();
            videoPlayer.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            videoPlayer.Play();
        }

        public override void PauseResume()
        {
            base.PauseResume();
            if (videoPlayer.isPaused)
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
            Stop();
			base.Clean();
        }

        public override void Stop()
		{
            base.Stop();
            videoPlayer.Stop();

            // TODO: streaming spatial audio
            // if (sTBE != null)
            // {
            //     sTBE.FlushQueue();
            // }
        }

		internal override void SetFormatData(AppDataStruct.FormatBlock formatData)
		{
			
		}
	}
}