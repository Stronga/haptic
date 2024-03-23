using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Headjack;

public class GazeFillMessageBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public Button button;
	public RawImage rawImage;
	public float fill;
	public float fillTime = 5f;

	private bool highlight=false;
	private Material mat;
	private bool waitForReset = false;
	private float fillTimeFactor = 0.2f;

	private void Awake()
	{
		mat = rawImage.material = new Material( rawImage.material);
		fillTimeFactor = 1f / fillTime;
	}
	void OnEnable()
	{
		highlight = false;
		waitForReset = false;
		fillTimeFactor = 1f / fillTime;
	}

	void OnValidate() {
		fillTimeFactor = 1f / fillTime;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		highlight = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		highlight = false;
		waitForReset = false;
	}
	void Update ()
	{
		bool isHighlighted = highlight && !waitForReset;

		fill = Mathf.Clamp01(fill + Time.deltaTime * (isHighlighted ? fillTimeFactor : -4f));
		mat.SetFloat("_Fill", fill);
		if (fill >= 1f)
		{
			waitForReset = true;
			fill = 0f;
			if (button != null) {
				button.onClick.Invoke();
			}
		}
	}
}
