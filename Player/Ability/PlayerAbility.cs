using UnityEngine;

public enum AbilityTargetMode
{
    /// <summary>
    /// Cast location is determined by raycasting in the camera's direction.
    /// </summary>
    Raycast,

    /// <summary>
    /// Cast location is always user's position.
    /// </summary>
    Self,
}

public enum AbilityCastMode
{
    /// <summary>
    /// Cast() is called once when input is released.
    /// </summary>
    Instant,

    /// <summary>
    /// Cast() is called every FixedUpdate() tick while input is held. Also calls OnCastStart() and OnCastStop().
    /// </summary>
    Continuous,
}

public partial class PlayerAbility : MonoBehaviour
{

    [Header("Identifier")]
    public virtual string AbilityName { get => "Base Ability"; }

    /// <summary>
    /// Player Animator "AbilityState" is set to this value.
    /// </summary>
    public virtual int AbilityAnimatorIndex { get => -1; }

    /// <summary>
    /// Prefab for the aim indicator, instantiated when aiming starts and destroyed when aiming stops.
    /// </summary>
    public GameObject IndicatorPrefab;

    [Header("Casting Parameters")]

    /// <summary>
    /// Amount of time that must pass in between casts.
    /// </summary>
    public float CastCooldown;

    /// <summary>
    /// Resource cost for casting. For Continuous cast mode, this is cost per second.
    /// </summary>
    public float CastCost;

    public virtual AbilityCastMode CastMode { get => AbilityCastMode.Instant; }

    [Header("Targetting Parameters")]
    /// <summary>
    /// Maximum distance of the raycast.
    /// </summary>
    public float RaycastDistance;

    /// <summary>
    /// Mask used for the raycast.
    /// </summary>
    public LayerMask RaycastMask;

    /// <summary>
    /// After raycasting in direction of aim, also raycast downward to place the target position on the ground.
    /// </summary>
    public bool RaycastGrounded;

    public virtual AbilityTargetMode TargetMode { get => AbilityTargetMode.Raycast; }

    [Header("Audio")]
    /// <summary>
    /// SFX played when the ability is cast.
    /// For continuous abilities, this is created and played once when casting starts and stopped when casting ends.
    /// </summary>
    public AudioClip AudioCast;

    /// <summary>
    /// SFX played when a Continuous ability stops being cast.
    /// </summary>
    public AudioClip AudioCastEnd;

    protected PlayerAbilityManager abilityManager;

    private void Start()
    {
        abilityManager = GetComponentInParent<PlayerAbilityManager>();
    }

    /// <summary>
    /// Check whether the ability is currently castable.
    /// </summary>
    /// <param name="castPosition">The cast location as determined by TargetMode.</param>
    /// <param name="castDirection">The cast direction as determined by TargetMode.</param>
    /// <returns>Return false to indicate that the ability cannot be cast currently.</returns>
    public virtual bool CanCast(Vector3 castPosition, Vector3 castDirection)
    {
        return true;
    }

    /// <summary>
    /// Called when the ability is casted.
    /// </summary>
    /// <param name="castPosition">The cast location as determined by TargetMode.</param>
    /// <param name="castDirection">The cast direction as determined by TargetMode.</param>
    public virtual void Cast(Vector3 castPosition, Vector3 castDirection) { }

    /// <summary>
    /// Called when the player starts aiming.
    /// </summary>
    /// <param name="indicator">An instantiated copy of IndicatorPrefab.</param>
    public virtual void OnAimStart(GameObject indicator) { }

    /// <summary>
    /// Called when the player stops aiming.
    /// </summary>
    public virtual void OnAimStop() { }

    /// <summary>
    /// Called every Update() tick when the player is aiming.
    /// </summary>
    /// <param name="indicator">An instantiated copy of IndicatorPrefab.</param>
    public virtual void AimThink(GameObject indicator) {}

    /// <summary>
    /// Called once when the user starts a Continuous cast. 
    /// </summary>
    /// <param name="castPosition">The cast location as determined by TargetMode.</param>
    /// <param name="castDirection">The cast direction as determined by TargetMode.</param>
    public virtual void OnCastStart(Vector3 castPosition, Vector3 castDirection) { }

    /// <summary>
    /// Called once when the user releases a Continuous cast. 
    /// </summary>
    /// <param name="castPosition">The cast location as determined by TargetMode.</param>
    /// <param name="castDirection">The cast direction as determined by TargetMode.</param>
    public virtual void OnCastStop(Vector3 castPosition, Vector3 castDirection) { }

    /// <summary>
    /// Calculate and returns the current casting cost.
    /// </summary>
    /// <returns>Cost of casting.</returns>
    public virtual float GetCastCost()
    {
        return CastCost;
    }

    /// <summary>
    /// Update castPosition and castDirection after PlayerAbilityManager performs TargetMode-based calculations.
    /// </summary>
    /// <param name="castPosition">The cast location as determined by TargetMode.</param>
    /// <param name="castDirection">The cast direction as determined by TargetMode.</param>
    public virtual void CorrectCastTarget(ref Vector3 castPosition, ref Vector3 castNormal)
    { 
    }
}
