using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handle Cardboard Overlay UI buttons with a custom solution, 
// Because two Canvas buttons cannot be activated simultaneously, so VR and overlay input conflicts
public class DirectButtonHelper : MonoBehaviour
{
    RectTransform buttonRect;
    Button button;

    // Start is called before the first frame update
    void Start()
    {
        buttonRect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        if (buttonRect != null && button != null) {
            // compare current touch/mouse position against button rect (on screen)
            if (Input.GetMouseButtonDown(0)) {
                if (RectTransformUtility.RectangleContainsScreenPoint(buttonRect, Input.mousePosition)) {
                    button.onClick.Invoke();
                }
            }
        }
    }
}
