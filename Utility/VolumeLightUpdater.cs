using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public sealed class VolumeLightUpdater : MonoBehaviour
{
    [SerializeField] private List<LightUpdateData> lightUpdateData;
    private float prevIntensity = -1;
    void LateUpdate() // executes after gameplay/animation update, but before rendering
    {
        // var camera = Camera.main;

        // if (!camera)
        //     return;

        // in HDRP, get the VolumeStack from the HDCamera associated with the main camera
        // var stack = UnityEngine.Rendering.HighDefinition.HDCamera
        //     .GetOrCreate(camera)
        //     .volumeStack;

        // in URP, obtain the VolumeStack from the UniversalAdditionalCameraData instead
        // var stack = camera
        //     .GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()
        //     .volumeStack;

        var stack = VolumeManager.instance.stack;
        if (stack == null || !stack.isValid)
        {
            return;
        }
        VolumeLightBlendComponent vlb = stack.GetComponent<VolumeLightBlendComponent>();

        float currentIntensity = vlb.Intensity.value;
        if (currentIntensity != prevIntensity)
        {
            foreach (var data in lightUpdateData)
            {
                if (data.light != null)
                {
                    data.light.intensity = Mathf.Lerp(data.volumeOffIntensity, data.volumeOnIntensity, vlb.Intensity.value);
                }
            }
            prevIntensity = currentIntensity;
        }
    }

    [Serializable]
    public class LightUpdateData
    {
        public Light light;
        public float volumeOffIntensity = 0;
        public float volumeOnIntensity = 1;
    }
}