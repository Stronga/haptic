using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Headjack;
using System;

public class VrInputModule : BaseInputModule {

    /// Time in seconds between the pointer down and up events sent by a trigger.
    /// Allows time for the UI elements to make their state transitions.
    private const float CLICK_TIME = 0.1f;
    // Based on default time for a button to animate to Pressed.

    private static Vector2 sphericalCoordinatesResult;
    private PointerEventData pointerData;
    private Vector2 lastPose;

    public override bool ShouldActivateModule() {
        return true;
    }

    /// @cond
    public override void DeactivateModule() {
        base.DeactivateModule();
        if (pointerData != null) {
            HandlePendingClick();
            HandlePointerExitAndEnter(pointerData, null);
            pointerData = null;
        }
        eventSystem.SetSelectedGameObject(null, GetBaseEventData());
    }
    /// @endcond

    /// @cond
    public override void Process() {

        // Save the previous Game Object
        GameObject previousObject = GetCurrentGameObject();

        CastRay();
        UpdateCurrentObject(previousObject);

        //if (pointerData != null && Time.unscaledTime - pointerData.clickTime < CLICK_TIME) {
        //    // delay subsequent click event
        //} else if (VRInput.Confirm.Pressed && !pointerData.eligibleForClick) {
        //    // New trigger action.
        //    HandleTriggerDown();
        //}
        //else if (!VRInput.Confirm.Hold) {
        //    // Check if there is a pending click to handle.
        //    HandlePendingClick();
        //}

        // Handle input
        if (!VRInput.Confirm.Pressed && VRInput.Confirm.Hold) {
            HandleDrag();
        }
        else if (VRInput.Confirm.Pressed && !pointerData.eligibleForClick) {
            // New trigger action.
            HandleTriggerDown();
        }
        else if (!VRInput.Confirm.Hold) {
            // Check if there is a pending click to handle.
            HandlePendingClick();
        }
    }
    /// @endcond

    /// @cond
    public override bool IsPointerOverGameObject(int pointerId) {
        if (pointerData != null && pointerData.pointerCurrentRaycast.gameObject != null) {
            return true;
        }
        return false;
    }
    /// @endcond

    private void CastRay() {
        App.RaycastSource tempSource = App.RaycastSource.MotionController;
        Vector3 rayForward = Vector3.forward;
        if (tempSource == App.RaycastSource.MotionController && VRInput.MotionControllerAvailable) {
            rayForward = VRInput.LaserTransform.forward;
        }
        else if (App.camera != null) {
            rayForward = App.camera.forward;
        }

        Vector2 currentPose = NormalizedCartesianToSpherical(rayForward);

        if (pointerData == null) {
            pointerData = new PointerEventData(eventSystem);
            lastPose = currentPose;
        }

        // Cast a ray into the scene
        pointerData.Reset();

        // Set the position to the center of the camera.
        // This is only necessary if using the built-in Unity raycasters.
        RaycastResult raycastResult;

        eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
        raycastResult = FindFirstRaycast(m_RaycastResultCache);

        pointerData.pointerCurrentRaycast = raycastResult;

        // Find the real screen position associated with the raycast
        // Based on the results of the hit and the state of the pointerData.
        if (raycastResult.gameObject != null) {
            pointerData.position = raycastResult.screenPosition;
        }
        else {
            float maxPointerDistance = 100;
            Vector3 pointerPos = Vector3.forward * maxPointerDistance;
            if (App.camera != null) {
                Transform pointerTransform = App.camera;
                pointerPos = pointerTransform.position + (pointerTransform.forward * maxPointerDistance);
            }
            if (pointerData.pressEventCamera != null) {
                pointerData.position = pointerData.pressEventCamera.WorldToScreenPoint(pointerPos);
            }
            else if (Camera.main != null) {
                pointerData.position = Camera.main.WorldToScreenPoint(pointerPos);
            }
        }

        m_RaycastResultCache.Clear();
        pointerData.delta = currentPose - lastPose;
        lastPose = currentPose;
    }

    private void UpdateCurrentObject(GameObject previousObject) {
        if (pointerData == null) {
            return;
        }

        // Send enter events and update the highlight.
        GameObject currentObject = GetCurrentGameObject(); // Get the pointer target

        HandlePointerExitAndEnter(pointerData, currentObject);

        // Update the current selection, or clear if it is no longer the current object.
        var selected = ExecuteEvents.GetEventHandler<ISelectHandler>(currentObject);
        if (selected == eventSystem.currentSelectedGameObject) {
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(),
              ExecuteEvents.updateSelectedHandler);
        }
        else {
            eventSystem.SetSelectedGameObject(null, pointerData);
        }
    }

    private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold) {
        if (!useDragThreshold)
            return true;

        return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
    }

    private void HandleDrag() {
        bool moving = pointerData.IsPointerMoving();
        bool shouldStartDrag = ShouldStartDrag(pointerData.pressPosition,
                                 pointerData.position,
                                 eventSystem.pixelDragThreshold,
                                 pointerData.useDragThreshold);

        if (moving && shouldStartDrag && pointerData.pointerDrag != null && !pointerData.dragging) {
            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData,
              ExecuteEvents.beginDragHandler);
            pointerData.dragging = true;
        }

        // Drag notification
        if (pointerData.dragging && moving && pointerData.pointerDrag != null) {
            // Before doing drag we should cancel any pointer down state
            // And clear selection!
            if (pointerData.pointerPress != pointerData.pointerDrag) {
                ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);

                pointerData.eligibleForClick = false;
                pointerData.pointerPress = null;
                pointerData.rawPointerPress = null;
            }

            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.dragHandler);
        }
    }

    private void HandlePendingClick() {
        if (pointerData == null || (!pointerData.eligibleForClick && !pointerData.dragging)) {
            return;
        }

        GameObject go = pointerData.pointerCurrentRaycast.gameObject;

        // Send pointer up and click events.
        ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);
        GameObject pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
        if (pointerData.pointerPress == pointerClickHandler && pointerData.eligibleForClick) {
            ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);
        }
        else if (pointerData.dragging) {
            ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.endDragHandler);
        }

        // Clear the click state.
        pointerData.pointerPress = null;
        pointerData.rawPointerPress = null;
        pointerData.eligibleForClick = false;
        pointerData.clickCount = 0;
        pointerData.clickTime = 0;
        pointerData.pointerDrag = null;
        pointerData.dragging = false;
    }

    private void HandleTriggerDown() {
        GameObject go = pointerData.pointerCurrentRaycast.gameObject;

        // Send pointer down event.
        pointerData.pressPosition = pointerData.position;
        pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;
        pointerData.pointerPress =
          ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerDownHandler)
        ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);

        // Save the pending click state.
        pointerData.rawPointerPress = go;
        pointerData.eligibleForClick = true;
        pointerData.delta = Vector2.zero;
        pointerData.dragging = false;
        pointerData.useDragThreshold = true;
        pointerData.clickCount = 1;
        pointerData.clickTime = Time.unscaledTime;

        // Save the drag handler as well
        pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
        if (pointerData.pointerDrag != null) {
            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.initializePotentialDrag);
        }
    }

    private GameObject GetCurrentGameObject() {
        if (pointerData != null) {
            return pointerData.pointerCurrentRaycast.gameObject;
        }

        return null;
    }

    // set raycaster on camera
    private bool rayCasterSet = false;
    public void Update() {
        if (!rayCasterSet && Camera.main != null) {
            rayCasterSet = true;
            Camera.main.gameObject.AddComponent<VrPhysicsRaycaster>();
        }
    }

    public static Vector2 NormalizedCartesianToSpherical(Vector3 cartCoords) {
        cartCoords.Normalize();

        if (cartCoords.x == 0) {
            cartCoords.x = Mathf.Epsilon;
        }

        float outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);

        if (cartCoords.x < 0) {
            outPolar += Mathf.PI;
        }

        float outElevation = Mathf.Asin(cartCoords.y);

        sphericalCoordinatesResult.x = outPolar;
        sphericalCoordinatesResult.y = outElevation;
        return sphericalCoordinatesResult;
    }
}
