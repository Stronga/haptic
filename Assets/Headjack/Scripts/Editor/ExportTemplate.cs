// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using UnityEditor;
using System.IO;
using Ionic.Zip;

namespace Headjack.Settings
{
    public class TemplateUtility : MonoBehaviour
    {
        [MenuItem("Headjack/Export Project as Template")]
        public static void Export()
        {
            // dev project should not be exported as template
            if (File.Exists(Application.dataPath + "/Headjack/Internal/HeadjackInternal.asmdef") ||
                Directory.Exists(Application.dataPath + "/Headjack/Scripts/Editor/CloudBuild")
            ) {
                EditorUtility.DisplayDialog("Can't export template", "Project contains dev scripts", "OK");
                return;
            }

            if (Directory.Exists(Application.dataPath + "/.AVProVideo") ||
                Directory.Exists(Application.dataPath + "/AVProVideo")
            ) {
                if (!EditorUtility.DisplayDialog(
                    "Warning", 
                    "Are you sure you want to export the proprietary AVProVideo in this template?", 
                    "OK", 
                    "Cancel"
                )) {
                    return;
                }
            }

            if (!EditorUtility.DisplayDialog("Warning", "Make sure all files necessary for this template are in the Assets/Template folder", "OK", "Cancel"))
            {
                return;
            }
            
            // check size of project so it does not exceed template max size
            long templateSize = 0;
            templateSize += DirSize(new DirectoryInfo(Application.dataPath));
            templateSize += DirSize(new DirectoryInfo(Application.dataPath + "/../Packages"));
            templateSize += DirSize(new DirectoryInfo(Application.dataPath + "/../CustomPackages"));
            if (templateSize > 1992294400)
            {
                EditorUtility.DisplayDialog("Can't export template", "Project is too large to export as template (>1900mb)", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Save Template", "", "template.zip", "zip");
            if (path.Length != 0)
            {
                string zipDir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(zipDir);
                File.Delete(path);

                using (ZipFile zip = new ZipFile(path))
                {
                    EditorUtility.DisplayCancelableProgressBar("Saving Template...", "", 0f);
                    zip.SaveProgress += Template_SaveProgress;

                    // temporarily remove App ID and AUTH Key from HeadjackSettings resource to not leak
                    // data in template.zip file
                    PlatformSettings pSettings = Resources.Load<PlatformSettings>("Headjack Settings");
                    string appID = pSettings.appID;
                    string authKey = pSettings.AUTHKey;
                    pSettings.appID = null;
                    pSettings.AUTHKey = null;
                    EditorUtility.SetDirty(pSettings);
                    AssetDatabase.SaveAssets();

                    try
                    {
                        zip.AddDirectory(Application.dataPath, "Assets");
                        if (zip.ContainsEntry("Assets/.gitignore")) {
                            zip.RemoveEntry("Assets/.gitignore");
                        }
                        zip.AddDirectory(Application.dataPath + "/../ProjectSettings", "ProjectSettings");
                        zip.AddDirectory(Application.dataPath + "/../Packages", "Packages");
                        if (Directory.Exists(Application.dataPath + "/../CustomPackages")) {
                            zip.AddDirectory(Application.dataPath + "/../CustomPackages", "CustomPackages");
                        }
                    }
                    catch (System.Exception)
                    {
                        Debug.LogError("Missing essential Unity project folder (/Assets, /Packages, /ProjectSettings)");
                        zip.Dispose();
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Template not exported", "Missing essential Unity project folder (/Assets, /Packages, /ProjectSettings)", "OK");
                        return;
                    }

                    zip.Save();

                    // restore App ID and Auth Key
                    pSettings.appID = appID;
                    pSettings.AUTHKey = authKey;
                    EditorUtility.SetDirty(pSettings);
                    AssetDatabase.SaveAssets();

                    EditorUtility.ClearProgressBar();
                    if (File.Exists(path))
                    {
                        EditorUtility.DisplayDialog("Template exported", "Project has been saved to template file: " + Path.GetFileName(path), "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Template not exported", "Template not exported, did you cancel it?", "OK");
                    }
                }
            }
        }

        private static void Template_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.TotalBytesToTransfer > 0)
            {
                if(EditorUtility.DisplayCancelableProgressBar("Saving Template...", e.CurrentEntry.FileName, (1f * e.BytesTransferred) / e.TotalBytesToTransfer))
                {
                    e.Cancel = true;
                }
            }
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }
    }
}
