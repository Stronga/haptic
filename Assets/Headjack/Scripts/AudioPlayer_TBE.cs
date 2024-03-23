// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using TBE;
using System;

namespace Headjack
{
    internal class AudioPlayer_TBE : AudioPlayer
    {
        private string path_ = "";
        private SpatDecoderFile decoder;
        private PlayState prePausedState;
        // private Transform leftEye;
        private bool firstPlayCalled = false;

        public static ChannelMap GetChannelMapFromChannelCount(int channels,string channelMap)
        {
            //string channelMap = "STEREO";
            // use default mapping or matching channelMap
            switch (channels)
            {
                case 1:
                    switch (channelMap)
                    {
                        case "HEADLOCKED_CHANNEL0":
                            return ChannelMap.HEADLOCKED_CHANNEL0;
                        case "HEADLOCKED_CHANNEL1":
                            return ChannelMap.HEADLOCKED_CHANNEL1;
                        case "TBE_CHANNEL0":
                            return ChannelMap.TBE_CHANNEL0;
                        case "TBE_CHANNEL1":
                            return ChannelMap.TBE_CHANNEL1;
                        case "TBE_CHANNEL2":
                            return ChannelMap.TBE_CHANNEL2;
                        case "TBE_CHANNEL3":
                            return ChannelMap.TBE_CHANNEL3;
                        case "TBE_CHANNEL4":
                            return ChannelMap.TBE_CHANNEL4;
                        case "TBE_CHANNEL5":
                            return ChannelMap.TBE_CHANNEL5;
                        case "TBE_CHANNEL6":
                            return ChannelMap.TBE_CHANNEL6;
                        case "TBE_CHANNEL7":
                            return ChannelMap.TBE_CHANNEL7;
                        case "MONO":
                        default:
                            return ChannelMap.INVALID;
                    }
                case 2:
                    switch (channelMap)
                    {
                        case "STEREO":
                        case "HEADLOCKED_STEREO":
                            return ChannelMap.HEADLOCKED_STEREO;
                        case "TBE_8_PAIR0":
                            return ChannelMap.TBE_8_PAIR0;
                        case "TBE_8_PAIR1":
                            return ChannelMap.TBE_8_PAIR1;
                        case "TBE_8_PAIR2":
                            return ChannelMap.TBE_8_PAIR2;
                        case "TBE_8_PAIR3":
                            return ChannelMap.TBE_8_PAIR3;
                        default:
                            return ChannelMap.INVALID; // let exoplayer handle regular STEREO tracks
                    }
                case 4:
                    switch (channelMap)
                    {
                        case "TBE_4":
                            return ChannelMap.TBE_4;
                        case "AMBIX_4":
                        default:
                            return ChannelMap.AMBIX_4;
                    }
                case 6:
                    switch (channelMap)
                    {
                        case "TBE_4_2":
                            return ChannelMap.TBE_4_2;
                        case "TBE_6":
                        default:
                            return ChannelMap.TBE_6;
                    }
                case 8:
                    switch (channelMap)
                    {
                        case "TBE_6_2":
                            return ChannelMap.TBE_6_2;
                        case "TBE_8":
                        default:
                            return ChannelMap.TBE_8;
                    }
                case 9:
                    return ChannelMap.AMBIX_9;
                case 10:
                    return ChannelMap.TBE_8_2;
                case 11:
                    return ChannelMap.AMBIX_9_2;
                default:
                    return ChannelMap.INVALID;

            }
        }

        internal void Awake()
        {
            Initialize();
            // if (leftEye == null)
            // {
            //     leftEye = App.camera.GetComponent<CameraInformation>().Left.transform;
            // }
        }

        protected override void SetVolume(float vol)
        {
            decoder.volume = vol;
        }
        protected override float GetVolume()
        {
            return decoder.volume;
        }

        protected override string GetPath()
        {
            return path_;
        }

        private void Initialize()
        {
            firstPlayCalled = false;

            // initialize Audio360 (once)
            if (FindObjectsOfType(typeof(Facebook.Audio.FBAudio360Bootstrapper)).Length <= 0)
            {
                const int objectPoolSize = 64;
                const int spatFilePoolSize = 4;
                const int spatQueuePoolSize = 2;
                Facebook.Audio.FBAudio360Bootstrapper.Init(null, RuntimeOptions.Create(objectPoolSize, spatFilePoolSize, spatQueuePoolSize), SampleRate.SR_48000, AudioDeviceType.DEFAULT);
            }

            GameObject TBEObject = GameObject.Find("TBAudioEngine");

            if (TBEObject == null)
            {
                TBEObject = new GameObject("TBAudioEngine");
            }
            if (decoder == null)
            {
                decoder = TBEObject.AddComponent<SpatDecoderFile>();
            }
        }

        internal override void Load(string FullPath)
        {

            Initialize();


			if (FullPath.Contains("STREAMINGASSET:"))
			{
				FullPath = FullPath.Replace("STREAMINGASSET:", "");
			}
            path_ = FullPath;
			decoder.open(FullPath);

#if UNITY_ANDROID && !UNITY_EDITOR
            SetVolume(1f);
            decoder.volume=1f;
#else
            SetVolume(1f);
#endif
            decoder.syncMode = SyncMode.EXTERNAL;
            decoder.setFreewheelTimeInMs(250.0f);
            decoder.setResyncThresholdMs(100f);
            decoder.useObjectRotation = false;
            decoder.customTransform = FindObjectOfType<AudioListener>().transform;
        }

		internal override void Pause()
        {
            decoder.pause();
        }

        internal override void Play()
        {
			Debug.Log("TBE Play Called");
            SetVolume(GetVolume());
            decoder.play();
            SetVolume(GetVolume());
		}

        internal override void Stop()
        {
            decoder.stop();
            firstPlayCalled = false;
        }

        internal override void Close()
        {
            decoder.close();
        }

        internal override void Sync(float ms)
        {
            if (!firstPlayCalled && ms > 0 && GetState() == State.STOPPED && decoder.isOpen())
            {
				SetVolume(GetVolume());

                decoder.play();
                SetVolume(GetVolume());
                firstPlayCalled = true;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            ms += 250f;
#endif

            // Debug.Log($"TBE audio sync to {ms}");

            decoder.setExternalClockInMs(ms);
        }

        internal override State GetState()
        {
            switch (decoder.getPlayState())
            {
                case PlayState.PAUSED:
                    return State.PAUSED;
                case PlayState.PLAYING:
                    return State.PLAYING;
                case PlayState.STOPPED:
                    return State.STOPPED;
                case PlayState.INVALID:
                default:
                    return State.INVALID;
            }
        }

        // internal void Update()
        // {
        //     if (leftEye == null)
        //     {
        //         leftEye = App.camera.GetComponent<CameraInformation>().Left.transform;
        //     }
		// 	//Debug.Log(decoder.getPlayState().ToString()+" - openL: "+decoder.isOpen());
		// 	//Debug.Log("TBE position " + decoder.getElapsedTimeInMs());
        // }

        internal override void Destroy()
        {
            if (decoder != null) {
                decoder.stop();
                decoder.close();
            }

            firstPlayCalled = false;

            GameObject TBEObject = GameObject.Find("TBAudioEngine");

            if (TBEObject != null)
            {
                Destroy(TBEObject);
            }
        }

        internal void OnApplicationPause(bool paused)
        {
            Debug.Log("TBE paused: " + paused);
            if (decoder != null)
            {
                if (paused)
                {
                    prePausedState = decoder.getPlayState();
                    if (prePausedState == PlayState.PLAYING)
                    {
                        Pause();
                    }
                }
                else
                {
                    if (prePausedState == PlayState.PLAYING)
                    {
                        Play();
                    }
                    prePausedState = PlayState.INVALID;
                }
            }
        }

        internal override long GetSeekMs()
        {
            return Convert.ToInt64(decoder.getElapsedTimeInMs());
        }

        internal override void SetSeekMs(long seek)
        {
            decoder.seekToMs((float)seek);
        }

        protected void OnDestroy()
        {
            Destroy();
        }
    }

}
