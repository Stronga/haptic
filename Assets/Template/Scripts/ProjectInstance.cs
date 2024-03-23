using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using TMPro;

public class ProjectInstance : MonoBehaviour
{
	public string id;
	public RawImage rawImage;
	public TextMeshProUGUI title, duration;
	public ScrollAnalog scrollAnalog;
	public App.ProjectMetadata projectMetadata;

	private Texture texture;
	private EssentialsManager essentialsManager;
	private App.VideoMetadata videoMetadata;

	public void Load(string id, EssentialsManager essentialsManager)
	{
		//Debug.Log("Loading " + id);
		this.id = id;
		gameObject.name = id;
		texture = App.GetImage(id, false);
		if (texture != null)
		{
			texture.filterMode = FilterMode.Trilinear;
			rawImage.texture = texture;
		}
		this.essentialsManager = essentialsManager;
		projectMetadata = App.GetProjectMetadata(id);
		videoMetadata = App.GetVideoMetadata(projectMetadata.VideoId);
		if (projectMetadata != null) {
			title.text = ArabicHelper.CorrectText(projectMetadata.Title);
		} else {
			title.text = "Unknown project";
		}
		if (videoMetadata != null) {
			duration.text = videoMetadata.DurationMMSS;
		} else {
			duration.text = "<color=red>no video</color>";
		}
	}
	public void Show()
	{
		// workaround for Gear VR headset touchpad scrolling
		// causing accidental show project button clicks
		if (scrollAnalog != null && scrollAnalog.isGearVr && scrollAnalog.isScrolling) {
			// current scrolling on GearVR, so ignore button input
			return;
		}

		essentialsManager.ShowProject(id);
	}
}
