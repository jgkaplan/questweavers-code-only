using System;
using UnityEngine;
using VolumetricFogAndMist2;

[RequireComponent(typeof(FogVoid))]
public class MistVoid : MonoBehaviour
{
    private FogVoid fogVoid;
    private Vector3 sizes;
    private float roundness;
    private float inverse_falloff;
    private bool zeroSize = false;
    [HideInInspector] public float activated = 3; // for the animator to trigger recalculation
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fogVoid = GetComponent<FogVoid>();
        SetupConstants();
    }


    public float ComputeInfluenceAtPoint(Vector3 position)
    {
        if (!fogVoid.isActiveAndEnabled || zeroSize) return 0;

        // TODO: handle rotation
        Vector3 vd = transform.position - position;
        // we square the point. on a rectangle, we're now working in squared coords. On a circle, this saves us a sqrt
        vd.Scale(vd);
        // warp rect to box, ellipsoid to circle
        vd.Scale(sizes);

        // float rect = Mathf.Max(vd.x, vd.y, vd.z);
        float rect = Mathf.Max(Mathf.Max(vd.x, vd.y), vd.z);
        float circ = vd.x + vd.y + vd.z;
        // 0 - 1 how close are we to the edge. Center is 0, edge is 1
        float voidd = Mathf.Lerp(rect, circ, roundness);
        // falloff calculation. I still don't fully understand this
        voidd = Mathf.Lerp(1.0f, voidd, inverse_falloff);
        voidd = 1.0f - voidd;
        voidd = Mathf.Clamp(voidd, 0, 1);
        return voidd;
    }

    void SetupConstants()
    {
        Vector3 scale = transform.lossyScale;
        zeroSize = false;
        if (scale.x < 0.01f)
        {
            zeroSize = true;
            scale.x = 0.01f;
        }
        if (scale.y < 0.01f)
        {
            zeroSize = true;
            scale.y = 0.01f;
        }
        if (scale.z < 0.01f)
        {
            zeroSize = true;
            scale.z = 0.01f;
        }
        scale *= 0.5f; // shrink by half so we only consider top right quadrant

        // has to be squared because everything else is squared
        inverse_falloff = 10f * (1f - fogVoid.falloff) * (1f - fogVoid.falloff);

        // We'll pre-square everything for calculation efficiency
        // This converts a box to a cube, and an ellipsoid to a circle
        sizes = new(1f / (0.0001f + scale.x * scale.x), 1f / (0.0001f + scale.y * scale.y), 1f / (0.0001f + scale.z * scale.z));

        roundness = fogVoid.roundness;
    }

    // For some reason, this isn't documented anywhere
    public void OnDidApplyAnimationProperties()
    {
        SetupConstants();
    }
}
/*
void SubmitFogVoidData() {

            bool allowRotation = VolumetricFogManager.allowFogVoidRotation;

            int count = 0;
            int fogVoidsCount = fogVoids.Count;
            for (int i = 0; count < MAX_FOG_VOID && i < fogVoidsCount; i++) {
                FogVoid fogVoid = fogVoids[i];
                if (fogVoid == null || !fogVoid.isActiveAndEnabled) continue;
                Transform t = fogVoid.transform;
                Vector3 pos = t.position;
                Vector3 scale = t.lossyScale;
                if (scale.x < 0.01f) scale.x = 0.01f;
                if (scale.y < 0.01f) scale.y = 0.01f;
                if (scale.z < 0.01f) scale.z = 0.01f;
                scale.x *= 0.5f;
                scale.y *= 0.5f;
                scale.z *= 0.5f;
                fogVoidPositions[count].x = pos.x;
                fogVoidPositions[count].y = pos.y;
                fogVoidPositions[count].z = pos.z;
                fogVoidPositions[count].w = 10f * (1f - fogVoid.falloff) * (1f - fogVoid.falloff);
                if (allowRotation) {
                    fogVoidMatrices[count] = Matrix4x4.TRS(pos, t.rotation, scale).inverse;
                } else {
                    float width = scale.x;
                    float height = scale.y;
                    float depth = scale.z;
                    fogVoidSizes[count].x = 1f / (0.0001f + width * width);
                    fogVoidSizes[count].y = 1f / (0.0001f + height * height);
                    fogVoidSizes[count].z = 1f / (0.0001f + depth * depth);
                }
                fogVoidSizes[count].w = fogVoid.roundness;
                count++;
            }
            Shader.SetGlobalInt(ShaderParams.VoidCount, count);
            if (count > 0) {
                Shader.SetGlobalVectorArray(ShaderParams.VoidPositions, fogVoidPositions);
                if (allowRotation) {
                    Shader.SetGlobalMatrixArray(ShaderParams.VoidMatrices, fogVoidMatrices);
                }
                Shader.SetGlobalVectorArray(ShaderParams.VoidSizes, fogVoidSizes);
            }
        }
*/

/*
half ApplyFogVoids(float3 wpos) {

    float sdf = 10.0;
    for (int k=0;k<_VF2_FogVoidCount;k++) {

        // sqr distance to void center
        #if defined(FOG_VOID_ROTATION)
            float3 vd = mul(_VF2_FogVoidMatrices[k], float4(wpos.xyz, 1.0)).xyz;
            vd *= vd;
        #else
            float3 vd = _VF2_FogVoidPositions[k].xyz - wpos.xyz;
            vd *= vd;
            // relative to void size
            vd *= _VF2_FogVoidSizes[k].xyz;
        #endif

        // rect
        float rect = max(vd.x, max(vd.y, vd.z));

        // circle
        float circ = vd.x + vd.y + vd.z;

        // roundness
        float voidd = lerp(rect, circ, _VF2_FogVoidSizes[k].w);

        // falloff
        voidd = lerp(1.0, voidd, _VF2_FogVoidPositions[k].w);

        // merge sdf
        sdf = min(sdf, voidd);
    }
    sdf = 1.0 - sdf;
    return saturate(sdf);
}

*/
