using UnityEngine;
using UnityEngine.Rendering;

[VolumeComponentMenu("Custom/Volume Light Blend")]
[DisplayInfo(name = "Volume Light Blend")]
public class VolumeLightBlendComponent : VolumeComponent
{
    public ClampedFloatParameter Intensity = new(value: 0f, min: 0, max: 1);
}
