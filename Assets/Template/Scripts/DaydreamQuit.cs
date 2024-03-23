using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;

public class DaydreamQuit : MonoBehaviour {
	public static DaydreamQuit instance;

	// Use this for initialization
	void Start () {
		if (instance == null) {
			DontDestroyOnLoad(this);
			instance = this;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)
			&& (App.CurrentPlatform == App.VRPlatform.Daydream 
				|| App.CurrentPlatform == App.VRPlatform.Cardboard)) {
			Debug.Log("Quitting application due to Escape button pressed on Daydream/Cardboard");
			Application.Quit();
		}
	}
}
