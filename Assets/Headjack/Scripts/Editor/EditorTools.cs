// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Text;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Headjack.EditorTools
{
    [InitializeOnLoad]
    public class TextBitmapScaler : EditorWindow
    {
        [MenuItem("Headjack/Tools/Font Bitmap Scaler")]
        static void Init()
        {
            TextBitmapScaler window = (TextBitmapScaler)GetWindow(typeof(TextBitmapScaler));
            window.titleContent.text = "Bitmap Scaler";
            window.Show();
        }
        private static TextMesh t;
        private static Font f;
        void OnGUI()
        {
            GUILayout.Label("TextMesh");
            t = (TextMesh)EditorGUILayout.ObjectField(t, typeof(TextMesh), true);
            if (t != null)
            {
                Font newf = (Font)EditorGUILayout.ObjectField(t.font, typeof(Font), true);
                if (newf != f)
                {
                    f = newf;
                    t.font = f;
                    t.GetComponent<MeshRenderer>().material = f.material;
                }
                float total = t.characterSize * (t.fontSize * 1f);
                GUILayout.Label("Bitmap Resolution");
                t.fontSize = EditorGUILayout.IntSlider(t.fontSize, 1, 256);
                t.characterSize = total / (t.fontSize * 1f);
            }
        }
    }
}   