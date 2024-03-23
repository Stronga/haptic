using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class Thumbnail : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public bool highlighted;
	public RawImage rawImage, highlight;
	private Material mat;
	private float zoom = 1;
	public void OnPointerEnter(PointerEventData eventData)
	{
		highlighted = highlight.enabled = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		highlighted = highlight.enabled = false;
	}
	void Start () {
		mat = rawImage.material = Instantiate(rawImage.material);
	}
	
	// Update is called once per frame
	void Update () {
		zoom = Mathf.Lerp(zoom, highlighted ? 0.9f : 1f, Time.deltaTime *(highlighted?2f:5f));
		mat.SetFloat("_Zoom", zoom);
	}
}
