// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;

namespace Headjack
{
    internal class TouchDrag : MonoBehaviour {
        public float VerticalDragCoef = 1f;
        public float HorizontalDragCoef = 1f;
        public GameObject CamObject;
		public Camera Cam;
        internal bool Dragging = false;
        internal bool Resizing=false;
        internal float ResizeStartDistance;
        internal float ResizeStartFov;
        internal float FovTimer = 0;
        internal float StartFov;
        private Vector3 offset = Vector3.zero;
        private Vector3 prevMousePos = Vector3.zero;
        private float PreviousXEuler;
        private Vector3 screenSize;
        private Vector3 fov;
        private Quaternion LerpedRot;
        private float targetLandscapeVerticalFov;
        private ScreenOrientation lastScreenOrientation;
        private bool firstSensorReset = false;
        private bool doRecenter = false;
// #if UNITY_EDITOR
//         private Quaternion editorRot;
// #endif

        // Use this for initialization
        void Start() {
            LerpedRot = new Quaternion();
// #if UNITY_EDITOR
//             editorRot = new Quaternion();
// #endif
            // Cam = CamObject.GetComponent<Camera>();

            // tweak vertical fov in case OS is portrait (as we assume the user still turns phone)
            targetLandscapeVerticalFov = Cam.fieldOfView;
            lastScreenOrientation = Screen.orientation;
            if (lastScreenOrientation == ScreenOrientation.Portrait ||
                lastScreenOrientation == ScreenOrientation.PortraitUpsideDown) {
                StartFov = targetLandscapeVerticalFov * (1f * Screen.height) / (1f * Screen.width);
            } else {
                StartFov = targetLandscapeVerticalFov;
            }
            fov = new Vector3(StartFov * ((1f*Screen.width) / (1f * Screen.height)), StartFov, 0);

            Dragging = false;
            Resizing = false;
            screenSize = new Vector3(1f / Screen.width, 1f / Screen.height, 0);
            PreviousXEuler = transform.localEulerAngles.x;
        }

		private void OnEnable()
		{
			
		}

		void OnDisable()
        {
            CamObject.transform.localEulerAngles = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            offset = Vector3.zero;
            firstSensorReset = false;
// #if UNITY_EDITOR
//             editorRot = new Quaternion();
// #endif
        }

        // Update is called once per frame
        void LateUpdate() {
            if (App.CurrentPlatform == App.VRPlatform.Cardboard)
            {
                if (!App.IsVRMode)
                {
                    // update fov if screen orientation preference is changed
                    if (Screen.orientation != lastScreenOrientation) {
                        lastScreenOrientation = Screen.orientation;
                        if (lastScreenOrientation == ScreenOrientation.Portrait ||
                            lastScreenOrientation == ScreenOrientation.PortraitUpsideDown) {
                            StartFov = targetLandscapeVerticalFov * (1f * Screen.height) / (1f * Screen.width);
                        } else {
                            StartFov = targetLandscapeVerticalFov;
                        }
                        fov = new Vector3(StartFov * ((1f*Screen.width) / (1f * Screen.height)), StartFov, 0);
                    }
            
                    if (Input.GetMouseButtonDown(0))
                    {
                        prevMousePos = Input.mousePosition;
                        if (App.TouchHit.collider == null)
                        {
                            Dragging = true;
                        }
                    }

                    if (Input.touchCount > 1)
                    {
                        FovTimer = 0;
                        Dragging = false;
                        if (!Resizing)
                        {
                            ResizeStartDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                            ResizeStartFov = Cam.fieldOfView;
                        }
                        Resizing = true;
                        float CurrentDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                        Cam.fieldOfView = Mathf.Clamp(ResizeStartFov * (ResizeStartDistance / CurrentDistance), 20f, 100f);
                    }
                    else
                    {
                        FovTimer += Time.deltaTime;
                        Resizing = false;
                    }
                    if (FovTimer > 1)
                    {
                        Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, StartFov, Time.deltaTime * 4f);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        Dragging = false;
                        Resizing = false;
                    }

                    if (Dragging)
                    {
                        FovTimer = 0;
                        Vector3 mouseDelta = Input.mousePosition - prevMousePos;
                        prevMousePos = Input.mousePosition;



                        mouseDelta.Scale(screenSize);
                        mouseDelta.Scale(fov);
                        Vector3 Rot = new Vector3((mouseDelta.y * VerticalDragCoef), (-mouseDelta.x * HorizontalDragCoef), 0);


                       // Debug.Log("screensize: " + screenSize + " fov: " + fov + " hordragcoef: " + HorizontalDragCoef+" ROT: "+Rot);


                        offset += Quaternion.Euler(0, 0, transform.localEulerAngles.z) * Rot;
                        float totalX = transform.localEulerAngles.x + offset.x;
                        if (totalX > 180f)
                        {
                            totalX -= 360f;
                        }
                        if (totalX > 85)
                        {
                            offset.x -= Mathf.Abs(totalX - 85f);
                        }
                        if (totalX < -85)
                        {
                            offset.x += Mathf.Abs(totalX + 85f);
                        }
                        totalX = transform.localEulerAngles.x + offset.x;
                        // Debug.Log(totalX);
                    }
                    else
                    {
                        float totalX = transform.localEulerAngles.x + offset.x;
                        if (totalX > 180f)
                        {
                            totalX -= 360f;
                        }
                        if (totalX > 85)
                        {
                            offset.x -= Mathf.Abs(totalX - 85f);
                        }
                        if (totalX < -85)
                        {
                            offset.x += Mathf.Abs(totalX + 85f);
                        }
                        float P = PreviousXEuler;
                        float C = transform.localEulerAngles.x;
                        if (P > 180f)
                        {
                            P -= 360f;
                        }
                        if (C > 180f)
                        {
                            C -= 360f;
                        }
                        float Diff = Mathf.Abs(P - C);
                        //Debug.Log(Diff);
                   
                        offset.x = Mathf.LerpUnclamped(offset.x, 0, 1f / 90f * Diff);
                        if (Mathf.Abs(offset.x) < 5)
                        {
                            offset.x = Mathf.Lerp(offset.x, 0, Time.deltaTime * 2f);
                        }
                    }

                    if (doRecenter) {
                        doRecenter = false;
                        offset = Vector3.zero;
                        LerpedRot = CamObject.transform.rotation;
                    }

                    transform.localRotation = CamObject.transform.rotation;
					transform.localEulerAngles += offset;
					LerpedRot = Quaternion.Lerp(LerpedRot, transform.localRotation, Time.deltaTime * 10f);

                    // Debug.Log($"3 {CamObject.transform.eulerAngles}  {LerpedRot.eulerAngles}  {offset}  {transform.localEulerAngles}");

					CamObject.transform.eulerAngles = LerpedRot.eulerAngles;
					PreviousXEuler = transform.localEulerAngles.x;
                }

            }
        }

        public void Recenter() {
            doRecenter = true;
        }
    }
}
