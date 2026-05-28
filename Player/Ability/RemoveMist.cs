using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

public class RemoveMist : PlayerAbility
{
    [SerializeField] GameObject gourdModel;
    [SerializeField] ParticleSystem removeMistParticle;
    [SerializeField] VisualEffect removeMistVFX;
    public override string AbilityName { get => "Remove Mist"; }
    public override int AbilityAnimatorIndex { get => 1; }
    public override AbilityCastMode CastMode { get => AbilityCastMode.Continuous; }
    public override AbilityTargetMode TargetMode { get => AbilityTargetMode.Raycast; }

    [Header("Ability Parameters")]

    /// <summary>
    /// Size of the mist.
    /// </summary>
    public float MistSize = 3f;

    /// <summary>
    /// Transition delay 
    /// </summary>
    public float MistDelay = 0.1f;


    [Header("Sound Effects")]
    [SerializeField] private EventReference mistRemoveSound;
    [SerializeField] private StudioEventEmitter mistBottleRemoveSoundEmitter;

    private PlayerAbilityManager playerAbilityManager;

    private Vector3 lastCastPosition;
    private float cachedCastCost;
    private float currentCastSize;

    void Start()
    {
        playerAbilityManager = GetComponentInParent<PlayerAbilityManager>();
        cachedCastCost = 0f;
        currentCastSize = MistSize;
    }

    public override bool CanCast(Vector3 castPosition, Vector3 castDirection)
    {
        return true;
        // return playerAbilityManager.MistResource < playerAbilityManager.MaxMist;
    }

    public override void OnAimStart(GameObject indicator)
    {
        indicator.transform.localScale = new Vector3(2 * MistSize, 1f, 2 * MistSize); // doubling radius to get diameter scale

        gourdModel.SetActive(true);
    }

    public override void OnAimStop()
    {
        base.OnAimStop();

        gourdModel.SetActive(false);
    }

    // public override void AimThink(GameObject indicator)
    // {
    //     indicator.transform.localScale = new Vector3(2 * currentCastSize, 1f, 2 * currentCastSize); // doubling radius to get diameter scale
    // }


    public override void Cast(Vector3 castPosition, Vector3 castDirection)
    {
        var density = MistManager.instance.GetMistDensityInRadius(castPosition, currentCastSize);
        MistManager.instance.RemoveMist(castPosition + Vector3.up * 0.5f, radius: currentCastSize, duration: MistDelay);
        // var dist = Vector3.Distance(lastCastPosition, castPosition);
        // currentCastSize = Mathf.MoveTowards(currentCastSize, MistSize, MistAddAmount * CastCooldown);
        // currentCastSize = Mathf.Max(MistSizeInitial, currentCastSize - dist);
        lastCastPosition = castPosition;
        cachedCastCost = -1 * (density) * CastCost * (currentCastSize / MistSize);
        //FMODUnity.RuntimeManager.PlayOneShot(mistCastSound, castPosition);
    }

    public override void OnCastStart(Vector3 castPosition, Vector3 castDirection)
    {
        base.OnCastStart(castPosition, castDirection);
        lastCastPosition = castPosition;
        // removeMistParticle.Play();
        removeMistVFX.Play();
        removeMistVFX.SetVector3("Start Position", lastCastPosition);
        removeMistVFX.SetFloat("Radius", MistSize);
        mistBottleRemoveSoundEmitter.SetParameter("Bottle", 0);
        mistBottleRemoveSoundEmitter.Play();
    }

    public override void OnCastStop(Vector3 castPosition, Vector3 castDirection)
    {
        base.OnCastStop(castPosition, castDirection);
        currentCastSize = MistSize;
        // removeMistParticle.Stop();
        removeMistVFX.Stop();
        cachedCastCost = 0f;

        mistBottleRemoveSoundEmitter.SetParameter("Bottle", 1);
    }

    public override void CorrectCastTarget(ref Vector3 castPosition, ref Vector3 castNormal)
    {
        removeMistVFX.SetVector3("Start Position", lastCastPosition);
        castNormal = Vector3.forward;
    }

    public override float GetCastCost()
    {
        return cachedCastCost;
    }
}
