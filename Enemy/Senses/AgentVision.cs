using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class AgentVision : AgentSense
{
    [SerializeField] Transform EyeTransform;
    [SerializeField] TextMeshProUGUI debugDetectionRate;
    [SerializeField] LayerMask OpaqueLayerMask;

    public float DetectionRate = 1f;
    public float DetectionFadeRate = 0.5f;

    /// <summary>
    /// A detected target will remain "seen" this amount even if raycast fails.
    /// </summary>
    public float DetectionLingerDelay = 1f;
    /// <summary>
    /// After this amount of time, detection amount will start to fade.
    /// </summary>
    public float DetectionFadeDelay = 3f;

    /// <summary>
    /// Within this range, the enemy detects targets at maximum effectiveness.
    /// </summary>
    public float VisionRange = 20f;
    /// <summary>
    /// Between VisionRange and this value, the enemy's detection effectiveness falls off quadratically.
    /// </summary>
    public float VisionRangeOuter = 30f;

    /// <summary>
    /// Within this horizontal angle, the enemy detects targets at maximum effectiveness.
    /// </summary>
    public float VisionConeInner = 60f;

    /// <summary>
    /// Between the inner and outer angle, the enemy's detection effectiveness falls off quadratically.
    /// </summary>
    public float VisionConeOuter = 90f;

    /// <summary>
    /// Within this range, vision cone and mist checks are ignored. (Line of sight is still respected.)
    /// </summary>
    public float OmniscientRange = 1.5f;


    /// <summary>
    /// Amount of mist that will fully block vision.
    /// </summary>
    public float MistOpaqueVolume = 3f;

    /// <summary>
    /// Amount of mist that will fully block vision while calm.
    /// </summary>
    public float MistObscureThreshold = 0.9f;

    /// <summary>
    /// Fraction of detection required to trigger a stimulus. Additionally, while cautious, detection will not fall below this treshold.
    /// </summary>
    [Range(0f, 1f)] public float AwarenessThreshold = 0.5f;

    /*
    [SerializeField] bool ShowDirectionalIndicator = true;

    [SerializeField] GameObject DirectionalIndicatorPrefab;

    [SerializeField] Light DetectionLight;
    [SerializeField] float LightIntensityMinimum = 1f;
    [SerializeField] float LightIntensityMaximum = 5f;
    [SerializeField] Color LightColorCalm = Color.gray;
    [SerializeField] Color LightColorAwareness = Color.lightCyan;
    [SerializeField] Color LightColorAlert = Color.mediumPurple;

    DirectionalIndicator indicator;
    */

    float detectAmount = 0f;
    float lastDetectTime = 0f;
    Vector3 lastDetectPosition = Vector3.zero;

    public override string SenseName => "Vision";

#if UNITY_EDITOR
    public void OnDrawGizmosSelected()
    {
        using (new Handles.DrawingScope(new Color(255, 0, 0, 0.05f)))
        {
            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, VisionConeOuter / 2, VisionRangeOuter);
            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, -VisionConeOuter / 2, VisionRangeOuter);
        }
        using (new Handles.DrawingScope(new Color(255, 0, 0, 0.2f)))
        {

            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, VisionConeInner / 2, VisionRange);
            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, -VisionConeInner / 2, VisionRange);
        }

    }
#endif

    public float GetDetectionAmount()
    {
        return detectAmount;
    }
    public float GetLastDetectTime()
    {
        return lastDetectTime;
    }

    public override bool IsSenseDetecting()
    {
        return brain.GetWorldState("target_visible") == 1;
    }

    public override void Think()
    {
        var detecting = false;
        Target = Player.instance.gameObject; // TODO cache this

        // simple distance and cone check
        var origin = EyeTransform.position;
        var targetPosition = Target.transform.position + Vector3.up;
        var dir = targetPosition - origin;
        var heightOffset = dir.y;
        dir.y = 0f;
        var angle = Vector3.Angle(dir.normalized, agent.transform.forward);
        var omniscience = (dir.magnitude <= OmniscientRange); // brain.Alertness != AgentAlertness.Calm && 

        // Debug.Log(dir.magnitude + " " + angle);

        if (omniscience || (dir.magnitude <= VisionRangeOuter
            && ((brain.Alertness == AgentAlertness.Calm && angle <= VisionConeInner) || (brain.Alertness != AgentAlertness.Calm && angle <= VisionConeOuter))))
        {
            // Raycast against opaque objects
            if (!Physics.Raycast(origin, targetPosition - origin, (targetPosition - origin).magnitude, OpaqueLayerMask, QueryTriggerInteraction.Ignore))
            {
                var visibleFraction = 1f;

                visibleFraction *= Mathf.Pow(Mathf.InverseLerp(VisionRangeOuter, VisionRange, dir.magnitude), 2);
                visibleFraction *= Mathf.Lerp(0.5f, 1f, Mathf.Pow(Mathf.InverseLerp(VisionConeInner, VisionConeInner / 2f, angle), 2));

                // Raymarch for mist obstruction
                float density = omniscience ? 0 : MistManager.instance.GetDensityBetweenPoints(origin, targetPosition, 0.5f);
                visibleFraction *= Mathf.Lerp(1f, 0f, density / MistOpaqueVolume);

                if (omniscience)
                {
                    visibleFraction = Mathf.Max(visibleFraction, 0.25f);
                }
                else if (density > MistObscureThreshold && brain.Alertness == AgentAlertness.Calm)
                {
                    visibleFraction = 0f;
                }

                if (visibleFraction > 0)
                {
                    detecting = true;
                    lastDetectTime = Time.time;
                    lastDetectPosition = Target.transform.position;

                    if (brain.Alertness == AgentAlertness.Alert)
                    {
                        //detectAmount = 1;
                        detectAmount = Mathf.MoveTowards(detectAmount, 1f, Time.fixedDeltaTime * DetectionRate * visibleFraction);
                    }
                    else
                    {
                        detectAmount = Mathf.MoveTowards(detectAmount, 1f, Time.fixedDeltaTime * DetectionRate * visibleFraction);
                    }
                    // detectAmount = Mathf.MoveTowards(detectAmount, 1f, Time.fixedDeltaTime * DetectionRate * visibleFraction);

                    if (detectAmount >= 1f)
                    {
                        brain.WorldStateMemory["target_visible"] = 1;
                        brain.WorldStateMemory["target_visible_recently"] = 1;
                    }
                    else if (detectAmount >= AwarenessThreshold)
                    {
                        brain.WorldStateMemory["target_visible_recently"] = 1;

                        if (!brain.HasStimulus())
                        {
                            brain.TriggerStimulus(this, lastDetectPosition, AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION);
                        }
                    }

                    /*
                    if (ShowDirectionalIndicator)
                    {
                        if (indicator == null && Target.CompareTag("Player"))
                        {
                            var indicatorObj = Instantiate(DirectionalIndicatorPrefab, GameObject.FindGameObjectWithTag("IndicatorCanvas").transform);
                            indicator = indicatorObj.GetComponent<DirectionalIndicator>();
                            indicator.target = transform;
                            indicator.player = Target.transform;
                        }

                        if (indicator != null)
                        {
                            indicator.gameObject.SetActive(true);
                            indicator.SetIntensity(detectAmount);
                        }
                    }
                    */
                }
            }
        }

        if (!detecting && Time.time - lastDetectTime > DetectionLingerDelay)
        {

            if (detectAmount > AwarenessThreshold)
            {
                brain.TriggerStimulus(this, lastDetectPosition, AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION);
            }

            if (Time.time - lastDetectTime > DetectionFadeDelay)
            {
                detectAmount = Mathf.MoveTowards(detectAmount, (brain.Alertness == AgentAlertness.Calm ? 0 : AwarenessThreshold), Time.fixedDeltaTime * DetectionFadeRate);

                /*
                if (ShowDirectionalIndicator)
                {
                    if (indicator != null && detectAmount <= 0f)
                    {
                        indicator.gameObject.SetActive(false);
                    }
                    else if (indicator != null)
                    {
                        indicator.SetIntensity(detectAmount);
                    }
                }
                */


                if (detectAmount <= AwarenessThreshold)
                {
                    brain.WorldStateMemory["target_visible_recently"] = 0;
                }
                else
                {
                    brain.WorldStateMemory["target_visible"] = 0;
                }
            }
        }
        /*
        if (DetectionLight != null)
        {
            DetectionLight.intensity = Mathf.Lerp(LightIntensityMinimum, LightIntensityMaximum, Mathf.Pow(detectAmount, 2f));
            if (detectAmount >= 1)
            {
                DetectionLight.color = LightColorAlert;
            }
            else
            {
                DetectionLight.color = Color.Lerp(LightColorCalm, LightColorAwareness, detectAmount / AwarenessThreshold);
            }
        }
        */
        debugDetectionRate.text = ((int)(detectAmount * 100)).ToString();
    }

    public override void OnPanic()
    {
        base.OnPanic();
        detectAmount = 0f;
        /*
        if (DetectionLight != null)
        {
            DetectionLight.intensity = 0f;
        }
        */
    }
}
