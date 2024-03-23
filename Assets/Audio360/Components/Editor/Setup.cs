#define FBA_EXTERNAL
// Copyright (c) 2018-present, Facebook, Inc. 


using UnityEngine;
using UnityEditor;
using Facebook.Audio;

namespace TBE
{
#if FBA_EXTERNAL
    using AudioBootstrapper = FBAudio360Bootstrapper;
#else
    using AudioBootstrapper = FBAudioBootstrapper;
#endif
    
    public class Setup : EditorWindow
    {
        public static Setup Instance { get; private set; }

        static Vector2 windowSize = new Vector2(300, 150);

        [MenuItem("Edit/FB Audio/Setup")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:     
            Instance = (Setup)GetWindow(typeof(Setup));
            Instance.ShowUtility();
            var content = new GUIContent {text = "Setup"};
            Instance.titleContent = content; 
            Instance.minSize = windowSize;
            Instance.maxSize = windowSize;
        }

        private void OnGUI()
        {   
            EditorGUILayout.Space();

            GUILayout.Label("FB Audio360 — Project Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Scene"))
            {
                CreateEngineObject();
            }
        }

        private static void CreateEngineObject()
        {   
            // Make sure the scene's not already set up.
            // If it is, leave it alone
            if (null != FindObjectOfType<AudioBootstrapper>())
            {
                Debug.LogWarning("Scene is already set up. Aborting.");
                return;
            }
            
            const string bsName = "[FBAudio] Bootstrapper";
            var go = new GameObject(bsName, typeof(AudioBootstrapper));
            var bootstrapper = go.GetComponent<AudioBootstrapper>();
            
            // This should never actually happen, but let's put this here to detect regressions.
            if (null == bootstrapper)
            {
                Debug.LogError("Failed to set up scene.");
            }
        }
    }
}
