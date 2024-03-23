using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Headjack;

public class VrPhysicsRaycaster : BaseRaycaster {

    public App.RaycastSource inputRaySource = App.RaycastSource.MotionController;

    private Camera _eventCam;

    public override Camera eventCamera {
        get {
            if (_eventCam == null) {
                _eventCam = GetComponent<Camera>();
            }
            if (_eventCam == null) {
                _eventCam = Camera.main;
            }
            // if (_eventCam == null) {
            //     _eventCam = Camera.current;
            // }
            return _eventCam;
        }
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
        RaycastHit hitInfo;
        if (inputRaySource == App.RaycastSource.MotionController && VRInput.MotionControllerAvailable) {
            hitInfo = App.LaserHit;
        } else {
            hitInfo = App.CrosshairHit;
        }

        if (hitInfo.collider != null) {
            RaycastResult raycastResult = new RaycastResult {
                gameObject = hitInfo.collider.gameObject,
                module = this,
                distance = hitInfo.distance,
                worldPosition = hitInfo.point,
                worldNormal = hitInfo.normal,
                screenPosition = eventCamera.WorldToScreenPoint(hitInfo.point),
                index = resultAppendList.Count,
                sortingLayer = 0,
                sortingOrder = 0
            };

            resultAppendList.Add(raycastResult);
        }
    }
}
