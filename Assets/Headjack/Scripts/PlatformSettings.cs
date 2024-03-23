// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System;
using UnityEngine;

namespace Headjack.Settings
{
    [System.Serializable]
    public class PlatformSettings : ScriptableObject
    {
        public enum BuildPlatform 
        {
            Oculus=0,
            OpenVR=1,
            Cardboard=2,
            [ObsoleteAttribute("Google has deprecated the Daydream VR platform.", false)]
            Daydream=3,
            [ObsoleteAttribute("PlayStation VR is a closed platform. Contact support for more information.", false)]
            PlaystationVR=4,
            [ObsoleteAttribute("Windows MR platform supports OpenVR apps directly, so use OpenVR platform instead.", false)]
			WindowsMixedReality=5,
			ViveWave=6,
			Pico=7,
            OculusLegacy=8,
            PicoPlatform=9
        }

        public string appID, AUTHKey;
        public BuildPlatform Platform;
    }

}
