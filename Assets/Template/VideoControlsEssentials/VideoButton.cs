using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class VideoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
	public bool isToggle;
	public bool activated;
	public Image block;
	public Image graphic;
	public Sprite originalGraphic, onGraphic, offGraphic;
	private VideoControls videoControls;

	private void Start()
	{
		videoControls = VideoControls.instance;

		if (originalGraphic == null) {
			originalGraphic = graphic.sprite;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (isToggle)
		{
			activated = !activated;
			graphic.sprite = activated ? onGraphic : offGraphic;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if(block!=null) block.color = videoControls.blockHighlight;
		graphic.color = videoControls.graphicHighlight;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (block != null) block.color = videoControls.blockNormal;
		graphic.color = videoControls.graphicNormal;
	}
	public void OnPointerDown(PointerEventData eventData)
	{
		if (block != null) block.color = videoControls.blockPressed;
		graphic.color = videoControls.graphicPressed;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (block != null && block.color == videoControls.blockPressed)
		{
			block.color = videoControls.blockHighlight;
			graphic.color = videoControls.graphicHighlight;
		}
		else
		{
			if (graphic.color == videoControls.graphicPressed)
			{
				graphic.color = videoControls.graphicHighlight;
			}
		}
	}

	public void SetIsToggle(bool newIsToggle) {
		if (newIsToggle) {
			activated = false;
		}
		isToggle = newIsToggle;
		if (isToggle) {
			graphic.sprite = activated ? onGraphic : offGraphic;
		} else {
			graphic.sprite = originalGraphic;
		}
	}

	void OnDisable() {
		if (videoControls != null) {
			if (block != null) {
				block.color = videoControls.blockNormal;
			}
			graphic.color = videoControls.graphicNormal;
		}
	}
}
