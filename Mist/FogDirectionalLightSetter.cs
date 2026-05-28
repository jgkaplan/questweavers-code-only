using UnityEngine;
using VolumetricFogAndMist2;

[ExecuteInEditMode]
[RequireComponent(typeof(Light))]
public class FogDirectionalLightSetter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        VolumetricFogManager fogManager = VolumetricFogManager.instance;
        if (fogManager != null)
        {
            fogManager.sun = GetComponent<Light>();
        }
    }
}
