using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
using Headjack.Utils;
using Headjack.Cinema;
using Headjack.Cinema.Messages;
using LiteNetLib;

/// <summary>
/// Handles all the basic cinema remote control events between this client and any (local) server
/// </summary>
public class CinemaHandler : MonoBehaviour {
    private const string NAME_PREFERENCE_KEY = "headjack_cinema_name";
    // (soft) singleton instance
    public static CinemaHandler instance;
    
    public bool CinemaStarted { get; private set; }
    // whether a video playback command from Cinema controller is currently active
    public bool IsPlaying { get; private set; }
    // whether the current playback is in a paused state
    public bool IsPaused { get; private set; }
    // whether currently connected to a Local Cinema controller
    public bool IsConnected { get; private set; }
    // whether any project is currently actively downloading
    public bool IsDownloading { get; private set; }
    public List<string> DownloadQueue { get; private set; }
    public string DeviceName { get; private set; }
    public string LatestMessage { get; private set; }

    // a reusable status message
    private MessageStatus statusMessage;
    // a reusable log message
    private MessageLog logMessage;
    // a reusable device health message
    private MessageDeviceHealth healthMessage;
    // a reusable playback status message
    private MessagePlayback playbackMessage;
    // a reusable viewing direction message
    private MessageViewDirection directionMessage;
    // coroutines that run to update the servers
    private Coroutine deviceHealthUpdate;
    private Coroutine playbackUpdate;
    private Coroutine viewDirectionUpdate;
    // stores previous actively playing project to check for changes
    private string previousActiveProject;
    // store previous application paused state, to stop video playback on return from pause
    private bool wasPaused = false;
    // while app is paused (or headset of off), store playback state to recover on resume
    private long previousPlayTimestamp = 0;
    private long previousVideoTime = 0;
    private long previousDuration = 0;
    private bool previousPaused = false;
    // headset on/off status (to resync playback)
    private bool HMDLost = false;

    void Start() {
        // save instance as only singleton instance
        if (instance != null) {
            Debug.LogWarning("[CinemaHandler] CinemaHandler singleton instance already exists");
        } else {
            instance = this;
            DontDestroyOnLoad(this);
        }
        // defaults
        IsPlaying = false;
        IsPaused = true;
        IsConnected = false;
        DownloadQueue = new List<string>();
        DeviceName = null;
        LatestMessage = null;

        statusMessage = new MessageStatus();
        logMessage = new MessageLog();
        healthMessage = new MessageDeviceHealth();
        playbackMessage = new MessagePlayback();
        directionMessage = new MessageViewDirection();

        // register cinema message callback
        LocalClient.Instance.RegisterMessageCallback(OnMessage);
    }

    void OnDestroy() {
        // unregister cinema message callback
        if (LocalClient.HasInstance) {
            LocalClient.Instance.UnregisterMessageCallback(OnMessage);
            if (instance == this) {
                instance = null;
            }
        }
    }

    private void OnMessage(NetPeer peer, MessageBase message) {
        // handle the different default messages and send back the current client status
        switch (message.MessageType) {
            case (int)DefaultMessages.Connect:
                // connected to server (before syncing)
                IsConnected = true;
                // start syncing
                statusMessage.status = MessageStatus.Status.Syncing;
                LocalClient.Instance.SendMessage(peer, statusMessage);

                // reset playback message so new server is ensured up to date playback status
                playbackMessage = new MessagePlayback();

                SendDeviceInfo(peer);
                SendSavedName(peer);
                SendAllDownloadProgress(peer);
                break;
            case (int)DefaultMessages.Synced:
                // successfully synced time with server, ready for cinema to start
                statusMessage.status = MessageStatus.Status.Ready;
                LocalClient.Instance.SendMessage(peer, statusMessage);

                // start coroutines to send device state to server(s)
                if (deviceHealthUpdate == null) {
                    deviceHealthUpdate = StartCoroutine(UpdateDeviceHealth());
                    playbackUpdate = StartCoroutine(UpdatePlaybackState());
                    viewDirectionUpdate = StartCoroutine(UpdateViewDirection());
                }
                break;
            case (int)DefaultMessages.Disconnect:

                if (peer.NetManager.PeersCount == 0) {
                    // no server connections left
                    IsConnected = false;

                    if (deviceHealthUpdate != null) {
                        // stop update loops
                        StopCoroutine(deviceHealthUpdate);
                        StopCoroutine(playbackUpdate);
                        StopCoroutine(viewDirectionUpdate);
                        
                        deviceHealthUpdate = null;
                        playbackUpdate = null;
                        viewDirectionUpdate = null;
                    }
                }
                break;
            case (int)DefaultMessages.Name:
                // received new device name from server
                MessageName nameMessage = (MessageName)message;
                PlayerPrefs.SetString(NAME_PREFERENCE_KEY, nameMessage.name);
                DeviceName = nameMessage.name;
                break;
            case (int)DefaultMessages.SetCinema:
                // cinema is starting (at timestamp)
                // setting cinema logic as coroutine to allow for delay based on server timestamp
                StartCoroutine(SetCinema(peer, (MessageSetCinema)message));
                break;
            case (int)DefaultMessages.PrepareVideo:
                PrepareVideo((MessagePrepareVideo)message);
                break;
            case (int)DefaultMessages.SeekTo:
                SeekTo(peer, (MessageSeekTo)message);
                break;
            case (int)DefaultMessages.PlayAt:
                PlayAt(peer, (MessagePlayAt)message);
                break;
            case (int)DefaultMessages.PauseAt:
                PauseAt(peer, (MessagePauseAt)message);
                break;
            case (int)DefaultMessages.StopPlayback:
                StartCoroutine(StopPlayback(peer, (MessageStopPlayback)message));
                break;
            case (int)DefaultMessages.DownloadProject:
                DownloadProject((MessageDownloadProject)message);
                break;
            case (int)DefaultMessages.CancelDownload:
                MessageCancelDownload cancelMessage = (MessageCancelDownload)message;
                App.Cancel(cancelMessage.projectId);
                DownloadQueue.Remove(cancelMessage.projectId);
                break;
            case (int)DefaultMessages.DeleteProject:
                MessageDeleteProject deleteMessage = (MessageDeleteProject)message;
                App.Delete(deleteMessage.projectId);
                StartCoroutine(UpdateDownloadProgress(deleteMessage.projectId));
                break;
            case (int)DefaultMessages.ShowMessage:
                LatestMessage = ((MessageShowMessage)message).message;
                break;
            case (int)DefaultMessages.ShowProject:
                // TODO: show specific project details in interface
                break;
            case (int)DefaultMessages.SetSubtitle:
                SetSubtitle((MessageSetSubtitle)message);
                break;
        }
    }

    private void SendDeviceInfo(NetPeer peer) {
        MessageDeviceInfo deviceInfo = new MessageDeviceInfo() {
            vrSdk = App.CurrentPlatform.ToString(),
            os = SystemInfo.operatingSystem,
            model = SystemInfo.deviceModel
        };
        LocalClient.Instance.SendMessage(peer, deviceInfo);
    }

    /// <summary>
    /// This client app stores its own custom name/alias and sends that
    /// on new server connection
    /// <summary>
    private void SendSavedName(NetPeer peer) {
        string currentName = PlayerPrefs.GetString(NAME_PREFERENCE_KEY);
        if (string.IsNullOrEmpty(currentName)) {
            // no stored name to send to server
            // so sending null so server can generate name
            currentName = null;
        } else {
            DeviceName = currentName;
        }

        MessageName nameMessage = new MessageName() {
            name = currentName
        };
        LocalClient.Instance.SendMessage(peer, nameMessage);
    }

    private IEnumerator SetCinema(NetPeer peer, MessageSetCinema startMessage) {
        // start downloading all app content immediately at server request
        if (startMessage.downloadAll) {
            string[] allProjects = App.GetProjects();
            for (int i=0; i < allProjects.Length; ++i) {
                int currentI = i;
                App.Download(allProjects[currentI], true, delegate(bool downloadSuccess, string downloadError) {
                    if (!downloadSuccess) {
                        // send download error message to server(s)
                        logMessage.level = MessageLog.Level.Error;
                        logMessage.message = $"Error downloading project {allProjects[currentI]}: {downloadError}";
                        LocalClient.Instance.SendMessage(logMessage);
                    }
                });
                StartCoroutine(UpdateDownloadProgress(allProjects[currentI]));
            }
        }

        ServerMetadata serverData = (ServerMetadata)peer.Tag;
        if (serverData.GetServerTime() < startMessage.timestamp) {
            yield return new WaitForSecondsRealtime(0.001f * (startMessage.timestamp - serverData.GetServerTime()));
        }

        // start/stop cinema
        CinemaStarted = startMessage.enable;

        if (CinemaStarted) {
            statusMessage.status = MessageStatus.Status.Ready;
        } else {
            statusMessage.status = MessageStatus.Status.Inactive;
        }
        LocalClient.Instance.SendMessage(peer, statusMessage);

        yield break;
    }

    /// <summary>
    /// Load the video player and a specific project
    /// </summary>
    private void PrepareVideo(MessagePrepareVideo prepareMessage) {
        if (App.Player != null) {
            App.Player.Stop();
            App.DestroyVideoPlayer();
        }

        IsPlaying = true;
        IsPaused = true;

        // update subtitles
        DownloadSubtitles(prepareMessage.projectId);

        App.Fade(true, 1f, delegate (bool s, string e)
        {
            VideoPlayerManager.Instance.Initialize(prepareMessage.projectId, !App.GotFiles(prepareMessage.projectId), delegate (bool playSuccess, string playError) {
                IsPlaying = false;
                IsPaused = true;
                if (!playSuccess) {
                    Debug.LogError($"[CinemaHandler] Error playing project {prepareMessage.projectId}: {playError}");
                    MessageLog logMessage = new MessageLog();
                    logMessage.level = MessageLog.Level.Error;
                    logMessage.message = $"Error playing project :project={prepareMessage.projectId}:: {playError}";
                    LocalClient.Instance.SendMessage(logMessage);
                    return;
                }

                Debug.Log($"[CinemaHandler] Finished playing project {prepareMessage.projectId}");
            }, false, true, prepareMessage.videoTime);
        });
    }

    /// <summary>
    /// Use existing video player and loaded project and seek to specific timecode
    /// </summary>
    private void SeekTo(NetPeer server, MessageSeekTo seekMessage) {
        // cannot start playing if the video player is not initialized
        if (App.Player == null) {
            Debug.LogError("[CinemaHandler] Error seeking (SeekTo) as the video player is not initialized");
            MessageLog logMessage = new MessageLog();
            logMessage.level = MessageLog.Level.Error;
            logMessage.message = $"Error seeking as the video player is not initialized";
            LocalClient.Instance.SendMessage(logMessage);
            return;
        }

        IsPaused = true;
        App.Player.SeekTo(seekMessage.GetLocalTimestamp(server), seekMessage.videoTime);
    }

    /// <summary>
    /// Use existing video player and loaded project and 
    /// start playing at a specific time from a specific video timestamp
    /// </summary>
    private void PlayAt(NetPeer server, MessagePlayAt playMessage) {
        // cannot start playing if the video player is not initialized
        if (App.Player == null) {
            Debug.LogError("[CinemaHandler] Error playing (PlayAt) as the video player is not initialized");
            MessageLog logMessage = new MessageLog();
            logMessage.level = MessageLog.Level.Error;
            logMessage.message = $"Error playing as the video player is not initialized";
            LocalClient.Instance.SendMessage(logMessage);
            return;
        }

        IsPaused = false;
        App.Player.PlayAt(playMessage.GetLocalTimestamp(server), playMessage.videoTime);
    }

    /// <summary>
    /// Pause currently playing video at a specific time
    /// </summary>
    private void PauseAt(NetPeer server, MessagePauseAt pauseMessage) {
        // cannot start playing if the video player is not initialized
        if (App.Player == null) {
            Debug.LogError("[CinemaHandler] Error pausing (PauseAt) as the video player is not initialized");
            MessageLog logMessage = new MessageLog();
            logMessage.level = MessageLog.Level.Error;
            logMessage.message = $"Error pausing as the video player is not initialized";
            LocalClient.Instance.SendMessage(logMessage);
            return;
        }

        IsPaused = true;
        App.Player.PauseAt(pauseMessage.GetLocalTimestamp(server));
    }

    private IEnumerator StopPlayback(NetPeer server, MessageStopPlayback stopMessage) {
        long stopTime = stopMessage.GetLocalTimestamp(server);

        if (stopTime < TimeStamp.GetMsLong()) {
            Debug.Log("[CinemaHandler] Stop playback command with timestamp in the past, stopping immediately");
        } else {
            yield return new WaitForSecondsRealtime(0.001f * (stopTime - TimeStamp.GetMsLong()));
        }
        
        IsPlaying = false;
        IsPaused = true;
        // enable menu
        MainVideoControls.instance.ToMenu();
        yield break;
    }

    /// <summary>
    /// Download a single project
    /// </summary>
    private void DownloadProject(MessageDownloadProject downloadMessage) {
        if (!DownloadQueue.Contains(downloadMessage.projectId)) {
            DownloadQueue.Add(downloadMessage.projectId);
        }

        if (!IsDownloading && DownloadQueue.Count > 0) {
            StartDownload(DownloadQueue[0]);
        }
    }

    private void StartDownload(string projectId) {
        IsDownloading = true;
        App.Download(projectId, false, delegate(bool downloadSuccess, string downloadError) {
            if (!downloadSuccess) {
                // send download error message to server(s)
                logMessage.level = MessageLog.Level.Error;
                logMessage.message = "Error downloading project :project=" + projectId + ":: " + downloadError;
                LocalClient.Instance.SendMessage(logMessage);
            }
            // start next download in queue
            DownloadQueue.Remove(projectId);
            if (DownloadQueue.Count > 0) {
                StartDownload(DownloadQueue[0]);
            } else {
                IsDownloading = false;
            }
        });
        StartCoroutine(UpdateDownloadProgress(projectId));
        // download subtitles
        DownloadSubtitles(projectId);
    }

    /// <summary>
    /// Infrequently update the server(s) with the current device health (battery/temp/etc.)
    /// NOTE: coroutine runs forever unless explicitly stopped
    /// <summary>
    private IEnumerator UpdateDeviceHealth() {
        int currentBattery = 0;
        int currentTemp = 0;

        while (true) {
            currentBattery = Mathf.RoundToInt(SystemInfo.batteryLevel * 100f);
            if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy) {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
                currentTemp = Mathf.RoundToInt(OVRManager.batteryTemperature);
#endif
            }
            // only update servers if device health changed
            if (healthMessage.battery != currentBattery || healthMessage.temperature != currentTemp) {
                healthMessage.battery = currentBattery;
                healthMessage.temperature = currentTemp;
                LocalClient.Instance.SendMessage(healthMessage);
            }

            yield return new WaitForSecondsRealtime(10f);
        }
    }

    /// <summary>
    /// Update server(s) on download progress of a specific project
    /// </summary>
    private IEnumerator UpdateDownloadProgress(string projectId, NetPeer peer = null) {
        if (string.IsNullOrEmpty(projectId)) {
            Debug.LogError("[CinemaHandler] Attempting to update download progress of non-existent project ID");
            yield break;
        }
        App.ProjectMetadata projectMeta = App.GetProjectMetadata(projectId, App.ByteConversionType.bytes);
        if (projectMeta == null) {
            Debug.LogError($"[CinemaHandler] Metadata for project ID {projectId} could not be retrieved. Cannot update download progress");
            yield break;
        }
        MessageDownloadProgress progressMessage = new MessageDownloadProgress() {
            projectId = projectId,
            total = System.Convert.ToInt64(projectMeta.TotalSize),
            progress = 0,
            status = MessageDownloadProgress.Status.None
        };
        bool progressChanged = false;
        while (true) {
            progressChanged = false;
            if (App.ProjectIsDownloading(projectId)) {
                float downloadProgress = System.Math.Min(1f, System.Math.Max(0f, 0.01f * App.GetProjectProgress(projectId)));
                long longProgress = System.Convert.ToInt64(downloadProgress * progressMessage.total);
                if (progressMessage.status != MessageDownloadProgress.Status.Downloading || progressMessage.progress != longProgress) {
                    progressChanged = true;
                }
                progressMessage.progress = longProgress;
                progressMessage.status = MessageDownloadProgress.Status.Downloading;
            } else if (App.UpdateAvailable(projectId)) {
                progressMessage.progress = 0;
                progressMessage.status = MessageDownloadProgress.Status.Downloadable;
                progressChanged = true;
            } else {
                progressMessage.progress = progressMessage.total;
                progressMessage.status = MessageDownloadProgress.Status.Downloaded;
                progressChanged = true;
            }

            if (progressChanged) {
                if (peer == null) {
                    LocalClient.Instance.SendMessage(progressMessage);
                } else {
                    LocalClient.Instance.SendMessage(peer, progressMessage);
                }
            }

            if (progressMessage.status != MessageDownloadProgress.Status.Downloading) {
                // if we are not currently downloading, then the download progress will not change,
                // so we can stop updating
                break;
            }

            yield return new WaitForSecondsRealtime(1f);
        }
        yield break;
    }

    /// <summary>
    /// Update server(s) on download progress of all projects
    /// </summary>
    private void SendAllDownloadProgress(NetPeer peer = null) {
        string[] allProjects = App.GetProjects();
        for (int i=0; i < allProjects.Length; ++i) {
            // TODO: prevent duplicate coroutines for same server and same project
            StartCoroutine(UpdateDownloadProgress(allProjects[i], peer));
        }
    }

    /// <summary>
    /// Updates all servers when the playback state changes (e.g. from buffering to playing)
    /// NOTE: coroutine runs forever unless explicitly stopped
    /// </summary>
    private IEnumerator UpdatePlaybackState() {
        while (true) {
             SendPlaybackState();
             if ((int)playbackMessage.status >= (int)MessagePlayback.Status.Buffering) {
                 // update more regularly when actively playing content
                 yield return new WaitForSecondsRealtime(1.5f);
             } else {
                 yield return new WaitForSecondsRealtime(3f);
             }
        }
    }

    private void SendPlaybackState() {
        MessagePlayback.Status currentStatus;
        if (App.Player == null) {
            currentStatus = MessagePlayback.Status.NotReady;
        } else {
            switch (App.Player.CurrentStatus) {
                case VideoPlayerBase.Status.VP_ERROR:
                    currentStatus = MessagePlayback.Status.Error;
                    break;
                case VideoPlayerBase.Status.VP_NOT_READY:
                default:
                    currentStatus = MessagePlayback.Status.NotReady;
                    break; 
                case VideoPlayerBase.Status.VP_END:
                case VideoPlayerBase.Status.VP_READY:
                case VideoPlayerBase.Status.VP_STOPPED:
                    currentStatus = MessagePlayback.Status.Ready;
                    break;
                case VideoPlayerBase.Status.VP_BUFFERING:
                    currentStatus = MessagePlayback.Status.Buffering;
                    break;
                case VideoPlayerBase.Status.VP_PLAYING:
                    currentStatus = MessagePlayback.Status.Playing;
                    break;
                case VideoPlayerBase.Status.VP_PAUSED:
                    currentStatus = MessagePlayback.Status.Paused;
                    break;
            }
        }

        if (currentStatus != playbackMessage.status || App.CurrentProject != previousActiveProject) {
            // playback state changed, updating server
            long localStartTime = 0;
            previousActiveProject = App.CurrentProject;
            playbackMessage.status = currentStatus;
            if ((int)playbackMessage.status >= (int)MessagePlayback.Status.Ready) {
                playbackMessage.activeProject = App.CurrentProject;
                playbackMessage.videoTotalTime = App.Player.Duration;

                // TODO: get correct timestamps and video timecodes from video player
                
                playbackMessage.videoStartTime = App.Player.SeekMs;
                localStartTime = TimeStamp.GetMsLong();
            } else {
                playbackMessage.activeProject = null;
                playbackMessage.videoTotalTime = 0;
                playbackMessage.videoStartTime = 0;
                playbackMessage.startTimestamp = 0;
            }

            if (localStartTime == 0) {
                LocalClient.Instance.SendMessage(playbackMessage);
            } else {
                // send each server their own corrected timestamp
                ServerMetadata serverMeta;
                foreach(NetPeer server in LocalClient.Instance.Servers) {
                    serverMeta = (ServerMetadata)server.Tag;
                    playbackMessage.startTimestamp = localStartTime + serverMeta.TimeOffset;
                    LocalClient.Instance.SendMessage(server, playbackMessage);
                }
            }
        }
    }

    /// <summary>
    /// When a video is playing (or paused), frequently update the server(s) with the current view direction
    /// NOTE: coroutine runs forever unless explicitly stopped
    /// </summary>
    private IEnumerator UpdateViewDirection() {
        ServerMetadata serverMeta = null;
        long localTimestamp = 0;
        while (true) {
            if (App.Player != null) {
                VideoPlayerBase.Status currentPlayState = App.Player.CurrentStatus;
                if (currentPlayState == VideoPlayerBase.Status.VP_PLAYING
                    || currentPlayState == VideoPlayerBase.Status.VP_PAUSED) {
                    directionMessage.viewDirection = App.camera.rotation;
                    directionMessage.videoTime = App.Player.SeekMs;

                    // send each server their own corrected timestamp
                    localTimestamp = TimeStamp.GetMsLong();
                    foreach(NetPeer server in LocalClient.Instance.Servers) {
                        serverMeta = (ServerMetadata)server.Tag;
                        directionMessage.timestamp = localTimestamp + serverMeta.TimeOffset;
                        LocalClient.Instance.SendMessage(server, directionMessage);
                    }

                    // update server frequently enough with view direction to allow
                    // believable motion to be interpolated
                    yield return new WaitForSecondsRealtime(0.25f);
                    continue;
                }
            }
            // check playback state less frequently when not currently playing
            yield return new WaitForSecondsRealtime(2.5f);
        }
    }

    /// <summary>
    /// resync playing video on resuming from paused app
    /// </summary>
    private void OnPauseStateChange(bool paused) {
        if (paused && !wasPaused) {
            // save current synced playback to resume after pause
            if (IsPlaying && App.Player != null) {
                previousPlayTimestamp = App.Player.LastTimestamp;
                previousVideoTime = App.Player.LastVideoTime;
                previousDuration = App.Player.Duration;
                previousPaused = IsPaused;
            }
        } else if (!paused && wasPaused) {
            // resuming from app pause
            // resume synced playback after pausing
            if (IsPlaying && App.Player != null) {
                // if synced video has already finished, do not bother resuming
                if (!previousPaused) {
                    long currentSyncedTime = previousVideoTime;
                    currentSyncedTime += TimeStamp.GetMsLong() - previousPlayTimestamp;
                    if (currentSyncedTime <= previousDuration) {
                        // start playback on this controller device
                        App.Player.PlayAt(previousPlayTimestamp, previousVideoTime);
                    } else {
                        // synced video finished, stop local playback
                        IsPlaying = false;
                        // enable menu
                        MainVideoControls.instance.ToMenu();
                    }
                }

                previousDuration = 0;
                previousPaused = true;
                previousPlayTimestamp = 0;
                previousVideoTime = 0;
            }
        }
        wasPaused = paused;
    }

    private void SetSubtitle(MessageSetSubtitle setSubMessage)
    {
        if (App.CurrentProject != setSubMessage.projectId) {
            logMessage.level = MessageLog.Level.Error;
            logMessage.message = $"Could not set subtitle for project {setSubMessage.projectId}, it is not the currently playing project";
            LocalClient.Instance.SendMessage(logMessage);
            return;
        }
        
        if (MainVideoControls.instance == null) {
            logMessage.level = MessageLog.Level.Error;
            logMessage.message = $"Could not set subtitle for project {setSubMessage.projectId}, the video player does not appear to be loaded (yet)";
            LocalClient.Instance.SendMessage(logMessage);
            return;
        }

        MainVideoControls.instance.subtitlesIndex = setSubMessage.subtitleIndex;
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

    void Update() {
        // handle headset events
        if (App.CurrentPlatform == App.VRPlatform.Oculus || App.CurrentPlatform == App.VRPlatform.OculusLegacy)
        {
#if USE_OCULUS_SDK || USE_OCULUS_LEGACY_SDK
            if (!HMDLost)
            {
                if (!OVRPlugin.hasVrFocus || !OVRPlugin.userPresent)
                {
                    OnPauseStateChange(true);
                    HMDLost = true;
                }
            }
            else
            {
                if (OVRPlugin.hasVrFocus && OVRPlugin.userPresent)
                {
                    OnPauseStateChange(false);
                    HMDLost = false;
                }
            }
#endif
        }
    }

    void OnApplicationPause(bool paused) {
        OnPauseStateChange(paused);
    }
}