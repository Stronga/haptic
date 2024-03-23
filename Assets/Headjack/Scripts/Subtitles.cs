// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
namespace Headjack
{
    public class Subtitles : MonoBehaviour 
    {
        public delegate void OnSRTLoaded();
        private Font font;
        private Color FontColor, OutlineColor;
        private Transform CameraTarget;
        private float Depth=.7f;
        private float Angle=-30;
        private Vector3 camForward;
        private float CharacterSize=.005f;
        private bool Ready;
        private TextMesh TargetTextMesh;
        private TextMesh[] TargetOutline;
        public int currentTime;
        private string previoustext;
        private bool UpdateRotation;

        public List<TextBlock> list;
        public struct TextBlock
        {
            public string text;
            public int start, end;
        }
            
        void Start () 
        {
            font = Resources.Load<Font>("Rubik");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            TargetOutline = new TextMesh[4];
            GameObject T = new GameObject("Subtitles");
            T.transform.parent = transform;
            TargetTextMesh = T.AddComponent<TextMesh>();
            TargetTextMesh.font = font;
            TargetTextMesh.GetComponent<MeshRenderer>().material = font.material;
            Texture temp = TargetTextMesh.transform.GetComponent<MeshRenderer>().material.mainTexture;
            TargetTextMesh.transform.GetComponent<MeshRenderer>().material = new Material((Shader)Resources.Load<Shader>("Shaders/SubtitlesInner"));
            TargetTextMesh.transform.GetComponent<MeshRenderer>().material.mainTexture = temp;
            TargetTextMesh.transform.GetComponent<MeshRenderer>().material.color = FontColor;
            TargetTextMesh.alignment = TextAlignment.Center;
            TargetTextMesh.anchor = TextAnchor.MiddleCenter;
            TargetTextMesh.fontSize = 48;
            TargetTextMesh.characterSize = CharacterSize;
            for (int i = 0; i < TargetOutline.Length; ++i)
            {
                T = new GameObject("Subtitle Shadow");
                TargetOutline[i] = T.AddComponent<TextMesh>();
                TargetOutline[i].font = font;
                TargetOutline[i].GetComponent<MeshRenderer>().material = font.material;
                temp = TargetOutline[i].transform.GetComponent<MeshRenderer>().material.mainTexture;
                TargetOutline[i].transform.GetComponent<MeshRenderer>().material = new Material((Shader)Resources.Load<Shader>("Shaders/SubtitlesOuter"));
                TargetOutline[i].transform.GetComponent<MeshRenderer>().material.mainTexture = temp;
                TargetOutline[i].transform.GetComponent<MeshRenderer>().material.color = OutlineColor;
                TargetOutline[i].alignment = TextAlignment.Center;
                TargetOutline[i].anchor = TextAnchor.MiddleCenter;
                TargetOutline[i].fontSize = 48;
                TargetOutline[i].characterSize = CharacterSize;
            }
            TargetOutline[0].transform.localPosition = new Vector3(0.001f, 0.001f, 0);
            TargetOutline[1].transform.localPosition = new Vector3(-0.001f, 0.001f, 0);
            TargetOutline[2].transform.localPosition = new Vector3(0.001f, -0.001f, 0);
            TargetOutline[3].transform.localPosition = new Vector3(-0.001f, -0.001f, 0);
            TargetOutline[0].transform.parent = TargetTextMesh.transform;
            TargetOutline[1].transform.parent = TargetTextMesh.transform;
            TargetOutline[2].transform.parent = TargetTextMesh.transform;
            TargetOutline[3].transform.parent = TargetTextMesh.transform;
        }

        public void Load(string id, SRTLoadMode loadMode=SRTLoadMode.FullPath,OnSRTLoaded onLoaded=null)
        {
            Ready = false;
            string fileName = App.BasePath + App.GetMediaPath(id);
            Debug.Log("loading srt " + fileName);
            StartCoroutine(LoadSrt(fileName,loadMode,onLoaded));
        }

        public void SetCameraTarget(Transform target)
        {
            CameraTarget = target;
        }

        public void SetColor(Color inner, Color outer)
        {
            FontColor = inner;
            OutlineColor = outer;
        }

        public void UpdateSubtitles(int timeMS)
        {
            if (Ready)
            {
                currentTime = timeMS;
                TargetTextMesh.text = getSubtitle(currentTime);
                for (int i = 0; i < TargetOutline.Length; ++i)
                {
                    TargetOutline[i].text = getSubtitle(currentTime);
                }
            }
        }

        void Update()
        {
            if (CameraTarget != null)
            {
                if (UpdateRotation)
                {
                    camForward = CameraTarget.forward;
                }
                float angle = Mathf.Deg2Rad * Angle;
                float Distance = Mathf.Cos(angle);
                Vector3 temp = camForward;
                temp = Vector3.Normalize(new Vector3(temp.x, 0, temp.z))*Distance;
                temp.y = Mathf.Sin(angle);
                TargetTextMesh.transform.position = CameraTarget.position + (temp * Depth);
                if (UpdateRotation)
                {
                    TargetTextMesh.transform.eulerAngles = new Vector3(-Mathf.Tan(temp.y / Distance) * Mathf.Rad2Deg, CameraTarget.eulerAngles.y, 0);
                }
                UpdateRotation = false;
            }
        }

        private int blockSize,SeekAt,TimeOut,m;
        private string returnString;
        public string getSubtitle(int seek)
        {

            blockSize = list.Count / 2;
            SeekAt = blockSize;
            returnString = null;
            TimeOut = 0;
            while (returnString == null&&TimeOut<4)
            {
                if (SeekAt < 0 || SeekAt > list.Count - 1)
                {
                    break;
                }
                m = match(SeekAt, seek);
                if (m == 0)
                {
                    returnString = list[SeekAt].text;
                    break;
                }
                else
                {
                    if (blockSize > 1)
                    {
                        blockSize = blockSize / 2;
                    }
                    else
                    {
                        TimeOut += 1;
                    }

                    SeekAt = SeekAt + (blockSize * m);
                }
            }
            if (returnString != previoustext) {
                if (CameraTarget != null)
                {
                    UpdateRotation = true;
                   
                }
            }
            previoustext = returnString;
            return returnString;
        }

        private int match(int block, int seek)
        {
            if (seek < list[block].start)
            {
                return -1;
            }
            if (seek > list[block].end)
            {
                return 1;
            }
            return 0;
        }

        public enum SRTLoadMode
        {
            FullPath,
            StreamingAssets,
            Resources
        }

        private IEnumerator LoadSrt(string filename, SRTLoadMode LoadMode=SRTLoadMode.FullPath, OnSRTLoaded onLoaded=null)
        {
            list = new List<TextBlock>();
            string[] splitter = new string[]{ " --> " };
            string[] allText=new string[0];

            if (LoadMode == SRTLoadMode.FullPath)
            {
                allText = File.ReadAllLines(filename);
            }
            if (LoadMode == SRTLoadMode.StreamingAssets)
            {
                allText = File.ReadAllLines(Application.streamingAssetsPath + "/" + filename);
            }
            if (LoadMode == SRTLoadMode.Resources) {
                allText = ((TextAsset)Resources.Load<TextAsset> ("subs")).text.Split(new string[]{"\n"},System.StringSplitOptions.None);
            }

            yield return null;
            bool ready = false;
            int currentLine = 0;
            int currentBlock = 1;
            while (!ready)
            {
                if (currentLine > allText.Length - 1)
                {
                    break;
                }
                if (currentBlock % 4 == 0)
                {
                    yield return null;
                }
                while (allText[currentLine].Trim() != currentBlock.ToString())
                {
                    currentLine += 1;
                    if (currentLine > allText.Length - 1)
                    {
                        ready = true;
                        break;
                    }
                }
                if (ready)
                {
                    break;
                }
                currentLine += 1;
                TextBlock T = new TextBlock();
                string[] times = allText[currentLine].Split(splitter,System.StringSplitOptions.None);
                T.start = timeStampToMilliseconds(times[0]);
                T.end = timeStampToMilliseconds(times[1]);
                T.text = "";
                currentLine += 1;
                while (allText[currentLine].Trim() != "")
                {
                    T.text+=allText[currentLine]+"\n";
                    currentLine += 1;
                    if (currentLine > allText.Length - 1)
                    {
                        break;
                    }
                }
                T.text = T.text.Substring(0, T.text.Length - 2);
                list.Add(T);
                currentBlock += 1;
            }
            Ready = true;
        }

        private int timeStampToMilliseconds(string timeStamp)
        {
            return (int.Parse(timeStamp.Substring(0, 2)) * 3600000) +
                (int.Parse(timeStamp.Substring(3, 2)) * 60000) +
                (int.Parse(timeStamp.Substring(6, 2)) * 1000) +
                int.Parse(timeStamp.Substring(9, 3));
        }
    }
}
