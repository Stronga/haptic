// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack.Settings
{
    [System.Serializable]
    public class TemplateSettings : ScriptableObject
    {
        public enum CBOrientation
        {
            Portrait = 0,
            PortraitUpsideDown = 1,
            LandscapeRight = 2,
            LandscapeLeft = 3,
            AutoRotation = 4
        }
        public CBOrientation Orientation;
        public bool CinemaSupported;
    }

}
