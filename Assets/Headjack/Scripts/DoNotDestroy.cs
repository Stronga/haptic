// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack
{
	internal class DoNotDestroy : MonoBehaviour
	{
		private void Start()
		{
			DontDestroyOnLoad(gameObject.transform);
		}
	}
}