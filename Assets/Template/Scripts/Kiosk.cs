using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using TMPro;
using UnityEngine.UI;
public class Kiosk : MonoBehaviour
{
	public bool canSelect = true;
	public EssentialsManager essentialsManager;
	public string[] projects;
	public List<string> projectsToDownload;
	public bool downloading = false;
	public int projectsFinished = 0;
	double totalSize = -1;
	public TextMeshProUGUI progressA, progressB, notReadyText;
	public Animator animator;
	public RawImage progressBar;
	public KioskSelect kioskSelect;
	public Dictionary<string, App.ProjectMetadata> projectMetadataCache;


	void Awake()
	{
		if (App.GetAppMetadata() == null) return;

		projects = App.GetProjects();
		projectsToDownload = new List<string>();
		projectMetadataCache = new Dictionary<string, App.ProjectMetadata>(projects.Length);
		if (projects != null) {
			for (int i = 0; i < projects.Length; ++i) {
				projectMetadataCache[projects[i]] = App.GetProjectMetadata(projects[i], App.ByteConversionType.Megabytes);
			}
		}
	}

	private void OnEnable()
	{
		if (projects == null) return;
		canSelect = essentialsManager.loadedVariables.kioskGazeMenu;
		animator.SetBool("KioskSelect", canSelect);
		if (!downloading)
		{
			animator.SetBool("Downloading", false);
			projectsToDownload.Clear();
			for (int i = 0; i < projects.Length; ++i)
			{
				if (!App.GotFiles(projects[i]) &&
					!App.IsLiveStream(projects[i]) && 
					(App.GetVideoAvailability(projects[i], false) == App.VideoAvailability.Available ||
					App.GetVideoAvailability(projects[i], false) == App.VideoAvailability.AvailableWillUpdate)
				) {
					projectsToDownload.Add(projects[i]);
				}
			}
			if (projectsToDownload.Count == 0)
			{
				animator.SetBool("GotFiles", true);
				if (canSelect)
				{
					CurvedCanvasInputModule.forceGaze = true;
				} else if (essentialsManager.loadedVariables.kioskAutoPlay) {
					Play();
				}
				
			}
			else
			{
				totalSize = 0;
				for (int i = 0; i < projectsToDownload.Count; ++i)
				{
					if (projectMetadataCache.ContainsKey(projectsToDownload[i]) && projectMetadataCache[projectsToDownload[i]] != null) {
						totalSize += projectMetadataCache[projectsToDownload[i]].TotalSize;
					} else {
						App.ProjectMetadata projectMeta = App.GetProjectMetadata(projectsToDownload[i], App.ByteConversionType.Megabytes);
						projectMetadataCache[projectsToDownload[i]] = projectMeta;
						totalSize += projectMeta.TotalSize;
					}
	
				}
				notReadyText.text = "Kiosk will be ready once all projects are downloaded\n(" + Mathf.RoundToInt((float)totalSize).ToString() + " MB)";
				animator.SetBool("GotFiles", false);
			}
		}
		else
		{
			animator.SetBool("Downloading", true);
		}
		}



	public void Download()
	{
		projectsFinished = 0;
		downloading = true;

		string[] downloadingProjects = projectsToDownload.ToArray();
		DownloadSingleProject(downloadingProjects);

		animator.SetBool("Downloading", true);
	}

	private void DownloadSingleProject(string[] projectsList) {
		if (projectsList == null) {
			Debug.Log("[Kiosk] nothing to download");
			downloading = false;
			OnEnable();
			return;		
		}

		if (!downloading) {
			Debug.Log("[Kiosk] downloads cancelled");
			OnEnable();
			return;
		}

		string currentProject = null;
		for (int i = 0; i < projectsList.Length; ++i) {
			if (string.IsNullOrEmpty(projectsList[i])) {
				continue;
			}
			if (App.IsLiveStream(projectsList[i])) {
				projectsList[i] = null;
				continue;
			}
			if (App.GetVideoAvailability(projectsList[i], false) != App.VideoAvailability.Available &&
				App.GetVideoAvailability(projectsList[i], false) != App.VideoAvailability.AvailableWillUpdate) {
				projectsList[i] = null;
				continue;
			}
			currentProject = projectsList[i];
			projectsList[i] = null;
			break;
		}

		if (!string.IsNullOrEmpty(currentProject)) {
			essentialsManager.DownloadSubtitles(currentProject);
			App.Download(currentProject, false, delegate (bool downloadSuccess, string downloadError) {
				if (!downloadSuccess) {
					downloading = false;
					if (downloadError != "Cancel") {
						PopupMessage.instance.Show("Download Failed\nYou need an active internet connection", PopupMessage.ButtonMode.Confirm, delegate (bool confirm)
						{
							OnEnable();
						});
					}
				} else {
					projectsFinished++;
					DownloadSingleProject(projectsList);
				}
			});
		} else {
			Debug.Log("[Kiosk] Downloads finished");

			downloading = false;
			OnEnable();
		}
	}

	public void Play()
	{
		App.Fade(true, 1f, delegate (bool s, string e)
		  {
			  VideoPlayerManager.Instance.Initialize(true);
		  });
	}

	public void Cancel()
	{
		PopupMessage.instance.Show("Are you sure you want to cancel this download ? ", PopupMessage.ButtonMode.YesNo, delegate (bool confirm)
		{
			if (confirm)
			{
				downloading = false;
				foreach (string s in projectsToDownload)
				{
					App.Cancel(s);
				}
				OnEnable();
			}
		}, 10f, true);
	}

	void Update()
	{
		if (downloading)
		{
			float progress=0;
			int total = 0;
			for (int i = 0; i < projectsToDownload.Count; ++i)
			{
				if (!App.GotFiles(projectsToDownload[i]))
				{
					progress = App.GetProjectProgress(projectsToDownload[i])*0.01f;
					if (projectMetadataCache.ContainsKey(projectsToDownload[i]) && projectMetadataCache[projectsToDownload[i]] != null) {
						total = (int)projectMetadataCache[projectsToDownload[i]].TotalSize;
					}
					break;
				}
			}
			
			if (progress < 0 || total < 0) {
				Debug.LogWarning("Displaying negative download progress?");
			}

			progressA.text = Mathf.RoundToInt(Mathf.Max(progress * total, 0f)).ToString() + " / " + total.ToString() + " MB";
			progressB.text = "Project "+(projectsFinished+1).ToString() + " / " + projectsToDownload.Count.ToString();
			progressBar.material.SetFloat("_Progress", progress);
		}
	}
}
