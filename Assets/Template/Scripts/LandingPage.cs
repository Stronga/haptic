using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LandingPage : MonoBehaviour
{
	public EssentialsManager essentialsManager;
	public Canvas canvas;
	public RectTransform allContent;
	public GridCellSize gridCellSize;
	public ProjectInstance projectInstanceTemplate;
	public CategoryRow categoryRowTemplate;
	public List<ProjectInstance> projects;
	public ScrollRect scrollRect;
	public VerticalLayoutGroup scrollVerticalLayoutGroup;
	public MeshCollider curvedCollider;
	public ScrollBarEssentials scrollBarEssentials;
	public void Initialize()
	{
		Debug.Log("Initializing landing page");
		GetComponent<CanvasGroup>().alpha = 0;
		RectTransform myRect = (RectTransform)transform;
		Vector2 size = new Vector2(myRect.rect.width, myRect.rect.height);
		((RectTransform)scrollRect.transform).sizeDelta = size;

		Transform lastCategory=transform;
		foreach (EssentialsManager.ProjectCollection projectCollection in essentialsManager.categories)
		{
			CategoryRow categoryRowInstance = Instantiate(categoryRowTemplate);
			categoryRowInstance.transform.SetParent(categoryRowTemplate.transform.parent);
			categoryRowInstance.transform.localPosition = categoryRowTemplate.transform.localPosition;
			categoryRowInstance.transform.localScale = Vector3.one;
			categoryRowInstance.Initialize(projectCollection);
			lastCategory = categoryRowInstance.transform;
		}
		categoryRowTemplate.gameObject.SetActive(false);

		if (essentialsManager.categories.Count == 1)
		{
			scrollVerticalLayoutGroup.spacing = 0.02f + 0.07f;
		}

		//Create uncategorized projects
		//Debug.Log(essentialsManager.uncategorized);
		//Debug.Log(essentialsManager.uncategorized.projects);
		foreach (string id in essentialsManager.uncategorized.projects)
		{
			ProjectInstance projectInstance = Instantiate(projectInstanceTemplate);
			projectInstance.gameObject.name = id;
			projectInstance.transform.SetParent(projectInstanceTemplate.transform.parent);
			projectInstance.transform.localPosition = projectInstanceTemplate.transform.localPosition;
			projectInstance.transform.localScale = Vector3.one;
			projectInstance.Load(id,essentialsManager);
		}
		projectInstanceTemplate.gameObject.SetActive(false);
		gridCellSize.transform.SetAsLastSibling();
		gridCellSize.UpdateGrid(essentialsManager.categories.Count==0);

		Canvas.ForceUpdateCanvases();

		//Resize content parent to make scrolling work
		Transform gridTransform = gridCellSize.transform;

		//Scale grid to 0 if there are no uncategorized videos

		RectTransform lastProjectTransform = (RectTransform)gridTransform.GetChild(gridTransform.childCount-1);
		//Debug.Log(lastProjectTransform.name);

		float contentBottom = lastProjectTransform.position.y;
		if (essentialsManager.uncategorized.projects.Length > 0)
		{
			contentBottom = lastProjectTransform.position.y - (lastProjectTransform.sizeDelta.y * 0.5f);
		}
		else
		{
			contentBottom = lastCategory.position.y - ((RectTransform)lastCategory).sizeDelta.y;
		}


		float contentTop = allContent.position.y;
		float contentHeight = Mathf.Abs( contentTop - contentBottom)+0.02f; // + borders
		//Debug.Log(contentTop + " " + contentBottom + " " + contentHeight);
		allContent.sizeDelta = new Vector2(allContent.sizeDelta.x, contentHeight);
		scrollBarEssentials.size = allContent.sizeDelta.y-1;
	}

	public void BlockInput()
	{
		curvedCollider.enabled = false;	
	}
	public void AllowInput()
	{
		curvedCollider.enabled = true;
	}
	}
