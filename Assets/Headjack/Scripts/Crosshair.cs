// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack
{
	public class Crosshair : MonoBehaviour 
	{
        public static Crosshair crosshair;
        internal Transform Target;
		internal bool Show=false;
        internal Material LoadingMat;
		internal Color C;
        internal Material MyMaterial;
        private Material cMat;
        private MeshRenderer MyRenderer;
        private bool nowLoading = false;

        private float _rotationInDegrees=0;
        public float rotationInDegrees
        {
            get
            {
                return _rotationInDegrees;
            }
            set
            {
                _rotationInDegrees = value;
            }
        }
        private float _distance = 1.5f;
        public float distance
        {
            get
            {
                return _distance;
            }
            set
            {
                _distance = value;
            }
        }
        public Vector2 scale
        {
            get
            {
                return new Vector2(transform.localScale.x, transform.localScale.y);
            }
            set
            {
                transform.localScale = new Vector3(value.x, value.y, 1);
            }
        }
        public Material material
        {
            get
            {
                return MyMaterial;
            }
            set
            {
                MyRenderer.material = value;
                MyMaterial = value;
            }
        }

		void Start () 
		{
            C = new Color(0,0,0,0);
			crosshair = this;
            MyRenderer = GetComponent<MeshRenderer>();
            MyMaterial = MyRenderer.material;
            cMat = MyRenderer.material;
		}
        public void SetLoading(bool enableLoading, float loadingPercent = 0f)
        {
            if (enableLoading && !nowLoading)
            {
                MyRenderer.material = LoadingMat;
                cMat = LoadingMat;
                nowLoading = true;
            }
            else if (!enableLoading && nowLoading)
            {
                MyRenderer.material = MyMaterial;
                cMat = MyMaterial;
                nowLoading = false;
            }

            if (enableLoading)
            {
                Vector3 localEulers = gameObject.transform.localRotation.eulerAngles;
                localEulers.z = -loadingPercent * 360f;
                gameObject.transform.localRotation = Quaternion.Euler(localEulers);
            }
        }

		void LateUpdate () 
		{
			if (Target == null) {
                Target= App.camera.gameObject.transform;
			} else {

                if (Target.gameObject.activeInHierarchy)
                {
                    transform.position = Target.position + (Target.forward * _distance);
                    transform.rotation = Target.rotation;
                    transform.localEulerAngles += new Vector3(0, 0, _rotationInDegrees);
                }
				
                C = cMat.color;
				if (Show) {
                    MyRenderer.enabled = true;
                    cMat.color = new Color (1, 1, 1, Mathf.Clamp (C.a + Time.deltaTime * 4f, 0, 1));
				} else {
                    cMat.color = new Color (1, 1, 1, Mathf.Clamp (C.a - Time.deltaTime * 4f, 0, 1));
                    if (cMat.color.a < 0.01)
                    {
                        MyRenderer.enabled = false;
                    }
				}
			}
		}
	}
}
