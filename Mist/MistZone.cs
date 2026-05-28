using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VolumetricFogAndMist2;

[RequireComponent(typeof(VolumetricFog))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(GuidComponent))]
public class MistZone : MonoBehaviour
{

    [HideInInspector] public VolumetricFog fogZone;
    private Texture2D runtimeTexture;

    [SerializeField] private Texture2D[] textures;
    [SerializeField] private int initialTextureIndex = 0;
    [SerializeField] private int currentTextureIndex = 0;

    private Guid guid;
    private BoxCollider _collider;
    public static UnityEvent<Guid, int> mistFOWTextureChange = new();

    void OnValidate()
    {
        if (initialTextureIndex < 0 || initialTextureIndex >= textures.Length)
        {
            initialTextureIndex = 0;
        }
        if (currentTextureIndex < 0 || currentTextureIndex >= textures.Length)
        {
            currentTextureIndex = 0;
        }
        if (textures.Length > 0)
        {
            GetComponent<VolumetricFog>().fogOfWarTexture = textures[currentTextureIndex];
        }
    }

    void SetFOWTexture(int textureIndex, bool notifyChange = true)
    {
        if (0 <= textureIndex && textureIndex < textures.Length)
        {
            // index is ok
            bool isDifferent = currentTextureIndex != textureIndex; // only send out event if we're switching to a new texture
            currentTextureIndex = textureIndex;
            runtimeTexture = Instantiate(textures[textureIndex]);
            fogZone.fogOfWarTexture = runtimeTexture;
            if (notifyChange && isDifferent)
            {
                mistFOWTextureChange.Invoke(guid, textureIndex);
            }
        }
        else
        {
            Debug.LogError("Invalid fog of war texture index");
        }
    }


    // Reset is only called in inspector
    // Make sure that the collider is definitely set to trigger
    void Reset()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.isTrigger = true;
        }
    }

    void Awake()
    {
    }

    void OnEnable()
    {
        Checkpoint.activateCheckpoint.AddListener(OnCheckpointGet);
        fogZone = GetComponent<VolumetricFog>();
        _collider = GetComponent<BoxCollider>();
        guid = GetComponent<GuidComponent>().GetGuid();
    }

    void OnDisable()
    {
        Checkpoint.activateCheckpoint.RemoveListener(OnCheckpointGet);
        GameManager.Instance.DoReset.RemoveListener(ResetMist);
    }

    void Start()
    {
        GameManager.Instance.DoReset.AddListener(ResetMist);
        ResetMist();
        if (Player.instance != null)
        {
            fogZone.fadeController = Player.instance.transform;
        }

    }

    void OnCheckpointGet(bool firstTime, Transform _)
    {
        if (!firstTime)
        {
            SetFOWTexture(currentTextureIndex);
        }
    }

    void ResetMist()
    {
        StartCoroutine(ResetMistCo());
    }

    IEnumerator ResetMistCo()
    {
        yield return new WaitUntil(() => SaveSystem.saveData != null);
        if (SaveSystem.saveData.mistZoneTextureIndexes.TryGetValue(guid.ToString(), out int texIndex))
        {
            SetFOWTexture(texIndex, false);
        }
        else
        {
            SetFOWTexture(initialTextureIndex, false);
        }
    }

    void OnDestroy()
    {
        fogZone.fogOfWarTexture = textures[initialTextureIndex];
    }

    /*
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Set this zone as the currently active one
            MistManager.instance.currentMistZone = this;
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && MistManager.instance.currentMistZone == this) // an added check for update order, so that if a player moves from zone A to zone B directly, it doesn't get overriden to null
        {
            MistManager.instance.currentMistZone = null;
        }
    }
    */

    /// <summary>
    /// Check if the given position is within the bounds of this mist zone
    /// </summary>
    /// <param name="position">The point to check</param>
    /// <returns>True if position is in the bounds of the mist creation zone, or false otherwise</returns>
    public bool IsPointInZone(Vector3 position)
    {
        return _collider.ClosestPoint(position) == position;
    }

    /// <summary>
    /// Calculate how much mist is between two points. It averages between the fog density at points along the ray
    /// Note: This has a small issue of potentially overcomputing the last step. This shouldn't be an issue if the step size is small
    /// </summary>
    /// <param name="startPos">Start postion for ray march</param>
    /// <param name="endPos">End position for ray march</param>
    /// <param name="raymarchDistance">How far to step each march. Reduce this for higher performance but lower quality</param>
    /// <returns>An accumulation of the amount of fog between the two points. 0 for no fog, dist(startPos, endPos) for max fog</returns>
    public float GetDensityBetweenPoints(Vector3 startPos, Vector3 endPos, float raymarchDistance = 0.1f)
    {
        Vector3 marcher = startPos;
        Vector3 marcherEnd;
        float density = 0;
        float raymarchDensityMultiplier = raymarchDistance / 2; // precompute constant of (start + end) / 2 * raymarchDistance

        float scaledDensityAtStart = GetMistDensityAtPoint(marcher) * raymarchDensityMultiplier;
        while (marcher != endPos) // Vector3 already does delta comparison
        {
            marcherEnd = Vector3.MoveTowards(marcher, endPos, raymarchDistance);
            float scaledDensityAtEnd = GetMistDensityAtPoint(marcherEnd) * raymarchDensityMultiplier;

            // density += (densityAtStart + densityAtEnd) / 2 * raymarchDistance; => densityAtStart * raymarchDensityMultiplier + densityAtEnd * raymarchDensityMultiplier
            density += scaledDensityAtStart + scaledDensityAtEnd;

            // Take one step. Next loop we compute the next interval
            marcher = marcherEnd;
            scaledDensityAtStart = scaledDensityAtEnd;
        }

        return density;
    }


    /// <summary>
    /// This differs from VolumetricFog.GetFogOfWarAlpha because it treats areas outside the map as having no mist (0 density instead of 1)
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns>A float, [0,1] of the mist density at that world position.</returns>
    public float GetMistDensityAtPoint(Vector3 worldPosition)
    {
        Vector3 fogOfWarCenter = fogZone.anchoredFogOfWarCenter;
        float tx = (worldPosition.x - fogOfWarCenter.x) / fogZone.fogOfWarSize.x + 0.5f;
        if (tx < 0 || tx > 1f)
            return 0f;
        float tz = (worldPosition.z - fogOfWarCenter.z) / fogZone.fogOfWarSize.z + 0.5f;
        if (tz < 0 || tz > 1f)
            return 0f;

        if (fogZone.settings.customHeight)
        {
            float ty = (worldPosition.y - fogOfWarCenter.y) / fogZone.settings.height + 0.5f;
            if (ty < 0 || ty > 1f)
                return 0f;
        }

        float fowAlpha = fogZone.GetFogOfWarAlpha(worldPosition);
        float voidInfluence = MistManager.instance.GetMistVoidInfluenceAtPoint(worldPosition);
        return Mathf.Max(fowAlpha - voidInfluence, 0);
    }
}
