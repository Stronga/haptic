// Copyright (c) 2018-present, Facebook, Inc. 


using UnityEditor;
using UnityEngine;

namespace TBE
{
    [CustomEditor(typeof (AudioObject))]
    [CanEditMultipleObjects]
    public class AudioObjectEditor : Editor
    {
        SerializedProperty file;
        SerializedProperty playOnStart;
        SerializedProperty loop;
        SerializedProperty volume;
        SerializedProperty pitch;
        SerializedProperty attenMode;
        SerializedProperty minimumDist;
        SerializedProperty maximumDist;
        SerializedProperty attenFactor;
        SerializedProperty directionality;
        SerializedProperty directionalityLevel;
        SerializedProperty directionalityConeArea;
        SerializedProperty spatialise;
        SerializedProperty maxDistMute;

        void OnEnable()
        {
            file = serializedObject.FindProperty("file");
            playOnStart = serializedObject.FindProperty("playOnStart");
            loop = serializedObject.FindProperty("loop_");
            volume = serializedObject.FindProperty("volume_");
            pitch = serializedObject.FindProperty("pitch_");
            attenMode = serializedObject.FindProperty("attenMode_");
            minimumDist = serializedObject.FindProperty("minDistance_");
            maximumDist = serializedObject.FindProperty("maxDistance_");
            attenFactor = serializedObject.FindProperty("attenFactor_");
            directionality = serializedObject.FindProperty("directionality_");
            directionalityLevel = serializedObject.FindProperty("directionalityLevel_");
            directionalityConeArea = serializedObject.FindProperty("directionalityConeArea_");
            spatialise = serializedObject.FindProperty("spatialise_");
            maxDistMute = serializedObject.FindProperty("maxDistanceMute_");
        }

        public override void OnInspectorGUI()
        {
            AudioObject ob = (AudioObject) target;
            serializedObject.Update();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(file, new GUIContent("File"));
            EditorGUILayout.PropertyField(playOnStart, new GUIContent("Play On Start"));
            EditorGUILayout.PropertyField(loop, new GUIContent("Loop"));
            EditorGUILayout.Slider(volume, 0.0f, 1.5f, "Volume");
            EditorGUILayout.Slider(pitch, 0.001f, 4f, "Pitch");

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(spatialise, new GUIContent("Spatialise"));
            EditorGUI.BeginDisabledGroup(!ob.spatialise);

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(attenMode, new GUIContent("Attenuation Mode"));
            EditorGUILayout.PropertyField(minimumDist, new GUIContent("Minimum Distance"));
            EditorGUILayout.PropertyField(maximumDist, new GUIContent("Maximum Distance"));
            EditorGUI.BeginDisabledGroup(ob.attenMode != AttenuationMode.LOGARITHMIC);
            EditorGUILayout.PropertyField(maxDistMute, new GUIContent("Max Distance Mute"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(attenFactor, new GUIContent("Atten Factor"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(directionality, new GUIContent("Directionality"));
            EditorGUI.BeginDisabledGroup(!ob.directionality);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.Slider(directionalityLevel, 0.0f, 1.0f, new GUIContent("Level"));
            EditorGUILayout.Slider(directionalityConeArea, 0.0f, 359.0f, new GUIContent("Cone Area"));
            EditorGUI.indentLevel = 0;
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup(); // spatialise

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying)
            {
                ob.ForceUpdateProps();
            }
        }
    }
}
