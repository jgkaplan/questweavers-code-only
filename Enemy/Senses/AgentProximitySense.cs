using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class AgentProximitySense : AgentSense
{
    [SerializeField] ParticleSystem detectionEffect;
    [SerializeField] float detectionEffectAmount = 20f;
    [SerializeField] float detectionEffectNoise = 0.5f;

    [SerializeField] TextMeshProUGUI debugDetectionRate;
    [SerializeField] LayerMask OpaqueLayerMask;

    [SerializeField] bool RequireLantern = false;
    [SerializeField] bool RequireLineOfSight = false;

    [SerializeField] float DetectionRate = 1f;
    [SerializeField] float DetectionFadeRate = 1f;

    [SerializeField] float DetectionFadeDelay = 1f;

    [SerializeField] float DetectionRange = 5f;

    [SerializeField] float DetectionRangeOuter = 8f;

    [SerializeField] float DetectionRangeHeight = 1f;

    [SerializeField][Range(0f, 1f)] float AwarenessThreshold = 0.5f;

    float detectAmount = 0f;
    float lastDetectTime = 0f;
    Vector3 lastDetectPosition = Vector3.zero;

    FMODUnity.StudioEventEmitter emitter;

    AgentLanternOccupier occupier;

    public override string SenseName => "Proximity";

#if UNITY_EDITOR
    public void OnDrawGizmosSelected()
    {
        using (new Handles.DrawingScope(new Color(255, 0, 0, 0.05f)))
        {
            Handles.DrawSolidDisc(transform.position, Vector3.up, DetectionRangeOuter);
        }
        using (new Handles.DrawingScope(new Color(255, 0, 0, 0.2f)))
        {
            Handles.DrawSolidDisc(transform.position, Vector3.up, DetectionRange);
        }

    }
#endif

    public override void Awake()
    {
        base.Awake();
        occupier = brain.GetComponent<AgentLanternOccupier>();
        emitter = GetComponent<FMODUnity.StudioEventEmitter>();
    }

    public override bool IsSenseDetecting()
    {
        return detectAmount >= 1 || brain.GetWorldState("lantern_attack") == 1;
    }

    public override void Think()
    {
        var detecting = false;
        Target = Player.instance.gameObject; // TODO cache this

        // simple distance and cone check
        var origin = brain.transform.position;
        var targetPosition = Target.transform.position + Vector3.up;
        var dir = targetPosition - origin;
        var heightOffset = dir.y;
        dir.y = 0f;

        if (RequireLantern && brain.GetWorldState("has_lantern") == 0)
        {
            detectAmount = 0f;
            if (emitter.IsPlaying())
            {
                emitter.Stop();
            }
            return;
        }

        var startDetectionRange = DetectionRangeOuter; //detectAmount <= 0 ? DetectionRange : DetectionRangeOuter;

        // Debug.Log(dir.magnitude + " " + angle);

        if (dir.magnitude <= startDetectionRange && heightOffset <= DetectionRangeHeight)
        {
            // Raycast against opaque objects
            if (!RequireLineOfSight || !Physics.Raycast(origin, targetPosition - origin, (targetPosition - origin).magnitude, OpaqueLayerMask, QueryTriggerInteraction.Ignore))
            {
                var visibleFraction = 1f;

                visibleFraction *= Mathf.Pow(Mathf.InverseLerp(DetectionRangeOuter, DetectionRange, dir.magnitude), 2);

                var max = dir.magnitude <= brain.MeleeDistance ? 1f : AwarenessThreshold;

                if (visibleFraction > 0 && (!RequireLantern || brain.GetWorldState("has_lantern") == 1))
                {
                    detecting = true;
                    lastDetectTime = Time.time;
                    lastDetectPosition = Target.transform.position;

                    detectAmount = Mathf.MoveTowards(detectAmount, 1f, Time.fixedDeltaTime * DetectionRate * visibleFraction);

                    if (detectAmount > AwarenessThreshold)
                    {
                        brain.WorldStateMemory["target_proximity"] = 1;
                    }
                    if (brain.GetWorldState("hibernate") == 1)
                    {
                        brain.WorldStateMemory["hibernate"] = 0;
                        // Debug.Log(agent.transform.name + " stopped hibernating");
                    }
                }
            }
        }

        if (!detecting && Time.time - lastDetectTime > DetectionFadeDelay)
        {
            detectAmount = Mathf.MoveTowards(detectAmount, 0, Time.fixedDeltaTime * DetectionFadeRate);
            if (detectAmount <= 0f)
            {
                if (brain.GetWorldState("target_proximity") == 1)
                {
                    brain.WorldStateMemory["bored"] = 1;
                }
                brain.WorldStateMemory["target_proximity"] = 0;
            }
        }

        if (occupier != null && occupier.currentLantern != null)
        {
            occupier.currentLantern.animator.SetFloat("Tremble", detectAmount);
        }

        // Handle sound
        if (detectAmount > 0 && emitter != null && !emitter.IsPlaying())
        {
            emitter.Play();
        }
        else if (detectAmount <= 0 && emitter != null && emitter.IsPlaying())
        {
            emitter.Stop();
        }
        if (detectAmount > 0 && emitter != null)
        {
            emitter.SetParameter("RockIntensity", detectAmount);
            emitter.SetParameter("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(transform.position));
        }

        var dSqrt = Mathf.Sqrt(detectAmount);

        if (detectionEffect != null)
        {
            var em = detectionEffect.emission;
            em.rateOverTime = Mathf.Lerp(0, detectionEffectAmount, dSqrt);
            var ns = detectionEffect.noise;
            ns.positionAmount = Mathf.Lerp(0, detectionEffectNoise, dSqrt);
        }

        occupier.currentLantern.SetIntensity(dSqrt);
        occupier.currentLantern.SetFlicker(dSqrt);

        debugDetectionRate.text = ((int)(detectAmount * 100)).ToString();
    }
}
