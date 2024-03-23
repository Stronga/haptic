using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Headjack
{
	public class TrackedController : MonoBehaviour
	{
		public Controller controllerType;
		public List<Mapping> mapping;
		private GameObject controller, optionalLeft;
		private bool initialized;
		public enum Controller
		{
			None = -1,
			OculusCV = 0,
			HTCVive = 1,
			Wave = 2,
			OculusGo = 3,
			GearVR = 4,
			QuestRiftS = 5
		}
		[System.Serializable]
		public class Mapping
		{
			public Controller controllerType;
			public GameObject gameObject, optionalLeftController;
		}
		public void Start()
		{
			controllerType = GetControllerType();
			foreach (Mapping m in mapping)
			{
				if (m.controllerType == this.controllerType)
				{
					controller = m.gameObject;
					optionalLeft = m.optionalLeftController;
				}
			}

			initialized = true;
		}
		private void Update()
		{
			if (!initialized || controllerType == Controller.None) return;
			if (optionalLeft == null)
			{
				controller.SetActive(VRInput.MotionControllerShow );
			}
			else
			{
				if (VRInput.remote.node == UnityEngine.XR.XRNode.LeftHand)
				{
					controller.SetActive(false);
					optionalLeft.SetActive(VRInput.MotionControllerShow);
				}
				else
				{
					controller.SetActive(VRInput.MotionControllerShow);
					optionalLeft.SetActive(false);
				}
			}
			
			transform.position = VRInput.LaserTransform.position;
			transform.rotation = VRInput.LaserTransform.rotation;
		}
		public Controller GetControllerType()
		{
			switch (App.CurrentPlatform)
			{
				case App.VRPlatform.Oculus:
#if USE_OCULUS_SDK
					OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
					switch (headset) {
						case OVRPlugin.SystemHeadset.Rift_DK1:
						case OVRPlugin.SystemHeadset.Rift_DK2:
						case OVRPlugin.SystemHeadset.Rift_CV1:
						case OVRPlugin.SystemHeadset.Rift_CB:
							return Controller.OculusCV;
						case OVRPlugin.SystemHeadset.Oculus_Quest:
						case OVRPlugin.SystemHeadset.Rift_S:
						default:
							return Controller.QuestRiftS;
					}
#else
					return Controller.QuestRiftS;
#endif
				case App.VRPlatform.OculusLegacy:
#if USE_OCULUS_LEGACY_SDK
					OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
					switch (headset) {
						case OVRPlugin.SystemHeadset.GearVR_R320:
						case OVRPlugin.SystemHeadset.GearVR_R321:
						case OVRPlugin.SystemHeadset.GearVR_R322:
						case OVRPlugin.SystemHeadset.GearVR_R323:
						case OVRPlugin.SystemHeadset.GearVR_R324:
						case OVRPlugin.SystemHeadset.GearVR_R325:
							return Controller.GearVR;
						case OVRPlugin.SystemHeadset.Oculus_Go:
							return Controller.OculusGo;
						case OVRPlugin.SystemHeadset.Rift_DK1:
						case OVRPlugin.SystemHeadset.Rift_DK2:
						case OVRPlugin.SystemHeadset.Rift_CV1:
						case OVRPlugin.SystemHeadset.Rift_CB:
							return Controller.OculusCV;
						case OVRPlugin.SystemHeadset.Oculus_Quest:
						case OVRPlugin.SystemHeadset.Rift_S:
						default:
							return Controller.QuestRiftS;
					}
#else
					return Controller.QuestRiftS;
#endif
				case App.VRPlatform.OpenVR:
					return Controller.HTCVive;
				case App.VRPlatform.ViveWave:
				default:
					return Controller.None;
			}
		}
		}
}