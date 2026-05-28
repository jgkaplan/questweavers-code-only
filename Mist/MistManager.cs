using System;
using UnityEngine;
using VolumetricFogAndMist2;

public class MistManager : MonoBehaviour
{
    // public MistZone currentMistZone;

    public static MistManager instance;

    // private MistZone[] activeZones;
    private MistZone activeZone;
    private MistVoid[] voids;

    public FMODUnity.EventReference fireExtinguishSound;

    void Awake()
    {
        // Checkpoint.activateCheckpoint.AddListener(ResetMistZones);
        instance = this;
    }

    void Start()
    {
        RefreshActiveZones();
    }
    public void UnsetActiveMistZones()
    {
        // activeZones = Array.Empty<MistZone>();
        activeZone = null;
        voids = Array.Empty<MistVoid>();
    }

    public void RefreshActiveZones()
    {
        // activeZones = FindObjectsByType<MistZone>(FindObjectsSortMode.None);
        activeZone = FindAnyObjectByType<MistZone>();
        voids = FindObjectsByType<MistVoid>(FindObjectsSortMode.None);
    }

    // TODO: this could be made more efficient with a better data structure
    public MistZone GetZoneAtPoint(Vector3 position)
    {
        /*
        foreach (MistZone zone in activeZones)
        {
            if (zone.IsPointInZone(position))
            {
                return zone;
            }
        }
        return null;
        */
        return activeZone;
    }


    /// <summary>
    /// Creates mist at a specified position, putting out fires in the radius
    /// </summary>
    /// <param name="pos">The world position to create the mist in</param>
    /// <param name="radius">How big of a disk of mist to create</param>
    /// <param name="duration">How long it should take for the mist to fade in</param>
    /// <returns>The amount of mist actually created. 0 if none, 1 if the entire circle was filled with full opacity mist</returns>
    public float CreateMist(Vector3 pos, float radius = 5.0f, float duration = 0.5f)
    {
        MistZone zone = GetZoneAtPoint(pos);
        if (!zone) return 0;

        // Extinguish fires
        var durationAdd = 0f;
        foreach (var col in Physics.OverlapSphere(pos, radius))
        {
            var fireBlocker = col.transform.GetComponent<FireBlocker>();
            var dir = col.transform.position - pos;
            if (fireBlocker != null && fireBlocker.IsFireActive()) // && !Physics.Raycast(pos + Vector3.up * 1f, dir.normalized, dir.magnitude, LayerMask.NameToLayer("Default"), QueryTriggerInteraction.Ignore)
            {
                fireBlocker.SetFireState(false);
                BackgroundMusicSystem.PlayOneShotSound(fireExtinguishSound, col.transform.position);
                durationAdd = 0.5f;
            }

            /*
            var enemyBrain = col.transform.GetComponentInChildren<AgentBrain>();
            if (enemyBrain != null && enemyBrain.Alertness == AgentAlertness.Calm)
            {
                enemyBrain.TriggerStimulus(null, col.transform.position + dir.normalized * (radius + 0.5f), AgentBrain.STIMULI_PRIORITY_MIST_ON_TOP);
            }
            */
        }

        float mistChange = zone.fogZone.SetFogOfWarAlpha(pos, radius, fogNewAlpha: 1, duration + durationAdd, zone.fogZone.fogOfWarSmoothness); // , FoWUpdateMethod.MainThread
        // Debug.Log($"Mist Change: {mistChange}");
        // Calculate how much mist we could have created, assuming we put 1 opacity everywhere
        float max_possible = Mathf.PI * radius * radius;
        float max_possible_scaled = max_possible * (zone.fogZone.fogOfWarTextureHeight * zone.fogZone.fogOfWarTextureWidth) / (zone.fogZone.fogOfWarSize.x * zone.fogZone.fogOfWarSize.z);
        // Debug.Log($"Max unscaled: {max_possible}    Max scaled: {max_possible_scaled}      Percent changed: {mistChange / max_possible_scaled}");
        return Mathf.Clamp01(mistChange / max_possible_scaled);
    }

    public void RemoveMist(Vector3 pos, float radius = 5.0f, float duration = 0.5f)
    {
        MistZone zone = GetZoneAtPoint(pos);
        if (!zone) return;
        zone.fogZone.SetFogOfWarAlpha(pos, radius, fogNewAlpha: 0, duration, zone.fogZone.fogOfWarSmoothness, FoWUpdateMethod.MainThread);
    }

    public float GetMistVoidInfluenceAtPoint(Vector3 position)
    {
        float influence = 0;
        foreach (MistVoid v in voids)
        {
            influence = Mathf.Max(influence, v.ComputeInfluenceAtPoint(position));
        }
        return influence;
    }

    // TODO: consider summing the points of all zones. Less efficient but it works with overlapping zones
    public float GetMistDensityAtPoint(Vector3 position)
    {
        MistZone zone = GetZoneAtPoint(position);
        if (zone == null)
        {
            return 0;
        }
        return zone.GetMistDensityAtPoint(position);
    }

    // TODO: this will return incorrect values if there are more than two zones this raycast passes through.
    //         I'm implementing it this way to save time, but if this becomes a problem we can fix it
    //         The ideal solution would check the mist zone at every raycast point and use that one, but we'd need more efficient lookup
    public float GetDensityBetweenPoints(Vector3 startPos, Vector3 endPos, float raymarchDistance = 0.1f)
    {
        MistZone startZone = GetZoneAtPoint(startPos);
        MistZone endZone = GetZoneAtPoint(endPos);
        if (startZone == null && endZone == null)
        {
            return 0;
        }
        if (startZone == endZone)
        {
            // nice case. Everything is in the same zone
            return startZone.GetDensityBetweenPoints(startPos, endPos, raymarchDistance);
        }
        else
        {

            float startZoneDensity = startZone == null ? 0 : startZone.GetDensityBetweenPoints(startPos, endPos, raymarchDistance);
            float endZoneDensity = endZone == null ? 0 : endZone.GetDensityBetweenPoints(startPos, endPos, raymarchDistance);
            return startZoneDensity + endZoneDensity;
        }
    }


    // public float GetMaxMistInRadius(Vector3 position, float radius)
    // {
    //     MistZone zone = GetZoneAtPoint(position);

    // }

    /// <summary>
    /// Returns how opaque the mist is in the given radius.
    /// </summary>
    /// <param name="worldPosition">World position to check.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <returns>Fraction from 0-1 for how much mist is in the radius.</returns>
    public float GetMistDensityInRadius(Vector3 worldPosition, float radius)
    {
        MistZone zone = GetZoneAtPoint(worldPosition);
        if (zone == null)
        {
            return 0;
        }
        // float t1 = zone.fogZone.GetFogOfWarVolume(worldPosition, radius);
        float t2 = zone.fogZone.GetFogOfWarVolumeWithTransitions(worldPosition, radius);
        // Debug.Log($"No transitions: {t1}\tTransitions: {t2}");
        return t2;
    }
}
