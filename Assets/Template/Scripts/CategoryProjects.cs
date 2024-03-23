using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class CategoryProjects : MonoBehaviour
{
	public string currentProject, category;
	public CategoryProject categoryProjectPrefab;
	public List<CategoryProject> categoryProjects;
	public RectTransform rightEmpty;
	public Dictionary<string, CategoryProject> cache;
	public ProjectPage projectPage;
	public RectTransform contentRect;
	public Transform middleLocation;

	void Start() {
		if (contentRect == null) {
			contentRect = GetComponent<RectTransform>();
		}
		if (contentRect == null) {
			Debug.LogWarning("[CategoryProjects] missing reference to Content RectTransform");
		}
	}

	public void Initialize(string id)
	{
		categoryProjects = new List<CategoryProject>();
		currentProject = id;
		category = App.GetProjectMetadata(id).Category;

		if (cache == null) cache = new Dictionary<string, CategoryProject>();
		foreach (KeyValuePair<string, CategoryProject> kv in cache)
		{
			kv.Value.selected = false;
			kv.Value.gameObject.SetActive(false);
		}
		foreach (string cat in App.GetProjects(category))
		{
			CategoryProject categoryProject;
			if (cache.ContainsKey(cat))
			{
				categoryProject = cache[cat];
			}
			else
			{
				categoryProject = Instantiate(categoryProjectPrefab);
				categoryProject.Initialize(cat);
				cache.Add(cat, categoryProject);
			}
			categoryProjects.Add(categoryProject);
			categoryProject.transform.SetParent(categoryProjectPrefab.transform.parent);
			categoryProject.transform.localPosition = Vector3.zero;
			categoryProject.selected = (id == cat);
			categoryProject.gameObject.SetActive(true);
		}
		categoryProjectPrefab.gameObject.SetActive(false);
		rightEmpty.SetAsLastSibling();
		Canvas.ForceUpdateCanvases();
		CenterCurrentProject(cache[id]);	
	}
	public void SwitchInCategory(string id)
	{
		cache[currentProject].selected = false;
		cache[id].selected = true;
		currentProject = id;
		projectPage.SwitchInCategory(id);
		Canvas.ForceUpdateCanvases();
		CenterCurrentProject(cache[id]);
	}

	private void CenterCurrentProject(CategoryProject currentProject) {
		if (currentProject == null) {
			return;
		}
		Vector3 contentPos = contentRect.position;
		contentPos.x += middleLocation.position.x - currentProject.transform.position.x;
		contentRect.position = contentPos;
	}
}
