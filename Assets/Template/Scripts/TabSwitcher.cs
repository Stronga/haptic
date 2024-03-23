using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;

public class TabSwitcher : MonoBehaviour
{
	public EssentialsManager essentialsManager;
	public enum Tab
	{
		Grid,
		Cinema,
		Kiosk
	}
	public Animator animator;
	public static Tab currentTab = Tab.Grid;
	public ProjectButton grid, cinema, kiosk;

	private void Start()
	{
		CurvedCanvasInputModule.forceGaze = false;
		switch (essentialsManager.template)
		{
			default:
			case EssentialsManager.Template.AllInOne:
				ShowTab(currentTab);
				break;
			case EssentialsManager.Template.Grid:
				ShowGrid();
				break;
			case EssentialsManager.Template.Cinema:
				ShowCinema();
				break;
			case EssentialsManager.Template.Kiosk:
				ShowKiosk();
				break;
		}
		gameObject.SetActive(essentialsManager.template == EssentialsManager.Template.AllInOne);
	}
	public void ShowGrid()
	{
		ShowTab(Tab.Grid);
	}
	public void ShowCinema()
	{
		ShowTab(Tab.Cinema);
	}
	public void ShowKiosk()
	{
		ShowTab(Tab.Kiosk);
	}
	public void ShowTab(Tab tab)
	{
		// Cache current tab as static variable
		EssentialsManager.variables.currentTab = tab;

		//if (tab == currentTab) return;
		animator.SetBool("Default", false);
		animator.SetBool("Cinema", false);
		animator.SetBool("Kiosk", false);
		grid.stayHighlighted = cinema.stayHighlighted = kiosk.stayHighlighted = false;
		grid.rawImage.texture = cinema.rawImage.texture = kiosk.rawImage.texture = grid.normalTexture;
		CurvedCanvasInputModule.forceGaze = false;
		switch (tab)
		{
			case Tab.Grid:
				animator.SetBool("Default", true);
				grid.stayHighlighted = true;
				break;
			case Tab.Cinema:
				animator.SetBool("Cinema", true);
				cinema.stayHighlighted = true;
				break;
			case Tab.Kiosk:
				DownloadAllSubtitles();
				animator.SetBool("Kiosk", true);
				kiosk.stayHighlighted = true;
				
				break;
		}
		currentTab = tab;
	}

	private void DownloadAllSubtitles() {
		string[] projects = App.GetProjects();
		if (projects == null) return;
		Debug.Log("Downloading subtitles!!!");
		for (int i = 0; i < projects.Length; ++i) {
			essentialsManager.DownloadSubtitles(projects[i]);
		}
	}
}
