using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GazeScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
	public enum ScrollDirection {
		None = 0,
		Up = 1,
		Down = 2
	};

	public ScrollDirection scrollDirection;
	public Button scrollButton;
	public KioskSelect kioskSelect;
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (scrollButton.interactable) {
			kioskSelect.SetScrollDirection(scrollDirection);
		} else {
			kioskSelect.SetScrollDirection(ScrollDirection.None);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		kioskSelect.SetScrollDirection(ScrollDirection.None);
	}
}
