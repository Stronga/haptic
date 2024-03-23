using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Headjack;
public class AnimatedDot : MonoBehaviour {

	private Vector3 previousPosition;
	private Quaternion previousRotation;
	public Transform innerPrefab, outerPrefab;
	private Transform[] inner, outer;
	private void Awake()
	{
		inner = new Transform[32];
		outer = new Transform[32];
		for (int i = 0; i < 32; ++i)
		{
			inner[i] = Instantiate(innerPrefab).transform;
			inner[i].SetParent(transform);
		
			outer[i] = Instantiate(outerPrefab).transform;
			outer[i].SetParent(transform);

			inner[i].localScale= outer[i].localScale = Vector3.one;
		}
	}
	private void OnEnable()
	{
		previousRotation = transform.rotation;
		previousPosition = transform.position;
	}
	void Update ()
	{
		for (int i = 0; i < 32; ++i)
		{
			float lerp = (1f / (32-1)) * i;
			outer[i].position = inner[i].position = Vector3.Lerp(previousPosition, transform.position, lerp);
			outer[i].rotation =  inner[i].rotation = Quaternion.Lerp(previousRotation, transform.rotation, lerp);
		}
		previousRotation = transform.rotation;
		previousPosition = transform.position;
	}
}
