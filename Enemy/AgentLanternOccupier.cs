using TMPro;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AgentBrain))]
public class AgentLanternOccupier : MonoBehaviour
{
    private static readonly int OccupiedHash = Animator.StringToHash("Occupied");
    [SerializeField] public string lanternTag = "";
    [SerializeField] bool startOccupied = false;
    [SerializeField] bool startHibernated = true;

    [SerializeField][HideInInspector] WispLantern startingLantern;

    [SerializeField] private FMODUnity.StudioEventEmitter lanternRattleSound;


    /// <summary>
    /// Enemy will only consider lanterns within this range of the player for ambushing.
    /// </summary>
    public float AmbushRange = 50f;

    public float AmbushMinimumRange = 7f;

    AgentBrain brain;
    public WispLantern currentLantern { get; private set; }
    public WispLantern lastLantern { get; private set; }
    public WispLantern reservedLantern { get; private set; }

    private int occupierLayerMask;
    CharacterController cc;

    void Start()
    {
        brain = GetComponent<AgentBrain>();
        cc = GetComponentInParent<CharacterController>();
        occupierLayerMask = LayerMask.GetMask("Default", "Terrain");
    }
    void FixedUpdate()
    {
        if (!brain.ShouldThink())
        {
            return;
        }

        if (startOccupied && startingLantern != null)
        {
            EnterLantern(startingLantern);
            startingLantern = null;
        }
        if (startHibernated)
        {
            startHibernated = false;
            brain.WorldStateMemory["hibernate"] = 1;
        }

        // STAY IN YOUR DAMN PLACE
        if (HasLantern())
        {
            cc.transform.position = currentLantern.transform.position;
        }
        else
        {
            brain.WorldStateMemory["near_lantern"] = (reservedLantern != null && (reservedLantern.transform.position - cc.transform.position).magnitude < 0.1f) ? 1 : 0;
        }

        // Hackity hack hack
        var playerPosition = Player.instance.transform.position;
        var dir = transform.position - playerPosition;
        brain.WorldStateMemory["target_los"] = (dir.magnitude > AmbushRange || Physics.Raycast(playerPosition, dir, dir.magnitude, occupierLayerMask, QueryTriggerInteraction.Ignore)) ? 0 : 1;
    }

    public bool HasLantern()
    {
        return currentLantern != null;
    }
    public void EnterLantern(WispLantern lantern)
    {
        if (currentLantern != null)
        {
            return;
        }
        if (lantern.currentOccupier != null)
        {
            Debug.LogWarning("Enemy tried to enter already occupied lantern");
            return;
        }

        if (reservedLantern != null)
        {
            reservedLantern.Unreserve(this);
            reservedLantern = null;
        }

        brain.WorldStateMemory["has_lantern"] = 1;
        currentLantern = lantern;

        // force move
        cc.enabled = false;
        cc.transform.position = currentLantern.transform.position;

        brain.Animator.SetBool("Occupied", true);
        lantern.Enter(this);

        // Debug.Log(cc.gameObject.name + " entered " + lantern.transform.name);
    }

    public void ExitLantern()
    {
        if (currentLantern == null)
        {
            return;
        }
        if (currentLantern.currentOccupier != this)
        {
            // Debug.LogWarning("Tried to exit lantern that isn't ours");
            return;
        }
#if UNITY_EDITOR
        Debug.Log(cc.gameObject.name + " exited " + currentLantern.transform.name);
#endif
        // force move
        cc.enabled = true;

        lastLantern = currentLantern;
        brain.WorldStateMemory["has_lantern"] = 0;
        brain.Animator.SetBool(OccupiedHash, false);
        currentLantern.Exit();
        currentLantern = null;
        lanternRattleSound.Stop();

        if (reservedLantern != null)
        {
            reservedLantern.Unreserve(this);
            reservedLantern = null;
        }

    }

    public void ReserveLantern(WispLantern lantern)
    {
        if (currentLantern != null)
        {
            Debug.LogWarning("Tried to reserve lantern while occupied");
            return;
        }
        if (reservedLantern != null)
        {
            reservedLantern.Unreserve(this);
        }
        reservedLantern = lantern;
        lantern.Reserve(this);
    }

    public void UnreserveLantern()
    {
        if (reservedLantern == null)
        {
            return;
        }
        if (reservedLantern.currentReserver != this && reservedLantern.currentReserver != null)
        {
            // Debug.LogWarning("Tried to unreserve a lantern not reserved by us");
            return;
        }
        reservedLantern = null;
    }


    /// <summary>
    /// Find the nearest unoccupied and unreserved lantern from the current position. Lanterns that are not recently occupied are considered first, followed by any lantern if set.
    /// </summary>
    /// <param name="recencyThreshold">Duration of time to consider "recently occupied".</param>
    /// <param name="enforceRecency">If true, "recently occupied" lanterns will be ignored in the search entirely; otherwise they are de-prioritized.</param>
    /// <param name="ignoreReservation">If true, reserved lanterns are included in the search.</param>
    /// <returns>An unoccupied lantern, if found.</returns>
    public WispLantern FindNearestEmptyLantern(float recencyThreshold = 15f, bool enforceRecency = false, bool ignoreReservation = false)
    {
        var position = transform.position;
        WispLantern nearestLantern = null, nearestLanternIgnoreRecency = null;
        float nearestDist = 0, nearestDistIgnoreRecency = 0;

        // foreach (var lantern in GameManager.Instance.GetWispLanterns())
        foreach (var lantern in GameManager.Instance.GetWispLanternsByTag(lanternTag))
        {
            if (lantern.IsOccupied() || (enforceRecency && lantern == lastLantern) || (!ignoreReservation && lantern.IsReserved()) || (lanternTag != "" && lantern.LanternTag != lanternTag))
            {
                continue;
            }

            var dist = Vector3.Distance(lantern.transform.position, position);

            if (nearestLanternIgnoreRecency == null || nearestDistIgnoreRecency > dist)
            {
                nearestLanternIgnoreRecency = lantern;
                nearestDistIgnoreRecency = dist;
            }
            if (Time.time - lantern.lastOccupiedTime >= recencyThreshold)
            {
                if (nearestLantern == null || nearestDist > dist)
                {
                    nearestLantern = lantern;
                    nearestDist = dist;
                }
            }
        }

        if (nearestLantern != null)
        {
            return nearestLantern;
        }
        return nearestLanternIgnoreRecency;
    }

    /// <summary>
    /// Finds an empty lantern sutiable for ambush. The lantern should be hidden from both the player and the current agent position. 
    /// </summary>
    /// <param name="position">Position to avoid visibility to.</param>
    /// <returns></returns>
    public WispLantern FindEmptyLanternForAmbush(Vector3 position)
    {
        WispLantern bestLantern = null;
        float bestScore = 0;

        // foreach (var lantern in GameManager.Instance.GetWispLanterns())
        foreach (var lantern in GameManager.Instance.GetWispLanternsByTag(lanternTag))
        {
            if (lantern.IsOccupied() || lantern.IsReserved() || lantern == lastLantern || (lanternTag != "" && lantern.LanternTag != lanternTag))
            {
                continue;
            }

            float score = 0;

            var dir = lantern.transform.position - position;
            var dir2 = lantern.transform.position - brain.transform.position;

            // Only consider lanterns closeish to the player
            if (dir.magnitude > AmbushRange || dir.magnitude <= AmbushMinimumRange)
            {
                continue;
            }

            // Target lantern should break line of sight to target position
            if (Physics.Raycast(position, dir, dir.magnitude, LayerMask.GetMask("Default", "Terrain"), QueryTriggerInteraction.Ignore))
            {
                score += 100;
            }

            // Target lantern should break line of sight to wisp position (so it looks like it's hiding)
            if (Physics.Raycast(brain.transform.position, dir2, dir2.magnitude, LayerMask.GetMask("Default", "Terrain"), QueryTriggerInteraction.Ignore))
            {
                score += 50;
            }

            // Target lantern should be as far as reasonable from current position
            score += Mathf.Min(dir2.magnitude, 50f);

            // Target lantern gains bonus for being on the flank (both in front and behind are undesirable)
            var dir3 = brain.transform.position - position;
            var dot = Mathf.Abs(Vector3.Dot(dir3.normalized, dir2.normalized));
            score += dot * 50f;

            // Target lantern should be ideally not recently used
            if (lantern.lastOccupiedTime < 0 || (Time.time - lantern.lastOccupiedTime) > 10f)
            {
                score += 25;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestLantern = lantern;
            }
        }

        return bestLantern;
    }

    private void OnValidate()
    {
        if (startOccupied)
        {
            startingLantern = null;
            foreach (var col in Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Character"), QueryTriggerInteraction.Ignore))
            {
                var lantern = col.GetComponentInParent<WispLantern>();
                if (lantern != null)
                {
                    startingLantern = lantern;
                    var parent = GetComponentInParent<NavMeshAgent>();
                    if (parent != null)
                    {
                        parent.transform.position = lantern.transform.position;
                    }
                    lanternTag = lantern.LanternTag;
                    break;
                }
            }
        }
    }
}
