using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class BufferingPosition : MonoBehaviour
{
	void Update () {
		//Debug.Log("POS "+App.camera.position);
		//Debug.Log("FOR " + App.camera.forward);
		transform.position = App.camera.position + (App.camera.forward * 0.9f);
		transform.rotation = App.camera.rotation;
	}
}
