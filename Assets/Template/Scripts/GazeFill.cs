using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Headjack;

public class GazeFill : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public ProjectInstance projectInstance;
	public RawImage rawImage;
	public float fill;

	private const float DEFAULT_FILL_WAIT = 4f;
	private bool highlight = false;
	private Material mat;
	private float fillWaitTime = 0f;
	private float fillTimeFactor = 0.25f;

	private void Awake()
	{
		mat = rawImage.material = new Material( rawImage.material);
		fillWaitTime = DEFAULT_FILL_WAIT;
	}

	void OnEnable()
	{
		highlight = false;
		fillTimeFactor = 1f / EssentialsManager.variables.kioskFillTime;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		highlight = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		highlight = false;
	}

	void Update ()
	{
		bool isHighlighted = highlight;
		// if this menu item is hidden behind the menu mask, disable gaze control
		// NOTE: the comparison values are "magic" values that correspond with the menu mask edges at time of writing
		if (rawImage.rectTransform.position.y > 1.8f || rawImage.rectTransform.position.y < 0.8f) {
			isHighlighted = false;
		}
		if (fillWaitTime > 0f) {
			isHighlighted = false;
			fillWaitTime -= Time.deltaTime;
		}

		fill = Mathf.Clamp01(fill + Time.deltaTime * (isHighlighted ? fillTimeFactor : -4f));
		mat.SetFloat("_Fill", fill);
		if (fill >= 1)
		{
			fill = 0;
			App.Fade(true, 1f, delegate (bool s, string e)
			{
				VideoPlayerManager.Instance.Initialize(projectInstance.id, false, delegate (bool ss, string ee)
				{
					if (!ss) {
						PopupMessage.instance.Show(ee, PopupMessage.ButtonMode.Timer, null, 10f);
					}
				}, true);
			});
			gameObject.SetActive(false);
		}
	}

	void OnApplicationPause(bool pauseStatus) {
		if (!pauseStatus) {
			fillWaitTime = DEFAULT_FILL_WAIT;
		}
	}
}
