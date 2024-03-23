using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using UnityEngine.UI;
public class EssentialsManager : MonoBehaviour
{
	public static EssentialsVariables variables;
	public Template template;
	public EssentialsVariables loadedVariables;
	public LandingPage landingPage;
	public ProjectPage projectPage;
	public Animator landingPageAnimator, projectPageAnimator;
	public List<ProjectCollection> categories;
	public ProjectCollection uncategorized;
	public CurvedUI curvedUI;
	public MeshRenderer backgroundPanoLeft;
	public MeshRenderer backgroundPanoRight;
	public RawImage logoOnTop;
	public KioskSelect kioskSelect;
	public MeshRenderer environment;
	public AudioSource audioSource;
	public GameObject roomtone;
	[System.Serializable]
	public class ProjectCollection
	{
		public ProjectCollection(string categoryId, string[] projects)
		{
			this.categoryId = categoryId;
			this.projects = projects;
		}
		public string categoryId;
		public string[] projects;
	}
	public enum Template
	{
		AllInOne = 0,
		Grid,
		Cinema,
		Kiosk
	}
	void Awake()
	{
		if (App.GetAppMetadata() == null)
		{
			//App is not initialized, go to splash screen first
			UnityEngine.SceneManagement.SceneManager.LoadScene("Template/App");
			return;
		}
	}
	private void Start()
	{
		//Debug.Log("SETTINGS LANDING PAGE VISIBLE TRUE");
		landingPageAnimator.SetBool("Visible", true);
		QualitySettings.antiAliasing = 2;
		Debug.Log("AA set to 2");
		//Debug.Log(landingPageAnimator.GetBool("Visible"));
		App.Fade(false, 1f, delegate (bool s, string e)
		{
			if (Splash.willShowError)
			{
				PopupMessage.instance.Show(Splash.messageToShow, PopupMessage.ButtonMode.None, null);
				Splash.willShowError = false;
			}
			if (Splash.willShowMessage)
			{
				PopupMessage.instance.Show(Splash.messageToShow, PopupMessage.ButtonMode.Confirm, null);
				Splash.willShowMessage = false;
			}
		});
		if (!Splash.willShowError)
		{
			if (App.GetAppMetadata() == null)
			{
				logoOnTop.enabled = false;
				return; //Skip Start if app metadata is not loaded
						//projectPage.gameObject.SetActive(false);
						//Load categories to create the Category classes
			}
			categories = new List<ProjectCollection>();
			foreach (string categoryId in App.GetCategories())
			{
				string[] catProjects = App.GetProjects(categoryId);
				if (catProjects != null && catProjects.Length > 0) {
					categories.Add(new ProjectCollection(categoryId, App.GetProjects(categoryId)));
				}
			}

			//Create uncategorized string array, even if there are no uncategorized videos
			string[] uncategorizedProjects = App.GetProjects(App.NO_CATEGORY);
			if (uncategorizedProjects == null) uncategorizedProjects = new string[0];

			uncategorized = new ProjectCollection(null, uncategorizedProjects);

			//Special case, if all projects have the same category and there are no uncategorized
			//Treat them as uncategorized
			if (categories.Count == 1 && (uncategorized.projects == null || uncategorized.projects.Length == 0))
			{
				uncategorized.projects = categories[0].projects;
				categories.Clear();
			}

			landingPage.Initialize();

			//custom variables
			loadedVariables = variables;

			roomtone.SetActive(!variables.disableBackgroundAudio);

			if (!string.IsNullOrEmpty(variables.backgroundPano))
			{
				backgroundPanoLeft.material.mainTexture = backgroundPanoRight.material.mainTexture = App.GetImage(variables.backgroundPano, true);
				backgroundPanoLeft.gameObject.SetActive(true);
				backgroundPanoRight.gameObject.SetActive(true);
				environment.transform.gameObject.SetActive(false);
				// setup stereoscopic/monoscopic background image
				if (variables.backgroundStereo) {
					backgroundPanoLeft.material.mainTextureScale = new Vector2(1f, 0.5f);
					backgroundPanoLeft.material.mainTextureOffset = new Vector2(0f, 0.5f);
					backgroundPanoRight.material.mainTextureScale = new Vector2(1f, 0.5f);
					backgroundPanoRight.material.mainTextureOffset = new Vector2(0f, 0f);
				} else {
					backgroundPanoLeft.material.mainTextureScale = new Vector2(1f, 1f);
					backgroundPanoLeft.material.mainTextureOffset = new Vector2(0f, 0f);
					backgroundPanoRight.material.mainTextureScale = new Vector2(1f, 1f);
					backgroundPanoRight.material.mainTextureOffset = new Vector2(0f, 0f);
				}
			}
			else
			{
				backgroundPanoLeft.gameObject.SetActive(false);
				backgroundPanoRight.gameObject.SetActive(false);
				environment.transform.gameObject.SetActive(true);
			}
			if (!string.IsNullOrEmpty(variables.logoOnTop))
			{
				logoOnTop.texture = App.GetImage(variables.logoOnTop, false);
				App.MediaMetadata meta = App.GetMediaMetadata(variables.logoOnTop);
				RectTransform rectLogo = logoOnTop.rectTransform;
				float maxWidth = rectLogo.sizeDelta.x;
				float maxHeight = rectLogo.sizeDelta.y;
				float newWidth = 0;
				float newHeight = 0;
				//try keeping height
				newWidth = (maxHeight / meta.Height) * meta.Width;
				if (newWidth > maxWidth)
				{
					newWidth = maxWidth;
					newHeight = (maxWidth / meta.Width) * meta.Height;
				}
				else
				{
					newHeight = maxHeight;
				}
				rectLogo.sizeDelta = new Vector2(newWidth, newHeight);
			}
			else
			{
				logoOnTop.gameObject.SetActive(false);
			}

			Material roomColors = environment.material;
			roomColors.SetColor("_Color1", variables.color1);
			roomColors.SetColor("_Color2", variables.color2);
			roomColors.SetColor("_Color3", variables.color3);

			if (variables.kioskGazeMenu) kioskSelect.Initialize(this);
			Canvas.ForceUpdateCanvases();
		}
		else
		{
			Debug.Log(" There was an error during initialization");
			logoOnTop.gameObject.SetActive(false);
		}
		curvedUI.Initialize();
		landingPage.curvedCollider = curvedUI.meshCollider;

		kioskSelect.gameObject.SetActive(false);

		Shader.SetGlobalVector("_FadeBorders", new Vector4(1.06f, 0.5f, 0.02f, 0.02f));
		if (App.GetProjects().Length == 1)
		{
			//directly to project page if only one app
			Debug.Log("Immedeately to project page");
			ShowProject(App.GetProjects()[0]);
			landingPageAnimator.enabled = false;
			landingPage.GetComponent<CanvasGroup>().alpha = 0;
			projectPageAnimator.enabled = false;
			projectPage.GetComponent<CanvasGroup>().alpha = 1;
		}

	}

	public void ShowProject(string id)
	{
		Debug.Log($"Show project {id}");

		DownloadSubtitles(id);
		landingPageAnimator.SetBool("Visible", false);
		projectPageAnimator.SetBool("Visible", true);
		projectPage.ShowProject(id);
		//landingPage.gameObject.SetActive(false);
	}

	public void DownloadSubtitles(string projectId)
	{

		DownloadSubtitle("subtitle_1", projectId);
		DownloadSubtitle("subtitle_2", projectId);
		DownloadSubtitle("subtitle_3", projectId);
		DownloadSubtitle("subtitle_4", projectId);
		DownloadSubtitle("subtitle_5", projectId);
		DownloadSubtitle("subtitle_6", projectId);
	}

	private void DownloadSubtitle(string var, string project)
	{
		string subtitleId;
		CustomVariables.TryGetVariable<string>(var, out subtitleId, project);
		if (!string.IsNullOrEmpty(subtitleId))
		{
			Debug.Log("Downloading subtitle file " + subtitleId);
			App.DownloadSingleMedia(subtitleId, false, null);
		}
	}


	public void ShowLandingPage()
	{
		QualitySettings.antiAliasing = 2;
		Debug.Log("AA set to 2");
		//landingPage.gameObject.SetActive(true);
		Debug.Log("Showing Landing Page");
		//projectPage.gameObject.SetActive(false);
		landingPageAnimator.SetBool("Visible", true);
		projectPageAnimator.SetBool("Visible", false);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			Time.timeScale = (Time.timeScale == 1) ? 0.1f : 1;
		}

		// if (CurvedCanvasInputModule.forceNoInput) {
		// 	App.ShowCrosshair = false;
		// 	VRInput.MotionControllerLaser = false;
		// } else
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
		if ((App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) && 
			 (!OVRManager.hasInputFocus || !OVRManager.instance.isUserPresent)
		) {
			App.ShowCrosshair = false;
			VRInput.MotionControllerLaser = false;
		} 
		else
#endif
		if (CurvedCanvasInputModule.forceGaze) {
			App.ShowCrosshair = true;
			VRInput.MotionControllerLaser = false;
		} else {
			App.ShowCrosshair = !VRInput.MotionControllerAvailable;
			VRInput.MotionControllerLaser = VRInput.MotionControllerAvailable;
		}

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		if (VRInput.Back.Pressed && App.CurrentPlatform == App.VRPlatform.Cardboard) {
			Application.Quit();
		}
#endif
	}
}
