// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using Headjack;

public class CameraManualStereo : MonoBehaviour
{
    public enum Eye {
        Left = 0,
        Right = 1
    }

    public Eye targetEye = Eye.Left;

    void OnPreRender() {
        if (App.Player != null && App.Player.ProjectionMesh != null && App.Player.ProjectionMesh.material != null) {
            App.Player.ProjectionMesh.material.SetInt("_StereoIndex", (int)targetEye);
        }
    }

    void OnPostRender() {
        if (App.Player != null && App.Player.ProjectionMesh != null && App.Player.ProjectionMesh.material != null) {
            App.Player.ProjectionMesh.material.SetInt("_StereoIndex", (int)Eye.Left);
        }
    }
}