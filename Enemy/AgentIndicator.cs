using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(AgentBrain))]
public class AgentIndicator : MonoBehaviour
{
    [SerializeField] AgentVision vision;
    [SerializeField] AgentHearing hearing;

    [Header("Directional Indicator")]
    [SerializeField] GameObject DirectionalIndicatorPrefab;
    [SerializeField] Sprite IndicatorSpriteVision;
    [SerializeField] Sprite IndicatorSpriteHearing;

    [Header("Vision Lighting")]
    [SerializeField] Light DetectionLight;
    [SerializeField] float LightIntensityMinimum = 1f;
    [SerializeField] float LightIntensityMaximum = 5f;
    [SerializeField] Color LightColorCalm = Color.gray;
    [SerializeField] Color LightColorAwareness = Color.lightCyan;
    [SerializeField] Color LightColorAlert = Color.mediumPurple;

    AgentBrain brain;
    DirectionalIndicator indicator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        brain = GetComponent<AgentBrain>();
    }

    // Update is called once per frame
    void Update()
    {
        var detectAmount = vision.GetDetectionAmount();
        var alertness = brain.Alertness;

        if (detectAmount > 0 && indicator == null)
        {
            var indicatorObj = Instantiate(DirectionalIndicatorPrefab, GameObject.FindGameObjectWithTag("IndicatorCanvas").transform);
            indicator = indicatorObj.GetComponent<DirectionalIndicator>();
            indicator.target = transform;
            indicator.player = Player.instance.transform;
            indicator.gameObject.SetActive(false);
        }

        if (indicator != null)
        {
            indicator.gameObject.SetActive((detectAmount > 0 && alertness == AgentAlertness.Calm) || alertness == AgentAlertness.Cautious || alertness == AgentAlertness.Alert);
            indicator.SetIntensity(detectAmount);
            indicator.SetAlertness(alertness);
            if (detectAmount >= 1 || alertness == AgentAlertness.Alert)
            {
                indicator.SetIcon(null);
            }
            else if (alertness != AgentAlertness.Panic && brain.HasStimulus() && brain.lastStimulusSense == hearing)
            {
                indicator.SetIcon(IndicatorSpriteHearing);
            }
            else if (detectAmount > 0 && Time.time - vision.GetLastDetectTime() <= vision.DetectionLingerDelay)
            {
                indicator.SetIcon(IndicatorSpriteVision);
            }
            else
            {
                indicator.SetIcon(null);
            }
        }

        if (DetectionLight != null)
        {
            if (alertness == AgentAlertness.Panic)
            {
                DetectionLight.intensity = 0f;
            }
            else
            {
                DetectionLight.intensity = Mathf.Lerp(LightIntensityMinimum, LightIntensityMaximum, Mathf.Pow(detectAmount, 2f));
            }

            if (detectAmount >= 1)
            {
                DetectionLight.color = LightColorAlert;
            }
            else
            {
                DetectionLight.color = Color.Lerp(LightColorCalm, LightColorAwareness, detectAmount / vision.AwarenessThreshold);
            }
        }
    }
}
