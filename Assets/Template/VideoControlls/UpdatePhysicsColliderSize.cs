using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class UpdatePhysicsColliderSize : MonoBehaviour {
    public float widthExpand = 0f;
    public float heightExpand = 0f;
    public RectTransform parent;

    BoxCollider physicsCollider;

	// Use this for initialization
	void OnEnable () {
        physicsCollider = GetComponent<BoxCollider>();
        if (parent == null) {
            parent = GetComponent<RectTransform>();
        }
	}
	
	// Update is called once per frame
	void Update () {
		// update size of physics collider to parent transform
        if (physicsCollider != null && parent != null) {
            Vector3 physSize = physicsCollider.size;
            physSize.x = parent.rect.width + widthExpand;
            physSize.y = parent.rect.height + heightExpand;
            physicsCollider.size = physSize;
        }
	}

    public void ShowMessage() {
        Debug.LogWarning("BUTTON CLICKED!!!!");
    }
}
