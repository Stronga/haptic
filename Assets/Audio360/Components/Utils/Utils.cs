// Copyright (c) 2018-present, Facebook, Inc. 


using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Facebook.Audio
{
    public class Const
    {
        public static string DEFAULT_GROUP_COLLECTION = "Default Group Collection";
        public static string DEFAULT_GROUP = "Default";

    }
}

namespace TBE
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EventTransportMessage
    {
        public EventTransportMessageType type;
        public ulong engineTimeSamples;
        public ulong posSamples;
        public byte channel;
        public byte id;
        public float value;
    }

    public delegate void EventTransportCallback(EventTransportMessage msg, System.IntPtr u);
    public delegate void EventCallback(Event e, global::System.IntPtr u);
    public delegate void AudioInterleavedCallback(System.IntPtr floatInterleavedAudio,
                                                  uint numChannels,
                                                  uint numSamplesPerChannel,
                                                  global::System.IntPtr userData);
    public delegate void AudioObjectBufferCallback(System.IntPtr floatInterleavedAudio,
                                                  uint numSamplesInAllChannels, 
                                                  uint numChannels,
                                                  global::System.IntPtr userData);

    public enum LoggerVerbosity
    {
        ALL,
        WARNINGS_AND_ERRORS,
        ERRORS,
        INFO
    }

    public enum SampleRate
    {
        SR_44100,
        SR_48000
    }

    public enum PathType
    {
        STREAMING_ASSETS,
        ABSOLUTE
    }

    public struct RuntimeOptions
    {
        public bool useFBA;
        public bool useVoiceVirtualization;
        
        // Non-FBA
        public int audioObjectPoolSize;
        public int spatDecoderFilePoolSize;
        public int spatQueuePoolSize;

        /// <summary>
        /// Create a RuntimeOptions struct (used in AudioManager/Boostrapper initialization) to setup the engine to work
        /// in legacy mode.
        /// </summary>
        /// <param name="objectPoolSize">Number of <code>NativeAudioObject</code> in the pool</param>
        /// <param name="spatFilePoolSize">Number of <code>NativeSpatDecoderFile</code> in the pool</param>
        /// <param name="queuePoolSize">Number of <code>NativeSpatDecoderQueue</code> in the pool</param>
        /// <returns></returns>
        public static RuntimeOptions Create(int objectPoolSize, int spatFilePoolSize, int queuePoolSize)
        {
            return new RuntimeOptions
            {
                useFBA = false, useVoiceVirtualization = false, audioObjectPoolSize = objectPoolSize,
                spatQueuePoolSize = queuePoolSize, spatDecoderFilePoolSize = spatFilePoolSize
            };
        }
    }

    public class Utils
    {
#if UNITY_IOS && !UNITY_EDITOR
        public const string DLL = "__Internal";
#else
        public const string DLL = "Audio360CSharp";
#endif

        // Do not change, unless you know what you are doing.
        public static float kFocusMin = -24.0f;
        public static float kFocusMax = 0.0f;
        public static float kFocusWidthMin = 40.0f;
        public static float kFocusWidthMax = 120.0f;
        
        // Change these if you need to tweak the queue sizes
        // for the messages coming from the audio engine,
        // which are moved over to the Unity main thread.
        public const uint kEventQueueSize = 32;
        public const uint kTransportEventQueueSize = 32;

        // Used to store the switch name in a behavior's context
        public const string kSwitchBehaviorContextKey = "Switch_CurrentAudioFile"; 

#if UNITY_STANDALONE_LINUX
        public static LoggerVerbosity logVerbosity = LoggerVerbosity.ERRORS;
#else
        public static LoggerVerbosity logVerbosity = LoggerVerbosity.WARNINGS_AND_ERRORS;
#endif

        static public Vector3 toVector3(TBVector vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }

        static public TBVector toTBVector(Vector3 vector)
        {
            return new TBVector(vector.x, vector.y, vector.z);
        }

        static public Quaternion toQuaternion(TBQuat quat)
        {
            return new Quaternion(quat.x, quat.y, quat.z, quat.w);
        }

        static public TBQuat toTBQuat(Quaternion quat)
        {
            return new TBQuat(quat.x, quat.y, quat.z, quat.w);
        }

        static public void logError(string messg, Object context)
        {
            if (logVerbosity != LoggerVerbosity.INFO)
            {
                Debug.LogError("[FBAudio] " + messg, context);
            }
        }

        static public void logWarning(string messg, Object context)
        {
            if (logVerbosity != LoggerVerbosity.ERRORS && logVerbosity != LoggerVerbosity.INFO)
            {
                Debug.LogWarning("[FBAudio] " + messg, context);
            }
        }

        static public void log(string messg, Object context)
        {
            if (logVerbosity == LoggerVerbosity.ALL || logVerbosity == LoggerVerbosity.INFO)
            {
                Debug.Log("[FBAudio] " + messg, context);
            }
        }

        static public float toSampleRateFloat(SampleRate sr)
        {
            switch (sr)
            {
                case SampleRate.SR_44100:
                    return 44100.0f;
                case SampleRate.SR_48000:
                    return 48000.0f;
                default:
                    return 48000.0f;
            }
        }

        static public SampleRate toSampleRateEnum(float sr)
        {
            if (sr == 44100.0f)
            {
                return SampleRate.SR_44100;
            }
            else if (sr == 48000.0f)
            {
                return SampleRate.SR_48000;
            }
            else
            {
                Utils.logError("Incompatible sample rate " + sr + ". Defaulting to 48000", null);
                return SampleRate.SR_48000;
            }
        }

        /// Resolve streaming assets path.
        static public string resolvePath(string path, PathType type)
        {
            if (type == PathType.STREAMING_ASSETS)
            {
                var streamingAssetsPath = Application.streamingAssetsPath;
                if (streamingAssetsPath.Contains("apk!") && Application.platform == RuntimePlatform.Android)
                {
                    streamingAssetsPath = "asset:///";
                }
                streamingAssetsPath = Path.Combine(streamingAssetsPath, path);
                return streamingAssetsPath;
            }

            return path;
        }

        static public float decibelsToLinear(float db)
        {
            return Mathf.Pow(10.0f, (db * 0.05f));
        }

        static public float linearToDecibels(float linear)
        {
            return 20.0f * Mathf.Log10(linear);
        }

        static public long msToSamps(float ms, float sampleRate)
        {
            return (long)(ms * (sampleRate * 0.001f));
        }

        static public float sampsToMs(long samples, float sampleRate)
        {
            if (sampleRate <= 0)
            {
                return 0;
            }
            return (samples) / (sampleRate * 0.001f);
        }

        static public bool isAmbisonic(ChannelMap map)
        {
            switch (map)
            {
                case ChannelMap.TBE_8_2:
                case ChannelMap.TBE_8:
                case ChannelMap.TBE_6_2:
                case ChannelMap.TBE_6:
                case ChannelMap.TBE_4_2:
                case ChannelMap.TBE_4:
                case ChannelMap.AMBIX_4:
                case ChannelMap.AMBIX_4_2:
                case ChannelMap.AMBIX_9:
                case ChannelMap.AMBIX_9_2:
                case ChannelMap.AMBIX_16:
                case ChannelMap.AMBIX_16_2:
                    return true;
                default:
                    return false;
            }
        }

        static public double usToMs(uint us)
        {
            return us * 0.001;
        }

        static public uint msToUs(double ms)
        {
            return (uint)(ms * 1000);
        }

        static public float getDistanceAttenuationGain(float distance,
                                                       float minDistance,
                                                       float maxDistance,
                                                       float factor,
                                                       bool maxMute,
                                                       AttenuationMode mode,
                                                       AnimationCurve customAtenuationCurve)
        {
            float result = 1.0f;
            float smallNumber = 1E-8f;

            distance = Mathf.Max(smallNumber, distance);
            minDistance = Mathf.Max(smallNumber, minDistance);
            maxDistance = Mathf.Max(smallNumber, maxDistance);
            factor = Mathf.Max(0.01f, factor);

            switch (mode)
            {
                case AttenuationMode.LOGARITHMIC:
                    {
                        if (distance <= minDistance)
                        {
                            result = 1.0f;
                        }
                        else if (distance >= maxDistance)
                        {
                            if (maxMute)
                            {
                                result = 0.0f;
                            }
                            else
                            {
                                result = Mathf.Pow((minDistance / maxDistance), factor);
                            }
                        }
                        else if (distance <= maxDistance)
                        {
                            result = Mathf.Pow((minDistance / distance), factor);
                        }
                    }
                    break;
                case AttenuationMode.LINEAR:
                    {
                        if (distance <= minDistance)
                        {
                            result = 1.0f;
                        }
                        else if (distance >= maxDistance)
                        {
                            result = 0.0f;
                        }
                        else if (distance <= maxDistance)
                        {
                            result = 1 - (distance / maxDistance);
                        }
                    }
                    break;
                case AttenuationMode.CUSTOM:
                    {
                        result = Mathf.Clamp01(customAtenuationCurve.Evaluate(distance));
                    }
                    break;
                default:
                    break;
            }
            result = Mathf.Min(1.0f, result);
            return result;
        }
    }
}
