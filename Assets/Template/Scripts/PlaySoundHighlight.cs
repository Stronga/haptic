using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaySoundHighlight : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
  public AudioClip highlightSound, clickSound;
  public EssentialsManager essentialsManager;
  public AudioSource overrideAudioSource;
  public Button btn;

  private void Start()
  {
    if (btn == null) {
      btn = GetComponent<Button>();
    }
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    PlaySound(clickSound);
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    PlaySound(highlightSound);
  }
  private void PlaySound(AudioClip clip)
  {
    if (clip == null) return;
    if (btn != null && !btn.interactable) return;
    AudioSource source = overrideAudioSource != null ? overrideAudioSource : essentialsManager.audioSource;
    source.pitch = Random.Range(0.995f, 1.005f);
    source.PlayOneShot(clip);
  }
}
