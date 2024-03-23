using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
public class CustomSubtitles : MonoBehaviour
{
	public static CustomSubtitles Instance;
	public delegate void OnSRTLoaded();
	private Font font;
	private Color FontColor, OutlineColor;
	private Vector3 camForward;
	private float CharacterSize = .005f;
	public bool Ready, disableRendering;
	public Text targetMesh;
	public RawImage block;
	public int currentTime;
	public int currentSubId = -1;
	public int previousSubId = -1;
	private string previoustext;
	private bool UpdateRotation;

	public List<TextBlock> list;
	[System.Serializable]
	public struct TextBlock
	{
		public string text;
		public int start, end;
	}

	void Start()
	{
		Instance = this;
		SetColor(Color.white);
	}

	public void Load(string filename, SRTLoadMode loadMode = SRTLoadMode.FullPath, OnSRTLoaded onLoaded = null)
	{
		Ready = false;
		currentSubId = -1;
		previousSubId = -1;
		Debug.Log("loading srt " + filename);
		StartCoroutine(LoadSrt(filename, loadMode, onLoaded));
	}
	public void SetColor(Color inner)
	{	
		targetMesh.color = inner;
	}

	public void UpdateSubtitles(int timeMS)
	{
		if (!Ready|| disableRendering)
		{
			targetMesh.text = "";
			block.enabled = false;
			return;
		}
		currentTime = timeMS;
		targetMesh.text = getSubtitle(currentTime);
		block.enabled = !string.IsNullOrEmpty(targetMesh.text);

		if (previousSubId != currentSubId) {
			previousSubId = currentSubId;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)this.transform);
		}
	}

	private int blockSize, SeekAt, TimeOut, m;
	private string returnString;
	public string getSubtitle(int seek)
	{
		blockSize = list.Count / 2;
		SeekAt = blockSize;
		returnString = null;
		TimeOut = 0;
		while (returnString == null && TimeOut < 8)
		{
			if (SeekAt < 0 || SeekAt > list.Count - 1) break;
			m = match(SeekAt, seek);
			if (m == 0)
			{
				returnString = list[SeekAt].text;
				currentSubId = SeekAt;
				break;
			}
			else
			{
				if (blockSize > 1)
				{
					blockSize = Mathf.CeilToInt(blockSize / 2.0f);
				}
				else
				{
					TimeOut += 1;
				}
				SeekAt = SeekAt + (blockSize * m);
			}
		}
		previoustext = returnString;
		return returnString;
	}

	private int match(int block, int seek)
	{
		if (seek < list[block].start) return -1;
		if (seek > list[block].end) return 1;
		return 0;
	}

	public enum SRTLoadMode
	{
		FullPath,
		StreamingAssets,
		Resources
	}

	private IEnumerator LoadSrt(string filename, SRTLoadMode LoadMode = SRTLoadMode.FullPath, OnSRTLoaded onLoaded = null)
	{
		list = new List<TextBlock>();
		string[] splitter = new string[] { " --> " };
		string[] allText = new string[0];

		if (LoadMode == SRTLoadMode.FullPath)
		{
			allText = File.ReadAllLines(filename);
		}
		if (LoadMode == SRTLoadMode.StreamingAssets)
		{
			allText = File.ReadAllLines(Application.streamingAssetsPath + "/" + filename);
		}
		if (LoadMode == SRTLoadMode.Resources)
		{
			allText = (Resources.Load<TextAsset>("subs")).text.Split(new string[] { "\n" }, System.StringSplitOptions.None);
		}

		yield return null;
		bool ready = false;
		int currentLine = 0;
		int currentBlock = 1;
		while (!ready)
		{
			if (currentLine > allText.Length - 1) break;
			if (currentBlock % 32 == 0) yield return null;
			while (allText[currentLine].Trim() != currentBlock.ToString())
			{
				currentLine += 1;
				if (currentLine > allText.Length - 1)
				{
					ready = true;
					break;
				}
			}
			if (ready) break;
			currentLine += 1;
			TextBlock T = new TextBlock();
			string[] times = allText[currentLine].Split(splitter, System.StringSplitOptions.None);
			T.start = timeStampToMilliseconds(times[0]);
			T.end = timeStampToMilliseconds(times[1]);
			T.text = "";
			currentLine += 1;
			while (allText[currentLine].Trim() != "")
			{
				T.text += allText[currentLine] + "\n";
				currentLine += 1;
				if (currentLine > allText.Length - 1) break;
			}
			if (T.text.StartsWith(@"{\an2}")) //fixes spanish problem
			{
				T.text = T.text.Substring(6, T.text.Length - 6);
			}
			T.text = T.text.Substring(0, T.text.Length - 1);
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