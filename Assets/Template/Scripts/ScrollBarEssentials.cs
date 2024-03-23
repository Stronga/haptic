using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScrollBarEssentials : MonoBehaviour {
	public EssentialsManager essentialsManager;
	public Transform top, bottom, allContent;
	public RawImage scrollBarSprite1, scrollBarSprite2;
	public float size;
	private void Start()
	{
		int categories = essentialsManager.categories.Count;
		int uncategorized = essentialsManager.uncategorized.projects.Length;

		bool disable = false;
		disable |= (categories == 2 && uncategorized == 0);
		disable |= (categories == 1 && uncategorized < 4);
		disable |= (categories == 0 && uncategorized < 7);
		scrollBarSprite1.enabled = scrollBarSprite2.enabled = !disable;
	}
	void Update ()
	{
		//Debug.Log((allContent.localPosition.y) / size);
		Vector3 newPosition = Vector3.Lerp(top.position, bottom.position, (allContent.localPosition.y) / size);
		if (!float.IsNaN(newPosition.x)) transform.position=newPosition;


	}
}
