using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
#if USE_OPENVR_SDK
using Valve.VR;
#endif

public class ScrollAnalog : MonoBehaviour {
	public bool horizontal, vertical;
	public Transform top, bottom;
	// store whether current device is GearVR for workaround
	// to accidental button presses
	public bool isGearVr = false;
	public bool isScrolling = false;

	private float previousVer, previousHor;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		//if (VRInput.Confirm.Pressed) Debug.Log(VRInput.Confirm.Pressed);
		//if (VRInput.Confirm.Hold) Debug.Log(VRInput.Confirm.Hold);
		bool swipe = false;
		float hor = 0;
		hor = CheckAxis(hor, "Horizontal");
		hor = CheckAxis(hor, "Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
		hor = CheckAxis(hor, "Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
		float ver = 0;
		ver = CheckAxis(ver, "Vertical");
		ver = CheckAxis(ver, "Oculus_CrossPlatform_PrimaryThumbstickVertical");
		ver = CheckAxis(ver, "Oculus_CrossPlatform_SecondaryThumbstickVertical");

		isGearVr = false;

		if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
			// scrolling on Oculus devices
#if USE_OCULUS_LEGACY_SDK
			if (((int)OVRPlugin.GetSystemHeadsetType()) <= 6 && OVRInput.GetConnectedControllers() == OVRInput.Controller.Touchpad) { // Gear VR headset only
				isGearVr = true;

				Vector2 f = OVRInput.Get(OVRInput.Axis2D.Any);
				hor = 4 * f.x;
				ver = 3 * f.y;

				// store whether we are currently scrolling (based on touch movement threshold)
				if (Mathf.Abs(hor - previousHor) > .040f) {
					isScrolling = true;
				} else if (Mathf.Abs(ver - previousVer) > .030f) {
					isScrolling = true;
				} else if (Mathf.Abs(hor) <= Mathf.Epsilon && Mathf.Abs(ver) <= Mathf.Epsilon) {
					isScrolling = false;
				}

				swipe = true;
			} 
			else 
#endif
			{
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
				// handle off-hand scrolling
				Vector2 f = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
				if (f.x != 0) hor = f.x;
				if (f.y != 0) ver = f.y;
				f = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
				if (f.x != 0) hor = f.x;
				if (f.y != 0) ver = f.y;
#endif
			}
#if USE_OCULUS_LEGACY_SDK
			OVRInput.Controller con = OVRInput.GetActiveController();
			if (con == OVRInput.Controller.RTrackedRemote || con == OVRInput.Controller.LTrackedRemote) swipe = true;
#endif
			// Debug.Log($"Swipe {swipe}");
		} else if (App.CurrentPlatform == App.VRPlatform.Daydream) {
#if USE_DAYDREAM_SDK
			swipe = true;
			//Debug.Log("Daydream");
			if (GvrControllerInput.State == GvrConnectionState.Connected)
			{
				//Debug.Log("connected");
				if (GvrControllerInput.IsTouching)
				{
					
					Vector2 f = GvrControllerInput.TouchPos;
					//Debug.Log($"touching {f}");
					if (f.x != 0) hor = f.x*2f;
					if (f.y != 0) ver = -f.y*2f;
				}
				else
				{
					//Debug.Log($"not touching {GvrControllerInput.TouchPos}");
					hor = 0;
					ver = 0;
				}
			} else {
				hor = 0;
				ver = 0;
			}
#endif
		} else if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
			swipe = false;
#if USE_OPENVR_SDK
			Vector2 scrollVec = SteamVR_Input.GetVector2("ScrollUI", SteamVR_Input_Sources.Any);
			hor = 1.6f * scrollVec.x;
			ver = 1.2f * scrollVec.y;
#endif
		} else if (App.CurrentPlatform == App.VRPlatform.ViveWave) {
			
		} else if (App.CurrentPlatform == App.VRPlatform.Pico) {
#if USE_PICO_SDK
			swipe = true;

			if (PicoVRHeadjack.instance != null && PicoVRHeadjack.instance.mainController != null && PicoVRHeadjack.instance.mainController.isTouching) {
				Vector2 touchPos = PicoVRHeadjack.instance.mainController.touchPadPosition;
				hor = 1.5f * (touchPos.y - 127.5f) / 127.5f;
				ver = 1.5f * (touchPos.x - 127.5f) / 127.5f;
			} else {
				hor = 0;
				ver = 0;
			}
#endif
		}

		if (Input.GetKey(KeyCode.UpArrow)) ver = 1;
		if (Input.GetKey(KeyCode.DownArrow)) ver = -1;
		if (Input.GetKey(KeyCode.RightArrow)) hor = 1;
		if (Input.GetKey(KeyCode.LeftArrow)) hor = -1;
		//Debug.Log(hor + " - " + ver);
		if (vertical)
		{
			Vector3 temp = transform.position;
			if (!swipe)
			{
				temp.y -= (ver * Time.deltaTime);
			}
			else
			{
				if (previousVer != 0 && ver != 0)
				{
					temp.y -= (previousVer - ver)*0.3f;
				}
			}
			transform.position = temp;
		}
		if (horizontal)
		{
			if (top != null && bottom != null)
			{
				RaycastHit hit = VRInput.MotionControllerAvailable ? App.LaserHit : App.CrosshairHit;
				if (hit.point.y < bottom.position.y || hit.point.y > top.position.y)
				{
					return;
				}
			}
			Vector3 temp = transform.position;
			if (!swipe)
			{
				temp.x -= (hor * Time.deltaTime);
			}
			else
			{
				if (previousHor != 0 && hor != 0)
				{
					temp.x -= (previousHor - hor) * 0.3f;
				}
			}

			transform.position = temp;
		}
		previousVer = ver;
		previousHor = hor;
	}
	
	private float CheckAxis(float current, string axis)
	{
		float f = Input.GetAxis(axis);
		//Debug.Log(f + " - " + axis);
		if (f != 0) return f;
		return current;
	}
}
