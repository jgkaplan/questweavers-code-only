using FMODUnity;
using UnityEngine;

public class CreateMist : PlayerAbility
{
    [SerializeField] GameObject gourdModel;
    [SerializeField] ParticleSystem createMistParticle;

    public override string AbilityName { get => "Create Mist"; }
    public override int AbilityAnimatorIndex { get => 1; }
    public override AbilityCastMode CastMode { get => AbilityCastMode.Continuous; }
    public override AbilityTargetMode TargetMode { get => AbilityTargetMode.Raycast; }

    [Header("Ability Parameters")]

    /// <summary>
    /// Size of the mist when fully charged.
    /// </summary>
    public float MistSize = 3f;

    /// <summary>
    /// Size of the mist on first use.
    /// </summary>
    public float MistSizeInitial = 0.5f;

    /// <summary>
    /// Mist size growth per second.
    /// </summary>
    public float MistAddAmount = 3f;

    /// <summary>
    /// Transition delay 
    /// </summary>
    public float MistDelay = 0.1f;

    [Header("Sound Effects")]
    public EventReference mistCastSound;
    public StudioEventEmitter mistBottleCastSoundEmitter;

    private PlayerAbilityManager playerAbilityManager;

    private Vector3 lastCastPosition;
    private float cachedCastCost;
    private float currentCastSize;

    void Start()
    {
        playerAbilityManager = GetComponentInParent<PlayerAbilityManager>();
        cachedCastCost = 0f;
    }

    public override bool CanCast(Vector3 castPosition, Vector3 castDirection)
    {
        // TODO: Make sure this is actually placed in a place that has a zone.
        // return playerAbilityManager.MistResource >= CastCost;
        return playerAbilityManager.MistResource > 0;
    }

    public override void OnAimStart(GameObject indicator)
    {
        // indicator.transform.localScale = 2 * MistSize * Vector3.one; // doubling radius to get diameter scale
        currentCastSize = MistSizeInitial;
        gourdModel.SetActive(true);
    }

    public override void OnAimStop()
    {
        base.OnAimStop();

        gourdModel.SetActive(false);
    }

    public override void AimThink(GameObject indicator)
    {
        indicator.transform.localScale = new Vector3(2 * currentCastSize, 1f, 2 * currentCastSize); // doubling radius to get diameter scale
    }

    public override void Cast(Vector3 castPosition, Vector3 castDirection)
    {
        var density = MistManager.instance.GetMistDensityInRadius(castPosition, currentCastSize);
        var f = MistManager.instance.CreateMist(castPosition + Vector3.up * 0.5f, radius: currentCastSize, duration: MistDelay);
        var dist = Vector3.Distance(lastCastPosition, castPosition);
        currentCastSize = Mathf.MoveTowards(currentCastSize, MistSize, MistAddAmount * CastCooldown);
        currentCastSize = Mathf.Max(MistSizeInitial, currentCastSize - dist);
        lastCastPosition = castPosition;
        cachedCastCost = (1 - density) * CastCost * (currentCastSize / MistSize);
        //FMODUnity.RuntimeManager.PlayOneShot(mistCastSound, castPosition);
    }

    public override void OnCastStart(Vector3 castPosition, Vector3 castDirection)
    {
        base.OnCastStart(castPosition, castDirection);
        lastCastPosition = castPosition;
        createMistParticle.Play();
        mistBottleCastSoundEmitter.SetParameter("Bottle", 0);
        mistBottleCastSoundEmitter.Play();
    }

    public override void OnCastStop(Vector3 castPosition, Vector3 castDirection)
    {
        base.OnCastStop(castPosition, castDirection);
        currentCastSize = MistSizeInitial;
        createMistParticle.Stop();
        cachedCastCost = 0f;

        mistBottleCastSoundEmitter.SetParameter("Bottle", 1);
    }

    public override void CorrectCastTarget(ref Vector3 castPosition, ref Vector3 castNormal)
    {
        castNormal = Vector3.forward;
    }

    public override float GetCastCost()
    {
        return cachedCastCost;
    }
}
