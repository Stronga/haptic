using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class ProjectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	//public CurvedUI curvedUI;
	private Button button;
	public Texture normalTexture, highlightTexture, pressedTexture;
	public Color normalColorText, highlightColorText, pressedColorText;
	public string normalText, highlightedText;
	public bool highlighted, pressed;
	public RawImage rawImage;
	public TextMeshProUGUI text;
	public bool stayHighlighted;


	public void OnPointerEnter(PointerEventData eventData)
	{
		highlighted = true;
		//curvedUI.SetMeshColor(rawImage, highlightColor);
		rawImage.texture = highlightTexture;
		if(text!=null) text.color = highlightColorText;
		if (text != null) text.text = highlightedText;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		highlighted = false;
		//curvedUI.SetMeshColor(rawImage, normalColor);
		rawImage.texture = stayHighlighted? pressedTexture : normalTexture;
		if (text != null) text.color = normalColorText;
		if (text != null) text.text = normalText;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		pressed = true;
		//curvedUI.SetMeshColor(rawImage, pressedColor);
		rawImage.texture = pressedTexture;
		if (text != null) text.color = pressedColorText;
		if (text != null) text.text = highlightedText;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		pressed = false;
		//curvedUI.SetMeshColor(rawImage, normalColor);
		rawImage.texture = stayHighlighted ? pressedTexture : normalTexture ;
		if (text != null) text.color = normalColorText;
		if (text != null) text.text = normalText;
	}

	private void OnEnable()
	{
		button = GetComponent<Button>();
		pressed = highlighted = false;
		rawImage.texture =stayHighlighted?pressedTexture : normalTexture;
		if (text != null) text.color = normalColorText;
		if (text != null) text.text = normalText;
	}
	private void Update()
	{
		if(stayHighlighted) rawImage.texture = pressedTexture;
		if (!button.interactable)
		{
			rawImage.color = new Color(1, 1, 1, 0);
			text.color= new Color(1, 1, 1, 0);
		}
	}
}
