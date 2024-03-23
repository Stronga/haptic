using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
public class CloseButton : MonoBehaviour {

	public RawImage closeImage;
	public Button button;
	void Start () {
		closeImage.enabled = (App.GetProjects().Length > 1);
	}

	// Update is called once per frame
	void Update()
	{
		if (closeImage.enabled && VRInput.Back.Pressed)
		{
			button.onClick.Invoke();
		}
	}
}
