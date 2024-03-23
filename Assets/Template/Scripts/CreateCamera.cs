using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class CreateCamera : MonoBehaviour {

	// Use this for initialization
	void Start()
	{
		App.Initialize(delegate (bool s, string e)
		{
			string projectId = App.GetProjects()[2];
			App.ShowCrosshair = true;
			//if (VRInput.MotionControllerAvailable)
			//{
			//	VRInput.MotionControllerLaser = true;
			//	App.ShowCrosshair = false;
			//}
			//else
			//{
			//	VRInput.MotionControllerLaser = false;
			//	App.ShowCrosshair = true;
			//}
			//App.Download(projectId, false, delegate (bool ss, string ee)
			//  {
			//	  App.Play(projectId, false, false, null);
			//  });
		}, true, true);


	}
	
	// Update is called once per frame
	void Update () {
		//if (VRInput.MotionControllerAvailable)
		//{
		//	VRInput.MotionControllerLaser = true;
		//	App.ShowCrosshair = false;
		//}
		//else
		//{
		//	VRInput.MotionControllerLaser = false;
		//	App.ShowCrosshair = true;
		//}
	}
}
