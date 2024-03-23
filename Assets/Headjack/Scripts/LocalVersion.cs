// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Headjack {
    public class LocalVersionTracking {
        [System.Serializable]
        public class LocalVersionArrays
        {
            public string[] c;
            public string[] v;
        }

        public Dictionary<string, string> LocalVersion;

        private string LocalDataPath = null;
        private bool savingData = false;


        public IEnumerator Initialize(string basePath, string appID, AppDataStruct appData) {
#if UNITY_EDITOR
            LocalDataPath = basePath + appID + ".v3.local";
#else
            LocalDataPath = basePath + "v3.local";
#endif
            LocalVersion = new Dictionary<string, string>();
            foreach (string mediaID in appData.Media.Keys) {
                LocalVersion[mediaID] = null;
            }
            foreach (string videoID in appData.Video.Keys) {
                LocalVersion[videoID] = null;
            }

            yield return LoadLocalData(appData);
        }

        public IEnumerator LoadLocalData(AppDataStruct appData) {
            if (!File.Exists(LocalDataPath)) {
                Debug.Log("No Local Data found, creating one");
            } else {
                string json = null;
                using (Task<string> readLocalVersions = File.ReadAllTextAsync(
                    LocalDataPath,
                    System.Text.Encoding.UTF8
                )) {
                    yield return new WaitUntil(() => readLocalVersions.IsCompleted);
                    if (readLocalVersions.Status != TaskStatus.RanToCompletion) {
                        Debug.LogError($"Failed to read local versions from file: {readLocalVersions.Exception.Message}");
                        yield break;
                    }
                    json = readLocalVersions.Result;
                }
                if (json.Length > 8) {
                    LocalVersionArrays SData = JsonUtility.FromJson<LocalVersionArrays>(json);
                    if (SData != null && SData.c != null) {
                        for (int i = 0; i < SData.c.Length; ++i) {
                            // Fix JsonUtility converting null strings to empty strings
                            if (string.IsNullOrEmpty(SData.v[i])) {
                                SData.v[i] = null;
                            }

                            if (appData.Packaged.ContainsKey(SData.c[i])) {
                                LocalVersion[SData.c[i]] = AppDataStruct.PACKAGED_LABEL;
                            } else {
                                if (LocalVersion.ContainsKey(SData.c[i])) {
                                    if (SData.v[i] != AppDataStruct.PACKAGED_LABEL) {
                                        if (LocalVersion[SData.c[i]] == null) {
                                            LocalVersion[SData.c[i]] = SData.v[i];
                                        } else if (LocalVersion[SData.c[i]] != SData.v[i]) {
                                            // stored local version does not match current local version
                                            // so delete stored local version
                                            string saneId = getSanitizedId(SData.c[i]);
                                            try {
                                                File.Delete(Headjack.App.BasePath + "Media/" + 
                                                    saneId + "/" + SData.v[i]);
                                            } catch (FileNotFoundException) {
                                                // File we're trying to delete was not found, fine.
                                            } catch (System.Exception e) {
                                                Debug.LogError("Failed to delete outdated local file: " + 
                                                    Headjack.App.BasePath + "Media/" + saneId + "/" + SData.v[i] +
                                                    " (" + e.Message + ")");
                                            }
                                            try {
                                                File.Delete(Headjack.App.BasePath + "Video/" + 
                                                    saneId + "/" + SData.v[i]);
                                            } catch (FileNotFoundException) {
                                                // File we're trying to delete was not found, fine.
                                            } catch (System.Exception e) {
                                                Debug.LogError("Failed to delete outdated local file: " + 
                                                    Headjack.App.BasePath + "Media/" + saneId + "/" + SData.v[i] +
                                                    " (" + e.Message + ")");
                                            }
                                        }
                                    }
                                } else {
                                    Debug.Log($"This file is not packaged or present in the current app data: {SData.c[i]}\n So remove it");

                                    string saneId = getSanitizedId(SData.c[i]);
                                    if (saneId.Length != 32) {
                                        Debug.LogWarning("ID in local version file with mismatched length (not 32)");
                                    }
                                    // try deleting the files associated with the orphaned media/video
                                    try {
                                        Directory.Delete(Headjack.App.BasePath + "Media/" + saneId, true);
                                    } catch (DirectoryNotFoundException) {
                                        // ID likely refers to a video, so /Media directory does not exist
                                    } catch (System.Exception e) {
                                        Debug.LogError($"Deleting orphaned media ({saneId}) failed, keeping reference in local version record.\n{e.Message}");
                                        LocalVersion[SData.c[i]] = SData.v[i];
                                    }
                                    try {
                                        Directory.Delete(Headjack.App.BasePath + "Video/" + saneId, true);
                                    } catch (DirectoryNotFoundException) {
                                        // ID likely refers to media, so /Video directory does not exist
                                    } catch (System.Exception e) {
                                        Debug.LogError($"Deleting orphaned video ({saneId}) failed, keeping reference in local version record.\n{e.Message}");
                                        LocalVersion[SData.c[i]] = SData.v[i];
                                    }
                                }
                            }
                            //Debug.Log(SData.c[i] + " | " + SData.v[i]);
                        }
                    } else {
                        Debug.Log("Local Data found, but appears to be corrupted");
                    }
                } else {
                    Debug.Log("Local Data found, but appears to be empty");
                }
                //Debug.Log("Local data loaded");
            }
            yield return SaveData();
        }

        public IEnumerator SaveData()
        {
            float timeout = 2f;
            while (savingData && timeout > 0f) {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (savingData) {
                Debug.LogWarning("Cannot save local versions data, already saving");
                yield break;
            }

            savingData = true;
            LocalVersionArrays SData = new LocalVersionArrays();
            SData.c = new string[LocalVersion.Count];
            SData.v = new string[LocalVersion.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> k in LocalVersion)
            {
                SData.c[i] = k.Key;
                SData.v[i] = k.Value;
                i += 1;
            }
            string json = JsonUtility.ToJson(SData,true);
            yield return null;
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(LocalDataPath));
            } catch (Exception e) {
                Debug.LogError($"Failed to create directory for storing local version metadata: {e.Message}");
                yield break;
            }
            using (Task writeLocalVersions = File.WriteAllTextAsync(
                LocalDataPath, 
                json, 
                System.Text.Encoding.UTF8
            )) {
                yield return new WaitUntil(() => writeLocalVersions.IsCompleted);
                if (writeLocalVersions.Status != TaskStatus.RanToCompletion) {
                    Debug.LogError($"Failed to write local versions to file: {writeLocalVersions.Exception.Message}");
                    try {
                        File.Delete(LocalDataPath);
                    } catch (Exception) {}
                }
            }
            yield return null;
            savingData = false;
        }

        private static string getSanitizedId(string rawId)
        {
            // sanitize local version string to only contain the media/video ID
            Match idRegexMatch = Regex.Match(rawId, @"^[^_\/]+");
            if (idRegexMatch.Success) {
                return idRegexMatch.Value;
                
            }
            return rawId;
        }
    }
}