// Copyright (c) 2018-present, Facebook, Inc. 


using TBE;
using UnityEngine;

namespace Facebook.Audio
{
    public abstract class BootstrapperBase<T> : MonoBehaviour where T : Component
    {
        protected const uint KDefaultMaxPhysicalObjects = 100;
        protected AudioEngineManager audioEngineManager;
        private AudioListener unityListener_ = null;
        private Vector3 listenerPos_ = Vector3.zero;
        private Vector3 listenerFwd_ = Vector3.forward;
        private Vector3 listenerUp_ = Vector3.up;
        private bool listenerErrorLogged_ = false;
        protected RuntimeOptions runtimeOptions;
        protected bool init = false;
        
        protected virtual void Awake()
        {
            if (init)
            {
                return;
            }

            InitInternal(new RuntimeOptions {useFBA = true, useVoiceVirtualization = true}, SampleRate.SR_48000,
                AudioDeviceType.DEFAULT);
        }
        
        protected virtual void Update()
        {
            if (!init)
            {
                return;
            }

            UpdatedListenerTransform();
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (!init)
            {
                return;
            }

            audioEngineManager.Suspend(pauseStatus);
        }

        private void UpdatedListenerTransform()
        {
            if (unityListener_ == null)
            {
                unityListener_ = FindFirstAudioListener();
                if (unityListener_ == null)
                {
                    if (!listenerErrorLogged_)
                    {
                        // Log this message only once
                        Utils.logWarning("Failed to find Unity's AudioListener. Audio will not be spatialised.", this);
                        listenerErrorLogged_ = true;
                    }
                    return;
                }
            }
            
            if (listenerErrorLogged_)
            {
                // Log once the listener is found (after a failure) to help with debugging when reading the log
                Debug.Log("[FBAudio] Found Unity's AudioListener!", this);
                listenerErrorLogged_ = false;   
            }

            var listenerTransform = unityListener_.transform;
            listenerPos_ = listenerTransform.position;
            listenerFwd_ = listenerTransform.forward;
            listenerUp_ = listenerTransform.up;
            audioEngineManager.SetAudioListener(listenerPos_, listenerFwd_, listenerUp_);
            audioEngineManager.Update();
        }

        private static AudioListener FindFirstAudioListener()
        {
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners == null || listeners.Length < 1)
            {
                return null;
            }

            return listeners[0];
        }

        protected abstract void InitInternal(RuntimeOptions options, SampleRate sampleRate,
            AudioDeviceType deviceType, string customDeviceName = "", uint maxPhysicalObjects = KDefaultMaxPhysicalObjects);

        /// <summary>
        /// Create and initialize the bootstrapper on a game object in cases where you might not want to use the
        /// prefab provided with the SDK. This method creates a child game object and marks at as "DontDestroyOnLoad".
        /// </summary>
        /// <param name="parentObject"></param>
        /// <param name="options"></param>
        /// <param name="sampleRate"></param>
        /// <param name="deviceType"></param>
        /// <param name="customDeviceName"></param>
        /// <param name="maxPhysicalObjects"></param>
        public static void Init(GameObject parentObject, RuntimeOptions options, SampleRate sampleRate,
            AudioDeviceType deviceType, string customDeviceName = "", uint maxPhysicalObjects = KDefaultMaxPhysicalObjects)
        {
            GameObject gameObject = new GameObject();
            DontDestroyOnLoad(gameObject);
            gameObject.name = "[FBAudio] Bootstrapper";
            if (parentObject != null)
            {
                gameObject.transform.parent = parentObject.transform;
            }
            
            gameObject.SetActive(false);
            var bootstrapper = gameObject.GetComponent<T>() as BootstrapperBase<T>;
            if (bootstrapper == null)
            {
                bootstrapper = gameObject.AddComponent<T>() as BootstrapperBase<T>;
            }

            if (bootstrapper != null)
            {
                bootstrapper.InitInternal(options, sampleRate, deviceType, customDeviceName, maxPhysicalObjects);    
            }
            else
            {
                Utils.logError("Failed to instantiate bootstrapper", gameObject);
            }
            gameObject.SetActive(true);
        }
        
        public RuntimeOptions RuntimeOptions
        {
            get { return runtimeOptions; }
        }
        
        public AudioEngineManager AudioEngineManager
        {
            get { return audioEngineManager; }
        }
    }
}
