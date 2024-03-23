using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using TMPro;

public class ProjectPage : MonoBehaviour
{
	public string currentId;
	public EssentialsManager essentialsManager;
	public LandingPage landingPage;
	public RectTransform myRect;
	public Canvas canvas;
	public RawImage thumbnail;
	public TextMeshProUGUI title, description;
	public ProjectButtonManager buttonManager;
	public CategoryProjects categoryProjects;
	public float categoryHeight = 0.08999997f;
	public GameObject otherVideos, projectArea;
	public MetaData metaData;
	
	public void ShowProject(string id)
	{
		currentId = id;
		thumbnail.texture = App.GetImage(id, false);
		title.text = ArabicHelper.CorrectText(App.GetTitle(id));
		description.text = ArabicHelper.CorrectText(App.GetDescription(id));
		buttonManager.Initialize(id, essentialsManager);
		string category = App.GetProjectMetadata(id).Category;
		if (category != null && App.GetProjects(category).Length > 1)
		{
			otherVideos.SetActive(true);
			categoryProjects.Initialize(id);
			projectArea.transform.localPosition = new Vector3(0, categoryHeight, 0);
		}
		else
		{
			otherVideos.SetActive(false);
			projectArea.transform.localPosition = new Vector3(0, 0, 0);
		}
		metaData.Initialize(id);
	}
	public void SwitchInCategory(string id)
	{
		currentId = id;
		thumbnail.texture = App.GetImage(id, false);
		title.text = ArabicHelper.CorrectText(App.GetTitle(id));
		description.text = ArabicHelper.CorrectText(App.GetDescription(id));
		buttonManager.Initialize(id, essentialsManager);
		metaData.Initialize(id);
	}

}
