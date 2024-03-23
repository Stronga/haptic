#if UNITY_IOS && USE_CARDBOARD_SDK
#define USING_IOS_XCODE
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if USING_IOS_XCODE
using UnityEditor.iOS.Xcode;
#endif

namespace MobfishCardboard.UEditor
{
    [InitializeOnLoad]
    public class CardboardEditorHelper : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 99999; } }

        public void OnPostprocessBuild(BuildReport report) {
#if USING_IOS_XCODE
            string path = report.summary.outputPath;
            Debug.Log("CardboardEditorHelper.PostProcessXCode() Start.");

            // Go get pbxproj file
            //string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            string projPath = PBXProject.GetPBXProjectPath(path);

            // PBXProject class represents a project build settings file,
            // here is how to read that in.
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            // This is the Xcode target in the generated project
            List<string> targetArray = new List<string>();

#if UNITY_2019_3_OR_NEWER
            targetArray.Add(proj.GetUnityMainTargetGuid());
            targetArray.Add(proj.GetUnityFrameworkTargetGuid());
#else
            targetArray.Add(proj.TargetGuidByName("Unity-iPhone"));
#endif

            // If building with the non-bitcode version of the plugin, these lines should be uncommented.
            foreach (string target in targetArray)
            {
                Debug.Log("CardboardEditorHelper.PostProcessXCode() Setting build property: ENABLE_BITCODE = NO, target=" + target);
                proj.AddBuildProperty(target, "ENABLE_BITCODE", "NO");
            }
            // Write PBXProject object back to the file
            proj.WriteToFile(projPath);

            Debug.Log("CardboardEditorHelper.PostProcessXCode() finished");
#endif
        }
    }
}