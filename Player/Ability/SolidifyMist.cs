using TMPro;
using UnityEngine;

public class SolidifyMist : PlayerAbility
{
    public override string AbilityName { get => "Solidify Mist"; }
    public override AbilityCastMode CastMode { get => AbilityCastMode.Instant; }
    public override AbilityTargetMode TargetMode { get => AbilityTargetMode.Raycast; }

    [Header("Ability Parameters")]

    [SerializeField] GameObject MistBlockPrefab;
    [SerializeField] Material IndicatorMaterial;
    [SerializeField] Material IndicatorMaterialInvalid;

    /// <summary>
    /// Vertical height of the mist block.
    /// </summary>
    public float BoxHeight = 2f;

    /// <summary>
    /// Minimum horizontal size of the mist block.
    /// </summary>
    public float BoxSizeMinimum = 3f;

    /// <summary>
    /// Maximum horizontal size of the mist block.
    /// </summary>
    public float BoxSizeMaximum = 3f;

    /// <summary>
    /// Transition delay 
    /// </summary>
    public float MistDelay = 0.1f;

    Vector3 currentSize;

    public override bool CanCast(Vector3 castPosition, Vector3 castDirection)
    {
        // TODO: Make sure this is actually placed in a place that has a zone.

        if (Physics.BoxCast(castPosition, currentSize / 2f, Vector3.up, Quaternion.identity, 0f, RaycastMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return true;
    }

    public override void CorrectCastTarget(ref Vector3 castPosition, ref Vector3 castDirection)
    {
        castPosition += Vector3.up * BoxHeight / 2f;
        var colliders = Physics.OverlapSphere(castPosition, BoxSizeMaximum / 2f, RaycastMask, QueryTriggerInteraction.Ignore);

        var startPos = castPosition;
        var col1 = abilityManager.CastIndicator.GetComponent<BoxCollider>();
        foreach (var col2 in colliders)
        {
            var overlap = Physics.ComputePenetration(
                col1, castPosition, Quaternion.identity,
                col2, col2.transform.position, col2.transform.rotation,
                out Vector3 direction,
                out float distance);

            if (overlap)
            {
                /*
                if (Vector3.Dot(direction, Vector3.up) >= 0)
                {
                    if (Vector3.Dot(direction, (castPosition - startPos).normalized) >= 0)
                    {
                        castPosition += direction * distance;
                    }
                    else
                    {
                        // TODO: shrink box to fit narrow gaps?
                    }
                }
                */
                castPosition += direction * distance;
            }
        }
        castDirection.y = 0f;
        castDirection.Normalize();
    }

    public override void OnAimStart(GameObject indicator)
    {
        currentSize = new Vector3(BoxSizeMaximum, BoxHeight, BoxSizeMaximum);
        indicator.transform.localScale = currentSize;
        indicator.transform.localPosition = Vector3.up * BoxHeight / 2f;
    }

    public override void Cast(Vector3 castPosition, Vector3 castDirection)
    {
        MistManager.instance.RemoveMist(castPosition, currentSize.magnitude, MistDelay);
        // MistManager.instance.currentMistZone.fogZone.SetFogOfWarAlpha(castPosition, radius: currentSize.magnitude, fogNewAlpha: 0, duration: MistDelay);
        var mistBlock = Instantiate(MistBlockPrefab);
        mistBlock.transform.position = castPosition;
        mistBlock.transform.rotation = Quaternion.LookRotation(castDirection, Vector3.up);
        mistBlock.transform.localScale = currentSize;
    }
}
