using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using Headjack.Utils;
using TMPro;

public class DebugFrameCount : MonoBehaviour {
	public TextMeshProUGUI debugFrameCount;
	
	private int counter = 0;
	private long averageTotal = 0;
	private long averageCount = 0;

	// Use this for initialization
	void Start () {
		if (debugFrameCount == null) {
			debugFrameCount = GetComponent<TextMeshProUGUI>();
		}
		if (debugFrameCount == null) {
			Debug.LogError("[DebugFrameCount] No debug frame count textmesh object found");
			return;
		}

		debugFrameCount.text = "";
	}
	
	// Update is called once per frame
	void Update () {
		++counter;
		if (debugFrameCount != null 
			&& debugFrameCount.gameObject.activeInHierarchy 
			&& App.Player != null
			&& (counter % 5) == 0) {
			long currentVideoTime = App.Player.SeekMs;
			long intendedVideoTime = 0;
			if (App.Player.CurrentStatus == VideoPlayerBase.Status.VP_PLAYING) {
				intendedVideoTime = TimeStamp.GetMsLong() - App.Player.LastTimestamp + App.Player.LastVideoTime;
				++averageCount;
				averageTotal += currentVideoTime - intendedVideoTime;
			} else {
				intendedVideoTime = App.Player.LastVideoTime;
				averageCount = 0;
				averageTotal = 0;
			}

			debugFrameCount.text = $"video: {currentVideoTime} - {intendedVideoTime} = {currentVideoTime - intendedVideoTime} ({averageTotal / System.Math.Max(1, averageCount)})\n{TimeStamp.GetMsLong()}";
		} 
	}
}
