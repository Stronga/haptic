using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Headjack;

public class GazeFillCatSwitch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public CategorySwitch categorySwitch;
	public RawImage rawImage;
	public float fill;

	private bool highlight=false;
	private Material mat;
	private bool waitForReset = false;
	private float fillTimeFactor = 0.5f;

	private void Awake()
	{
		mat = rawImage.material = new Material( rawImage.material);
	}
	void OnEnable()
	{
		highlight = false;
		fillTimeFactor = 1f / (EssentialsManager.variables.kioskFillTime * 0.7f);
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
			if (categorySwitch != null) {
				categorySwitch.FilterByCategory();
			}
		}
	}
}
