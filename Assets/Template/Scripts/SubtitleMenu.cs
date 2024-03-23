using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class SubtitleMenu : MonoBehaviour {

	// Use this for initialization
	public MainVideoControls videoControls;
	public SubtitleOption subtitleOptionPrefab;
	public List<SubtitleOption> subtitleOptions;
	public List<string> Initialize(string[] options)
	{
		List<string> subtitleNames = new List<string>(options.Length);

		if (subtitleOptions != null)
		{
			for (int i = 0; i < subtitleOptions.Count; ++i)
			{
				Destroy(subtitleOptions[i].gameObject);
			}
		}
		subtitleOptionPrefab.gameObject.SetActive(true);
		subtitleOptions = new List<SubtitleOption>(options.Length+1);
		for (int i = 0; i < options.Length+1; ++i)
		{
			if (i < options.Length && string.IsNullOrEmpty(options[i])) {
				subtitleNames.Add(null);
				continue;
			}
			SubtitleOption currentSubtitle = Instantiate(subtitleOptionPrefab);
		
			currentSubtitle.transform.SetParent(subtitleOptionPrefab.transform.parent);
			currentSubtitle.transform.localPosition = Vector3.zero;
			currentSubtitle.transform.localEulerAngles = Vector3.zero;
			currentSubtitle.transform.localScale = Vector3.one;
			string text = "None";
			int index = i;
			if (i < options.Length)
			{
				text = new System.IO.FileInfo(App.GetMediaFullPath(options[i])).Name;
				text = text.Substring(0, text.Length - 4);
				subtitleNames.Add(text);
				Debug.Log(text);
			}
			else
			{
				index = -1;
			}
			currentSubtitle.Initialize(index, text);

			subtitleOptions.Add(currentSubtitle);
		}
		subtitleOptionPrefab.gameObject.SetActive(false);

		return subtitleNames;
	}

	public void SetActive(int subIndex) {
		for (int i=0; i < subtitleOptions.Count; ++i) {
			if (subtitleOptions[i] == null) {
				continue;
			}
			if (subtitleOptions[i].index == subIndex) {
				subtitleOptions[i].currentSubtitleCheck.SetActive(true);
			} else {
				subtitleOptions[i].currentSubtitleCheck.SetActive(false);
			}
		}
	}
}
