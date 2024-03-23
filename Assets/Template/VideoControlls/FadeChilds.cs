using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FadeChilds : MonoBehaviour
{
	public float value;
	public List<Image> imageElements;
	public List<Text> textElements;
	void Start()
	{
		imageElements = new List<Image>();
		textElements = new List<Text>();
		AddElementsFromChildren(transform);
		Update();
	}

	// Update is called once per frame
	void Update()
	{
		for (int i = 0; i < imageElements.Count; ++i)
		{
			imageElements[i].color = new Color(imageElements[i].color.r, imageElements[i].color.g, imageElements[i].color.b, value);
		}
		for (int i = 0; i < textElements.Count; ++i)
		{
			textElements[i].color = new Color(textElements[i].color.r, textElements[i].color.g, textElements[i].color.b, value);
		}
	}

	void AddElementsFromChildren(Transform t)
	{
		Image i = t.GetComponent<Image>();
		Text tex = t.GetComponent<Text>();
		if (i != null)
		{
			imageElements.Add(i);
		}
		if (tex != null)
		{
			textElements.Add(tex);
		}
		foreach (Transform child in t)
		{
			AddElementsFromChildren(child);
		}
	}
}
