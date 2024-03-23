using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class PlayVideo : MonoBehaviour {

	// Use this for initialization
	void Start () {
		App.Initialize(delegate (bool s, string e)
		{
			string[] projects = App.GetProjects();
			Debug.Log(projects.Length);
			string p = projects[0];
			App.Download(p, false, delegate (bool ss, string ee)
			  {
				  App.Play(p, false, false, null);
			  });
			}, false, false);

		}
	
	// Update is called once per frame
	void Update () {
		
	}
}
