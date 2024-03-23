using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;


public class KioskSelect : MonoBehaviour {

	private EssentialsManager essentialsManager;
	public GridCellSize gridCellSize;
	public ProjectInstance projectInstancePrefab;
	public List<ProjectInstance> projectInstances;
	public CategorySwitch categorySwitch;
	public CanvasGroup categorySwitchGroup;
	
	public Button scrollUp;
	public Button scrollDown;
	public RectTransform gridRect;
	public float scrollRate = 1f;

	public Action<App.CategoryMetadata> categoryChangeEvent;

	private float minScrollPos = 0f;
	private float preferredHeight = -1f;
	private float maxScrollPos = 0f;
	private int projectCount = 0;
	private GazeScroll.ScrollDirection previousScrollDirection = GazeScroll.ScrollDirection.None;
	private GazeScroll.ScrollDirection currentScrollDirection = GazeScroll.ScrollDirection.None;
	private float scrollTarget = 0f;

	// Use this for initialization
	public void Initialize(EssentialsManager essentialsManager)
	{
		this.essentialsManager = essentialsManager;
		string[] projects = App.GetProjects();

		// reset scroll direction
		previousScrollDirection = GazeScroll.ScrollDirection.None;
		currentScrollDirection = GazeScroll.ScrollDirection.None;

		projectInstances = new List<ProjectInstance>();

		projectCount = 0;
		List<string> page = new List<string>();
		for (int i = 0; i < projects.Length; ++i)
		{
			if (App.IsLiveStream(projects[i])) continue;
			page.Add(projects[i]);
			projectCount++;
		}

		for (int i = 0; i < page.Count; ++i)
		{
			ProjectInstance projectInstance = Instantiate(projectInstancePrefab);
			projectInstance.transform.SetParent(projectInstancePrefab.transform.parent);
			projectInstance.transform.localPosition = Vector3.zero;
			projectInstance.transform.localEulerAngles = Vector3.zero;
			projectInstance.transform.localScale = Vector3.one;
			projectInstance.Load(page[i],essentialsManager);
			projectInstances.Add(projectInstance);
		}
		projectInstancePrefab.gameObject.SetActive(false);

		// construct list of category filter buttons
		if (essentialsManager != null && 
			categorySwitch != null && 
			categorySwitchGroup != null &&
			essentialsManager.template == EssentialsManager.Template.Kiosk) {
			// only show when categories in use
			if (essentialsManager.categories.Count == 0) {
				categorySwitchGroup.alpha = 0f;
				categorySwitchGroup.interactable = false;
				categorySwitchGroup.blocksRaycasts = false;
			} else {
				// duplicate "no filter" template object and instantiate with category info
				int maxCategories = 5;
				foreach (EssentialsManager.ProjectCollection category in essentialsManager.categories) {
					App.CategoryMetadata categoryMetadata = App.GetCategoryMetadata(category.categoryId);
					if (categoryMetadata != null) {
						CategorySwitch newCatSwitch = Instantiate<CategorySwitch>(
							categorySwitch, 
							categorySwitch.transform.parent, 
							true);
						newCatSwitch.transform.SetParent(categorySwitch.transform.parent);
						newCatSwitch.CategoryMeta = categoryMetadata;
						maxCategories--;
						if (maxCategories <= 0) {
							break;
						}
					}
				}

				categorySwitchGroup.alpha = 1f;
				categorySwitchGroup.interactable = true;
				categorySwitchGroup.blocksRaycasts = true;
			}
		}

		gridCellSize.UpdateGrid(true);
		Canvas.ForceUpdateCanvases();
		// calculate the maximum vertical position the kiosk menu can scroll to
		minScrollPos = gridRect.localPosition.y;
		if (projectCount > 6) {
			maxScrollPos = gridRect.localPosition.y + 
						   preferredHeight - 
						   ((RectTransform)gridRect.parent).rect.height;
		}
	}

	public void SetScrollDirection (GazeScroll.ScrollDirection newScrollDirection) {
		previousScrollDirection = currentScrollDirection;
		currentScrollDirection = newScrollDirection;

		// calculate auto-scroll target
		if (newScrollDirection == GazeScroll.ScrollDirection.None && 
			previousScrollDirection != GazeScroll.ScrollDirection.None) {
			Vector3 currentLocalPos = gridRect.localPosition;
			// NOTE: division value is menu element height + menu spacing (at time of writing)
			float correctedPos = (currentLocalPos.y - minScrollPos) / 0.49f;
			float targetDown = Mathf.Ceil(correctedPos);
			float targetUp = Mathf.Max(0f, Mathf.Floor(correctedPos));
			if (targetDown - correctedPos < 0.2) {
				// close to target when auto-scrolling down
				previousScrollDirection = GazeScroll.ScrollDirection.Down;
			} else if (correctedPos - targetUp < 0.2) {
				// close to target when auto-scrolling up
				previousScrollDirection = GazeScroll.ScrollDirection.Up;
			}
			float target = (previousScrollDirection == GazeScroll.ScrollDirection.Down) ? targetDown : targetUp;
			scrollTarget = Mathf.Clamp(target * 0.49f + minScrollPos, minScrollPos, maxScrollPos);
		}
	}

	public void ScrollUp() {
		Vector3 currentLocalPos = gridRect.localPosition;
		currentLocalPos.y = Mathf.Max(0f, currentLocalPos.y - Time.deltaTime * scrollRate);
		gridRect.localPosition = currentLocalPos;
	}

	public void ScrollDown() {
		Vector3 currentLocalPos = gridRect.localPosition;
		currentLocalPos.y = Mathf.Min(maxScrollPos, currentLocalPos.y + Time.deltaTime * scrollRate);
		gridRect.localPosition = currentLocalPos;
	}

	public void FilterByCategory(App.CategoryMetadata categoryMeta) {
		int visibleProjectCount = 0;
		foreach (ProjectInstance project in projectInstances) {
            if (project != null && project.projectMetadata != null) {
                // category metadata being null indicates no category filter (so show everything)
				if (categoryMeta == null || project.projectMetadata.Category == categoryMeta.Id) {
					project.gameObject.SetActive(true);
					visibleProjectCount++;
				} else {
					project.gameObject.SetActive(false);
				}
            }
        }

		projectCount = visibleProjectCount;
		Canvas.ForceUpdateCanvases();
		preferredHeight = -1f;
		Vector3 currentScrollPos = gridRect.localPosition;
		currentScrollPos.y = minScrollPos;
		gridRect.localPosition = currentScrollPos;

		categoryChangeEvent(categoryMeta);
	}

	void Update() {
		// enable/disable scroll buttons
		if (projectCount > 6 && gridRect.localPosition.y > minScrollPos + 0.005f) {
			scrollUp.interactable = true;
		} else {
			scrollUp.interactable = false;
		}

		if (projectCount > 6 && gridRect.localPosition.y < maxScrollPos - 0.005f) {
			scrollDown.interactable = true;
		} else {
			scrollDown.interactable = false;
		}

		if (projectCount > 6) {
			// calculate max scroll position (once the preferred height of the element is set)
			if (preferredHeight <= 0f) {
				preferredHeight = LayoutUtility.GetPreferredHeight(gridRect);
				maxScrollPos = gridRect.localPosition.y + preferredHeight - ((RectTransform)gridRect.parent).rect.height;
			}

			// scroll
			Vector3 currentLocalPos = gridRect.localPosition;
			if (currentScrollDirection == GazeScroll.ScrollDirection.Up) {
				currentLocalPos.y = Mathf.Max(minScrollPos, currentLocalPos.y - Time.deltaTime * scrollRate);
			} else if (currentScrollDirection == GazeScroll.ScrollDirection.Down) {
				currentLocalPos.y = Mathf.Min(maxScrollPos, currentLocalPos.y + Time.deltaTime * scrollRate);
			} else if (previousScrollDirection != GazeScroll.ScrollDirection.None) {
				// auto-scroll
				float scrollDirection = Mathf.Sign(scrollTarget - currentLocalPos.y);
				if (scrollDirection < 0) {
					currentLocalPos.y = Mathf.Max(scrollTarget, currentLocalPos.y - Time.deltaTime * scrollRate);
				} else {
					currentLocalPos.y = Mathf.Min(scrollTarget, currentLocalPos.y + Time.deltaTime * scrollRate);
				}
				// stop auto-scrolling when target is hit
				if (Mathf.Abs(scrollTarget - currentLocalPos.y) < Mathf.Epsilon) {
					previousScrollDirection = GazeScroll.ScrollDirection.None;
				}
			}
			gridRect.localPosition = currentLocalPos;
		}
	}
}
