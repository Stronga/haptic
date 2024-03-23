// Copyright (c) 2018-present, Facebook, Inc. 


using UnityEngine;

namespace TBE
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour, IObject
    {
        private static object lock_ = new object();
        private static T instance_;
        static bool onDestroyCalled_ = false;

        public static T Instance
        {
            get
            {
                lock (lock_)
                {
                    if (onDestroyCalled_)
                    {
                        return null;
                    }

                    if (instance_ == null)
                    {
                        instance_ = (T)FindObjectOfType(typeof(T));
                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Utils.logError("There is more than one instance of " + typeof(T).ToString() + ". Restart your scene?", null);
                            return instance_;
                        }
                        if (instance_ == null)
                        {
                            GameObject singleton = new GameObject();
                            instance_ = singleton.AddComponent<T>();
                            singleton.name = "[" + typeof(T).ToString() + "]";
                            if (instance_.mustNotDestroyOnLoad())
                            {
                                if (Application.isPlaying)
                                {
                                    DontDestroyOnLoad(singleton);
                                }
                            }
                            instance_.onInit();
                        } 
                    }
                    return instance_;
                }
            }
        }

        public static void forceResetState()
        {
            onDestroyCalled_ = false;
            instance_ = null;

        }

        void OnDestroy()
        {
            if (instance_ != null)
            {
                instance_.onTerminate();
            }
            onDestroyCalled_ = true;
        }
    }
}
