// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
#if USE_AVPROVIDEO
using RenderHeads.Media.AVProVideo;
#endif

namespace Headjack {

	/// <summary>
	/// Check for codec support on device
	/// NOTE: requires AVPro platform compatibility
	/// </summary>
	public class CodecSupport {
		public enum Codec {
			HEVC = 0
		};

		public enum VideoApi {
			None = 0,
			MediaFoundation,
			DirectShow,
			AndroidMediaPlayer,
			ExoPlayer,
			GenericPlayer
		};

		public delegate void CodecSupported(bool supported, VideoApi videoApi);

		private static readonly string[] codecToVideo = {
			"Headjack/empty_hevc.mp4" // HEVC
		};

		/// <summary>
		/// Check device capability for playing back a specific video codec,
		/// using an actual video player.
		/// </summary>
		public static void CheckCodec(Codec codec, CodecSupported onCodecSupported) {
#if USE_AVPROVIDEO
			if ((int)codec >= codecToVideo.Length) {
				// no test video for this codec exists
				Debug.LogError($"[CodecSupport] Test video missing for codec \"{codec}\", cannot determine codec support");
				onCodecSupported(false, VideoApi.None);
				return;
			}

			// create temporary video player
			GameObject codecCheckObj = new GameObject("[temp] CodecCheckPlayer");
			GameObject.DontDestroyOnLoad(codecCheckObj);
			// codecCheckObj.SetActive(false);
			MediaPlayer mediaPlayer = codecCheckObj.AddComponent<MediaPlayer>();
			// listen to video player events
			mediaPlayer.Events.AddListener(delegate (MediaPlayer mp,  RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType et, ErrorCode code) {
				// call callback on success or failed playback event
				switch (et) {
					case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.Error:
						// failure
						DestroyPlayer(mediaPlayer);
						onCodecSupported(false, GetVideoApi(mp.GetCurrentPlatformOptions()));
						break;
					case RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.FirstFrameReady:
						// success
						DestroyPlayer(mediaPlayer);
						onCodecSupported(true, GetVideoApi(mp.GetCurrentPlatformOptions()));
						break;
				}
			});
			// prepare and play test video
			mediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder, codecToVideo[(int)codec], true);
#else
			onCodecSupported(true, VideoApi.None);
#endif
		}

#if USE_AVPROVIDEO
		/// stops test playback, destroys videoplayer and removes temporary gameobject
		private static void DestroyPlayer(MediaPlayer mediaPlayer) {
			if (mediaPlayer == null) {
				return;
			}
			// remove player event listeners to avoid repeated events/destroys
			mediaPlayer.Events.RemoveAllListeners();
			// stop and clean up playback
			mediaPlayer.Stop();
			mediaPlayer.CloseVideo();
			GameObject.Destroy(mediaPlayer.gameObject);
			mediaPlayer = null;
		}
#endif

#if USE_AVPROVIDEO
		private static VideoApi GetVideoApi(MediaPlayer.PlatformOptions platformOptions) {
			if (platformOptions is MediaPlayer.OptionsAndroid) {
				switch (((MediaPlayer.OptionsAndroid)platformOptions).videoApi) {
					case Android.VideoApi.ExoPlayer:
						return VideoApi.ExoPlayer;
					case Android.VideoApi.MediaPlayer:
						return VideoApi.AndroidMediaPlayer;
				}
			} else if (platformOptions is MediaPlayer.OptionsWindows) {
				switch (((MediaPlayer.OptionsWindows)platformOptions).videoApi) {
					case Windows.VideoApi.DirectShow:
						return VideoApi.DirectShow;
					case Windows.VideoApi.MediaFoundation:
						return VideoApi.MediaFoundation;
				}
			}
			return VideoApi.GenericPlayer;
		}
#endif
	}
}
