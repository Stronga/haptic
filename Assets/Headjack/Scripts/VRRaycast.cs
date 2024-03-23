// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack
{
    internal class VRRaycast : MonoBehaviour
    {
        void Update()
        {
			Physics.Raycast (transform.position, transform.forward, out App.CrosshairHit);
            if (Camera.main != null) {
                Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out App.TouchHit);
            }
        }
    }
}
