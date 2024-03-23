// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
namespace Headjack
{
    internal class PPDownloader : MonoBehaviour 
    {
        private Dictionary<string, JobInfo> Job;
        protected Dictionary<string,string> Response;
        internal Dictionary<string,bool> Active;
        internal Dictionary<string,float> Progress;
        protected Dictionary<string,long> ProjectSize;
        protected Dictionary<string,OnEnd> ProjectOnEnd;
        protected List<string> queue;
        protected List<string> textureQueue;
        //protected float ProgressShow = 0;
        protected float TotalProgress=0;
        protected float TimeOutTimer = 0;

        internal class JobInfo
        {
            internal string Url, Destination;
            internal List<string> ProjectId;
            // TODO: Fix multiple projectIds belonging to same job id

            internal long Size, Progress;
            internal bool IsDone;
            internal OnEnd onEnd;
        }

        internal virtual void Initialize()
        {
            queue = new List<string>();
            textureQueue = new List<string>();
            Job = new Dictionary<string, JobInfo>();
            Active = new Dictionary<string, bool>();
            Progress = new Dictionary<string, float>();
            ProjectSize = new Dictionary<string, long>();
            ProjectOnEnd = new Dictionary<string, OnEnd>();

            Response = new Dictionary<string, string>();
            Response.Add("200", "OK");
            Response.Add("202", "Accepted");
            Response.Add("204", "No Content");
            Response.Add("400", "Bad Request");
            Response.Add("401", "Unauthorized");
            Response.Add("403", "Forbidden");
            Response.Add("404", "Not Found");
            Response.Add("405", "Method Not Allowed");
            Response.Add("406", "Not Acceptable");
            Response.Add("408", "Request Timeout");
            StartCoroutine(DownloadManager(queue));
            StartCoroutine(DownloadManager(textureQueue));
        }

        internal virtual void DownloadMedia(string MediaId, bool wifiOnly, OnEnd onEnd=null)
        {
            string downloadId = MediaId + "/" + App.Data.Media[MediaId].Filename;

            textureQueue.Add(downloadId);
            if (Job.ContainsKey(downloadId)) {
                // same texture download already queued, add callback to existing job
                Job[downloadId].onEnd += onEnd;
            } else {
                JobInfo NewJob = new JobInfo();
                NewJob.Url = /*App.Host + */App.GetMediaUrl(MediaId);
                NewJob.Destination = App.BasePath + App.GetMediaPath(MediaId);
                NewJob.Size = App.Data.Media[MediaId].FileSize;
                NewJob.Progress = 0;
                NewJob.IsDone = false;
                NewJob.ProjectId = new List<string>();
                NewJob.onEnd = onEnd;
                Job.Add(downloadId, NewJob);
            }
        }

        internal virtual float getProjectProgress(string pID)
        {
            if (Progress.ContainsKey(pID) && ProjectSize.ContainsKey(pID) && ProjectSize[pID] > 0)
            {
                return Mathf.Max(0f, (100f * Progress[pID] / ProjectSize[pID]));
            }
            else
            {
                return -1;
            }
        }

        internal virtual float getMediaProgress(string mediaId) {
            JobInfo mediaJob;
            if (!Job.TryGetValue(mediaId, out mediaJob) ||
                mediaJob.Size <= 0 ||
                mediaJob.Progress < 0) {
                return -1f;
            }

            return Mathf.Clamp(100f * mediaJob.Progress / mediaJob.Size, 0f, 100f);
        }

        internal virtual long getMediaDownloadedBytes(string mediaId) {
            JobInfo mediaJob;
            if (!Job.TryGetValue(mediaId, out mediaJob)) {
                return -1;
            }

            return mediaJob.Progress;
        }

        internal virtual void DownloadProject(string ProjectId, bool WifiOnly, OnEnd onEnd=null)
        {
			Debug.Log ("Downloading project " + ProjectId);
            if (!Active.ContainsKey(ProjectId))
            {
                Active.Add(ProjectId, true);
				Debug.Log("Added downloader key: " + ProjectId);
                Progress.Add(ProjectId, 0);
                long TotalSize = 0;
                
				string AudioId = App.GetProjectAudioId(ProjectId);
                if (AudioId != null && App.UpdateAvailableMedia(AudioId))
                {
                    string downloadId = AudioId + "/" + App.Data.Media[AudioId].Filename;
                    if (!queue.Contains(downloadId))
                    {
                        queue.Add(downloadId);
                        JobInfo NewJob = new JobInfo();
                        NewJob.Url = /*App.Host +*/ App.GetMediaUrl(AudioId);
                        NewJob.Destination = App.BasePath + App.GetMediaPath(AudioId);
                        NewJob.Size = App.Data.Media[AudioId].FileSize;
                        TotalSize += NewJob.Size;
                        NewJob.Progress = 0;
                        NewJob.IsDone = false;
                        NewJob.ProjectId = new List<string>
                        {
                            ProjectId
                        };
                        NewJob.onEnd = null;

                        Job[downloadId] = NewJob;
                    }
                    else if (Job.ContainsKey(downloadId) && !Job[downloadId].ProjectId.Contains(ProjectId))
                    {
                        Job[downloadId].ProjectId.Add(ProjectId);
                    }
                }

				string videoId = App.GetProjectVideoId(ProjectId);
				if (videoId != null && App.UpdateAvailableVideo(videoId))
				{
					Debug.Log("downloading video " + videoId);

                    DeleteExistingVideo(videoId);

					string videoFilename = App.GetVideoFilename(videoId);
                    string downloadId = videoId + "/" + videoFilename;
                    if (!queue.Contains(downloadId))
                    {
                        queue.Add(downloadId);
                        Debug.Log ("Adding " + downloadId + " to queue");
                        JobInfo NewJob = new JobInfo();
                        NewJob.Url = App.GetVideoUrl(videoId);
                        NewJob.Destination = App.BasePath + App.GetVideoLocalSubPath(videoId, true);
                        NewJob.Size = App.GetVideoSize(videoId);
                        NewJob.Progress = 0;
                        NewJob.IsDone = false;
                        NewJob.ProjectId = new List<string>
                        {
                            ProjectId
                        };
                        NewJob.onEnd = null;
                        Job[downloadId] = NewJob;
                    }
                    else if (Job.ContainsKey(downloadId) && !Job[downloadId].ProjectId.Contains(ProjectId))
                    {
                        Job[downloadId].ProjectId.Add(ProjectId);
                    }
				}
				TotalSize += App.GetVideoFormatData(videoId, false).Filesize;
				
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
			}
			else
			{
				if (onEnd != null)
				{
					onEnd(false, "Project is already downloading");
				}
			}
        }

        protected void DeleteExistingVideo(string VideoId)
        {
            try { 
                Directory.Delete(App.BasePath + App.GetVideoPath(VideoId), true);
            } catch (DirectoryNotFoundException) {

            } catch (System.Exception e) {
                Debug.LogError($"Failed to delete video folder {VideoId}: {e.Message}");
            }
        }

        internal void CancelProject(string ProjectId)
        {
            if (Active.ContainsKey(ProjectId))
            {
                Active[ProjectId] = false;
            }
            else
            {
                Debug.LogWarning("Can't cancel project " + ProjectId + ", project is not active");
            }
        }

        private IEnumerator DownloadManager(List<string> dlQueue)
        {
            string CurrentDownload = "";
            UnityWebRequest www = null;
            while (true)
            {
                // remove all occurrences of the previous download ID from the queue
                while (dlQueue.Remove(CurrentDownload)) {}
                yield return null;

                // turn off never screen sleep
                Startup.NeverSleepFlag &= ~(uint)1;

                while (dlQueue.Count == 0)
                {
                    // Wait for next job
                    yield return new WaitForSeconds(1f);
                }

                // turn on never screen sleep
                Startup.NeverSleepFlag |= (uint)1;

                CurrentDownload = dlQueue[0];
                dlQueue.RemoveAt(0);
                if (!Job.ContainsKey(CurrentDownload)) {
                    Debug.LogError($"[PPDownloader] Current download in queue does not have associated Job: {CurrentDownload}");
                    continue;
                }

                www = UnityWebRequest.Get(Job[CurrentDownload].Url);
                www.disposeDownloadHandlerOnDispose = true;
				Debug.Log("Current Download: " + CurrentDownload);// +" - "+Job[CurrentDownload].Url);
				
               
                //Debug.Log("Downloading App Data");
                yield return null;
                DataStream Handler = new DataStream(Job[CurrentDownload].Destination);
                yield return null;
                www.downloadHandler = Handler;
                www.SendWebRequest();

                yield return null;
                long PreviousBytes = 0;
                List<string> Cancel = new List<string>();
                bool Cancelled = false;
                bool TimeOut = false;
                while (!www.isDone)
                {
                    yield return null;

                    if (www.isNetworkError || TimeOut)
                    {
                        break;
                    }
                    if (!Job.ContainsKey(CurrentDownload))
                    {
                        break;
                    }
                    Job[CurrentDownload].Progress = Handler.DownloadedBytes;
                    if (Job[CurrentDownload].ProjectId.Count == 0)
                    {
                        // Debug.Log("TEST");
                        //ProgressShow = ((Job[CurrentDownload].Progress * 1f) / (Job[CurrentDownload].Size * 1f)) * 100f;
                        yield return null;
                    }
                    else
                    {
                        //ProgressShow = Progress[Job[CurrentDownload].ProjectId];
                        for (int i = Job[CurrentDownload].ProjectId.Count - 1; i >= 0; --i)
                        {
                            string ThisProject = Job[CurrentDownload].ProjectId[i];
                            long CurrentProgress = 0;

                            foreach (KeyValuePair<string, JobInfo> j in Job)
                            {
                                if (j.Value.ProjectId.Contains(ThisProject))
                                {
								//	Debug.Log(j.Value.Destination + " - " + j.Value.Progress);
                                    CurrentProgress += j.Value.Progress;
                                }
                            }
							//Debug.Log(CurrentProgress);
							Progress[ThisProject] = CurrentProgress;// ((CurrentProgress * 1f) / (ProjectSize[ThisProject] * 1f)) * 100f;
                            if (!Active[ThisProject])
                            {
                                // TODO: only cancel when all projects are cancelled
                                Cancel.Add(ThisProject);
                                Job[CurrentDownload].ProjectId.RemoveAt(i);
                                
                                yield return null;
                            }
                        }

                        if (Job[CurrentDownload].ProjectId.Count == 0)
                        {
                            www.Abort();
                            Cancelled = true;
                            //Debug.Log("BREAK");
                            break;
                        }

                    }
                    if (PreviousBytes == Handler.DownloadedBytes)
                    {
                        TimeOutTimer += Time.deltaTime;
                        if (TimeOutTimer > 10)
                        {
                            TimeOut = true;
                            break;
                        }
                    }
                    else
                    {
                        TimeOutTimer = 0;
                    }

                    PreviousBytes = Handler.DownloadedBytes;
                }
                string Error = www.error;
                bool IsError = www.isNetworkError;
                long ResponseCode = www.responseCode;
                TimeOutTimer = 0;
                yield return null;

                for (int cI = 0; cI < Cancel.Count; ++cI)
                {
                    string cancelledProject = Cancel[cI];

                    //Debug.Log("Cancel Detected removing others in queue");
                    for (int i = dlQueue.Count - 1; i >= 0; --i)
                    {
                        if (Job.ContainsKey(dlQueue[i]))
                        {
                            if (Job[dlQueue[i]].ProjectId.Contains(cancelledProject))
                            {
                                Job[dlQueue[i]].ProjectId.Remove(cancelledProject);
                                if (Job[dlQueue[i]].ProjectId.Count == 0)
                                {
                                    // all projects that downloaded this file were cancelled
                                    // so cancel the download
                                    dlQueue.RemoveAt(i);
                                }
                            }
                        }
                        else
                        {
                            dlQueue.RemoveAt(i);
                        }
                    }
                    Active.Remove(cancelledProject);
                    Progress.Remove(cancelledProject);

                    if (ProjectOnEnd[cancelledProject] != null)
                    {
                        ProjectOnEnd[cancelledProject](false, "Cancel");
                    }
                    ProjectOnEnd.Remove(cancelledProject);
                }

                if (IsError || ResponseCode == 404 || TimeOut || Cancelled)
                {

                    Handler.FlushAndClose();
                    www.Dispose();
                    try
                    {
                        if (Job.ContainsKey(CurrentDownload))
                        {
                            File.Delete(Job[CurrentDownload].Destination);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error deleting failed downloaded file: " + e);
                    }

                    if (Cancelled)
                    {
                        // not sure what to do here
                    }
                    else
                    {
                        string ResponseMeaning = "";
                        if (Response.ContainsKey(ResponseCode.ToString()))
                        {
                            ResponseMeaning = Response[ResponseCode.ToString()];
                        }
                        Debug.Log("Given Error: " + Error + ". Response Code: " + ResponseCode + " " + ResponseMeaning + ". Time Out: " + TimeOut + " " + Job[CurrentDownload].Url);
                        if (Job[CurrentDownload].onEnd != null)
                        {
                            Job[CurrentDownload].onEnd(false, "Given Error: " + Error + ". Response Code: " + ResponseCode + " " + ResponseMeaning + ". Time Out: " + TimeOut);
                        }
                        Job[CurrentDownload].onEnd = null;
                    }
                    //remove all jobs from current project
                    if (Job[CurrentDownload].ProjectId.Count == 0)
                    {
                        Job.Remove(CurrentDownload);
                    }
                    else
                    {
                        for (int projs = 0; projs < Job[CurrentDownload].ProjectId.Count; ++projs)
                        {
                            // NOTE: The below section is kind of a mess, where failed jobs in a project
                            // with other downloads are not removed properly
                            
                            bool FoundMore = false;
                            string ThisProject = Job[CurrentDownload].ProjectId[projs];
                            yield return null;
                            for (int i = 0; i < dlQueue.Count; ++i)
                            {
                                if (Job[dlQueue[i]].ProjectId.Contains(ThisProject))
                                {
                                    FoundMore = true;
                                    break;
                                }
                            }
                            yield return null;
                            if (!FoundMore)
                            {
                                
                                Active.Remove(ThisProject);
                                Progress.Remove(ThisProject);
                                if (!Cancel.Contains(ThisProject))
                                {
                                    if (ProjectOnEnd[ThisProject] != null)
                                    {
                                        string ResponseMeaning = "";
                                        if (Response.ContainsKey(ResponseCode.ToString()))
                                        {
                                            ResponseMeaning = Response[ResponseCode.ToString()];
                                        }
                                        ProjectOnEnd[ThisProject](false, "Error: " + Error + " " + ResponseCode + " " + ResponseMeaning);
                                    }
                                    ProjectOnEnd.Remove(ThisProject);
                                }
                                //Debug.Log("This was the last download of the project"); 
                            }
                        }
                    }

                    
                }
                else
                {
                    Handler.FlushAndClose();

                    www.disposeDownloadHandlerOnDispose = true;
                    www.Dispose();

                    yield return null;
                    if (ResponseCode == 200 || !Job[CurrentDownload].Url.Contains("http"))
                    {
                        if (App.Data != null)
                        {
							Debug.Log("Trying to set local version of " + CurrentDownload);

							int slashPos = CurrentDownload.IndexOf("/");
                            if (slashPos > 0) {
                                string mId = CurrentDownload.Substring(0, slashPos);
                                string mPath = CurrentDownload.Substring(slashPos+1);

                                App.LocalVersionTracking.LocalVersion[mId] = mPath;

                                yield return App.LocalVersionTracking.SaveData();
                            }
                        }
                        yield return null;
                        if (Job[CurrentDownload].ProjectId.Count == 0)
                        {
                            if (Job[CurrentDownload].onEnd != null)
                            {
                                Job[CurrentDownload].onEnd(true, null);
                            }
                            Job[CurrentDownload].onEnd = null;
                            yield return null;
                            Job.Remove(CurrentDownload);
                        }
                        else
                        {
                            for (int projs = 0; projs < Job[CurrentDownload].ProjectId.Count; ++projs)
                            {
                                bool FoundMore = false;
                                string ThisProject = Job[CurrentDownload].ProjectId[projs];
                                yield return null;
                                for (int i = 0; i < dlQueue.Count; ++i)
                                {
                                    if (Job[dlQueue[i]].ProjectId.Contains(ThisProject))
                                    {
                                        FoundMore = true;
                                        break;
                                    }
                                }
                                yield return null;
                                if (!FoundMore)
                                {
									Active.Remove(ThisProject);
                                    Progress.Remove(ThisProject);
                                    if (ProjectOnEnd[ThisProject] != null)
                                    {
                                        ProjectOnEnd[ThisProject](true, "");
                                    }
                                    ProjectOnEnd.Remove(ThisProject);
                                    Debug.Log("This was the last download of the project"); 
                                }
                            }
                        }
                    }
                    else
                    {
                        if (File.Exists(Job[CurrentDownload].Destination))
                        {
                            File.Delete(Job[CurrentDownload].Destination);
                            Debug.LogWarning("Incomplete file deleted. ID: "+ CurrentDownload);
                        }
                        string ResponseMeaning = "";
                        if (Response.ContainsKey(ResponseCode.ToString()))
                        {
                            ResponseMeaning = Response[ResponseCode.ToString()];
                        }
                        if (Job[CurrentDownload].onEnd != null)
                        {
                            Job[CurrentDownload].onEnd(false, "Given Error: " + Error + ". Response Code: " + ResponseCode + " " + ResponseMeaning + "Time Out: " + TimeOut);
                        }
                        Job[CurrentDownload].onEnd = null;
                        if (Job[CurrentDownload].ProjectId.Count == 0)
                        {
                            Job.Remove(CurrentDownload);
                        }
                    }
                }
            }
        }

        internal class DataStream : DownloadHandlerScript 
        {
            private string Path;
            private FileStream FS;
            internal ulong ContentLength=1;
            internal long DownloadedBytes=0;
            internal DataStream(string path): base() 
            {
				
                string dir=new FileInfo(path).Directory.FullName;
#if UNITY_IOS
                UnityEngine.iOS.Device.SetNoBackupFlag(dir);
#endif
				Directory.CreateDirectory(dir);
                //Debug.Log("Creating Directory");
                FS=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Write,512,true);

            }


            internal void FlushAndClose()
            {
				FS.Flush();
				FS.Close();
            }

            internal DataStream(byte[] buffer): base(buffer) 
            {
            }
            protected override byte[] GetData() { return null; }
            protected override bool ReceiveData(byte[] data, int dataLength) 
            {
                if(data == null || data.Length < 1) {
                    Debug.Log("received a null/empty buffer");
                    return false;
                }
                FS.Write(data, 0, dataLength);
                DownloadedBytes += (long)dataLength;
                return true;
            }
            protected override void ReceiveContentLengthHeader(ulong contentLength) 
            {
                ContentLength = contentLength;
            }
				
        }
    }
}
