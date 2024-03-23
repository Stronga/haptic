using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Headjack;
using TMPro;

public class CategorySwitch : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _categoryTitle;
    public TextMeshProUGUI CategoryTitle {
        get { return _categoryTitle; }
        set { 
            _categoryTitle = value;
            UpdateTitle();
        }
    }

    [SerializeField]
    private KioskSelect kioskSelect;
    [SerializeField]
    private RawImage switchBackground;
    [SerializeField]
    private Texture2D regularSwitchBackground;
    [SerializeField]
    private Texture2D selectedSwitchBackground;
    [SerializeField]
    private PlaySoundHighlight playButtonSound;


    private App.CategoryMetadata _categoryMeta;
    public App.CategoryMetadata CategoryMeta {
        get { return _categoryMeta; }
        set { 
            _categoryMeta = value;
            UpdateTitle();
        }
    }

    public void Start() {
        if (kioskSelect != null) {
            kioskSelect.categoryChangeEvent += OnCategoryChange;
        }

        // set initial/default selected category state
        OnCategoryChange(null);
    }

    private void OnCategoryChange(App.CategoryMetadata newCatMeta) {
        // indicate whether category is currently selected
                if ((newCatMeta == null && _categoryMeta == null) ||
                        newCatMeta?.Id == _categoryMeta?.Id) {
                    switchBackground.texture = selectedSwitchBackground;
                } else {
                    switchBackground.texture = regularSwitchBackground;
                }
    }

    private void UpdateTitle() {
        if (_categoryTitle != null) {
            _categoryTitle.text = _categoryMeta != null ? _categoryMeta.Name : "All";
        }
    }

    public void FilterByCategory() {
        if (kioskSelect == null) {
            return;
        }
        
        kioskSelect.FilterByCategory(_categoryMeta);

        // play button pressed sound
        playButtonSound.OnPointerClick(null);
    }

    void OnDestroy() {
        if (kioskSelect != null) {
            kioskSelect.categoryChangeEvent -= OnCategoryChange;
        }
    }
}
