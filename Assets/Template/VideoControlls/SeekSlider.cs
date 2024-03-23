using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Headjack;

public class SeekSlider : Slider {
	public bool holdingDown;
	public bool hover;
    public GameObject sliderHandle;

    bool updateToCurrentSeek = true;

    public override void OnPointerDown(PointerEventData eventData) {
        base.OnPointerDown(eventData);
		holdingDown = true;
		updateToCurrentSeek = false;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
		holdingDown = false;

		if (App.Player != null)
        {
            App.Player.Seek = value;
        }
        updateToCurrentSeek = true;
    }
	
    public override void OnPointerEnter(PointerEventData eventData)
    {
		hover = true;

		base.OnPointerEnter(eventData);

        if (interactable)
        {
           // sliderHandle.SetActive(true);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
		hover = false;

		base.OnPointerExit(eventData);

        //sliderHandle.SetActive(false);
    }

    void Update()
    {

        // update seek bar (if not livestream)
        if (App.ProjectIsLiveStream(App.CurrentProject))
        {
            // disable seek bar
            interactable = false;
            value = 0f;
            //sliderHandle.SetActive(false);
        }
        else
        {
            interactable = true;
            if (App.Player != null && updateToCurrentSeek)
            {
                value = App.Player.Seek;
            }
        }
    }
}
