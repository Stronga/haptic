using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using TMPro;
public class CategoryRow : MonoBehaviour
{
	private RectTransform myRect;
	public RectTransform scrollViewRect, contentRect;
	public EssentialsManager essentialsManager;
	public string id;
	public List<ProjectInstance> projects;
	public ProjectInstance projectInstanceTemplate;
	public TextMeshProUGUI categoryTitle;
	public void Initialize(EssentialsManager.ProjectCollection category)
	{
		myRect = (RectTransform)transform;
		id = category.categoryId;
		categoryTitle.text = ArabicHelper.CorrectText(App.GetCategoryMetadata(id).Name);
		float cellWidth = (scrollViewRect.rect.width - (0.02f * 2f)) / 3f;
		float cellHeight = scrollViewRect.rect.height;
		projects = new List<ProjectInstance>();
		//myRect.sizeDelta = new Vector2(myRect.sizeDelta.x, cellHeight);
		foreach (string projectId in category.projects)
		{
			ProjectInstance projectInstance = Instantiate(projectInstanceTemplate);
			projectInstance.transform.SetParent(projectInstanceTemplate.transform.parent);
			projectInstance.transform.localPosition = projectInstanceTemplate.transform.localPosition;
			projectInstance.transform.localScale = Vector3.one;
			((RectTransform)projectInstance.transform).sizeDelta = new Vector2(cellWidth, cellHeight);
			projectInstance.Load(projectId,essentialsManager);
			projects.Add(projectInstance);
		}
		projectInstanceTemplate.gameObject.SetActive(false);

		Canvas.ForceUpdateCanvases();

		RectTransform lastProjectTransform = (RectTransform)contentRect.GetChild(contentRect.childCount-1);
		//Debug.Log(lastProjectTransform.name);
		float contentRight = lastProjectTransform.position.x + (lastProjectTransform.sizeDelta.x*0.5f);
		//Debug.Log(contentRight);
		float contentLeft = contentRect.position.x;
		float contentWidth = Mathf.Abs(contentRight - contentLeft); // + borders
		contentRect.sizeDelta = new Vector2(contentWidth,contentRect.sizeDelta.y );
	}
}