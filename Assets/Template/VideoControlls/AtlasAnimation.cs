using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AtlasAnimation : MonoBehaviour
{
	public int elements = 4;
	public int rows = 4;
	public float fps = 15;

	private float t=0;
	private int i = 0;
	private RawImage target;
	// Use this for initialization
	void Start()
	{
		target = GetComponent<RawImage>();
	}

	// Update is called once per frame
	void Update()
	{
		target.material.mainTextureScale = Vector2.one;
		target.material.mainTextureOffset = Vector2.zero;
		t += Time.deltaTime * fps;
		Vector2 scale = new Vector2(1f / elements, 1f / rows);
		
		if (t > 1f)
		{
			t -= 1f;
			i += 1;
			if (i == elements * rows) i = 0;
		}
		int x = i % elements;
		int row = (i - x) / elements;
		target.uvRect = new Rect(new Vector2((x * scale.x), (1f - row)*scale.y + scale.y), scale);
	}
}
