// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

#if USE_AVPROVIDEO

using UnityEngine;
using RenderHeads.Media.AVProVideo;
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
	public class AVProVideoSpatialAudioOutput : MonoBehaviour
	{
		public AudioOutput.AudioOutputMode _audioOutputMode = AudioOutput.AudioOutputMode.Multiple;

		[SerializeField]
		public MediaPlayer _mediaPlayer;
		// placeholder Unity AudioSource to sync audio with spatial decoder
		public AudioSource _audioSource;

		[HideInInspector]
		public int _channelMask = -1;

		private NativeSpatDecoderQueue _spatQueue;
		private ChannelMap _channelMap = ChannelMap.STEREO;
		private int _channels = 2;
		// re-usable data array
		private float[] _spatialData;
		// in order to remap from Unity sample rate to spatial audio sample rate
		private int _unitySampleRate;
		private int _tbeSampleRate;

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

			if (_audioSource == null) {
				_audioSource = this.GetComponent<AudioSource>();
			}

			if (_mediaPlayer == null) {
				_mediaPlayer = this.GetComponent<MediaPlayer>();
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
		}

		void Update()
		{
			if (_mediaPlayer != null && _mediaPlayer.Control != null && _mediaPlayer.Control.IsPlaying())
			{
				ApplyAudioSettings(_mediaPlayer, _audioSource, _spatQueue);
			}
		}

		public void ChangeMediaPlayer(MediaPlayer newPlayer)
		{
			// When changing the media player, handle event subscriptions
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Events.RemoveListener(OnMediaPlayerEvent);
				_mediaPlayer = null;
			}

			_mediaPlayer = newPlayer;
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
			}
		}

		public void SetChannels(int channels, ChannelMap channelMap) 
		{
			_channels = channels;
			_channelMap = channelMap;
		}

		// Callback function to handle events
		private void OnMediaPlayerEvent(MediaPlayer mp, RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType et, ErrorCode errorCode)
		{
			switch (et)
			{
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.Closing:
					Debug.Log("[AVPSpatialAudioOutput] Stopping audio sources");

					_audioSource.Stop();
					_spatQueue.stop();
					break;
				case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.Started:
					Debug.Log("[AVPSpatialAudioOutput] Starting audio sources");

					ApplyAudioSettings(_mediaPlayer, _audioSource, _spatQueue);
					_audioSource.Play();
					_spatQueue.play();
					break;
			}
		}

		private static void ApplyAudioSettings(MediaPlayer player, AudioSource audioSource, NativeSpatDecoderQueue spatQueue)
		{
			// Apply volume and mute from the MediaPlayer to the AudioSource
			if (player != null && player.Control != null && spatQueue != null)
			{
				float volume = player.Control.GetVolume();
				bool isMuted = player.Control.IsMuted();
				float rate = player.Control.GetPlaybackRate();
				audioSource.volume = volume;
				// always mute placeholder AudioSource
				audioSource.mute = true;
				audioSource.pitch = rate;

				spatQueue.setVolume(isMuted ? 0f : volume, 0);
			}
		}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA_10_0 || UNITY_WINRT_8_1
		void OnAudioFilterRead(float[] data, int channels)
		{
			// Debug.Log($"[AVPSpatialAudioOutput] Requested {channels} ({_unitySampleRate}) channels with channel map {_channelMap} ({_channels}) and {data.Length} samples total");

			if (_mediaPlayer == null || _mediaPlayer.Control == null || !_mediaPlayer.Control.IsPlaying() || _spatQueue == null)
			{
				// Debug.Log("[AVPSpatialAudioOutput] AVProVideo media player not ready");
				return;
			}

			// int mediaPlayerChannels = _mediaPlayer.Control.GetNumAudioChannels();
			// if (mediaPlayerChannels != _channels) {
			// 	Debug.LogWarning($"[AVPSpatialAudioOutput] media player detected different channel count ({mediaPlayerChannels}) from app metadata ({_channels})");
			// }

			// convert data request by Unity to spatial audio channel layout (re-calculate sample count)
			int desiredSamples = (_channels * _tbeSampleRate * data.Length) / (channels * _unitySampleRate);
			// create re-used data array if one with the right size does not exist
			if (_spatialData == null || _spatialData.Length != desiredSamples) {
				// Debug.Log($"[AVPSpatialAudioOutput] Resizing spatial data array to {desiredSamples}");

				_spatialData = new float[desiredSamples];
			}

			_mediaPlayer.Control.GrabAudio(_spatialData, _spatialData.Length, _channels);

			// AudioOutputManager.Instance.RequestAudio(this, _mediaPlayer, _spatialData, _channelMask, _channels, _audioOutputMode);

			for (int i=0; i < _spatialData.Length; ++i) {
				if (_spatialData[i] != 0) {
					continue;
				}
			}

			// int queueFree = _spatQueue.getFreeSpaceInQueue(_channelMap);
			// Debug.Log($"[AVPSpatialAudioOutput] Spatial Decoder has {queueFree} samples free space");

			int enqueued = _spatQueue.enqueueData(_spatialData, _spatialData.Length, _channelMap);

			// Debug.Log($"[AVPSpatialAudioOutput] Spatial Decoder enqueued {enqueued} samples of {_spatialData.Length} total\nSpatial decoder is playing: {_spatQueue.getPlayState()}");
		}
#endif
	}
}

#endif