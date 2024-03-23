using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using UnityEngine.UI;
using TMPro;
public class VideoControls : MonoBehaviour
{
	public static VideoControls instance;
	public bool show = true;
	public Color blockNormal, blockHighlight, blockPressed;
	public Color graphicNormal, graphicHighlight, graphicPressed;
	public bool showNextPrevious;
	public Transform posX0Y1, posX1Y1, posX4Y1, posX5Y1;
	public GameObject home, previous, next;
	public VideoButton subtitles;
	public Image seekBarGraphic;
	private Material seekBarGraphicMaterial;
	public Slider slider;
	public CanvasGroup canvasGroup;
	public MainVideoControls mainVideoControls;
	public TextMeshProUGUI timeText, projectTitle;
	private string durationHHMMSS;
	public Animator animator;
	public Menu currentMenu=Menu.Main;

	private int currentSubtitleCount = -1;

	public enum Menu
	{
		Main,
		Subtitle
	}

	private void Awake()
	{
		instance = this;
		seekBarGraphicMaterial = seekBarGraphic.material;
	}
	private void OnEnable()
	{
		if (VideoPlayerManager.Instance == null) return;
		bool hasMoreVideos = VideoPlayerManager.Instance.projects != null && VideoPlayerManager.Instance.projects.Length > 1;
		showNextPrevious = VideoPlayerManager.Instance.isKiosk && hasMoreVideos;
		if (showNextPrevious)
		{
			next.SetActive(true);
			previous.SetActive(true);
			next.transform.position = posX4Y1.position;
			previous.transform.position = posX1Y1.position;
			home.transform.position = posX5Y1.position;
			subtitles.transform.position = posX0Y1.position;
		}
		else
		{
			next.SetActive(false);
			previous.SetActive(false);
			home.transform.position = posX4Y1.position;
			subtitles.transform.position = posX1Y1.position;
		}
	}
	public void SetDuration(string videoId)
	{
		durationHHMMSS = App.GetVideoMetadata(videoId).DurationHHMMSS;
	}
	private void Update()
	{
		animator.SetBool("SubtitlesMenu", currentMenu == Menu.Subtitle);
		seekBarGraphicMaterial.SetFloat("_Value", slider.value);
		canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha + (Time.deltaTime * (show ? 8f : -8f)));
		App.VideoMetadata metaData = App.GetVideoMetadata(App.CurrentVideo);

		if (App.Player == null || metaData == null || App.CurrentProject==null) return;
		timeText.text = MS2HHMMSS(App.Player.SeekMs) + " / " + ((metaData.Duration > 3600000) ? metaData.DurationHHMMSS : metaData.DurationMMSS);
		projectTitle.text = ArabicHelper.CorrectText(App.GetTitle(App.CurrentProject));

		// disable subtitle button if there are no subtitles
		if (mainVideoControls.nrSubtitles != currentSubtitleCount) {
			currentSubtitleCount = mainVideoControls.nrSubtitles;
			if (currentSubtitleCount > 0) {
				subtitles.gameObject.SetActive(true);
				if (currentSubtitleCount == 1) {
					// when only one subtitle is available for this project, load it directly instead of showing the subtitle selection menu
					subtitles.SetIsToggle(true);
				} else {
					subtitles.SetIsToggle(false);
				}
			} else {
				subtitles.gameObject.SetActive(false);
			}
		}
	}
	public void Next()
	{
		VideoPlayerManager.Instance.kioskCurrentVideo++;
		mainVideoControls.state = MainVideoControls.State.Hide;
		App.Fade(true, 1f, delegate (bool s2, string e2)
		{
			App.DestroyVideoPlayer();
			App.Fade(false, 1f, delegate (bool s3, string e3)
			{
				VideoPlayerManager.Instance.PlayNextKiosk();
			});
			
		});
	}
	public void Previous()
	{
		mainVideoControls.state = MainVideoControls.State.Hide;
		VideoPlayerManager.Instance.kioskCurrentVideo--;
		App.Fade(true, 1f, delegate (bool s2, string e2)
		{
			App.DestroyVideoPlayer();
			App.Fade(false, 1f, delegate (bool s3, string e3)
			{
				VideoPlayerManager.Instance.PlayNextKiosk();
			});
		});
	}

	public void ShowSubtitleMenu()
	{
		if (currentSubtitleCount == 1 && mainVideoControls.subtitleMenu.subtitleOptions.Count > 1) {
			if (subtitles.activated) {
				mainVideoControls.subtitleMenu.subtitleOptions[1].OnClick();
			} else {
				mainVideoControls.subtitleMenu.subtitleOptions[0].OnClick();
			}
		} else {
			currentMenu = Menu.Subtitle;
		}
	}

	public string MS2HHMMSS(long timestamp)
	{
		System.TimeSpan t = System.TimeSpan.FromMilliseconds(timestamp);
		if (t.Hours < 1)
		{
			return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
		}
		else
		{
			return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
		}
	}
}
