using Bhaptics.SDK2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using UnityEngine.SceneManagement;

public class VideoPlayerManager : MonoBehaviour
{
	private static VideoPlayerManager _instance;
	public OnEnd onVideoEnd;
	public int kioskCurrentVideo = 0;
	public bool isKiosk;
	public bool isStream;
	public bool resetOnHmdPause = false;
	private float HMDOffTime = 0;
	private float onPauseTime = 0;
	public string[] projects; //kiosk playlist

	// store delegates for HMD mount/unmount actions so they can be removed after use
	private System.Action hmdUnmount = null;
	private System.Action hmdMount = null;

	public static VideoPlayerManager Instance
	{
		get
		{
			return _instance;
		}
	}
	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(this);
		}
		else
		{
			gameObject.SetActive(false);
		}
	}
	IEnumerator PlayNextVideoAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay); // Wait for the specified delay
			BhapticsLibrary.Play("yes_360");
			
}
	// void update()
	// {
	// 	if (Headjack.App.Player.IsPlaying) {
	// 		// Call your specific function here
	// 		 BhapticsLibrary.Play("yes_360");
	// 		 StartCoroutine(PlayNextVideoAfterDelay(3));
	// 	}

	// }


	public void Initialize(string projectId, bool stream, OnEnd onVideoEnd, bool resetOnPause = false, bool prepareOnly = false, long videoTime = 0)
	{
		
		StartCoroutine(PlayNextVideoAfterDelay(1.1f));
		//BhapticsLibrary.Play("yes_360");
		this.onVideoEnd += onVideoEnd;
		Debug.Log("Playing project " + projectId);
		isKiosk = false;
		isStream = stream;
		resetOnHmdPause = resetOnPause;
		kioskCurrentVideo = 0;
		SceneManager.LoadScene("Template/VideoPlayer", LoadSceneMode.Single);
		App.Fade(false, 1f, delegate (bool s, string e)
		{
			string monoScaleStr = null;
			if (CustomVariables.TryGetVariable<string>("mono_sphere_radius", out monoScaleStr, 
				projectId)) {
				float monoScale = string.IsNullOrEmpty(monoScaleStr) ? 10f : float.Parse(monoScaleStr);
				App.MonoSphereRadius = monoScale;
			}

			if (prepareOnly) {
				App.Prepare(projectId, stream, false, OnVideoEnd, videoTime);
			} else {
				App.Play(projectId, stream, false, OnVideoEnd);
			}
			QualitySettings.antiAliasing = 0;
			Debug.Log("AA to 0");
		});

		if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
		{
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
			if (hmdUnmount != null) {
				OVRManager.HMDUnmounted -= hmdUnmount;
				hmdUnmount = null;
			}
			if (hmdMount != null) {
				OVRManager.HMDMounted -= hmdMount;
				hmdMount = null;
			}

			if (resetOnPause) {
				hmdUnmount = delegate () {
					if (App.Player == null) return;
					Debug.Log("OVR headset unmounted");
					HMDOffTime = Time.realtimeSinceStartup;
				};

				OVRManager.HMDUnmounted += hmdUnmount;
				hmdMount = delegate () {
					if (App.Player == null) return;
					Debug.Log("OVR headset mounted");
					if (Time.realtimeSinceStartup - HMDOffTime > 4f)
					{
						if (!isKiosk)
						{
							StopVideo();
						}
						else
						{
							App.DestroyVideoPlayer();
							App.Fade(true, 1f, delegate (bool s2, string e2)
							{
								App.Fade(false, 0f, null);
								kioskCurrentVideo = 0;
								PlayNextKiosk();
							});
						}

					}
					HMDOffTime = -1f;
				};
				OVRManager.HMDMounted += hmdMount;
			}
#endif
		}
	}

	public void Initialize(bool resetOnPause = false)
	{
		Initialize(App.GetProjects(), resetOnPause);
	}

	public void Initialize(string[] playlist, bool resetOnPause = false) //Kiosk mode if there are no parameters
	{
		isKiosk = true;
		resetOnHmdPause = resetOnPause;
		projects = playlist;
		SceneManager.LoadScene("Template/VideoPlayer", LoadSceneMode.Single);
		App.Fade(false, 1f, delegate (bool s, string e)
		{
			QualitySettings.antiAliasing = 0;
			Debug.Log("AA to 0");
			PlayNextKiosk();
		});


		//5 second thing
		if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
		{
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
			if (hmdUnmount != null) {
				OVRManager.HMDUnmounted -= hmdUnmount;
				hmdUnmount = null;
			}
			if (hmdMount != null) {
				OVRManager.HMDMounted -= hmdMount;
				hmdMount = null;
			}

			if (resetOnPause) {
				hmdUnmount = delegate () {
					if (App.Player == null) return;
					Debug.Log("OVR headset unmounted");
					HMDOffTime = Time.realtimeSinceStartup;
				};
				OVRManager.HMDUnmounted += hmdUnmount;

				hmdMount = delegate () {
					if (App.Player == null) return;
					Debug.Log("OVR headset mounted");
					if (Time.realtimeSinceStartup - HMDOffTime > 4f)
					{
						if (!Instance.isKiosk)
						{
							StopVideo();
						}
						else
						{
							App.DestroyVideoPlayer();
							App.Fade(true, 1f, delegate (bool s2, string e2)
							{
								App.Fade(false, 0f, null);
								kioskCurrentVideo = 0;
								PlayNextKiosk();
							});
						}

					}
					HMDOffTime = -1f;
				};
				OVRManager.HMDMounted += hmdMount;
			}
#endif
		}
	}
	
	private void OnApplicationPause(bool pause)
	{
		if (App.Player == null) return;
		if (App.CurrentPlatform != App.VRPlatform.Oculus && 
			App.CurrentPlatform != App.VRPlatform.OculusLegacy && 
			resetOnHmdPause
		) {
			if (pause)
			{
				Debug.Log("headset unmounted");
				onPauseTime = Time.realtimeSinceStartup;
			}
			else
			{
				Debug.Log("headset mounted");
				if (Time.realtimeSinceStartup - onPauseTime > 4f)
				{
					if (!Instance.isKiosk)
					{
						StopVideo();
					}
					else
					{
						App.DestroyVideoPlayer();
						App.Fade(true, 1f, delegate (bool s2, string e2)
						{
							App.Fade(false, 0f, null);
							kioskCurrentVideo = 0;
							PlayNextKiosk();
						});
					}
				}
				onPauseTime = -1f;
			}
		}
	}

	public void PlayNextKiosk(bool increment=true)
	{
		Debug.Log("KIOSK Play");
		
		kioskCurrentVideo = (int)Mathf.Repeat(kioskCurrentVideo, projects.Length);
		Debug.Log("Current kiosk index: " + kioskCurrentVideo);
		Debug.Log("Current kiosk project: " + projects[kioskCurrentVideo]);
		isStream = false;

		string monoScaleStr = null;
		if (CustomVariables.TryGetVariable<string>("mono_sphere_radius", out monoScaleStr, 
			projects[kioskCurrentVideo])) {
			float monoScale = string.IsNullOrEmpty(monoScaleStr) ? 10f : float.Parse(monoScaleStr);
			App.MonoSphereRadius = monoScale;
		}

		App.Play(projects[kioskCurrentVideo], false, false, delegate (bool s, string e)
		{
			App.MonoSphereRadius = 10f;

			if(increment) kioskCurrentVideo++;
			App.DestroyVideoPlayer();
			App.Fade(true, 1f, delegate (bool s2, string e2)
			 {
				 App.Fade(false, 0f, null);
				 PlayNextKiosk();
			 });
		});
	}

	public void StopVideo()
	{
		App.Player?.Pause();
		App.Player?.Stop();
		OnVideoEnd(true,null);
	}

	public void OnVideoEnd(bool succes, string error)
	{
		App.MonoSphereRadius = 10f;

		Debug.Log("OnVideoEnd called");
		App.Fade(true, 1f, delegate (bool s, string e) {
			if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
				if (hmdUnmount != null) {
					OVRManager.HMDUnmounted -= hmdUnmount;
					hmdUnmount = null;
				}
				if (hmdMount != null) {
					OVRManager.HMDMounted -= hmdMount;
					hmdMount = null;
				}
#endif
			}

			SceneManager.LoadScene("Template/MainMenu", LoadSceneMode.Single);
			App.Fade(false, 1f, delegate (bool fadeOutSuccess, string fadeOutError) {
				onVideoEnd?.Invoke(succes, error);
				onVideoEnd = null;
			});
		});
	}
}
