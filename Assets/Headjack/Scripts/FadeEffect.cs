// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
namespace Headjack
{
    internal class FadeEffect : MonoBehaviour
    {
		private float currentOpacity=0f;
        private OnEnd _onEnd;
		private Coroutine currentFade;
		private MeshRenderer meshRenderer;
		private Material material;
        void Start()
        {
			meshRenderer = GetComponent<MeshRenderer>();
			material = meshRenderer.material;
			meshRenderer.enabled = false;
        }
		public void Fade(bool toBlack, float duration, OnEnd onEnd)
		{
			//Abort current fade if it exists
			if (currentFade != null) StopCoroutine(currentFade);
			currentFade = null;
			
			OnEnd invokeOnEnd = _onEnd;
			_onEnd = null;
			
			invokeOnEnd?.Invoke(true, null);

			_onEnd = onEnd;

			if (!this.gameObject.activeInHierarchy) {
				OnEnd invokeOnEnd2 = _onEnd;
				_onEnd = null;
				currentFade = null;
				invokeOnEnd2?.Invoke(true, null);
				return;
			}

			float target = toBlack ? 1f : 0f;
			float amountPerSecond = (Mathf.Abs(currentOpacity - target) / Mathf.Max(duration, Mathf.Epsilon))*(toBlack?1f:-1f);
			currentFade = StartCoroutine(Fading(target, amountPerSecond));
		}

		private IEnumerator Fading(float target, float amountPerSecond)
		{
			meshRenderer.enabled = true;
			while (currentOpacity != target)
			{
				currentOpacity = Mathf.Clamp01(currentOpacity + (amountPerSecond * Time.deltaTime));
				material.SetFloat("_Fade", currentOpacity);
				yield return null;
			}
			if (target == 0) meshRenderer.enabled = false;
			OnEnd invokeOnEnd = _onEnd;
			_onEnd = null;
			currentFade = null;
			invokeOnEnd?.Invoke(true, null);
		}

        void OnDisable()
        {
			material?.SetFloat("_Fade", currentOpacity = 0);
			if (meshRenderer != null) meshRenderer.enabled = false;
        }
    }
}
