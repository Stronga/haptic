using UnityEngine;

public class GammaCorrection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) {
            Shader.EnableKeyword("GAMMA_CORRECT");
            Debug.Log("GAMMA_CORRECT");
        } else {
            Shader.DisableKeyword("GAMMA_CORRECT");
        }
    }
}
