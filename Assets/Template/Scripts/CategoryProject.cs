using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using UnityEngine.EventSystems;

public class CategoryProject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string id;
	public RawImage rawImage, outline;
	public bool selected;
	private bool highlighted;
	public CategoryProjects categoryProjects;
	public void Initialize(string id)
	{
		this.id = id;
		rawImage.texture = App.GetImage(id);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		highlighted = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		highlighted = false;
	}

	private void Update()
	{
		RectTransform r = (RectTransform)transform;
		r.sizeDelta = Vector2.Lerp(r.sizeDelta,new Vector2(0.284f, 0.16f) * (selected ? 1f : 0.8f),Time.deltaTime*10f);
		outline.enabled = (highlighted || selected);
	}

	public void SwitchProject()
	{
		categoryProjects.SwitchInCategory(id);
	}
	}
