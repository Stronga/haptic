// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using UnityEngine.SpatialTracking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if USE_OPENVR_SDK
using Valve.VR;
#endif

namespace Headjack
{
	public class VRCamera : MonoBehaviour
	{
		private GameObject HMD;
		private GameObject GoogleVR;
		public GameObject[] Laser;
		private Transform centerEye;
		public static Shader fadeShader;

		void Awake()
		{
			gameObject.AddComponent<DoNotDestroy>();
			DontDestroyOnLoad(gameObject);
			// GetComponent<DoNotDestroy>().hideFlags = HideFlags.HideInInspector;
			fadeShader = Resources.Load<Shader>("Shaders/Fade");
			VRInput.VideoShader = Resources.Load<Shader>("Shaders/VideoProjection");
// #if UNITY_STANDALONE || UNITY_EDITOR
// 			Shader.SetGlobalFloat("_MinBlackLevel", (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) ? 0.02f : 0f);
// #else
        	Shader.SetGlobalFloat("_MinBlackLevel", 0f);
// #endif

			if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
			{
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
				BuildHMD();
				App.CameraParent = HMD;
				App.Recenter();
				OVRPlatformMenu OVRMenu = gameObject.AddComponent<OVRPlatformMenu>();
				// GetComponent<OVRPlatformMenu>().hideFlags = HideFlags.HideInInspector;
				OVRMenu.enabled = true;
				App.OculusMenu = OVRMenu;
#endif
			} else if (App.CurrentPlatform == App.VRPlatform.Cardboard)
			{
				BuildCardboard();
				App.Recenter();
			} else if (App.CurrentPlatform == App.VRPlatform.OpenVR || App.CurrentPlatform == App.VRPlatform.WindowsMixedReality)
			{
				BuildHMD();
				App.CameraParent = HMD;
				App.Recenter();
			} else if (App.CurrentPlatform == App.VRPlatform.ViveWave) {
				BuildHMD();
				App.CameraParent = HMD;
				App.Recenter();
			} else if (App.CurrentPlatform == App.VRPlatform.Pico) {
				BuildPico();
			} else if (App.CurrentPlatform == App.VRPlatform.PicoPlatform) {
#if USE_PICO_PLATFORM_SDK
				BuildHMD();
				App.CameraParent = HMD;

				gameObject.AddComponent<Unity.XR.PXR.PXR_Manager>();

				App.Recenter();
#endif
			}
			
			gameObject.AddComponent<VRInput>();
			// GetComponent<VRInput>().hideFlags = HideFlags.HideInInspector;

			if (App.CurrentPlatform == App.VRPlatform.OpenVR || 
				App.CurrentPlatform == App.VRPlatform.Oculus || 
				App.CurrentPlatform == App.VRPlatform.OculusLegacy || 
				App.CurrentPlatform == App.VRPlatform.WindowsMixedReality || 
				App.CurrentPlatform == App.VRPlatform.Daydream || 
				App.CurrentPlatform == App.VRPlatform.ViveWave || 
				App.CurrentPlatform == App.VRPlatform.Pico || 
				App.CurrentPlatform == App.VRPlatform.PicoPlatform)
			{
				Transform laserpointer = new GameObject().transform;
				laserpointer.gameObject.name = "Laser";
				// laserpointer.gameObject.hideFlags = HideFlags.HideInHierarchy;
				GameObject laserMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
				laserMesh.GetComponent<MeshFilter>().mesh = Generate.LaserMesh();
				laserMesh.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/Laser"));
				Destroy(laserMesh.GetComponent<BoxCollider>());
				laserMesh.name = "mesh";
				laserMesh.transform.SetParent(laserpointer);
				laserMesh.transform.localPosition = new Vector3(0, 0, 0.5f);
				laserMesh.transform.localScale = new Vector3(0.005f, 0.005f, 1f);
				laserpointer.gameObject.AddComponent<DoNotDestroy>();
				DontDestroyOnLoad(laserpointer.gameObject);

				VRInput.remote = new VRInput.Remote(laserpointer);
#if USE_OPENVR_SDK
				if (App.CurrentPlatform == App.VRPlatform.OpenVR) {
					// track SteamVR controller(s)
					GameObject leftSteamController = new GameObject("Left SteamVR Controller");
					leftSteamController.AddComponent<DoNotDestroy>();
					DontDestroyOnLoad(leftSteamController);
					SteamVR_Behaviour_Pose leftPose = leftSteamController.AddComponent<SteamVR_Behaviour_Pose>();
					leftPose.inputSource = SteamVR_Input_Sources.LeftHand;
					VRInput.remote.leftSteamController = leftPose;

					GameObject rightSteamController = new GameObject("Right SteamVR Controller");
					rightSteamController.AddComponent<DoNotDestroy>();
					DontDestroyOnLoad(rightSteamController);
					SteamVR_Behaviour_Pose rightPose = rightSteamController.AddComponent<SteamVR_Behaviour_Pose>();
					rightPose.inputSource = SteamVR_Input_Sources.RightHand;
					VRInput.remote.rightSteamController = rightPose;

					SteamVR.Initialize();
				}
#endif
			}
			BuildCrosshair();
			BuildMessageObject();
			BuildFadeObject();
		}

		void Update()
		{
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
			if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
			{
				if (OVRPlugin.shouldRecenter)
				{
					App.Recenter();
				}
			}
#endif
		}

		void BuildPico()
		{
#if UNITY_IOS
			Debug.LogError("Pico platform is not supported on iOS");
			return;
#else
#if USE_PICO_SDK
			Debug.Log("Building Pico camera");
			GameObject picoParent = Instantiate(Resources.Load<GameObject>("Prefabs/Pvr_UnitySDK"));
			picoParent.transform.parent = transform;
			// initialize Headjack handling of Pico input
			PicoVRHeadjack picoVRHeadjack = picoParent.AddComponent<PicoVRHeadjack>();
			picoVRHeadjack.SetControllers(
				FindChildRecursive(picoParent.transform, "PvrController0").transform,
				FindChildRecursive(picoParent.transform, "PvrController1").transform
			);
			picoVRHeadjack.centerEye = FindChildRecursive(picoParent.transform, "BothEye").transform;
			picoVRHeadjack.head = FindChildRecursive(picoParent.transform, "Head").transform;
			// Add pre/post render script to right eye camera to ensure correct stereoscopic video display
			CameraManualStereo rightCamStereo = FindChildRecursive(picoParent.transform, "RightEye")
													.gameObject
													.AddComponent<CameraManualStereo>();
			rightCamStereo.targetEye = CameraManualStereo.Eye.Right;
			// Video projection shader should use alternative stereo rendering
			Shader.EnableKeyword("MANUAL_STEREO_INDEX");

			centerEye = picoVRHeadjack.centerEye;
			centerEye.gameObject.AddComponent<VRRaycast>();
			App.camera = centerEye;
			App.CameraParent = picoVRHeadjack.head.gameObject;

			// initialize Pico proximity sensor
			Pvr_UnitySDKAPI.Sensor.UPvr_InitPsensor();
#endif //#if USE_PICO_SDK
#endif
		}

		void BuildCardboard()
		{
			Debug.Log("Building Cardboard camera");
			GameObject cbParent = Instantiate(Resources.Load<GameObject>("Prefabs/CardboardCamera"));
			cbParent.transform.parent = transform;
			centerEye = FindChildRecursive(cbParent.transform, "CamPost");
			centerEye.gameObject.AddComponent<VRRaycast>();
			App.camera = centerEye;
			App.CameraParent = cbParent;
			FindChildRecursive(cbParent.transform, "NoVRCamGroup").gameObject.AddComponent<VRRaycast>();
			// create helper object for cardboard touch look around (in non-VR mode)
			GameObject touchHelper = new GameObject("Touch Helper");
			touchHelper.transform.parent = cbParent.transform;
			CenterEye CE = centerEye.gameObject.AddComponent<CenterEye>();
			CE.touchHelper = touchHelper;
			// add overlay UI for Cardboard soft buttons
			GameObject cbUi = Instantiate(Resources.Load<GameObject>("Prefabs/CardboardUIOverlay"));
			cbUi.transform.SetParent(transform, false);
			// make sure Cardboard UI Overlay always has an EventSystem in the scene
			// SceneManager.sceneLoaded += CheckCardboardOverlayEventSystem;
			// Add pre/post render script to right eye camera to ensure correct stereoscopic video display
			CameraManualStereo rightCamStereo = FindChildRecursive(cbParent.transform, "CamRight")
				.gameObject
				.AddComponent<CameraManualStereo>();
			rightCamStereo.targetEye = CameraManualStereo.Eye.Right;
			// Video projection shader should use alternative stereo rendering
			Shader.EnableKeyword("MANUAL_STEREO_INDEX");

#if USE_CARDBOARD_SDK
			// Set initial default cardboard lens profile to standard Google Cardboard View model
			MobfishCardboard.CardboardManager.SetCardboardInitialProfile("https://arvr.google.com/cardboard/download/?p=CghIZWFkamFjaxIQQ2FyZGJvYXJkIFZpZXdlch0J-SA9JQHegj0qEAAASEIAAEhCAABIQgAASEJYADUpXA89OggxCKw-aJENP1AAYAI");
#endif
		}

		void BuildHMD()
		{
			HMD = new GameObject("Head Mounted Display");
			HMD.transform.parent = transform;
			GameObject HMDCenter = new GameObject("Center Eye");
			HMDCenter.transform.parent = HMD.transform;
			centerEye = HMDCenter.transform;
			HMDCenter.AddComponent<AudioListener>();
			Camera vrCam = HMDCenter.AddComponent<Camera>();
			vrCam.clearFlags = CameraClearFlags.SolidColor;
			vrCam.backgroundColor = Color.black;
			vrCam.nearClipPlane = 0.05f;
			vrCam.depth = -2;
			vrCam.renderingPath = RenderingPath.Forward;
			vrCam.stereoTargetEye = StereoTargetEyeMask.Both;
			HMDCenter.AddComponent<FlareLayer>();
			HMDCenter.tag = "MainCamera";
			// Add TrackedPoseDriver component to register camera with XR system
			TrackedPoseDriver centerTrackedPose = HMDCenter.AddComponent<TrackedPoseDriver>();
			centerTrackedPose.SetPoseSource(centerTrackedPose.deviceType, TrackedPoseDriver.TrackedPose.Center);
			centerTrackedPose.UseRelativeTransform = true;
			HMDCenter.AddComponent<VRRaycast>();
			App.camera = HMDCenter.transform;

			// use Unity's own stereo rendering in video projection shader
			Shader.DisableKeyword("MANUAL_STEREO_INDEX");
		}

		void BuildCrosshair()
		{
			GameObject CrosshairObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
			CrosshairObject.name = "Crosshair";
			CrosshairObject.transform.parent = transform;
			if (CrosshairObject.GetComponent<MeshCollider>() != null) {
				Destroy(CrosshairObject.GetComponent<MeshCollider>());
			}
			Shader crosshairShader = Resources.Load<Shader>("Shaders/Crosshair");
			Material CrosshairMaterial = new Material(crosshairShader);
			Material CrosshairMaterialLoading = new Material(crosshairShader);
			CrosshairMaterial.mainTexture = Resources.Load<Texture2D>("Textures/Crosshair");
			CrosshairMaterialLoading.mainTexture = Resources.Load<Texture2D>("Textures/CrosshairLoading");
			CrosshairObject.GetComponent<MeshRenderer>().material = CrosshairMaterial;
			App.Crosshair = CrosshairObject.AddComponent<Crosshair>();
			App.Crosshair.Target = centerEye;
			App.Crosshair.LoadingMat = CrosshairMaterialLoading;
			CrosshairObject.transform.localScale = new Vector3(0.05f, 0.05f, 1);
		}

		void BuildMessageObject()
		{
			GameObject messageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			messageQuad.name = "Message";
			if (messageQuad.GetComponent<MeshCollider>()) {
				Destroy(messageQuad.GetComponent<MeshCollider>());
			}
			messageQuad.transform.position = new Vector3(0, 0, 1.5f);
			messageQuad.transform.parent = centerEye;
			centerEye.eulerAngles = new Vector3(-10, 0, 0);
			messageQuad.transform.parent = null;
			centerEye.eulerAngles = new Vector3(0, 0, 0);
			messageQuad.transform.parent = centerEye;
			messageQuad.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
			messageQuad.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/Message"));
			messageQuad.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Textures/MessageBack");

			GameObject textObject = new GameObject();
			textObject.name = "Message Text";
			textObject.transform.parent = messageQuad.transform;
			textObject.transform.localPosition = Vector3.zero;
			textObject.transform.localEulerAngles = Vector3.zero;

			TextMesh tm = textObject.AddComponent<TextMesh>();
			tm.font = Resources.Load<Font>("Fonts/Rubik");
			textObject.GetComponent<MeshRenderer>().material = tm.font.material;
			tm.text = "Test";
			tm.characterSize = 0.015f;
			tm.fontSize = 32;
			tm.anchor = TextAnchor.MiddleCenter;
			tm.alignment = TextAlignment.Left;

			Texture temp = textObject.GetComponent<MeshRenderer>().material.mainTexture;
			textObject.GetComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/MessageText"));
			textObject.GetComponent<MeshRenderer>().material.mainTexture = temp;

			MessageQueue mq = messageQuad.AddComponent<MessageQueue>();
			mq.text = tm;
		}

		private void BuildFadeObject()
		{
			GameObject fadeObject = new GameObject("Fade");
			App.fade = fadeObject.AddComponent<FadeEffect>();
			fadeObject.AddComponent<MeshFilter>().mesh = Generate.FadeMesh();
			fadeObject.AddComponent<MeshRenderer>().material = new Material(Resources.Load<Shader>("Shaders/Fade"));
			fadeObject.transform.SetParent(App.CameraParent.transform);
			fadeObject.transform.localScale = new Vector3(100, 100, 100);
		}

		void OnDestroy() {
#if USE_OPENVR_SDK
			SteamVR.SafeDispose();
#endif
		}

		public static Transform FindChildRecursive(Transform parent, string name)
		{
			foreach (Transform child in parent)
			{
				if (child.name.Contains(name))
					return child;

				var result = FindChildRecursive(child, name);
				if (result != null)
					return result;
			}
			return null;
		}
	}
}