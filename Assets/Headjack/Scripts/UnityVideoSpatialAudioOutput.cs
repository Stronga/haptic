// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Experimental.Video;
using UnityEngine.Experimental.Audio;
using Unity.Collections;
using TBE;

//-----------------------------------------------------------------------------
// Copyright 2015-2018 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// modified by Headjack
//-----------------------------------------------------------------------------

namespace Headjack
{
	/// <summary>
	/// This is an experimental feature and only works in Windows currently
	/// Audio is grabbed from the MediaPlayer and rendered via Unity
	/// This allows audio to have 3D spatial control, effects applied and to be spatialised for VR
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class UnityVideoSpatialAudioOutput : MonoBehaviour
	{
		// public AudioOutput.AudioOutputMode _audioOutputMode = AudioOutput.AudioOutputMode.Multiple;

		[SerializeField]
		public VideoPlayer _mediaPlayer;

		[HideInInspector]
		public int _channelMask = -1;

		private AudioSampleProvider _asp;
		// TBE
		private NativeSpatDecoderQueue _spatQueue;
		private ChannelMap _channelMap = ChannelMap.STEREO;
		private int _channels = 2;
		// re-usable data arrays
		private float[] _spatialData = null;
		NativeArray<float> _nativeBuffer;
		// in order to remap from Unity sample rate to spatial audio sample rate
		private int _unitySampleRate;
		private int _tbeSampleRate;
		private ushort _trackIndex = 0;
		private bool _isPlaying = false;

		void Awake()
		{
			// initialize Audio360 (once)
            if (FindObjectsOfType(typeof(Facebook.Audio.FBAudio360Bootstrapper)).Length <= 0)
            {
                const int objectPoolSize = 64;
                const int spatFilePoolSize = 4;
                const int spatQueuePoolSize = 2;
                Facebook.Audio.FBAudio360Bootstrapper.Init(null, TBE.RuntimeOptions.Create(objectPoolSize, spatFilePoolSize, spatQueuePoolSize), TBE.SampleRate.SR_48000, TBE.AudioDeviceType.DEFAULT);
            }

			if (_mediaPlayer == null) {
				_mediaPlayer = this.GetComponent<VideoPlayer>();
			}

			_tbeSampleRate = 48000;
			_unitySampleRate = UnityEngine.AudioSettings.GetConfiguration().sampleRate;
		}

		void Start()
		{
			ChangeMediaPlayer(_mediaPlayer);
			// initialize queue-based Spatial Decoder
			_spatQueue = AudioEngineManager.Instance.nativeEngine.createSpatDecoderQueue();
		}

		void OnDestroy()
		{
			ChangeMediaPlayer(null);

			if (AudioEngineManager.Instance.nativeEngine != null)
            {
                if (_spatQueue != null)
                {
                    _spatQueue.flushQueue();
                    AudioEngineManager.Instance.nativeEngine.destroySpatDecoderQueue(_spatQueue);
                    _spatQueue = null;
                }
            }

			if (_nativeBuffer.IsCreated) {
				_nativeBuffer.Dispose();
			}
		}

		void Update()
		{
			if (_mediaPlayer != null && _mediaPlayer.isPlaying)
			{
				ApplyAudioSettings(_mediaPlayer, _spatQueue);
				_isPlaying = true;
			} else {
				_isPlaying = false;
			}
		}

		public void ChangeMediaPlayer(VideoPlayer newPlayer)
		{
			// When changing the media player, handle event subscriptions
			if (_mediaPlayer != null)
			{
				_mediaPlayer.prepareCompleted -= OnMediaPlayerPrepared;
				_mediaPlayer.loopPointReached  -= OnMediaPlayerClosing;
				_mediaPlayer = null;
			}

			_mediaPlayer = newPlayer;
			if (_mediaPlayer != null)
			{
				_mediaPlayer.prepareCompleted += OnMediaPlayerPrepared;
				_mediaPlayer.loopPointReached  += OnMediaPlayerClosing;
			}
		}

		public void SetChannels(int channels, ChannelMap channelMap) 
		{
			_channels = channels;
			_channelMap = channelMap;
		}

		private void OnMediaPlayerPrepared(VideoPlayer mp) {
			Debug.Log("[UnityVideoSpatialAudioOutput] Starting audio sources");
			_asp = VideoPlayerExtensions.GetAudioSampleProvider(mp, _trackIndex);
			_asp.sampleFramesAvailable += SampleFramesAvailable;
			_asp.enableSampleFramesAvailableEvents = true;
			_asp.freeSampleFrameCountLowThreshold = _asp.maxSampleFrameCount - 2048;

			ApplyAudioSettings(mp, _spatQueue);
			_spatQueue.play();
		}

		private void OnMediaPlayerClosing(VideoPlayer mp) {
			if(mp.isLooping) {
				Debug.Log("[UnityVideoSpatialAudioOutput] not stopping spatial audio as video is looping");
				return;
			}

			Debug.Log("[UnityVideoSpatialAudioOutput] Stopping audio sources");
			_spatQueue.stop();
		}	

		private void ApplyAudioSettings(VideoPlayer player, NativeSpatDecoderQueue spatQueue)
		{
			// Apply volume and mute from the MediaPlayer to the AudioSource
			if (player != null && spatQueue != null)
			{
				float volume = player.GetDirectAudioVolume(_trackIndex);
				bool isMuted = player.GetDirectAudioMute(_trackIndex);
				spatQueue.setVolume(isMuted ? 0f : volume, 0);
			}
		}

		void SampleFramesAvailable(AudioSampleProvider provider, uint sampleFrameCount) {
			// resizing reusable float array if necessary
			if (_spatialData == null || _spatialData.Length < (int)sampleFrameCount * provider.channelCount) {
				Debug.Log($"[UnityVideoSpatialAudioOutput] Resizing reusable float array from to {(int)sampleFrameCount * provider.channelCount}");
				_spatialData = new float[(int)sampleFrameCount * provider.channelCount];
				if (_nativeBuffer.IsCreated) {
					_nativeBuffer.Dispose();
				}
				_nativeBuffer = new NativeArray<float>((int)sampleFrameCount * provider.channelCount, Allocator.Persistent);
			}

			if (provider.channelCount != _channels) {
				Debug.LogError($"[UnityVideoSpatialAudioOutput] Audio provider channel count ({provider.channelCount}) does not match expected channel count ({_channels}).");
			} else if (_tbeSampleRate != _unitySampleRate) {
				Debug.LogError($"[UnityVideoSpatialAudioOutput] Unity sample rate ({_unitySampleRate}) does not match spatial audio sample rate ({_tbeSampleRate}).");
			} else {
				uint sfCount = provider.ConsumeSampleFrames(_nativeBuffer);
				// if (sfCount != sampleFrameCount) {
				// 	Debug.LogWarning($"[UnityVideoSpatialAudioOutput] mismatch between expected sample count {sampleFrameCount} and retrieved sample count {sfCount}");
				// }
				_nativeBuffer.CopyTo(_spatialData);
				int enqueued = _spatQueue.enqueueData(_spatialData, (int)sfCount * provider.channelCount, _channelMap);
				// Debug.Log($"[UnityVideoSpatialAudioOutput] SampleFramesAvailable got {sfCount} sample frames. successfully enqueued {enqueued}.");
			}
		}
	}
}
