// Copyright (c) 2018-present, Facebook, Inc. 


using System;
using UnityEngine;

namespace TBE
{
    /// <summary>
    /// Manage the audio engine.
    /// It should be used via the FBAudioBootstrapper or the soon to be deprecated AudioEngineManager.Instance.
    /// </summary>
    public class AudioEngineManager
    {
        public struct Statistics
        {
            public double audioCallbackTimeMs;
            public double decoderThreadTimeMs;
            public uint numActiveAudioObjects;
            public uint numAudioObjectsPlaying;
            public uint numActiveSpatDecoderQueues;
            public uint numSpatDecoderQueuesPlaying;
            public float assetMegabytesInMemory;
        }
    
        /// <summary>
        /// A helper class to hold a reference to AudioEngineManager. The reference is notified and set to null
        /// when the AudioEngineManager is destroyed.
        /// Any objects that depend on the lifetime of AudioEngineManager should access it via this Client object. A new
        /// instance can be created via AudioEngineManager.NewClient
        /// </summary>
        public class Client
        {
            public event EventHandler OnInvalid;
            private WeakReference manager_;
            private WeakReference nativeEngine_;

            internal Client(AudioEngineManager engineManager)
            {
                manager_ = new WeakReference(engineManager);
                nativeEngine_ = new WeakReference(engineManager.nativeEngine);
                engineManager.OnEngineDestroy += OnEngineDestroy;
            }

            ~Client()
            {
                if (manager_ == null || manager_.Target == null)
                {
                    return;
                }

                var man = manager_.Target as AudioEngineManager;
                if (man == null)
                {
                    return;
                }

                man.OnEngineDestroy -= OnEngineDestroy;
                manager_ = null;
                nativeEngine_ = null;
            }

            private void OnEngineDestroy(object sender, EventArgs e)
            {
                Debug.Assert(sender is AudioEngineManager);
                var man = sender as AudioEngineManager;
                man.OnEngineDestroy -= OnEngineDestroy;
                
                var handler = OnInvalid;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                manager_ = null;
                nativeEngine_ = null;
            }

            public bool GetManager(out AudioEngineManager engineManager)
            {
                engineManager = null;
                if (manager_ == null)
                {
                    return false;
                }

                engineManager = manager_.Target as AudioEngineManager;
                return true;
            }

            public bool GetNativeEngine(out AudioEngine engine)
            {
                engine = null;
                if (nativeEngine_ == null)
                {
                    return false;
                }

                engine = nativeEngine_.Target as AudioEngine;
                return true;
            }
        }

        private TBVector tempVector_;
        private TBVector tempVector2_;
        private event EventHandler OnEngineDestroy;
        private Statistics statistics_;
        private RuntimeOptions options_;

        public Client NewClient()
        {
            return new Client(this);
        }

        public void SetAudioListener(Vector3 listenerPos, Vector3 listenerForward, Vector3 listenerUp)
        {
            if (nativeEngine != null)
            {
                tempVector_.set(listenerPos.x, listenerPos.y, listenerPos.z);
                nativeEngine.setListenerPosition(tempVector_);
                tempVector_.set(listenerForward.x, listenerForward.y, listenerForward.z);
                tempVector2_.set(listenerUp.x, listenerUp.y, listenerUp.z);
                nativeEngine.setListenerRotation(tempVector_, tempVector2_);
            }
        }

        public void Update()
        {
            if (nativeEngine != null)
            {
                nativeEngine.update();
            }
        }

        public bool Init(RuntimeOptions options, SampleRate sampleRate, AudioDeviceType deviceType, string customDeviceName = "")
        {
            if (nativeEngine != null)
            {
                return true;
            }

            Debug.LogFormat(
                "[FB Audio] Trying to initialize engine with options: Using FBA: {0}, Using Virtualization: {1}, Sample Rate: {2}, Device: {3}",
                options.useFBA, options.useVoiceVirtualization, sampleRate, deviceType);

            tempVector_ = new TBVector();
            tempVector2_ = new TBVector();

            EngineInitSettings settings = new EngineInitSettings();
            settings.memorySettings.spatQueueSizePerChannel = 4096 * 4;
            settings.audioSettings.sampleRate = Utils.toSampleRateFloat(sampleRate);

            // Use the audio output from OVRManager.
            // This requires the Oculus Utilities package to be imported in your project.
#if AUDIO360_USE_OVR_AUDIO_OUT
            string ovrAudioGuid = OVRManager.audioOutId;
            if (!string.IsNullOrEmpty(ovrAudioGuid))
            {
                string deviceName = AudioEngine.getAudioDeviceNameFromId(ovrAudioGuid);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    deviceType = AudioDeviceType.CUSTOM;
                    customDeviceName = deviceName;
                    Debug.Log("[FB Audio] Using audio device from OVRManager: " + deviceName);
                }
            }
#endif
            settings.audioSettings.deviceType = deviceType;
#if UNITY_STANDALONE_LINUX
            settings.audioSettings.deviceType = AudioDeviceType.DISABLED;
#endif
            if (settings.audioSettings.deviceType == AudioDeviceType.CUSTOM)
            {
                settings.audioSettings.customAudioDeviceName = customDeviceName;
            }

            settings.memorySettings.audioObjectPoolSize = options.audioObjectPoolSize; 
            settings.memorySettings.spatDecoderFilePoolSize = options.spatDecoderFilePoolSize;
            settings.memorySettings.spatDecoderQueuePoolSize = options.spatQueuePoolSize;
            settings.experimental.useFba = options.useFBA;

            nativeEngine = null;
            nativeEngine = AudioEngine.create(settings);
            if (nativeEngine != null)
            {
                options_ = options;
                SampleRate = nativeEngine.getSampleRate();
                BufferSize = nativeEngine.getBufferSize();
                Debug.LogFormat("[FB Audio] Initialised Engine. {0} {1}. v{2}.{3}.{4}-{5}. [Output Device: {6}]", 
                    SampleRate, BufferSize, nativeEngine.getVersionMajor(), 
                    nativeEngine.getVersionMinor(), nativeEngine.getVersionPatch(), nativeEngine.getVersionHash(), 
                    nativeEngine.getOutputAudioDeviceName());
                nativeEngine.start();
                return true;
            }
            else
            {
                Debug.LogError("[FB Audio] Failed to initialise engine");
                return false;
            }
        }

        public void Destroy()
        {
            var handler = OnEngineDestroy;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            if (nativeEngine != null)
            {
                nativeEngine.Dispose();
                nativeEngine = null;
                Debug.Log("[FB Audio] Engine destroyed");
            }
        }

        public void Suspend(bool suspend)
        {
            if (nativeEngine != null)
            {
                if (suspend)
                {
                    nativeEngine.suspend();
                }
                else
                {
                    nativeEngine.start();
                }
            }
        }

        public int objectPoolSize
        {
            get { return options_.audioObjectPoolSize; }
        }

        public int spatFilePoolSize
        {
            get { return options_.spatDecoderFilePoolSize; }
        }

        public AudioEngine nativeEngine { get; private set; }

        private void UpdateStats()
        {
            if (nativeEngine != null)
            {
                var st = nativeEngine.getStats();
                statistics_.audioCallbackTimeMs = Utils.usToMs(st.audioCallbackTimeMicroSec);
                statistics_.decoderThreadTimeMs = Utils.usToMs(st.decoderThreadTimeMicroSec);
                statistics_.numActiveAudioObjects = st.numActiveAudioObjects;
                statistics_.numAudioObjectsPlaying = st.numAudioObjectsPlaying;
                statistics_.numActiveSpatDecoderQueues = st.numActiveSpatDecoderQueues;
                statistics_.numSpatDecoderQueuesPlaying = st.numSpatDecoderQueuesPlaying;

                var assetManager = nativeEngine.getAudioAssetManager();
                statistics_.assetMegabytesInMemory = assetManager.getBytesInMemory() / 1024.0f / 1024.0f;
            }
        }
        
        public Statistics Stats
        {
            get
            {
                UpdateStats();
                return statistics_;
            }
        }

        public float SampleRate { get; private set; }

        public int BufferSize { get; private set; }

        #region Static Methods — Backward Compat
        public static AudioEngineManager Instance { get; private set; }

        public static void SetStaticInstance(AudioEngineManager audioEngineManager)
        {
            Instance = audioEngineManager;
        }
        #endregion Static Methods — Backward Compat
    }
}
