using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using TMPro;
public class ProjectButtonManager : MonoBehaviour
{
	public GameObject play, playSmall, download, stream, delete, cancel, progress;
	public string projectId;
	public State state;
	
	public RawImage progressGraphic;
	public TextMeshProUGUI progressText;

	public ProjectButton deleteBtn;

	public Animator animator;

	private EssentialsManager essentialsManager;
	private Material progressMaterial;
	private long fileSize;
	private App.VideoMetadata videoMeta;
	private bool deleteDisabled = false;

	public enum State
	{
		BigPlay,
		GotFiles,
		NoFiles,
		Downloading
	}

	public void Initialize(string projectId, EssentialsManager essentialsManager)
	{
		string disableDelete = "False";
		if (CustomVariables.TryGetVariable<string>("disable_delete_option", out disableDelete)) {
			deleteDisabled = (disableDelete == "True");
		}

		this.essentialsManager = essentialsManager;
		this.projectId = projectId;
		this.videoMeta = App.GetVideoMetadata(projectId);
		animator.SetTrigger("Restart");
		App.VideoAvailability dlAvailable = App.GetVideoAvailability(projectId, false);
		if (App.GotFiles(projectId))
		{
			if (dlAvailable == App.VideoAvailability.NoVideoInProject) {
				SetState(State.NoFiles);
			} else {
				SetState(State.GotFiles);
			}
		}
		else
		{
			SetState(State.BigPlay);
		}
		animator.SetTrigger("Restart");
		progressMaterial = progressGraphic.material;
		fileSize = (long)App.GetProjectMetadata(projectId, App.ByteConversionType.Megabytes).TotalSize;
	}

	public void BigPlayButton()
	{
		if (App.GotFiles(projectId))
		{
			SetState(State.GotFiles);
		} 
		else if (App.ProjectIsDownloading(projectId))
		{
			SetState(State.Downloading);
		}
		else
		{
			SetState(State.NoFiles);
		}
	}

	public void Download()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable) {
			PopupMessage.instance.Show(
				"Oops, it looks like you don't have a working internet connection on this device\n" +
				"Please check your internet connection and try again.", 
				PopupMessage.ButtonMode.Confirm, 
				null
			);
		} else {
			App.VideoAvailability streamAvailable = App.GetVideoAvailability(projectId, false);
			switch (streamAvailable) {
				case App.VideoAvailability.Disabled:
					PopupMessage.instance.Show(
						"Oops, it looks like downloading has been disabled for this video\n" +
						"Please return to the Headjack CMS and enable downloading for this video.", 
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.FormatNotSelected:
					PopupMessage.instance.Show(
						"Oops, you did not configure this video to be compatible with this platform. Please return to the Headjack CMS and set up your video for this device.\n" +
						"In the Headjack CMS, navigate to Content > \"Video Name\" > Device Settings to enable this platform for your video.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.UnavailableWillUpdate:
					PopupMessage.instance.Show(
						"Oops, the video is still being processed for this platform\n" +
						"Please restart the app when the video transcoding is completed.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.Incompatible:
					PopupMessage.instance.Show(
						"Oops, this device is not capable of playing this video\n" +
						"Please try using this app on a more powerful device.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.IncompatibleWillUpdate:
					PopupMessage.instance.Show(
						"Oops, the video is still being processed for this platform\n" +
						"Please restart the app when the video transcoding is completed.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.NoVideoInProject:
					PopupMessage.instance.Show(
						"Oops, It looks like this project doesn't contain any video\n" +
						"Please return to the Headjack CMS and add a video to your project and restart this app.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
			}
			
			essentialsManager.DownloadSubtitles(projectId);
			App.Download(projectId, false, delegate (bool s, string e)
			{
				if (s)
				{
					BigPlayButton();
				}
				else
				{
					if (e != "Cancel")
					{
						PopupMessage.instance.Show(
							"Oops, your video stopped downloading\n" +
							"Please check if you have enough free space on your device or check your internet connection and try again.",
							PopupMessage.ButtonMode.Confirm, 
							null
						);
					}
					SetState(State.NoFiles);
				}
			});
			SetState(State.Downloading);
		}
	}
	public void Cancel()
	{
		PopupMessage.instance.Show("Are you sure you want to cancel this download?", PopupMessage.ButtonMode.YesNo, delegate (bool confirm)
		{
			if (confirm)
			{
				if (App.GotFiles(projectId))
				{
					App.Delete(projectId);
					SetState(State.NoFiles);
				}
				else
				{
					App.Cancel(projectId);
					SetState(State.NoFiles);
				}
			}
		});
	}
	public void Delete()
	{
		if (App.UpdateAvailable(projectId)) {
			Download();
		} else {
			PopupMessage.instance.Show("Are you sure you want to delete this project?", 
				PopupMessage.ButtonMode.YesNo, delegate (bool confirm)
			{
				if (confirm)
				{
					App.Delete(projectId);
					SetState(State.NoFiles);
				}
			});
		}
	}

	public void Play()
	{
		App.Fade(true, 1f, delegate (bool s, string e)
		  {
			  VideoPlayerManager.Instance.Initialize(projectId, false, delegate (bool ss, string ee)
				{
					if (!ss) {
						string msg = null;
						if (App.GetVideoProfile(projectId) == App.VideoProfile.Original) {
							msg = "Oops, it looks like this device can't play your original video\n" +
							"Please choose one of our transcoding profiles or check out our knowledge base for more information.";
						} else {
							msg = "Oops, it looks like this device can't play your video\n" +
							"Please consider using a more powerful device or re-downloading the video.";
						}
						PopupMessage.instance.Show(msg, PopupMessage.ButtonMode.Confirm, null);
					}
				});
		  });
	}

	public void Stream()
	{
		if (Application.internetReachability == NetworkReachability.NotReachable) {
			PopupMessage.instance.Show("Oops, it looks like you don't have a working internet connection on this device\nPlease check your internet connection and try again.", PopupMessage.ButtonMode.Confirm, null);
		} else {
			App.VideoAvailability streamAvailable = App.GetVideoAvailability(projectId, true);
			switch (streamAvailable) {
				case App.VideoAvailability.Disabled:
					PopupMessage.instance.Show(
						"Oops, it looks like streaming has been disabled for this video\n" +
						"Please return to the Headjack CMS and enable streaming for this video.", 
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.FormatNotSelected:
					PopupMessage.instance.Show(
						"Oops, you did not configure this video to be compatible with this platform. Please return to the Headjack CMS and set up your video for this device.\n" +
						"In the Headjack CMS, navigate to Content > \"Video Name\" > Device Settings to enable this platform for your video.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.UnavailableWillUpdate:
					PopupMessage.instance.Show(
						"Oops, the video is still being processed for this platform\n" +
						"Please restart the app when the video transcoding is completed.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.Incompatible:
					PopupMessage.instance.Show(
						"Oops, this device is not capable of playing this video\n" +
						"Please try using this app on a more powerful device.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.IncompatibleWillUpdate:
					PopupMessage.instance.Show(
						"Oops, the video is still being processed for this platform\n" +
						"Please restart the app when the video transcoding is completed.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
				case App.VideoAvailability.NoVideoInProject:
					PopupMessage.instance.Show(
						"Oops, It looks like this project doesn't contain any video\n" +
						"Please return to the Headjack CMS and add a video to your project and restart this app.",
						PopupMessage.ButtonMode.Confirm, 
						null
					);
					return;
			}

			App.Fade(true, 1f, delegate (bool s, string e)
			{
				VideoPlayerManager.Instance.Initialize(projectId, true, delegate (bool ss, string ee)
				{
					if (!ss) {
						PopupMessage.instance.Show("Oops, the video stream failed\nPlease check your internet connection and try again.", PopupMessage.ButtonMode.Confirm, null);
					}
				});
			});
		}
	}

	public void SetState(State state)
	{
		DeactivateAllButtons();
		this.state = state;
		animator.SetBool("NoDelete", deleteDisabled);

		switch (state)
		{
			case State.BigPlay:
				animator.SetBool("ShowBigPlay", true);
				animator.SetBool("Downloading", false);
				animator.SetBool("GotFiles", false);
				//Debug.Log("Setting bigplay");
				break;
			case State.Downloading:
				animator.SetBool("ShowBigPlay", false);
				animator.SetBool("Downloading", true);
				animator.SetBool("GotFiles", false);
				//Debug.Log("Setting downloading");
				break;
			case State.GotFiles:
				animator.SetBool("ShowBigPlay", false);
				animator.SetBool("Downloading", false);
				animator.SetBool("GotFiles", true);
				delete.GetComponent<Button>().interactable = App.GotFiles(projectId);
				// if update available, change delete to update button
				if (App.UpdateAvailable(projectId)) {
					deleteBtn.normalText = "Update";
					deleteBtn.highlightedText = "Update";
				} else {
					deleteBtn.normalText = "Delete";
					deleteBtn.highlightedText = "Delete";
				}
				break;
			case State.NoFiles:
				animator.SetBool("ShowBigPlay", false);
				animator.SetBool("Downloading", false);
				animator.SetBool("GotFiles", false);
				
				App.VideoAvailability streamAvailable = App.GetVideoAvailability(projectId, true);
				App.VideoAvailability dlAvailable = App.GetVideoAvailability(projectId, false);
				stream.GetComponent<Button>().interactable = 
					(streamAvailable != App.VideoAvailability.Disabled &&
					streamAvailable != App.VideoAvailability.NoVideoInProject);
				download.GetComponent<Button>().interactable = 
					(dlAvailable != App.VideoAvailability.Disabled &&
					dlAvailable != App.VideoAvailability.NoVideoInProject);
				break;
		}
	}

	private void Update()
	{
		if (state == State.Downloading)
		{
			float progress = App.GetProjectProgress(projectId) * 0.01f;
			progressMaterial.SetFloat("_Progress", progress );
			progressText.text = $"{((long)(fileSize * progress))} / {fileSize} MB";
		}
		// if (state == State.GotFiles)
		// {
		// 	SetState(State.GotFiles);
		// }
	}

	private void OnEnable()
	{
		SetState(this.state);
	}

	public void DeactivateAllButtons()
	{
		play.SetActive(false);
		playSmall.SetActive(false);
		download.SetActive(false);
		stream.SetActive(false);
		delete.SetActive(false);
		cancel.SetActive(false);
		progress.SetActive(false);
	}
}
