// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_WSA_10_0

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Networking;

namespace Headjack
{
    internal class BgDownloader : PPDownloader
    {
        private Dictionary<string, JobInfo> Job;
        private Dictionary<string, int> ProjectActiveDLs;
        private Dictionary<string, long> ProjectProgress;

        internal new class JobInfo
        {
            internal string Url, Destination;
            internal List<string> ProjectIDs = new List<string>();

            internal BackgroundDownload Download;
            internal BackgroundDownloadStatus Status = BackgroundDownloadStatus.Downloading;
            internal string Error = null;

            internal bool RemoveNow;
            internal long Size, Progress;
            internal OnEnd onEnd;
        }

        internal override void Initialize()
        {
            Job = new Dictionary<string, JobInfo>();
            Active = new Dictionary<string, bool>();
            ProjectSize = new Dictionary<string, long>();
            ProjectOnEnd = new Dictionary<string, OnEnd>();
            ProjectActiveDLs = new Dictionary<string, int>();
            ProjectProgress = new Dictionary<string, long>();

            Debug.Log("Starting BgDownloader Coroutine");
            StartCoroutine(BgDownloadManager());
        }

        internal override float getProjectProgress(string pID)
        {
            if (ProjectSize.ContainsKey(pID) && ProjectSize[pID] > 0)
            {
				return Mathf.Max(0f, (100f * ProjectProgress[pID]) / ProjectSize[pID]);
            }
            else
            {
                Debug.LogWarning("Progress could not be retrieved");
                return -1f;
            }
        }

        internal override float getMediaProgress(string mediaId) {
            JobInfo mediaJob;
            if (!Job.TryGetValue(mediaId, out mediaJob) ||
                mediaJob.Size <= 0 ||
                mediaJob.Progress < 0) {
                return -1f;
            }

            return Mathf.Clamp(100f * mediaJob.Progress / mediaJob.Size, 0f, 100f);
        }

        internal override long getMediaDownloadedBytes(string mediaId) {
            JobInfo mediaJob;
            if (!Job.TryGetValue(mediaId, out mediaJob)) {
                return -1;
            }

            return mediaJob.Progress;
        }

        private bool isJobActive(string id)
        {
            if(Job.ContainsKey(id) && Job[id].Status == BackgroundDownloadStatus.Downloading)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns Job Info if job associated with Media ID is active,
        /// otherwise returns null
        /// </summary>
        private JobInfo GetActiveJob(string id) {
            if(Job.ContainsKey(id) && Job[id].Status == BackgroundDownloadStatus.Downloading)
            {
                return Job[id];
            }
            return null;
        }

        internal override void DownloadMedia(string MediaId, bool wifiOnly, OnEnd onEnd = null)
        {
            string downloadId = MediaId + "/" + App.Data.Media[MediaId].Filename;

            JobInfo job = GetActiveJob(downloadId);
            if (job == null)
            {
                // Create and register download Job for media
                JobInfo NewJob = new JobInfo();
                NewJob.Url = App.GetMediaUrl(MediaId);
                NewJob.Destination = App.BasePath + App.GetMediaPath(MediaId);
                NewJob.Size = App.Data.Media[MediaId].FileSize;
                NewJob.Progress = 0L;
                NewJob.RemoveNow = false;
                NewJob.onEnd = onEnd;

                BackgroundDownloadConfig dlConfig = new BackgroundDownloadConfig() {
                    url = new Uri(NewJob.Url),
                    filePath = NewJob.Destination,
                    uid = downloadId,
                    policy = wifiOnly ? BackgroundDownloadPolicy.UnrestrictedOnly : 
                            BackgroundDownloadPolicy.AlwaysAllow,
                    showNotification = false
                };
                try {
                    NewJob.Download = BackgroundDownload.Start(dlConfig);
                } catch (ArgumentException) {
                    BackgroundDownload[] downloads = BackgroundDownload.backgroundDownloads;
                    foreach (BackgroundDownload download in downloads) {
                        if (download.config.filePath == dlConfig.filePath) {
                            download.Dispose();
                            break;
                        }
                    }
                    NewJob.Download = BackgroundDownload.Start(dlConfig);
                }

                Job[downloadId] = NewJob;
                Debug.Log("Started Media download: " + downloadId);
            } else
            {
                Debug.Log("Media download already active: " + downloadId);
                job.onEnd += onEnd;
            }
        }

        internal override void DownloadProject(string ProjectId, bool wifiOnly, OnEnd onEnd = null)
        {
            if (!Active.ContainsKey(ProjectId))
            {
                Debug.Log("Adding new project to download: " + ProjectId);
                Active[ProjectId] = true;
                ProjectActiveDLs[ProjectId] = 0;
                ProjectProgress[ProjectId] = 0L;                
                long TotalSize = 0;

                string AudioId = App.GetProjectAudioId(ProjectId);
                if (AudioId != null && App.UpdateAvailableMedia(AudioId))
                {
                    string downloadId = AudioId + "/" + App.Data.Media[AudioId].Filename;
                    if (isJobActive(downloadId))
                    {
                        // Job already active, so just add project id to list
                        // NOTE: danger of adding project id multiple times to list, so testing for this
                        if (!Job[downloadId].ProjectIDs.Contains(ProjectId))
                        {
                            Job[downloadId].ProjectIDs.Add(ProjectId);
                            ++ProjectActiveDLs[ProjectId];
                            Debug.Log("Added " + ProjectId + " to list of projects of media " + downloadId);
                        }
                        ProjectProgress[ProjectId] += Job[downloadId].Progress;
                        TotalSize += Job[downloadId].Size;
                    }
                    else
                    {
                        // Add new Job
                        JobInfo NewJob = new JobInfo();
                        NewJob.Url = App.GetMediaUrl(AudioId);
                        NewJob.Destination = App.BasePath + App.GetMediaPath(AudioId);
                        NewJob.Size = App.Data.Media[AudioId].FileSize;
                        TotalSize += NewJob.Size;
                        NewJob.Progress = 0;
                        NewJob.RemoveNow = false;
                        NewJob.ProjectIDs.Add(ProjectId);
                        ++ProjectActiveDLs[ProjectId];
                        NewJob.onEnd = null;

                        BackgroundDownloadConfig dlConfig = new BackgroundDownloadConfig() {
                            url = new Uri(NewJob.Url),
                            filePath = NewJob.Destination,
                            uid = downloadId,
                            policy = wifiOnly ? BackgroundDownloadPolicy.UnrestrictedOnly : 
                                    BackgroundDownloadPolicy.AlwaysAllow,
                            showNotification = false
                        };
                        try {
                            NewJob.Download = BackgroundDownload.Start(dlConfig);
                        } catch (ArgumentException) {
                            BackgroundDownload[] downloads = BackgroundDownload.backgroundDownloads;
                            foreach (BackgroundDownload download in downloads) {
                                if (download.config.filePath == dlConfig.filePath) {
                                    download.Dispose();
                                    break;
                                }
                            }
                            NewJob.Download = BackgroundDownload.Start(dlConfig);
                        }

                        Job[downloadId] = NewJob;
                        Debug.Log("Started download of media " + downloadId + "for project " + ProjectId);
                    }
                }

				string videoId = App.GetProjectVideoId(ProjectId);
				if (videoId != null && App.UpdateAvailableVideo(videoId))
				{
					Debug.Log("downloading video " + videoId);
					
                    DeleteExistingVideo(videoId);

                    string videoFilename = App.GetVideoFilename(videoId);
                    string downloadId = videoId + "/" + videoFilename;
                    if (isJobActive(downloadId))
                    {
                        // Job already active, so just add project id to list
                        if (!Job[downloadId].ProjectIDs.Contains(ProjectId))
                        {
                            Job[downloadId].ProjectIDs.Add(ProjectId);
                            ++ProjectActiveDLs[ProjectId];
                            Debug.Log("Added " + ProjectId + " to list of projects of video " + downloadId);
                        }
                        ProjectProgress[ProjectId] += Job[downloadId].Progress;
                    }
                    else
                    {
                        JobInfo NewJob = new JobInfo();
                        NewJob.Url = App.GetVideoUrl(videoId);
                        NewJob.Destination = App.BasePath + App.GetVideoLocalSubPath(videoId, true);
                        NewJob.Size = App.GetVideoSize(videoId);
                        NewJob.Progress = 0;
                        NewJob.RemoveNow = false;
                        NewJob.ProjectIDs.Add(ProjectId);
                        ++ProjectActiveDLs[ProjectId];
                        NewJob.onEnd = null;

                        BackgroundDownloadConfig dlConfig = new BackgroundDownloadConfig() {
                            url = new Uri(NewJob.Url),
                            filePath = NewJob.Destination,
                            uid = downloadId,
                            policy = wifiOnly ? BackgroundDownloadPolicy.UnrestrictedOnly : 
                                    BackgroundDownloadPolicy.AlwaysAllow,
                            showNotification = true,
                            notificationTitle = App.Data.Project[ProjectId].Title
                        };
                        try {
                            NewJob.Download = BackgroundDownload.Start(dlConfig);
                        } catch (ArgumentException) {
                            BackgroundDownload[] downloads = BackgroundDownload.backgroundDownloads;
                            foreach (BackgroundDownload download in downloads) {
                                if (download.config.filePath == dlConfig.filePath) {
                                    download.Dispose();
                                    break;
                                }
                            }
                            NewJob.Download = BackgroundDownload.Start(dlConfig);
                        }

                        Job[downloadId] = NewJob;
                        Debug.Log("Started download of file " + downloadId + "for project " + ProjectId);
                    }
				}
				TotalSize += App.GetVideoFormatData(videoId, false).Filesize;

				Debug.Log("Total Size of " + ProjectId + ": " + TotalSize);
                if (!ProjectSize.ContainsKey(ProjectId))
                {
                    ProjectSize.Add(ProjectId, TotalSize);
					Debug.Log("Project Size: " + TotalSize);
				}

                if (!ProjectOnEnd.ContainsKey(ProjectId))
                {
                    ProjectOnEnd.Add(ProjectId, onEnd);
					Debug.Log("onend added project download queue: " + ProjectId);
				}
                else if (onEnd != null)
                {
                    onEnd(false, "another onEnd for this project was already registered");
                }
            }
            else
            {
                Debug.Log("Project already active: " + ProjectId);
                if (onEnd != null)
                {
                    onEnd(false, "Project is already downloading");
                }
            }
        }


        /// <summary>
        /// Loads background downloads that continued after the app closed last time
        /// and adds them to the current download queue to be handled.
        /// NOTE: requires the app data to be initialized
        /// </summary>
        internal void LoadExistingDownloads()
        {
            if (App.Data == null) {
                Debug.LogError("[BgDownloader] Cannot load existing downloads without app data");
                return;
            }

            BackgroundDownload[] downloads = BackgroundDownload.backgroundDownloads;
            if (downloads.Length <= 0) {
                return;
            }

            for (int i = 0; i < downloads.Length; i++) {
                BackgroundDownload download = downloads[i];
                // this might be a new download started before this load function, so skip it
                string uid = download.config.uid;
                if (Job.ContainsKey(uid)) { continue; }
                // extract media or video ID from UID
                int slashPos = uid.IndexOf("/");
                string id = uid;
                if (slashPos > 0) {
                    id = uid.Substring(0, slashPos);
                }
                // stop if download UID is no longer in current version of the app
                if (!App.Data.Media.ContainsKey(id) && !App.Data.Video.ContainsKey(id)) {
                    Debug.Log("[BgDownloader] disposing of old background download that is no longer in app");
                    download.Dispose();
                    continue;
                }
                // adds valid existing download to queue, which will handle the status and dispose
                // of finished/failed downloads
                JobInfo oldJob = new JobInfo();
                oldJob.Download = download;
                oldJob.Url = download.config.url.OriginalString;
                oldJob.Destination = download.config.filePath;
                oldJob.RemoveNow = false;
                if (App.Data.Media.ContainsKey(id)) {
                    oldJob.Size = App.Data.Media[id].FileSize;
                } else if (App.Data.Video.ContainsKey(id)) {
                    oldJob.Size = App.GetVideoSize(id);
                }
                oldJob.Progress = 0L;
                Job[uid] = oldJob;
            }
        }

        private IEnumerator BgDownloadManager()
        {
            string[] jobList = new string[16];
            string id;
            JobInfo cJobInfo;

            while (true)
            {
                if (Job.Count > jobList.Length) {
                    jobList = new string[Job.Count + 16];
                } else {
                    for (int i = 0; i < jobList.Length; ++i) {
                        jobList[i] = null;
                    }
                }
                
                Job.Keys.CopyTo(jobList, 0);
                // loop over active Jobs and update stats
                for (int cJobIndex = 0; cJobIndex < jobList.Length; ++cJobIndex) {
                    id = jobList[cJobIndex];
                    if (string.IsNullOrEmpty(id) || !Job.ContainsKey(id)) {
                        continue;
                    }

                    cJobInfo = Job[id];
                    if (cJobInfo == null) {
                        continue;
                    }

                    if (isJobActive(id) && !cJobInfo.RemoveNow) {
                        long PrevProgress = cJobInfo.Progress;
                        long PrevSize = cJobInfo.Size;

                        cJobInfo.Progress = cJobInfo.Download.downloadedBytes;
                        long fileSize = cJobInfo.Download.totalBytes;
                        if (fileSize > 0L) {
                            cJobInfo.Size = fileSize;
                        }
                        // handle changed progress and/or size for projects
                        foreach (string pID in cJobInfo.ProjectIDs)
                        {
                            ProjectProgress[pID] += cJobInfo.Progress - PrevProgress;
                            ProjectSize[pID] += cJobInfo.Size - PrevSize;

                            if (cJobInfo.Progress < PrevProgress || cJobInfo.Size != PrevSize)
                            {
                                Debug.LogWarning("Progress decreased or size changed, from " + PrevProgress + "/" + PrevSize + " to " + cJobInfo.Progress + "/" + cJobInfo.Size);
                            }
                        }

                        cJobInfo.Status = cJobInfo.Download.status;
                        cJobInfo.Error = cJobInfo.Download.error;

                        // handle status changes
						if (cJobInfo.Status == BackgroundDownloadStatus.Done)
						{
							if (!System.IO.File.Exists(cJobInfo.Destination))
							{
								Debug.LogWarning("File does not exist, but status is succesful");
								cJobInfo.Status = BackgroundDownloadStatus.Failed;
								cJobInfo.Error = "Downloaded file does not exist after successful download";
							}
						}

						if (cJobInfo.Status == BackgroundDownloadStatus.Done)
                        {
                            Debug.Log(id + " succesfully downloaded");
                            // dispose of backend download of finished download
                            cJobInfo.Download.Dispose();

                            // handle status == successful
                            if (App.Data != null)
                            {
                                int slashPos = id.IndexOf("/");
                                if (slashPos > 0) {
                                    string mId = id.Substring(0, slashPos);
                                    string mPath = id.Substring(slashPos+1);

                                    App.LocalVersionTracking.LocalVersion[mId] = mPath;

                                    yield return App.LocalVersionTracking.SaveData();
                                }
                            }
                            if (cJobInfo.onEnd != null)
                            {
                                cJobInfo.onEnd(true, null);
                                cJobInfo.onEnd = null;
                            }
                            for (int i = cJobInfo.ProjectIDs.Count - 1; i >= 0; --i)
                            {
                                string pID = cJobInfo.ProjectIDs[i];
                                --ProjectActiveDLs[pID];
                                if (ProjectActiveDLs[pID] == 0)
                                {
                                    Debug.Log("Project " + pID + " is finished downloading");

									// Project Finished, Handle this
									cJobInfo.ProjectIDs.RemoveAt(i);
                                    foreach (string jID in Job.Keys)
                                    {
                                        if (jID != id && Job[jID].Status == BackgroundDownloadStatus.Done && Job[jID].ProjectIDs.Contains(pID))
                                        {
                                            Job[jID].ProjectIDs.Remove(pID);
                                            if (Job[jID].ProjectIDs.Count == 0)
                                            {
                                                Debug.Log("Successful job " + jID + " has no unsuccessful project associated anymore, so we remove it");
                                                // Successful job has no unsuccessful project associated anymore, so we remove it
                                                Job[jID].RemoveNow = true;
                                            }
                                        }
                                    }
                                    // call project OnEnd
                                    if (ProjectOnEnd[pID] != null)
                                    {
                                        ProjectOnEnd[pID](true, null);
                                    }
                                    // clean up project
                                    Active.Remove(pID);
                                    ProjectActiveDLs.Remove(pID);
                                    ProjectProgress.Remove(pID);
                                    ProjectSize.Remove(pID);
                                    ProjectOnEnd.Remove(pID);
                                }
                            }

                            // remove finished job
                            if (cJobInfo.ProjectIDs.Count == 0)
                            {
                                Debug.Log("Successful job " + id + " has no unsuccessful project associated anymore, so we remove it (version2)");
                                cJobInfo.RemoveNow = true;
                            }
                        }
                        else if (cJobInfo.Status == BackgroundDownloadStatus.Failed)
                        {
                            Debug.Log(id + " failed");
                            // dispose of backend download of finished download
                            cJobInfo.Download.Dispose();

                            // download failed, delete any partial files
                            try {
                                System.IO.File.Delete(cJobInfo.Destination + ".part");
                            } catch (System.Exception) { }
                            try {
                                System.IO.File.Delete(cJobInfo.Destination);
                            } catch (System.Exception) { }

                            // handle status == failed
                            if (cJobInfo.onEnd != null)
                            {
                                cJobInfo.onEnd(false, cJobInfo.Error);
                                cJobInfo.onEnd = null;
                            }
                            // handle project failed
                            for (int i = cJobInfo.ProjectIDs.Count - 1; i >= 0; --i)
                            {
                                string failedProject = cJobInfo.ProjectIDs[i];
                                cJobInfo.ProjectIDs.RemoveAt(i);
                                // failed job, remove one active dl
                                --ProjectActiveDLs[failedProject];
                                foreach (string jID in Job.Keys)
                                {
                                    if (id != jID && Job[jID].Status != BackgroundDownloadStatus.Done && Job[jID].ProjectIDs.Contains(failedProject))
                                    {
                                        Job[jID].ProjectIDs.Remove(failedProject);
                                        --ProjectActiveDLs[failedProject];
                                        if (Job[jID].ProjectIDs.Count == 0 && Job[jID].onEnd == null)
                                        {
                                            Debug.Log("Only project of this job " + jID + " failed, canceling this");
                                            // Only project of this job failed, cancel this
                                            Debug.Log(jID + " canceled");
                                            cancelDownload(Job[jID]);
                                        }
                                    }
                                    
                                }

                                if (ProjectActiveDLs[failedProject] != 0)
                                {
                                    Debug.LogError("something went wrong with active dl count on project: " + failedProject);
                                }

                                // call project OnEnd with failure
                                if (ProjectOnEnd[failedProject] != null)
                                {
                                    ProjectOnEnd[failedProject](false, cJobInfo.Error);
                                }
                                // clean up project
                                Active.Remove(failedProject);
                                ProjectActiveDLs.Remove(failedProject);
                                ProjectProgress.Remove(failedProject);
                                ProjectSize.Remove(failedProject);
                                ProjectOnEnd.Remove(failedProject);
                                Debug.LogWarning("Job " + id + " of this project " + failedProject + " failed so project canceled");
                            }

                            cJobInfo.RemoveNow = true;
                        }
                        else if (cJobInfo.ProjectIDs.Count > 0)
                        {
                            // check for project cancel
                            for (int i = cJobInfo.ProjectIDs.Count - 1; i >= 0; --i)
                            {
                                string atProject = cJobInfo.ProjectIDs[i];
                                if (!Active[atProject])
                                {
                                    Debug.Log("Project " + cJobInfo.ProjectIDs[i] + " is being cancelled");
                                    // remove cancelled project from job project list and all lists if active project dls == 0
                                    cJobInfo.ProjectIDs.RemoveAt(i);
                                    --ProjectActiveDLs[atProject];
                                    if (ProjectActiveDLs[atProject] <= 0)
                                    {
                                        // call project OnEnd with cancel message
                                        if (ProjectOnEnd[atProject] != null)
                                        {
                                            ProjectOnEnd[atProject](false, "Cancel");
                                        }

                                        // all dls of this cancelled project have been cancelled, so removing project
                                        Active.Remove(atProject);
                                        ProjectActiveDLs.Remove(atProject);
                                        ProjectProgress.Remove(atProject);
                                        ProjectSize.Remove(atProject);
                                        ProjectOnEnd.Remove(atProject);
                                        Debug.Log("Project cancelled succesfully");
                                    }
                                }
                            }

                            if (cJobInfo.ProjectIDs.Count == 0)
                            {
                                // All projects this file belongs to have been cancelled, cancelling download
                                Debug.Log("Job " + id + " canceled because project cancel");
                                cancelDownload(cJobInfo);
                            }
                        }
                    }

                    // Copy each Job that has not had RemoveNow set to new Dictionary
                    // This is how we remove elements from the dictionary while iterating over it
                    if (cJobInfo.RemoveNow)
                    {
                        // removing this download from dictionary
                        Job.Remove(id);
                    }
                }

                // wait sometime before checking status again
                yield return new WaitForSeconds(1f);
            }
        }

        private void cancelDownload(JobInfo job) {
            job.Download.Dispose();
            if (job.Status != BackgroundDownloadStatus.Done) {
                job.Progress = 0L;
                job.Status = BackgroundDownloadStatus.Failed;
                job.Error = "Cancel";
                if (job.onEnd != null) {
                    job.onEnd(false, job.Error);
                    job.onEnd = null;
                }

                // download cancelled, delete any partial files
                try {
                    System.IO.File.Delete(job.Destination + ".part");
                } catch (System.Exception) { }
                try {
                    System.IO.File.Delete(job.Destination);
                } catch (System.Exception) { }
            }
            job.RemoveNow = true;
        }

        private static string getProductName()
        {
            if (App.Data != null
                && App.Data.App.ProductName != null)
            {
                return App.Data.App.ProductName;
            } else
            {
                return Application.productName;
            }
        }
	}
}

#endif