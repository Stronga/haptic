using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using TMPro;
public class Splash : MonoBehaviour
{
	public RectTransform scaleRect;
	public RawImage splashImage;
	public AspectRatioFitter aspectRatioFitter;
	public TextMeshProUGUI text;
	public RawImage progressBar;
	public GameObject cinemaListener;

	private bool alreadyGotTextures = true;
	private bool trackingOriginWasSet = false;

	void Start()
	{
		App.ShowCrosshair = false;
		VRInput.MotionControllerLaser = false;

		progressBar.material.SetFloat("_Progress", 0);
		splashImage.texture = Resources.Load<Texture2D>("Textures/Splash");
		//First load Headjack. This creates the VR Camera
		App.Initialize(OnHeadjackInitialized, true, true);
	}

	public static bool willShowError=false;
	public static bool willShowMessage=false;
	public static string messageToShow = null;

	void OnHeadjackInitialized(bool succes, string error)
	{
		cinemaListener.SetActive(true);
		// Now that there is a VR Camera, show the splash animation
		StartCoroutine(SplashAnimation(SplashAnimationType.Show));
		App.Fade(false, 0, null);

		//If initialization failed, show an error message and return
		if (!succes)
		{
			willShowError = true;
			if (error == "Unpublished")
			{
				messageToShow = "<size=150%>This app is set to unpublished in the Headjack CMS</size>\n\n" +
					"Please get in touch with your app administrator or set the app to published on the app page.";
			}
			else if (error == "No Offline Mode")
			{
				messageToShow = "<size=150%>Offline mode is not available</size>\n\n" +
					"Make sure you are connected to a working internet connection and restart the app.";
			}
			else if (error == "Bandwidth Limit")
			{
				messageToShow = "<size=150%>You have used all of your download credit in the Free plan</size>\n\n" +
					"Please upgrade to a Pro Subscription in the Headjack CMS";
			}
			else if (error == "Views Limit")
			{
				messageToShow = "<size=150%>You have reached the maximum number of views allowed in the Free plan</size>\n\n" +
					"Please upgrade to a Pro Subscription in the Headjack CMS";
			}
			else
			{
				messageToShow= "<size=150%>This app requires an internet connection to start</size>\n\n" +
					"Make sure you are connected to a working internet connection and restart the app.";
			}
			OnAllTexturesDownloaded(true, null);
			return;
		}
		else
		{
			if (error == "Offline")
			{
				text.text = "Preparing in Offline Mode";
			}
			if(App.GetProjects().Length==0)
			{
				willShowError = true;
				messageToShow = "<size=150%>This app doesn't contain any projects</size>\n\n" +
					"Please return to the Headjack CMS and add a project to this app.";
			}
		}
		alreadyGotTextures = true;
		foreach (string s in App.GetProjects())
		{
			//Debug.Log(s + " " + App.GotMediaFile(App.GetProjectMetadata(s).ThumbnailId));
			//Debug.Log(s);
			alreadyGotTextures &= App.GotMediaFile(App.GetProjectMetadata(s).ThumbnailId);
		}

		// load template variables
		EssentialsManager.variables = new EssentialsVariables();
		string disableControls="False";
		string gazeMenu = "True";
		string backgroundStereo = "Monoscopic";
		string kioskAutoPlay = "False";
		string alwaysShowStream = "False";
		string kioskFillTime = "3";
		float kioskFillTimeParsed = 3f;
		string disableBackgroundAudio = "False";

		CustomVariables.TryGetVariable("background_panorama", out EssentialsManager.variables.backgroundPano, null);
		if (!CustomVariables.TryGetVariable("background_stereo", out backgroundStereo, null)) backgroundStereo = "Monoscopic";
		CustomVariables.TryGetVariable("logo_on_top", out EssentialsManager.variables.logoOnTop, null);

		if (!CustomVariables.TryGetVariable("color_1", out EssentialsManager.variables.color1, null)) EssentialsManager.variables.color1 = new Color(204f/255f, 204f/255f, 204f/255f);
		if (!CustomVariables.TryGetVariable("color_2", out EssentialsManager.variables.color2, null)) EssentialsManager.variables.color2 = new Color(153f/255f, 217f/255f, 255f/255f);
		if (!CustomVariables.TryGetVariable("color_3", out EssentialsManager.variables.color3, null)) EssentialsManager.variables.color3 = new Color(208f/255f, 238f/255f, 255f/255f);

		if (!CustomVariables.TryGetVariable("disable_media_controls", out disableControls, null)) disableControls = "False";
		if (!CustomVariables.TryGetVariable("kiosk_gaze", out gazeMenu, null)) gazeMenu = "True";
		if(disableControls!=null) EssentialsManager.variables.disableMediaControls = (disableControls.ToLower() == "true");
		if (gazeMenu != null) EssentialsManager.variables.kioskGazeMenu = (gazeMenu.ToLower() == "true");
		if (backgroundStereo != null) EssentialsManager.variables.backgroundStereo = (backgroundStereo.ToLower() == "stereoscopic");
		
		if (!CustomVariables.TryGetVariable("kiosk_auto_play", out kioskAutoPlay, null)) kioskAutoPlay = "False";
		if (kioskAutoPlay != null) EssentialsManager.variables.kioskAutoPlay = (kioskAutoPlay.ToLower() == "true");

		if (!CustomVariables.TryGetVariable("always_show_stream_option", out alwaysShowStream, null)) alwaysShowStream = "False";
		if (alwaysShowStream != null) EssentialsManager.variables.alwaysShowStream = (alwaysShowStream.ToLower() == "true");

		if (!CustomVariables.TryGetVariable("kiosk_gaze_fill_timer", out kioskFillTime, null)) {
			kioskFillTime = "3";
		}
		if (kioskFillTime != null && float.TryParse(kioskFillTime, out kioskFillTimeParsed) && kioskFillTimeParsed > 0f) {
			EssentialsManager.variables.kioskFillTime = kioskFillTimeParsed;
		}

		if (CustomVariables.TryGetVariable<string>("disable_menu_audio", out disableBackgroundAudio) &&
			disableBackgroundAudio == "True"
		) {
			EssentialsManager.variables.disableBackgroundAudio = true;
		} else {
			EssentialsManager.variables.disableBackgroundAudio = false;
		}

		StartCoroutine(DownloadMediaCoroutine());
	}

	private IEnumerator DownloadMediaCoroutine()
	{
		bool wait = false;
		if (!string.IsNullOrEmpty(EssentialsManager.variables.backgroundPano))
		{
      wait = true;
      App.DownLoadSingleTexture(EssentialsManager.variables.backgroundPano, delegate(bool s, string e)
			{
				wait = false;
			}, true, false);
		}
		while (wait) yield return null;
		
		if (!string.IsNullOrEmpty(EssentialsManager.variables.logoOnTop))
		{
      wait = true;
      App.DownLoadSingleTexture(EssentialsManager.variables.logoOnTop, delegate (bool s, string e)
			{
				wait = false;
			}, false, true);
		}
		while (wait) yield return null;
		//Download and decode all textures before entering main menu
		//If textures are already downloaded, the app will only decode
		//Add a progress listener to show percentage
		App.DownloadAllTextures(OnAllTexturesDownloaded, true, OnDownloadProgress);
	}

	void OnDownloadProgress(ImageLoadingState loadingState, float processed, float total)
	{
		string message = "";
		float progress01;
		int progressPercentage;
		switch (loadingState)
		{
			case ImageLoadingState.Downloading:
				message += "Preparing the environment \n";
				progress01 = processed / Mathf.Max(total, Mathf.Epsilon); //< avoids division by 0
				progressPercentage = Mathf.RoundToInt(Mathf.Clamp(progress01 * 100, 0, 100));
				message += (progressPercentage/2).ToString() + " %";
				progressBar.material.SetFloat("_Progress", (progressPercentage / 2) * 0.01f);
				break;
			case ImageLoadingState.Decoding:
				message += "Preparing the environment \n";
				progress01 = processed / Mathf.Max(total, Mathf.Epsilon); //< avoids division by 0
				progressPercentage = Mathf.RoundToInt(Mathf.Clamp(progress01 * 100, 0, 100));
				if (!alreadyGotTextures)
				{
					//message += (progressPercentage / 2 + 50).ToString() + " %";
					progressBar.material.SetFloat("_Progress", (progressPercentage / 2 + 50) * 0.01f);
				}
				else
				{
					//message += (progressPercentage).ToString() + " %";
					progressBar.material.SetFloat("_Progress", progressPercentage * 0.01f);
				}
				break;
		}
		
		//text.text = message;
	}

	void OnAllTexturesDownloaded(bool succes, string error)
	{
		//If there was an error, show an message but do continue to menu
		if (!succes)
		{
			App.ShowMessage(error);
		}

#if USE_AVPROVIDEO && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
		// check if HEVC codec is available for AVPro video player
		CodecSupport.CheckCodec(CodecSupport.Codec.HEVC, delegate (bool supported, CodecSupport.VideoApi videoApi) {
			if (!supported && videoApi != CodecSupport.VideoApi.None) {
				Debug.LogWarning("HEVC codec not installed, warning user");
				willShowMessage = true;
				switch (videoApi) {
					case CodecSupport.VideoApi.MediaFoundation:
						System.Diagnostics.Process.Start("ms-windows-store://pdp?productId=9N4WGH0Z6VHQ");
						messageToShow = "<size=150%>Missing video support</size>\n\nPlease install \"HEVC Video Extensions from Device Manufacturer\" from the Microsoft Store and restart this app.\n\nOr contact support@headjack.io";
						break;
					case CodecSupport.VideoApi.DirectShow:
						messageToShow = "<size=150%>Missing video support</size>\n\nPlease install a codec (pack) with HEVC support, like \"K-Lite Codec Pack Basic\" and restart this app.\n(install at your own risk)\n\nOr contact support@headjack.io";
						break;
					default:
						messageToShow = "<size=150%>Missing video support</size>\n\nVideo playback may not work correctly on this device.\n\nIf you experience issues, please contact support@headjack.io";
						break;
				}
			}

			App.Fade(true, 1f, delegate (bool s, string e)
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene("Template/MainMenu");
			});
		});
#else // USE_AVPROVIDEO && (UNITY_STANDALONE || UNITY_EDITOR)
		App.Fade(true, 1f, delegate (bool s, string e)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene("Template/MainMenu");
		});
#endif
	}

	public enum SplashAnimationType
	{
		Show,
		Hide
	}
	private IEnumerator SplashAnimation(SplashAnimationType animationType)
	{
		yield return null;
	}

	void Update() {
		if (!trackingOriginWasSet && App.camera != null && App.CameraParent != null) {
			// set tracking origin to floor
			trackingOriginWasSet = App.SetTrackingOrigin(App.TrackingOrigin.FloorLevel);
		}
	}

}
