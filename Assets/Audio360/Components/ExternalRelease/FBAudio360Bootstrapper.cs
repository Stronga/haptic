// Copyright (c) 2018-present, Facebook, Inc. 


using System.Collections;
using System.Collections.Generic;
using Facebook.Audio;
using TBE;
using UnityEngine;
using UnityEngine.Assertions;

namespace Facebook.Audio
{
    /// <summary>
    /// Add to a game object to initialize all the FB Audio managers. This script ensures that the managers
    /// are created and destroyed in the correct order.
    /// The object will function as a singleton.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class FBAudio360Bootstrapper : BootstrapperBase<FBAudio360Bootstrapper>
    {
        protected override void Awake()
        {
            if (init)
            {
                return;
            }

            const int objectPoolSize = 64;
            const int spatFilePoolSize = 4;
            const int spatQueuePoolSize = 2;
            InitInternal(RuntimeOptions.Create(objectPoolSize, spatFilePoolSize, spatQueuePoolSize), SampleRate.SR_48000,
                AudioDeviceType.DEFAULT);
        }

        
        protected override void InitInternal(RuntimeOptions options, SampleRate sampleRate, AudioDeviceType deviceType,
            string customDeviceName = "", uint maxPhysicalObjects = KDefaultMaxPhysicalObjects)
        {
            // Guard against cases where multiple instances might be in the scene (e.g scene transitions)
            if (FindObjectsOfType(typeof(FBAudio360Bootstrapper)).Length > 1)
            {
                init = false;
                gameObject.SetActive(false);
                return;
            }

            runtimeOptions = options;

            DontDestroyOnLoad(gameObject);

            audioEngineManager = new AudioEngineManager();
            audioEngineManager.Init(runtimeOptions, sampleRate, deviceType, customDeviceName);

            AudioEngineManager.SetStaticInstance(audioEngineManager);
            Assert.IsNotNull(AudioEngineManager.Instance);

            init = true;
        }

        private void OnDestroy()
        {
            if (!init)
            {
                return;
            }

            // The destruction order here is critical

            AudioEngineManager.SetStaticInstance(null);
            audioEngineManager.Destroy();
            audioEngineManager = null;
        }
    }
}
