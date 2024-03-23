// Copyright (c) 2018-present, Facebook, Inc. 


using UnityEditor;
using UnityEngine;

namespace TBE
{
    [CustomEditor(typeof (SpatDecoderFile))]
    [CanEditMultipleObjects]
    public class SpatDecoderFileEditor : Editor
    {
        SerializedProperty file;
        SerializedProperty playOnStart;
        SerializedProperty loop;
        SerializedProperty volume;
        SerializedProperty syncMode;
        SerializedProperty focus;
        SerializedProperty offFocusLeveldB;
        SerializedProperty focusWidth;
        SerializedProperty useObjectRotation;

        void OnEnable()
        {
            file = serializedObject.FindProperty("file");
            playOnStart = serializedObject.FindProperty("playOnStart");
            loop = serializedObject.FindProperty("loop_");
            volume = serializedObject.FindProperty("volume_");
            syncMode = serializedObject.FindProperty("syncMode_");
            focus = serializedObject.FindProperty("focus_");
            offFocusLeveldB = serializedObject.FindProperty("offFocusLeveldB_");
            focusWidth = serializedObject.FindProperty("focusWidth_");
            useObjectRotation = serializedObject.FindProperty("useObjectRotation_");
        }

        public override void OnInspectorGUI()
        {
            SpatDecoderFile sp = (SpatDecoderFile) target;
            serializedObject.Update();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(file, new GUIContent("File"));
            EditorGUILayout.PropertyField(playOnStart, new GUIContent("Play On Start"));
            EditorGUILayout.PropertyField(loop, new GUIContent("Loop"));
            EditorGUILayout.Slider(volume, 0.0f, 1.5f, "Volume");
            EditorGUILayout.PropertyField(useObjectRotation, new GUIContent("Use Object Rotation", "Use the GameObject's rotation to rotate the ambisonic field"));
            EditorGUILayout.PropertyField(syncMode, new GUIContent("Sync Mode"));
            EditorGUILayout.PropertyField(focus, new GUIContent("Focus"));

            EditorGUI.BeginDisabledGroup(!sp.focus);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.Slider(offFocusLeveldB, Utils.kFocusMin, Utils.kFocusMax, "Off-Focus Level (dB)");
            EditorGUILayout.Slider(focusWidth, Utils.kFocusWidthMin, Utils.kFocusWidthMax, "Focus Width (deg)");

            EditorGUI.indentLevel = 0;
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying)
            {
                sp.updateProps();
            }
        }
    }
}
