using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
public class MainVideoControls : MonoBehaviour {
	public const string LAST_SUBTITLE_PREF = "PP_LAST_SUBTITLE_NAME";

	public static MainVideoControls instance;
	public float fadeAfter = 3f;
	public Sprite playSprite, pauseSprite;
	public State state = State.Hide;
	public static int selectedSubtitles = 0;
	public Button backToMenuButton;
	public Text timeStamp;
	public float hideTimer = 0f;
	public VideoControls videoControlsGraphics;
	public CanvasGroup conrolsAlpha;
	public GameObject buffering, homeOnly;
	public Image playPauseImage;

	public string currentProject = null;
	public CustomSubtitles customSubtitles;
	public int subtitlesIndex = -1;
	public int nrSubtitles = 0;
	private int currentSubtitleIndex = -1;
	private int currentSubId = -1;
	public SubtitleMenu subtitleMenu;
	private List<string> subtitleNames = null;

	public Sprite playGraphic, pauseGraphic;
	public Canvas canvas;
	// after how many degrees angle difference reset the menu/subtitle position
	public float angleDiffReset = 45f;
	public float menuCameraHeightDiff = 0.15f;
	public float menuHeightDiffReset = 0f;
	public float menuPositionDiffReset = 0f;

	// subtitle positioning (relative to menu)
	public float subtitleNormalHeight = 0.1f;
	public float subtitleMenuHeight = -0.23f;
	public Transform subtitleTransform;

	// background environment for flat video
	public GameObject environment;

	public enum State
	{
		Hide,
		Show
	}

	void Awake()
	{
		instance = this;
		canvas.worldCamera = Camera.main;
	}

	public void Hide()
	{
		state = State.Hide;
		App.Player.SetStereoscopicRendering(true);
		//CrosshairHandler.showPointer = false;
		Debug.Log("Set state to Hide");
		videoControlsGraphics.currentMenu = VideoControls.Menu.Main;

		// move subtitles up
		Vector3 subPos = subtitleTransform.localPosition;
		subPos.y = subtitleNormalHeight;
		subtitleTransform.localPosition = subPos;
	}

	public void Show()
	{
		Vector3 forward = App.camera.forward;
		forward.y = 0;
		forward.Normalize();
		transform.forward = forward;

		hideTimer = fadeAfter;
		state = State.Show;
		App.Player.SetStereoscopicRendering(false);
		Debug.Log("Set state to show. Setting hide timer to "+hideTimer);
		videoControlsGraphics.currentMenu = VideoControls.Menu.Main;
		//CrosshairHandler.showPointer = true;

		// move subtitles down to make room for menu
		Vector3 subPos = subtitleTransform.localPosition;
		subPos.y = subtitleMenuHeight;
		subtitleTransform.localPosition = subPos;
	}
	
	public void Seek(int milliseconds)
	{
		App.Player.SeekMs += milliseconds;
	}

	public void PauseResume()
	{
		//playPauseImage.sprite = App.Player.IsPlaying?playSprite:pauseSprite;
		App.Player.PauseResume();
	
	}

	private void Update()
	{
		if (App.Player == null) return;
		bool livestream = App.IsLiveStream(App.CurrentProject);
		buffering.SetActive(App.Player.Buffering);
		//Debug.Log(VRInput.Confirm.Pressed);
		if (EssentialsManager.variables.disableMediaControls
			|| CinemaListenerExample.instance.playing
			|| CinemaHandler.instance.IsPlaying)
		{
			state = State.Hide;
		}
#if UNITY_ANDROID && USE_OCULUS_LEGACY_SDK
		if (App.CurrentPlatform == App.VRPlatform.OculusLegacy)
		{
			if (OVRInput.GetControllerWasRecentered())
			{
				Debug.Log("Controller Recenter");
				App.Recenter();
			}
		}
#endif

		switch (state)
		{
			case State.Hide:
				//blockGraphic.SetActive(true);
				homeOnly.SetActive(false);
				videoControlsGraphics.show = App.ShowCrosshair = VRInput.MotionControllerLaser = false;
				if (VRInput.Confirm.Pressed 
					&& !EssentialsManager.variables.disableMediaControls 
					&& !CinemaListenerExample.instance.playing
					&& !CinemaHandler.instance.IsPlaying)
				{
					Debug.Log("Showing");
					Show();
				}
				if (VRInput.Back.Pressed 
					&& !CinemaListenerExample.instance.playing
					&& !CinemaHandler.instance.IsPlaying)
				{
					Debug.Log("Exiting");
					backToMenuButton.onClick.Invoke();
				}
				videoControlsGraphics.gameObject.SetActive(conrolsAlpha.alpha > 0);
				// if (conrolsAlpha.alpha <= 0)
				// {
				// 	Vector3 forward = App.camera.forward;
				// 	forward.y = 0;
				// 	forward.Normalize();
				// 	transform.forward = forward;
				// }

				if (customSubtitles != null && currentSubId != customSubtitles.currentSubId) {
					currentSubId = customSubtitles.currentSubId;
					Vector3 cameraForward = App.camera.forward;
					cameraForward.y = 0;
					cameraForward.Normalize();
					transform.forward = cameraForward;
				}
				break;
			case State.Show:
				videoControlsGraphics.show = true;
				App.ShowCrosshair = (!VRInput.MotionControllerAvailable);
				VRInput.MotionControllerLaser = VRInput.MotionControllerAvailable;
				videoControlsGraphics.gameObject.SetActive(!livestream);
				homeOnly.SetActive(livestream);
				playPauseImage.sprite = App.Player.IsPlaying ? pauseGraphic : playGraphic;

				if (hideTimer > 0)
				{
					if (VRInput.Back.Pressed)
					{
						Debug.Log("Hiding");
						hideTimer = -1;
					}
					RaycastHit hitInfo = VRInput.MotionControllerAvailable ? App.LaserHit : App.CrosshairHit;
					if (hitInfo.collider != null)
					{

						hideTimer = fadeAfter;

					}
					else
					{
						if (VRInput.Confirm.Pressed && hideTimer != fadeAfter)
						{
							hideTimer = 0;
						}
					}
					hideTimer -= Time.deltaTime;
					if (hideTimer <= 0)
					{
						Hide();
					}
				}
				break;
		}
		//CustomSubtitles.Instance.disableRendering = seekFade.value > 0 || subsFade.value > 0 || quitFade.value > 0;
		if (App.Player != null && timeStamp!=null)
		{
			timeStamp.text = FormatTime(App.Player.SeekMs) + " / " + FormatTime(App.Player.Duration);
			//CustomSubtitles.Instance.UpdateSubtitles((int)App.Player.SeekMs);
		}

		if (currentProject != App.CurrentProject || subtitlesIndex != currentSubtitleIndex)
		{
			if (currentProject != App.CurrentProject)
			{
				// update subtitles
				subtitlesIndex = -1;
				currentProject = App.CurrentProject;
				nrSubtitles = 0;
				string mediaId;
				List<string> options = new List<string>();
				CustomVariables.TryGetVariable("subtitle_1", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				CustomVariables.TryGetVariable("subtitle_2", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				CustomVariables.TryGetVariable("subtitle_3", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				CustomVariables.TryGetVariable("subtitle_4", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				CustomVariables.TryGetVariable("subtitle_5", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				CustomVariables.TryGetVariable("subtitle_6", out mediaId, currentProject);
				options.Add(mediaId);
				if (!string.IsNullOrEmpty(mediaId)) nrSubtitles++;
				subtitleNames = subtitleMenu.Initialize(options.ToArray());
				Debug.Log($"Subtitle menu initialized with number of subtitles: {nrSubtitles}");

				// auto-load first subtitle
				string autoLoadFirst = "False";
				if (CustomVariables.TryGetVariable("auto_load_subtitle_1", out autoLoadFirst) &&
					autoLoadFirst == "True" &&
					!string.IsNullOrEmpty(options[0])
				) {
					subtitlesIndex = 0;
				}

				// auto-load same language subtitle as previous selection in app
				if (subtitlesIndex == -1) {
					string lastSubName = PlayerPrefs.GetString(LAST_SUBTITLE_PREF, null);
					if (!string.IsNullOrEmpty(lastSubName)) {
						for (int i = 0; i < subtitleNames.Count; ++i) {
							if (lastSubName == subtitleNames[i]) {
								subtitlesIndex = i;
								break;
							}
						}
					}
				}

				// update environment
				App.VideoMetadata videoMeta = App.GetVideoMetadata(App.CurrentProject);
				if (videoMeta != null) {
					switch (videoMeta.ProjectionType) {
						case "FLAT":
							environment.SetActive(true);
							break;
						case "EQUIRECT":
						default:
							environment.SetActive(false);
							break;
					}
				}
			}
			currentSubtitleIndex = subtitlesIndex;
			if (currentSubtitleIndex == -1)
			{
				customSubtitles.Ready = false;
				customSubtitles.UpdateSubtitles((int)App.Player.SeekMs);
			}
			else
			{
				string mediaId;
				string varName;
				switch (currentSubtitleIndex)
				{
					default:
					case 0:
						varName = "subtitle_1";
						break;
					case 1:
						varName = "subtitle_2";
						break;
					case 2:
						varName = "subtitle_3";
						break;
					case 3:
						varName = "subtitle_4";
						break;
					case 4:
						varName = "subtitle_5";
						break;
					case 5:
						varName = "subtitle_6";
						break;
				}
				CustomVariables.TryGetVariable(varName, out mediaId, currentProject);
				if (mediaId != null)
				{
					Debug.Log("Loading subtitle id " + mediaId);
					customSubtitles.Load(App.GetMediaFullPath(mediaId), CustomSubtitles.SRTLoadMode.FullPath, null);
				}
			}

			subtitleMenu.SetActive(currentSubtitleIndex);
		}
		else
		{
			if (!customSubtitles.Ready)
			{
				//Debug.Log("Subtitles are not yet ready");
			}
			else
			{
				//Debug.Log("Updating subtitles");
				customSubtitles.UpdateSubtitles((int)App.Player.SeekMs);
			}
		}
		
		// adjust angle of videocontrols if they are too far from camera angle
		Vector3 forward = App.camera.forward;
		forward.y = 0;
		forward.Normalize();
		Vector3 currentForward = transform.forward;
		currentForward.y = 0;
		currentForward.Normalize();
		float forwardDiff = Mathf.Abs(Vector3.Angle(currentForward, forward));
		if (forwardDiff > angleDiffReset) {
			transform.forward = forward;
		}
		// adjust height of videocontrols if they are too far from the camera height
		Vector3 camPos = App.camera.transform.position;
		Vector3 menuPos = transform.position;
		float heightDiff = Mathf.Abs(camPos.y - (menuPos.y + menuCameraHeightDiff));
		if (heightDiff > menuHeightDiffReset) {
			menuPos.y = camPos.y - menuCameraHeightDiff;
			transform.position = menuPos;
		}

		// set horizontal position of menu to camera if position difference is too large
		camPos.y = 0;
		menuPos.y = 0;
		float positionDiff = Vector3.Distance(camPos, menuPos);
		if (positionDiff > menuPositionDiffReset) {
			menuPos = camPos;
			menuPos.y = transform.position.y;
			transform.position = menuPos;
		}
	}

	public void ToMenu()
	{
		//CustomSubtitles.Instance.Ready = false;
		Hide();
		//menu.EndVideoCinema();
		VideoPlayerManager.Instance.StopVideo();
	}

	private static string FormatTime(long milliseconds)
	{
		return new System.TimeSpan(0, 0, 0, 0, (int)milliseconds).ToString(@"mm':'ss");
	}
}
