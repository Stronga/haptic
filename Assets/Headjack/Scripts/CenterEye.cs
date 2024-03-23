// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack
{
    internal class CenterEye : MonoBehaviour
    {
        // public Transform LeftEye, RightEye;
        public bool FullscreenMode = false;
        public GameObject touchHelper;
#if UNITY_STANDALONE || UNITY_EDITOR
        internal Vector3 euler;
#endif
    }
}
