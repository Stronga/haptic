using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Headjack;
using SimpleJSON;

/*
 * Registers listeners for Cinema server messages and handles them
 */
public class CinemaListenerExample : MonoBehaviour {
	public static CinemaListenerExample instance;
    public bool playing = false;
    public bool cinemaConnected = false;
    long currentVideoLength = 0;
    string playingProjectId = null;

    List<string> downloadingProjects = new List<string>();
    List<float> projectSize = new List<float>();
    float finishedDownloadingSize = 0;

    Coroutine updatingDownloadProgress = null;
    Coroutine updatingPlaybackPosition = null;
	public List<string> downloadQueue;
	public string deviceName = "";
	public string message = "";

	// Use this for initialization
	void OnEnable () {
		instance = this;
        // verify that Cinema is enabled both in this template and on the server
        if (!App.CinemaSupported)
        {
            Debug.LogWarning("This template either does not have the \"Cinema supported\" template setting enabled, or the Cinema feature was not switched on for this app on the cinema control webpage");
        }

		downloadQueue = new List<string>();

			
		

		DontDestroyOnLoad(this);
		Debug.Log("CINEMA START");
        // The Cinema host has changed the name of the device running this app
        App.RegisterCinemaListener("changeAlias", delegate (JSONArray args) {
			// args[0] contains the new device name/alias
			Debug.Log("Name changed to " + args[0]);
			deviceName = args[0];
        });
        // The host wants the app to download a specific project
        App.RegisterCinemaListener("download", delegate (JSONArray args) {
            // args[0] contains project ID
            string projectId = args[0];
            if (!App.GotFiles(projectId)) {
                // download subtitles for project
                DownloadSubtitles(projectId);
                // download project
                TrackDownloadProgress(projectId);
				downloadQueue.Add(projectId);
				Debug.Log($"Adding {projectId} to cinema download queue");
				App.Download(projectId, false, delegate (bool succ, string err) {
                    UntrackDownloadProgress(projectId);
                    // send status
                    if (succ) {
                        App.SendCinemaStatus(GetCurrentStatus(), "Downloaded " + GetProjectTitle(projectId));
                    } else if (err != "Cancel") {
                        App.SendCinemaStatus("error", "Failed to download " + GetProjectTitle(projectId) + ": " + err);
                    }
					Debug.Log($"Removing {projectId} from cinema download queue");
					downloadQueue.Remove(projectId);
				});


                // send status
                App.SendCinemaStatus(GetCurrentStatus(), "Downloading " + GetProjectTitle(projectId));
            } else {
                // send status
                App.SendCinemaStatus(GetCurrentStatus(), "Already downloaded " + GetProjectTitle(projectId));
            }
        });
        // The host wants the app to stop downloading a specific project
        App.RegisterCinemaListener("cancel", delegate (JSONArray args) {
            // args[0] contains project ID
            string projectId = args[0];
            if (App.ProjectIsDownloading(projectId)) {
                App.Cancel(projectId);
                UntrackDownloadProgress(projectId, false);
                // send success status
                App.SendCinemaStatus(GetCurrentStatus(), "Canceled downloading " + GetProjectTitle(projectId), 0, 0);
            }
            else {
                // send not applicable status
                App.SendCinemaStatus(GetCurrentStatus(), "Can't cancel download, project not downloading: " + GetProjectTitle(projectId));
            }
        });
        // The host wants the app to play a specific project
        App.RegisterCinemaListener("play", delegate (JSONArray args) {
			Debug.Log("CINEMA play command received");
            // args[0] contains project ID
            string projectId = args[0];
            playing = true;
            
            // set current video length
            if (projectId != null)
            {
                App.VideoMetadata videoData = App.GetVideoMetadata(projectId);
                if (videoData != null)
                {
                    currentVideoLength = System.Math.Max(videoData.Duration, 0);
                }

                playingProjectId = projectId;

                if (updatingPlaybackPosition == null)
                {
                    updatingPlaybackPosition = StartCoroutine(UpdatePlaybackPosition());
                }
            }
			App.Fade(true, 1f, delegate (bool s, string e)
			{
				VideoPlayerManager.Instance.Initialize(projectId, !App.GotFiles(projectId), delegate (bool succ, string err)
				{
					playing = false;
					currentVideoLength = 0;
					playingProjectId = null;


					// send finished status
					if (succ)
					{
						App.SendCinemaStatus(GetCurrentStatus(), "Finished playing " + GetProjectTitle(projectId), 0, 0);
					}
					else
					{
						// send playback error status
						App.SendCinemaStatus("error", "Playing " + GetProjectTitle(projectId) + " failed: " + err, 0, 0);
					}
				});
			});
            // send success status
            App.SendCinemaStatus("playing", "Playing " + GetProjectTitle(projectId));
        });
        // The host wants the app to delete a specific downloaded project
        App.RegisterCinemaListener("delete", delegate (JSONArray args) {
            // args[0] contains project ID
            string projectId = args[0];
            App.Delete(projectId);
            // send success status
            App.SendCinemaStatus(GetCurrentStatus(), "Deleted " + GetProjectTitle(projectId));
        });
        // The host wants the app to stop playing whatever video is currently playing.
        App.RegisterCinemaListener("stop", delegate {
            if (App.Player != null) {
                playing = false;
                currentVideoLength = 0;
                playingProjectId = null;
				// enable menu
				MainVideoControls.instance.ToMenu();
                // send success status
                App.SendCinemaStatus(GetCurrentStatus(), "Stopped playback", 0, 0);
            }
            else {
                // send error: nothing playing
                App.SendCinemaStatus(GetCurrentStatus(), "Could not stop playback, nothing was playing");
            }
        });
        // The host wants the app to pause playback of the current playing video.
        App.RegisterCinemaListener("pause", delegate {
            if (App.Player != null) {
                App.Player.Pause();
                // send success status
                App.SendCinemaStatus("paused", "Paused playback");
            }
            else {
                // send error: nothing playing
                App.SendCinemaStatus(GetCurrentStatus(), "Could not pause playback, nothing was playing");
            }
        });
        // The host wants the app to resume playback of the current paused video.
        App.RegisterCinemaListener("resume", delegate {
            if (App.Player != null) {
                App.Player.Resume();
                // send success status
                App.SendCinemaStatus("playing", "Resumed playback");
            }
            else {
                // send error: nothing playing
                App.SendCinemaStatus(GetCurrentStatus(), "Could not resume playback, nothing was playing");
            }
        });
        // The host wants the app to display the metadata (title, description and/or thumbnail) of a specific project.
        App.RegisterCinemaListener("view", delegate (JSONArray args) {
            // args[0] contains project ID

            // TODO: use Headjack.App.GetProjectMetadata(args[0]) to display relevant project's metadata

        });
        // The host wants the app to display a customized message.
        App.RegisterCinemaListener("message", delegate (JSONArray args) {
			// args[0] contains customized message string
			message = args[0];
            // TODO: show message to user

        });
    }
	
    string GetCurrentStatus() {
        if (playing) {
            if (App.Player != null && !App.Player.IsPlaying && !App.Player.Buffering) {
                    return "paused";
            }
            return "playing";
        }
        if (downloadingProjects.Count > 0) {
            return "downloading";
        }
        return "idle";
    }

    string GetProjectTitle(string projectId) {
        if (string.IsNullOrEmpty(projectId))
        {
            return "";
        }

        string title = App.GetProjectMetadata(projectId).Title;
        if (string.IsNullOrEmpty(title)) {
            return "";
        }

        return ArabicHelper.CorrectText(title);
    }

	// Update is called once per frame
	void Update()
	{
		//Check for cinema connection changes
		if (cinemaConnected != App.CinemaConnected)
		{
			cinemaConnected = App.CinemaConnected;
			Debug.Log("Cinema connection status changed to: " + cinemaConnected);
		}
	}

	void TrackDownloadProgress(string projectId)
    {
        if (projectId == null || projectId.Length == 0)
        {
            return;
        }

        if (downloadingProjects.Contains(projectId))
        {
            return;
        }

        App.ProjectMetadata projectData = App.GetProjectMetadata(projectId);

        if (projectData == null || projectData.TotalSize <= 0)
        {
            return;
        }

        downloadingProjects.Add(projectId);
        projectSize.Add(projectData.TotalSize);

        if (updatingDownloadProgress == null)
        {
            updatingDownloadProgress = StartCoroutine(UpdateDownloadProgress());
        }
    }

    void UntrackDownloadProgress(string projectId, bool countDownloaded = true)
    {
        if (projectId == null || projectId.Length == 0)
        {
            return;
        }

        int idIndex = downloadingProjects.IndexOf(projectId);
        if (idIndex < 0)
        {
            return;
        }

        if (countDownloaded)
        {
            finishedDownloadingSize += projectSize[idIndex];
        }

        downloadingProjects.RemoveAt(idIndex);
        projectSize.RemoveAt(idIndex);
    }

    IEnumerator UpdateDownloadProgress()
    {
        yield return new WaitForSecondsRealtime(5f);
        string statusText = "Downloading projects";

        while (downloadingProjects.Count > 0)
        {
            if (!playing)
            {
                float totalSize = finishedDownloadingSize;
                float totalProgress = finishedDownloadingSize;

                for (int i = 0; i < downloadingProjects.Count; ++i)
                {
                    totalSize += projectSize[i];
                    totalProgress += 0.01f * App.GetProjectProgress(downloadingProjects[i]) * projectSize[i];
                }

                statusText = "Downloading " + downloadingProjects.Count + " projects";
                if (downloadingProjects.Count == 1)
                {
                    statusText = "Downloading " + GetProjectTitle(downloadingProjects[0]);
                }

                App.SendCinemaStatus("downloading", statusText, totalProgress, totalSize, "MB");
            }
            yield return new WaitForSecondsRealtime(5f);
        }

        finishedDownloadingSize = 0;
        updatingDownloadProgress = null;
        yield break;
    }

    IEnumerator UpdatePlaybackPosition()
    {
        yield return null;

        while (playing)
        {
            if (App.Player != null)
            {
                long seekSec = System.Math.Max(App.Player.SeekMs, 0);
                if (currentVideoLength > 0)
                {
                    seekSec = System.Math.Min(seekSec, currentVideoLength);
                }
                App.SendCinemaStatus(GetCurrentStatus(), "Playing " + GetProjectTitle(playingProjectId), (float)seekSec, (float)currentVideoLength, "ms");
            }

            yield return new WaitForSecondsRealtime(5f);
        }

        updatingPlaybackPosition = null;
        yield break;
    }

    private void DownloadSubtitles(string projectId)
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
}
