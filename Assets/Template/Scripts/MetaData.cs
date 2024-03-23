using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Headjack;
public class MetaData : MonoBehaviour
{
	public TextMeshProUGUI duration, stereo, ambisonics;
	public ProjectButton downloadButton;
	public void Initialize(string projectId)
	{
		App.ProjectMetadata metadata = App.GetProjectMetadata(projectId,App.ByteConversionType.Megabytes);
		if (metadata != null) {
			ambisonics.text = metadata.AudioId != null ? "Spatial Audio" : "";
			downloadButton.highlightedText = Mathf.RoundToInt(metadata.TotalSize).ToString() + " MB";
		} else {
			ambisonics.text = "";
			downloadButton.highlightedText = "0 MB";
		}

		App.VideoMetadata videoMetadata = App.GetVideoMetadata(projectId);
		if (videoMetadata != null) {
			duration.text = ( videoMetadata.Duration > 3600000) ? videoMetadata.DurationHHMMSS:videoMetadata.DurationMMSS;
			stereo.text = videoMetadata.Stereo ? "3D" : "";
		} else {
			duration.text = "<color=red>no video</color>";
			stereo.text = "";
		}
	}
}
