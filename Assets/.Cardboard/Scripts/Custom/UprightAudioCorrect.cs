using UnityEngine;

public class UprightAudioCorrect : MonoBehaviour
{
    public GameObject VRCam;
    public GameObject NonVRCam;
    public AudioListener UprightAudioListener;

    void Start() {
        transform.parent = transform.parent.parent;

        if (UprightAudioListener == null) {
            UprightAudioListener = GetComponent<AudioListener>();
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (VRCam.activeInHierarchy) {
            transform.rotation = VRCam.transform.rotation;
            if (UprightAudioListener != null) {
                UprightAudioListener.enabled = true;
            }
        } else if (NonVRCam.activeInHierarchy) {
            transform.rotation = Quaternion.LookRotation(NonVRCam.transform.forward, Vector3.up);
            if (UprightAudioListener != null) {
                UprightAudioListener.enabled = true;
            }
        } else {
            transform.rotation = Quaternion.identity;
            if (UprightAudioListener != null) {
                UprightAudioListener.enabled = false;
            }
        }
    }
}
