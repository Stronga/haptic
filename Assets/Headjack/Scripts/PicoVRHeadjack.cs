// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

#if USE_PICO_SDK
using Pvr_UnitySDKAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PicoVRHeadjack : MonoBehaviour {
	public static PicoVRHeadjack instance;
	public List<ControllerInstance> controllers;
	public ControllerInstance mainController;
	public Transform centerEye, head;
	[System.Serializable]
	public class ControllerInstance
	{
		public ControllerInstance(int hand, Transform transform)
		{
			this.hand = hand;
			this.transform = transform;
		}
		public int hand;
		public Transform transform;
		public Vector3 position;
		public Quaternion rotation;
		public bool enabled = false;
		public bool confirm, confirmDown, confirmUp, back, backDown, backUp;
		public Vector2 touchPadPosition;
		public bool isTouching = false;
		public void Update()
		{
#if !UNITY_IOS
			// Debug.Log($"Hand {hand} enabled: {enabled}. position {position}. rotation {rotation}");
			enabled = (Controller.UPvr_GetControllerState(hand) == ControllerState.Connected);

			if (!enabled) return;
			position = transform.position;
			rotation = transform.rotation;
			confirm = Controller.UPvr_GetKey(hand, Pvr_KeyCode.TRIGGER);
			confirmDown = Controller.UPvr_GetKeyDown(hand, Pvr_KeyCode.TRIGGER);
			confirmUp = Controller.UPvr_GetKeyUp(hand, Pvr_KeyCode.TRIGGER);
			confirm |= Controller.UPvr_GetKey(hand, Pvr_KeyCode.TOUCHPAD);
			confirmDown |= Controller.UPvr_GetKeyDown(hand, Pvr_KeyCode.TOUCHPAD);
			confirmUp |= Controller.UPvr_GetKeyUp(hand, Pvr_KeyCode.TOUCHPAD);
			back = Controller.UPvr_GetKey(hand, Pvr_KeyCode.APP);
			backDown = Controller.UPvr_GetKeyDown(hand, Pvr_KeyCode.APP);
			backUp = Controller.UPvr_GetKeyUp(hand, Pvr_KeyCode.APP);
			// touchpad
			isTouching = Controller.UPvr_IsTouching(hand);
			touchPadPosition = Controller.UPvr_GetTouchPadPosition(hand);
#endif // #if !UNITY_IOS
		}
	}
	void Awake ()
	{
		instance = this;
	}

	public void SetControllers(Transform leftController, Transform rightController) {
		controllers = new List<ControllerInstance>();
		controllers.Add(new ControllerInstance(0, leftController));
		controllers.Add(new ControllerInstance(1, rightController));
	}

	void Update ()
	{
#if !UNITY_IOS
		if (controllers.Count > 1) {
			mainController = controllers[Controller.UPvr_GetMainHandNess()];

			// Debug.Log($"Main hand is {mainController.hand}");
			controllers[0].Update();
			controllers[1].Update();
		}
#endif // #if !UNITY_IOS
	}
}
#endif