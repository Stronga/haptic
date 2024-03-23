// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEngine.XR;
#if USE_XR_CARDBOARD
using Google.XR.Cardboard;
#endif
#if USE_OPENVR_SDK
using Valve.VR;
#endif

namespace Headjack
{
    public class VRInput : MonoBehaviour
    {
        #region PRIVATE

        private void KeyboardMouseTouchInput()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR && (USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK)
            if (
                (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) && 
                (!OVRManager.hasInputFocus || !OVRManager.instance.isUserPresent)
            ) {
                // Do not handle button input when we have no input focus on Oculus
                return;
            }
#endif // #if UNITY_STANDALONE && UNITY_EDITOR

            Back.Hold |= Input.GetKey(KeyCode.Escape);
            Back.Pressed |= Input.GetKeyDown(KeyCode.Escape);
            Back.Released |= Input.GetKeyUp(KeyCode.Escape);

            bool mouseHold = Input.GetMouseButton(0);
            bool mouseDown = Input.GetMouseButtonDown(0);
            bool mouseUp = Input.GetMouseButtonUp(0);

#if UNITY_ANDROID && USE_OCULUS_SDK
            if (App.CurrentPlatform == App.VRPlatform.Oculus
                && (OVRInput.GetConnectedControllers() & (OVRInput.Controller.LTouch
                | OVRInput.Controller.RTouch)) != OVRInput.Controller.None)
            {
                mouseHold = mouseDown = mouseUp = false;
            }
#endif
#if UNITY_ANDROID && USE_OCULUS_LEGACY_SDK
            if (App.CurrentPlatform == App.VRPlatform.OculusLegacy
                && (OVRInput.GetConnectedControllers() & (
                    OVRInput.Controller.LTouch | 
                    OVRInput.Controller.RTouch | 
                    OVRInput.Controller.RTrackedRemote | 
                    OVRInput.Controller.LTrackedRemote
                )) != OVRInput.Controller.None)
            {
                mouseHold = mouseDown = mouseUp = false;
            }
#endif

 			Confirm.Hold |= Input.GetKey(KeyCode.Space) || mouseHold;
            Confirm.Pressed |= Input.GetKeyDown(KeyCode.Space) || mouseDown;
            Confirm.Released |= Input.GetKeyUp(KeyCode.Space) || mouseUp;

            if (App.CurrentPlatform == App.VRPlatform.Oculus ||
                App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
                Back.Hold |= Input.GetKey("joystick button 1") || Input.GetKey("joystick button 3") || Input.GetButton("Cancel");
                Back.Pressed |= Input.GetKeyDown("joystick button 1") || Input.GetKeyDown("joystick button 3") || Input.GetButtonDown("Cancel");
                Back.Released |= Input.GetKeyUp("joystick button 1") || Input.GetKeyUp("joystick button 3") || Input.GetButtonUp("Cancel");

                Confirm.Hold |= Input.GetKey("joystick button 0") || Input.GetKey("joystick button 2") || Input.GetButton("Submit");
                Confirm.Pressed |= Input.GetKeyDown("joystick button 0") || Input.GetKeyDown("joystick button 2") || Input.GetButtonDown("Submit");
                Confirm.Released |= Input.GetKeyUp("joystick button 0") || Input.GetKeyUp("joystick button 2") || Input.GetButtonUp("Submit");
            } else if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
                Back.Hold |= Input.GetKey("joystick button 0") || Input.GetKey("joystick button 2");
                Back.Pressed |= Input.GetKeyDown("joystick button 0") || Input.GetKeyDown("joystick button 2");
                Back.Released |= Input.GetKeyUp("joystick button 0") || Input.GetKeyUp("joystick button 2");

                Confirm.Hold |= Input.GetKey("joystick button 1") || Input.GetKey("joystick button 3");
                Confirm.Pressed |= Input.GetKeyDown("joystick button 1") || Input.GetKeyDown("joystick button 3");
                Confirm.Released |= Input.GetKeyUp("joystick button 1") || Input.GetKeyUp("joystick button 3");
            }

            if (Input.GetKeyDown(KeyCode.R)) App.Recenter();
        }

        private static XRNode waveNode = XRNode.RightHand;
        private bool waveConfirmWasDown = false;
        private bool waveBackWasDown = false;
		private void ViveWaveInput()
		{
#if UNITY_ANDROID && USE_WAVE_SDK
            if (App.CurrentlyLoadedPlatform == App.VRPlatform.ViveWave) {
                bool confirmDown = false;
                bool backDown = false;
                // Left Controller
                InputDevice controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                if (controlDevice != null) {
                    bool primDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primDown);
                    bool secDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secDown);
                    bool trigDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.triggerButton, out trigDown);
                    bool menuDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuDown);
                    bool touchpadDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out touchpadDown);
                    
                    confirmDown |= primDown | trigDown | touchpadDown;
                    backDown |= secDown | menuDown;
                    
                    if (primDown || secDown || trigDown || menuDown || touchpadDown) {
                        waveNode = XRNode.LeftHand;
                    }
                }
                // Right Controller
                controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (controlDevice != null) {
                    bool primDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primDown);
                    bool secDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secDown);
                    bool trigDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.triggerButton, out trigDown);
                    bool menuDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuDown);
                    bool touchpadDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out touchpadDown);
                    
                    confirmDown |= primDown | trigDown | touchpadDown;
                    backDown |= secDown | menuDown;
                    
                    if (primDown || secDown || trigDown || menuDown || touchpadDown) {
                        waveNode = XRNode.RightHand;
                    }
                }

                Confirm.Pressed |= (!waveConfirmWasDown && confirmDown);
                Confirm.Hold |= confirmDown;
                Confirm.Released |= (waveConfirmWasDown && !confirmDown);

                Back.Pressed |= (!waveBackWasDown && backDown);
                Back.Hold |= backDown;
                Back.Released |= (waveBackWasDown && !backDown);

                waveConfirmWasDown = confirmDown;
                waveBackWasDown = backDown;

                // headset buttons
                // return button
                Back.Pressed |= Input.GetKeyDown(KeyCode.Escape);
                Back.Hold |= Input.GetKey(KeyCode.Escape);
                Back.Released |= Input.GetKeyUp(KeyCode.Escape);
                // confirm button
                Confirm.Pressed |= Input.GetKeyDown(KeyCode.JoystickButton0);
                Confirm.Hold |= Input.GetKey(KeyCode.JoystickButton0);
                Confirm.Released |= Input.GetKeyUp(KeyCode.JoystickButton0);
            }
#endif
		}

		private void PicoInput()
		{
#if UNITY_ANDROID && USE_PICO_SDK
			if (App.CurrentlyLoadedPlatform == App.VRPlatform.Pico && PicoVRHeadjack.instance.mainController != null)
			{
				PicoVRHeadjack picoHVRHeadjack = PicoVRHeadjack.instance;
				Confirm.Pressed |= picoHVRHeadjack.mainController.confirmDown | Input.GetButtonDown("Submit");
				Confirm.Hold |= picoHVRHeadjack.mainController.confirm | Input.GetButton("Submit");
				Confirm.Released |= picoHVRHeadjack.mainController.confirmUp | Input.GetButtonUp("Submit");
				Back.Pressed |= picoHVRHeadjack.mainController.backDown | Input.GetButtonDown("Cancel");
				Back.Hold |= picoHVRHeadjack.mainController.back | Input.GetButton("Cancel");
				Back.Released |= picoHVRHeadjack.mainController.backUp | Input.GetButtonUp("Cancel");
			}
#endif
		}

        private static XRNode picoNode = XRNode.RightHand;
        private bool picoConfirmWasDown = false;
        private bool picoBackWasDown = false;
		private void PicoPlatformInput()
		{
#if UNITY_ANDROID && USE_PICO_PLATFORM_SDK
            if (App.CurrentlyLoadedPlatform == App.VRPlatform.PicoPlatform) {
                bool confirmDown = false;
                bool backDown = false;
                // Left Controller
                InputDevice controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                if (controlDevice != null) {
                    bool primDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primDown);
                    bool secDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secDown);
                    bool trigDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.triggerButton, out trigDown);
                    bool menuDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuDown);
                    
                    confirmDown |= primDown | trigDown;
                    backDown |= secDown | menuDown;
                    
                    if (primDown || secDown || trigDown || menuDown) {
                        picoNode = XRNode.LeftHand;
                    }
                }
                // Right Controller
                controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (controlDevice != null) {
                    bool primDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primDown);
                    bool secDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secDown);
                    bool trigDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.triggerButton, out trigDown);
                    bool menuDown = false;
                    controlDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuDown);
                    
                    confirmDown |= primDown | trigDown;
                    backDown |= secDown | menuDown;
                    
                    if (primDown || secDown || trigDown || menuDown) {
                        picoNode = XRNode.RightHand;
                    }
                }

                Confirm.Pressed |= (!picoConfirmWasDown && confirmDown);
                Confirm.Hold |= confirmDown;
                Confirm.Released |= (picoConfirmWasDown && !confirmDown);

                Back.Pressed |= (!picoBackWasDown && backDown);
                Back.Hold |= backDown;
                Back.Released |= (picoBackWasDown && !backDown);

                picoConfirmWasDown = confirmDown;
                picoBackWasDown = backDown;

                // headset buttons
                // return button
                Back.Pressed |= Input.GetKeyDown(KeyCode.Escape);
                Back.Hold |= Input.GetKey(KeyCode.Escape);
                Back.Released |= Input.GetKeyUp(KeyCode.Escape);
                // confirm button
                Confirm.Pressed |= Input.GetKeyDown(KeyCode.JoystickButton0);
                Confirm.Hold |= Input.GetKey(KeyCode.JoystickButton0);
                Confirm.Released |= Input.GetKeyUp(KeyCode.JoystickButton0);
            }
#endif
		}

		private bool previousOculusTriggerPressed;
		private void OculusInput()
		{
#if UNITY_ANDROID && USE_OCULUS_SDK
			if (App.CurrentPlatform == App.VRPlatform.Oculus
				&& remote.hasRemote)
			{
				bool oculusTriggerPressed = (Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger") > 0.5);
				oculusTriggerPressed |= (Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger") > 0.5);

				Confirm.Hold |= oculusTriggerPressed;
				Confirm.Pressed |= (oculusTriggerPressed && !previousOculusTriggerPressed);
				Confirm.Released |= (!oculusTriggerPressed && previousOculusTriggerPressed);

				previousOculusTriggerPressed = oculusTriggerPressed;

                Confirm.Hold |= OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);

                Confirm.Pressed |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

                Confirm.Released |= OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);

                Back.Hold |= OVRInput.Get(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                
                Back.Pressed |= OVRInput.GetDown(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);

                Back.Released |= OVRInput.GetUp(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);
			}
			else
			{
				//Debug.Log("No remote");
			}
#endif

#if UNITY_ANDROID && USE_OCULUS_LEGACY_SDK
			if (App.CurrentPlatform == App.VRPlatform.OculusLegacy
				&& remote.hasRemote)
			{
				bool oculusTriggerPressed = (Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger") > 0.5);
				oculusTriggerPressed |= (Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger") > 0.5);

				Confirm.Hold |= oculusTriggerPressed;
				Confirm.Pressed |= (oculusTriggerPressed && !previousOculusTriggerPressed);
				Confirm.Released |= (!oculusTriggerPressed && previousOculusTriggerPressed);

				previousOculusTriggerPressed = oculusTriggerPressed;

                Confirm.Hold |= OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTrackedRemote);

                Confirm.Pressed |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTrackedRemote);

                Confirm.Released |= OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTrackedRemote);

                Back.Hold |= OVRInput.Get(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTrackedRemote);
                
                Back.Pressed |= OVRInput.GetDown(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTrackedRemote);

                Back.Released |= OVRInput.GetUp(OVRInput.Button.Back, OVRInput.Controller.All)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTrackedRemote)
                    || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTrackedRemote);
			}
			else
			{
				//Debug.Log("No remote");
			}
#endif
		}

        private void CardboardInput() 
        {
            if (App.CurrentPlatform != App.VRPlatform.Cardboard || !App.IsVRMode) 
            {
                return;
            }

            


#if !UNITY_EDITOR && USE_XR_CARDBOARD
            if (Api.IsGearButtonPressed)
            {
                Api.ScanDeviceParams();
            }

            if (Api.IsCloseButtonPressed)
            {
                Back.Pressed = true;

                // DEBUG
                // Application.Quit();
            }

            if (Api.IsTriggerPressed) 
            {
                Confirm.Pressed = true;
            }

            if (Api.HasNewDeviceParams())
            {
                Api.ReloadDeviceParams();
            }
#endif
        }

		private void ForceInputHandler()
		{
			Confirm.Pressed |= forceConfirmPressed;
			Back.Pressed |= forceBackPressed;
			forceConfirmPressed = forceBackPressed = false;
		}

        void Start()
        {
            Confirm = new VrButton();
            Back = new VrButton();

            // setup default cardboard close button action
#if USE_CARDBOARD_SDK
            if (App.CurrentlyLoadedPlatform == App.VRPlatform.Cardboard && MobfishCardboard.CardboardUIOverlay.instance != null) {
                MobfishCardboard.CardboardUIOverlay.instance.closeButtonEvent.AddListener(() => {forceBackPressed = true;/*MobfishCardboard.CardboardManager.SetVRViewEnable(false);*/});
                MobfishCardboard.CardboardUIOverlay.instance.showCloseButton = true;
                // MobfishCardboard.CardboardUIOverlay.instance.showSwitchVRButton = true;
            }
#endif
        }
        void Update()
        {
            Confirm.ToFalse();
            Back.ToFalse();

            KeyboardMouseTouchInput();
            OculusInput();
			ViveWaveInput();
			PicoInput();
            PicoPlatformInput();
            CardboardInput();

            if (remote != null)
            {
                remote.UpdateRemote();
            }

			ForceInputHandler(); // this one last
		}
		#endregion
		#region PUBLIC
		public static bool forceConfirmPressed, forceBackPressed;
        /**
        <summary>
        Show the motion controller, it will be glass/seethrough if false
        </summary>
        <example>
        <code>
        public void ShowControllerWhenHoldingTrigger()
        {
        if (VRInput.Confirm.Hold)
        {
            VRInput.MotionControllerShow=true;
        }else
        {
            VRInput.MotionControllerShow=false;
        }
        }
        </code>
        </example>
        */
        public static bool MotionControllerShow {
            get {
                return MotionControllerLaser;
            }
            set {
                MotionControllerLaser = value;
            }
        }
		public static bool laserWorldSpace = true;
		/**
        <summary>
        Check if a motion controller is connected
        </summary>
        <returns>True if a motion controller is connected</returns>
        */
		public static bool MotionControllerAvailable {
            get {
                if (remote != null) return remote.hasRemote;
                return false;
            }
        }
        /**
        <summary>
        The color of the laser pointer, can be set or get
        </summary>
		<returns>Current color of the motion controller laser</returns>
        */
        public static Color MotionControllerColor {
            set {
                Shader.SetGlobalColor("_LaserColor", value);
            }
            get {
                return Shader.GetGlobalColor("_LaserColor");
            }
        }
        /**
        <summary>
        Get the transform of the motion controller laser (if available)
        </summary>
        <example>
        <code>
        public void GameObjectToLaserPosition(GameObject target)
        {
           target.transform.position = Headjack.VRInput.LaserTransform.position;
        }
        </code>
        </example>
        */
        public static Transform LaserTransform {
            get {
                if (remote != null) {
                    return remote.LaserPointer;
                }
                return null;
            }
        }
        /**
        <summary>
        VR Input button
        </summary>
        <remarks>
        &gt; [!NOTE] 
        &gt; Left mouse button - Space - Screen tap - VR Controller trigger
        </remarks>
        <example>
        <code>
        public void CheckIfClickedOnMe()
        {
        if (App.IsCrosshairHit(gameObject))
        {
            if (VRInput.Confirm.Pressed)
            {
                print("Clicked on me!");
            }
        }
        }
        </code>
        </example>
        */
        public static VrButton Confirm = new VrButton();
        /**
        <summary>
        VR Back button
        </summary>
        <remarks>
        &gt; [!NOTE] 
        &gt; Escape key - Android back button - VR controller back button
        </remarks>
        <example>
        <code>
        void Update()
        {
			//Destroy videoplayer when "Back button" is pressed
            if (VRInput.Back.Pressed)
            {
                App.DestroyVideoPlayer();
            }
        
        }
        </code>
        </example>
        */
        public static VrButton Back = new VrButton();
        /**
        <summary>
        Show the laser of the motion controller
        </summary>
        <example>
        <code>
        public void ShowLaserWhenHoldingTrigger()
        {
           if (VRInput.Confirm.Hold)
           {
               VRInput.MotionControllerLaser=true;
           }else
           {
               VRInput.MotionControllerLaser=false;
           }
        }
        </code>
        </example>
        */
        public static bool MotionControllerLaser;

        /**
        <summary>
        Holds metadata about currently used controller (e.g. handedness)
        </summary>
        */
        public static Remote remote;
        #endregion
        #region INTERNAL
        internal static Shader VideoShader, VideoShaderEquiCube;

        #endregion
        #region SUB CLASSES
        /**
        <summary>
        Button class, Confirm and Back are universal across all platforms
        </summary>
        */
        public class VrButton
        {
			private bool pressed, hold, released;
			public bool Pressed
			{
				get
				{
					return pressed;
				}
				set
				{
					pressed = value;
					//if(value==true) Debug.Log("Here");
				}
			}
			public bool Hold
			{
				get
				{
					return hold;
				}
				set
				{
					hold = value;
					//if (value == true) Debug.Log("Here");
				}
			}
			public bool Released
			{
				get
				{
					return released;
				}
				set
				{
					released = value;
					//if (value == true) Debug.Log("Here");
				}
			}
			internal void ToFalse()
            {

				pressed = hold = released = false;
            }
        }
        /// <summary>
        /// Class managing the motion controller
        /// </summary>
        public class Remote
        {
            public Remote(Transform laserPointer)
            {
                LaserPointer = laserPointer;
                MotionControllerColor = Color.white;
                MotionControllerLaser = false;
            }

            public Transform LaserPointer;
            public string controllerId = "Right";
            public bool hasRemote = false;
            public UnityEngine.XR.XRNode node = UnityEngine.XR.XRNode.RightHand;
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
			public OVRInput.Controller active = OVRInput.Controller.RTouch;
#endif
#if USE_OPENVR_SDK
            public SteamVR_Behaviour_Pose leftSteamController = null;
            public SteamVR_Behaviour_Pose rightSteamController = null;
            public SteamVR_Behaviour_Pose activeSteamController = null;
#endif

            private string confirmButtonName = "Headjack_Confirm_Right";
            private string backButtonName = "Headjack_Back_Right";
            private bool remoteConfirm, remoteBack;
#if UNITY_ANDROID && !UNITY_EDITOR && (USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK)
            private OVRInput.Handedness previousHandedness = OVRInput.Handedness.RightHanded;
#endif //UNITY_ANDROID && !UNITY_EDITOR
			private float _ShowController;
            
            public void UpdateRemote()
            {
#if (UNITY_STANDALONE || UNITY_ANDROID) && (USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK) && !UNITY_EDITOR
                if (
                    (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) && 
                    (!OVRManager.hasInputFocus || !OVRManager.instance.isUserPresent)
                ) {
                    // Do not handle button input when we have no input focus on Oculus
                    hasRemote = false;
                    LaserPointer.gameObject.SetActive(false);
                    return;
                }
#endif // #if UNITY_STANDALONE && UNITY_EDITOR

                string[] controllerNames = Input.GetJoystickNames();
                bool gotController = false;
#if UNITY_STANDALONE || UNITY_EDITOR
                for (int i = 0; i < controllerNames.Length; ++i)
                {
                    //Debug.Log(controllerNames[i]);

                    if (controllerNames[i].Contains("OpenVR Controller") || controllerNames[i] == "Oculus Touch Controller - Left")
                    {
                        gotController = true;
                    }
                    if (controllerNames[i] == "OpenVR Controller - Right" || controllerNames[i] == "Oculus Touch Controller - Right")
                    {
                        gotController = true;
                    }
                }
#if USE_OPENVR_SDK
                if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
                    // SteamVR controllers do not use Unity's normal controller system
                    if (rightSteamController != null && rightSteamController.isActive && rightSteamController.isValid) {
                        gotController = true;

                        if (activeSteamController == null) {
                            activeSteamController = rightSteamController;
                        }
                    }

                    if (leftSteamController != null && leftSteamController.isActive && leftSteamController.isValid) {
                        gotController = true;
                    }
                }
#endif
#endif
#if UNITY_ANDROID
#if USE_OCULUS_SDK
                if (App.CurrentPlatform == App.VRPlatform.Oculus
                    && (OVRInput.GetConnectedControllers() & (OVRInput.Controller.LTouch
                    | OVRInput.Controller.RTouch)) != OVRInput.Controller.None)
                {
                    gotController = true;
                }
#endif

#if USE_OCULUS_LEGACY_SDK
                if (App.CurrentPlatform == App.VRPlatform.OculusLegacy
                    && (OVRInput.GetConnectedControllers() & (OVRInput.Controller.LTouch
                    | OVRInput.Controller.RTouch | OVRInput.Controller.RTrackedRemote | OVRInput.Controller.LTrackedRemote)) != OVRInput.Controller.None)
                {
                    gotController = true;
                }
#endif

#if USE_WAVE_SDK
                if (App.CurrentPlatform == App.VRPlatform.ViveWave) {
                    bool leftConnected = Wave.Native.Interop.WVR_Base.Instance.IsDeviceConnected(Wave.Native.WVR_DeviceType.WVR_DeviceType_Controller_Left);
                    bool rightConnected = Wave.Native.Interop.WVR_Base.Instance.IsDeviceConnected(Wave.Native.WVR_DeviceType.WVR_DeviceType_Controller_Right);
                    if (leftConnected) {
                        gotController = true;
                        if (!rightConnected) {
                            waveNode = XRNode.LeftHand;
                        }
                    }
                    if (rightConnected) {
                        gotController = true;
                        if (!leftConnected) {
                            waveNode = XRNode.RightHand;
                        }
                    }
                }
#endif

#if USE_PICO_SDK
				if (App.CurrentPlatform == App.VRPlatform.Pico && 
                    PicoVRHeadjack.instance.mainController != null && 
                    PicoVRHeadjack.instance.mainController.enabled
                ) {
                    gotController = true;
				}
#endif
#if USE_PICO_PLATFORM_SDK
				if (App.CurrentPlatform == App.VRPlatform.PicoPlatform) {
                    bool leftConnected = Unity.XR.PXR.PXR_Input.IsControllerConnected(Unity.XR.PXR.PXR_Input.Controller.LeftController);
                    bool rightConnected = Unity.XR.PXR.PXR_Input.IsControllerConnected(Unity.XR.PXR.PXR_Input.Controller.RightController);
                    if (leftConnected) {
                        gotController = true;
                        if (!rightConnected) {
                            picoNode = XRNode.LeftHand;
                        }
                    }
                    if (rightConnected) {
                        gotController = true;
                        if (!leftConnected) {
                            picoNode = XRNode.RightHand;
                        }
                    }
                }
#endif
#endif // #if UNITY_ANDROID
				if (gotController)
                {
                    //Debug.Log("Got Controller");
                    hasRemote = true;
                    //Laser
                    if (VRInput.MotionControllerLaser)
                    {
                        LaserPointer.gameObject.SetActive(true);
                        if (Physics.Raycast(LaserPointer.position, LaserPointer.forward, out App.LaserHit))
                        {
                            // Debug.Log("ha");
                            LaserPointer.localScale = new Vector3(1, 1, App.LaserHit.distance);
                            Shader.SetGlobalFloat("_LaserLength", App.LaserHit.distance);
                        }
                        else
                        {
                            LaserPointer.localScale = new Vector3(1, 1, 20);
                            Shader.SetGlobalFloat("_LaserLength", 20);
                        }

                        //Debug.Log("Set");
                    }
                    else
                    {
                        LaserPointer.gameObject.SetActive(false);
                    }

                    //Input
#if UNITY_STANDALONE || UNITY_EDITOR
                    if (controllerId == "Right" && (Input.GetAxis("Headjack_Confirm_Left") > .5 || Input.GetButton("Headjack_Back_Left")))
                    {
                        controllerId = "Left";
                        confirmButtonName = "Headjack_Confirm_Left";
                        backButtonName = "Headjack_Back_Left";
                        node = UnityEngine.XR.XRNode.LeftHand;

                    }
                    if (controllerId == "Left" && (Input.GetAxis("Headjack_Confirm_Right") > .5 || Input.GetButton("Headjack_Back_Right")))
                    {
                        controllerId = "Right";
                        confirmButtonName = "Headjack_Confirm_Right";
                        backButtonName = "Headjack_Back_Right";
                        node = UnityEngine.XR.XRNode.RightHand;
                    }

                    if (MotionControllerLaser)
                    {
                        LaserPointer.gameObject.SetActive(true);
                        // do not update laser position if unable to track controller
                        InputDevice controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                        Vector3 controlPos = Vector3.zero;
                        Quaternion controlRot = Quaternion.identity;
                        if (controlDevice != null) {
                            controlDevice.TryGetFeatureValue(CommonUsages.devicePosition, out controlPos);
                            controlDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out controlRot);
                        }
                        if (controlPos != Vector3.zero)
                        {
                            LaserPointer.position = controlPos;
                            LaserPointer.position += App.CameraParent.transform.localPosition;
                        }
                        // if initial position, do not show laser
                        if (LaserPointer.position == Vector3.zero)
                        {
                            LaserPointer.gameObject.SetActive(false);
                        }
                        LaserPointer.rotation = controlRot;
#if USE_OPENVR_SDK
                        if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
                            // SteamVR controllers do not use Unity's normal controller system
                            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.LeftHand) || SteamVR_Input.GetStateDown("BackUI", SteamVR_Input_Sources.LeftHand)) {
                                activeSteamController = leftSteamController;
                            } else if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.RightHand) || SteamVR_Input.GetStateDown("BackUI", SteamVR_Input_Sources.RightHand)) {
                                activeSteamController = rightSteamController;
                            }

                            if (activeSteamController != null && activeSteamController.transform.position != Vector3.zero) {
                                LaserPointer.position = activeSteamController.transform.position;
                                LaserPointer.rotation = activeSteamController.transform.rotation;
                            }
                        }
#endif
                    }
                    if (Input.GetAxis(confirmButtonName) > .5)
                    {
                        Confirm.Hold = true;
                        if (!remoteConfirm)
                        {
                            Confirm.Pressed |= true;
                            remoteConfirm = true;
                        }
                    }
                    else
                    {
                        if (remoteConfirm)
                        {
                            Confirm.Released |= true;
                            remoteConfirm = false;
                        }
                    }
                    if (Input.GetButton(backButtonName))
                    {
                        Back.Hold |= true;
                        if (!remoteBack)
                        {
                            Back.Pressed |= true;
                            remoteBack = true;
                        }
                    }
                    else
                    {
                        if (remoteBack)
                        {
                            Back.Released |= true;
                            remoteBack = false;
                        }
                    }
#if USE_OPENVR_SDK
                    if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
                        // SteamVR controllers do not use Unity's normal controller system
                        Confirm.Pressed |= SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any);
                        Confirm.Released |= SteamVR_Input.GetStateUp("InteractUI", SteamVR_Input_Sources.Any);
                        Confirm.Hold |= SteamVR_Input.GetState("InteractUI", SteamVR_Input_Sources.Any);

                        Back.Pressed |= SteamVR_Input.GetStateDown("BackUI", SteamVR_Input_Sources.Any);
                        Back.Released |= SteamVR_Input.GetStateUp("BackUI", SteamVR_Input_Sources.Any);
                        Back.Hold |= SteamVR_Input.GetState("BackUI", SteamVR_Input_Sources.Any);
                    }
#endif
#if USE_PICO_SDK
                    if (App.CurrentPlatform == App.VRPlatform.Pico && PicoVRHeadjack.instance.mainController != null)
                    {
                        LaserPointer.position = PicoVRHeadjack.instance.mainController.position;
                        LaserPointer.rotation = PicoVRHeadjack.instance.mainController.rotation;
                    }
#endif
#endif
#if UNITY_ANDROID && !UNITY_EDITOR

                    if (MotionControllerLaser)
                    {
                        LaserPointer.gameObject.SetActive(true);

                        if (App.CurrentPlatform == App.VRPlatform.Oculus ||
                            App.CurrentPlatform == App.VRPlatform.OculusLegacy)
                        {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
                            if (OVRInput.GetDominantHand() != previousHandedness) {
                                previousHandedness = OVRInput.GetDominantHand();
                                if (previousHandedness == OVRInput.Handedness.LeftHanded) {
                                    active = OVRInput.Controller.LTouch;
                                    node = UnityEngine.XR.XRNode.LeftHand;
                                } else {
                                    active = OVRInput.Controller.RTouch;
                                    node = UnityEngine.XR.XRNode.RightHand;
                                }
                            }

							if (OVRInput.GetDown(OVRInput.Button.Any, OVRInput.Controller.RTouch)) {
                                active = OVRInput.Controller.RTouch;
                                node = UnityEngine.XR.XRNode.RightHand;
                            }
							if (OVRInput.GetDown(OVRInput.Button.Any, OVRInput.Controller.LTouch)) {
                                active = OVRInput.Controller.LTouch;
                                node = UnityEngine.XR.XRNode.LeftHand;
                            }

							if(laserWorldSpace) {
								LaserPointer.position = OVRInput.GetLocalControllerPosition(active);
								LaserPointer.rotation = OVRInput.GetLocalControllerRotation(active);
							} else {
								LaserPointer.localPosition = OVRInput.GetLocalControllerPosition(active);
								LaserPointer.localRotation = OVRInput.GetLocalControllerRotation(active);
							}
#endif
                        }
#if USE_WAVE_SDK
                        if (App.CurrentPlatform == App.VRPlatform.ViveWave)
                        {
                            // do not update laser position if unable to track controller
                            Vector3 controlPos = Vector3.zero;
                            Quaternion controlRot = Quaternion.identity;
                            InputDevice controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(waveNode);
                            if (controlDevice != null) {
                                controlDevice.TryGetFeatureValue(CommonUsages.devicePosition, out controlPos);
                                controlDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out controlRot);
                            }

                            if (controlPos != Vector3.zero)
                            {
                                LaserPointer.position = controlPos;
                                LaserPointer.position += App.CameraParent.transform.localPosition;
                                // Debug.Log($"[VRInput ViveWave] set new laser position to: {LaserPointer.position}");
                            }
                            // if initial position, do not show laser
                            if (LaserPointer.position == Vector3.zero)
                            {
                                // Debug.LogWarning($"[VRInput ViveWave] ViveWave controller position is zero/origin");
                                LaserPointer.gameObject.SetActive(false);
                            }
                            LaserPointer.rotation = controlRot;
                        }
#endif
#if USE_PICO_SDK
						if (App.CurrentPlatform == App.VRPlatform.Pico && PicoVRHeadjack.instance.mainController != null)
						{
							LaserPointer.position = PicoVRHeadjack.instance.mainController.position;
							LaserPointer.rotation = PicoVRHeadjack.instance.mainController.rotation;
						}
#endif

#if USE_PICO_PLATFORM_SDK
						if (App.CurrentPlatform == App.VRPlatform.PicoPlatform)
                        {
                            // do not update laser position if unable to track controller
                            Vector3 controlPos = Vector3.zero;
                            Quaternion controlRot = Quaternion.identity;
                            InputDevice controlDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(picoNode);
                            if (controlDevice != null) {
                                controlDevice.TryGetFeatureValue(CommonUsages.devicePosition, out controlPos);
                                controlDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out controlRot);
                            }

                            if (controlPos != Vector3.zero)
                            {
                                LaserPointer.position = controlPos;
                                LaserPointer.position += App.CameraParent.transform.localPosition;
                                // Debug.Log($"[VRInput Pico] set new laser position to: {LaserPointer.position}");
                            }
                            // if initial position, do not show laser
                            if (LaserPointer.position == Vector3.zero)
                            {
                                // Debug.LogWarning($"[VRInput Pico] Pico controller position is zero/origin");
                                LaserPointer.gameObject.SetActive(false);
                            }
                            LaserPointer.rotation = controlRot;
                        }
#endif
					}
                    else
                    {
                        LaserPointer.gameObject.SetActive(false);
                    }

#endif // #if UNITY_ANDROID && !UNITY_EDITOR
				}
				else
                {
                    hasRemote = false;
                    LaserPointer.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}
