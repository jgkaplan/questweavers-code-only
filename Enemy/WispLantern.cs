#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class WispLantern : MonoBehaviour
{
    private static readonly int OccupiedHash = Animator.StringToHash("Occupied");
    private static readonly int TrembleHash = Animator.StringToHash("Tremble");
    public Light LanternLight;

    public string LanternTag;

    [SerializeField] Color lightColor;
    [SerializeField] Color lightColorOccupied;
    [SerializeField] Color lightColorDetection;
    [SerializeField] float lightStrength;
    [SerializeField] float flickerFrequency;
    [SerializeField] float flickerIntensity;

    public float lastOccupiedTime { get; private set; }
    public AgentLanternOccupier currentOccupier { get; private set; }
    public AgentLanternOccupier currentReserver { get; private set; }

    public Animator animator { get; private set; }

    float flickerAmount = 0f;
    float seed; // For noise

    private void Start()
    {
        animator = GetComponent<Animator>();
        seed = Random.Range(0, Mathf.Infinity);

        SetIntensity(0);
        SetFlicker(0);
    }

    public bool IsOccupied()
    {
        return currentOccupier != null;
    }

    public bool IsReserved()
    {
        return IsOccupied() || currentReserver != null;
    }

    private void Update()
    {
        if (flickerAmount > 0f)
        {
            Flicker();
        }
    }

    public void Enter(AgentLanternOccupier occupier)
    {
        if (currentOccupier != null)
        {
            return;
        }
#if UNITY_EDITOR
        if (currentReserver != null && occupier != currentReserver)
        {
            Debug.LogWarning("Reserver and occupier are not the same");
        }
#endif
        currentOccupier = occupier;
        lastOccupiedTime = Time.time;
        currentReserver = null;
        animator.SetFloat(TrembleHash, 0);
        animator.SetBool(OccupiedHash, true);
        SetIntensity(0);
        SetFlicker(0);
    }

    public void Exit()
    {
        if (currentOccupier == null)
        {
            return;
        }
        currentOccupier = null;
        lastOccupiedTime = Time.time;
        currentReserver = null;
        animator.SetFloat(TrembleHash, 0);
        animator.SetBool(OccupiedHash, false);
        SetIntensity(0);
        SetFlicker(0);
    }

    public void Reserve(AgentLanternOccupier occupier)
    {
        currentReserver = occupier;
    }

    public void Unreserve(AgentLanternOccupier occupier)
    {
        if (currentReserver == occupier)
        {
            currentReserver = null;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Unreserving lantern by incorrect reserver!");
#endif
        }
    }

    public void SetFlicker(float v)
    {
        flickerAmount = Mathf.Clamp01(v);
        Flicker();
    }

    public void SetIntensity(float v)
    {
        LanternLight.color = IsOccupied() ? Color.Lerp(lightColorOccupied, lightColorDetection, v) : lightColor;
    }

    void Flicker()
    {
        // LanternLight.intensity = lightStrength * Mathf.Lerp(1, flickerIntensity, flickerAmount * Mathf.Clamp01(Mathf.PerlinNoise(seed, Time.time * flickerFrequency)));
        LanternLight.intensity = lightStrength + flickerIntensity * Mathf.Sin(Time.time * flickerFrequency);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // cheaper than searching for lanternoccupier
        if (Selection.activeGameObject != null && (
            (Selection.activeGameObject.name.Contains("EnemyWisp") && Selection.activeGameObject.GetComponentInChildren<AgentLanternOccupier>()?.lanternTag == LanternTag)
            || (Selection.activeGameObject.name.Contains("StoneLantern") && Selection.activeGameObject.GetComponent<WispLantern>()?.LanternTag == LanternTag)))
        {
            Handles.Label(transform.position, LanternTag);
        }
    }
#endif
}
