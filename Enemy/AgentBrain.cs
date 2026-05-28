using UnityEngine;
using GOAP;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Linq;
using System.Collections;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public enum AgentAlertness
{
    Calm = 0, // Agent has no target or stimulus
    Cautious = 1, // Target not detected (but may exist), agent has stimulus
    Alert = 2, // Target is detected
    Panic = 3, // Agent cannot detect or perform most actions
}
public enum AgentState
{
    Goto, // Current action requires moving to a location via NavMeshAgent
    Animate // Current action does not require NavMeshAgent
}
public class AgentBrain : MonoBehaviour
{
    public static int STIMULI_PRIORITY_FULL_DETECTION = 1000;
    public static int STIMULI_PRIORITY_PARTIAL_DETECTION = 500;
    public static int STIMULI_PRIORITY_AUDIO_MINIMUM = 300;
    public static int STIMULI_PRIORITY_AUDIO_MAXIMUM = 399;
    public static int STIMULI_PRIORITY_MIST_ON_TOP = 200;

    /// <summary>
    /// The state the agent is in. This is either Goto (navmesh moving) or Animate (no navmesh action).
    /// </summary>
    [HideInInspector] public AgentState State;

    // Goal-Oriented Action Planning
    /// <summary>
    /// A list of goals for the agent. Each goal is a component in a child GameObject named "Goals".
    /// </summary>
    public List<Goal> GoalSet { private set; get; }

    /// <summary>
    /// A list of actions the agent can perform. Each action is a component in a child GameObject named "Actions".
    /// </summary>
    public List<Action> ActionSet { private set; get; }

    /// <summary>
    /// The agent's current goal.
    /// </summary>
    public Goal CurrentGoal { private set; get; }

    /// <summary>
    /// An ordered list of actions that tries to achieve the worldstate in CurrentGoal. The action at index 0 is the currently executing one.
    /// </summary>
    public List<Action> ActionPlan { private set; get; }

    List<AgentSense> senses;


    // Awareness & Memory
    [Header("Awareness")]

    [SerializeField] GameObject CautiousIndicatorPrefab;
    [SerializeField] GameObject AlertIndicatorPrefab;

    /// <summary>
    /// Time required for the agent to forget about a last known position if they are cautious.
    /// After interest is lost, they return to calm state.
    /// </summary>
    public float LoseInterestTimeCalm = 5f;

    /// <summary>
    /// Time required for the agent to forget about a last known position if they are cautious.
    /// After interest is lost, they return to calm state.
    /// </summary>
    public float LoseInterestTimeCautious = 10f;

    /// <summary>
    /// Time required for the agent to forget about a last known position if they are alerted.
    /// After interest is lost, they return to cautious state (?).
    /// </summary>
    public float LoseInterestTimeAlert = 15f;

    float loseInterestTime;

    /// <summary>
    /// How actively the agent is seeking the player.
    /// </summary>
    AgentAlertness alertness;
    public AgentSpeaker speaker { private set; get; }

    [Header("Sound Events")]
    [SerializeField] FMODUnity.EventReference loseDetectionSound;
    [SerializeField] FMODUnity.EventReference cautiousSound;

    [HideInInspector]
    public AgentAlertness Alertness
    {
        get { return alertness; }
        set
        {
            if (value != alertness)
            {
                if (alertnessIndicator != null)
                {
                    Destroy(alertnessIndicator.gameObject);
                }
                if (value == AgentAlertness.Cautious)
                {
                    // var obj = Instantiate(CautiousIndicatorPrefab, GameObject.FindGameObjectWithTag("IndicatorCanvas").transform);
                    // alertnessIndicator = obj.GetComponent<PositionIndicator>();
                    if (alertness == AgentAlertness.Calm && !cautiousSound.IsNull)
                    {
                        BackgroundMusicSystem.PlayOneShotSound(cautiousSound, transform.position);
                    }
                    else if (alertness == AgentAlertness.Alert && !loseDetectionSound.IsNull)
                    {
                        BackgroundMusicSystem.PlayOneShotSound(loseDetectionSound, transform.position);
                    }
                }
                else if (value == AgentAlertness.Alert)
                {
                    // var obj = Instantiate(AlertIndicatorPrefab, GameObject.FindGameObjectWithTag("IndicatorCanvas").transform);
                    // alertnessIndicator = obj.GetComponent<PositionIndicator>();

                    // speaker.SpeakLine("event:/VO_BasicGuard/BarkAlert", VoicelineType.Bark);
                }
                // if (alertnessIndicator != null)
                // {
                //     alertnessIndicator.target = transform;
                // }
                new Analytics.EnemyDetectionStateChangedEvent()
                {
                    EnemyName = transform.parent.name,
                    EnemyXPosition = transform.parent.position.x,
                    EnemyYPosition = transform.parent.position.y,
                    EnemyZPosition = transform.parent.position.z,
                    PreviousDetectionState = alertness,
                    NewDetectionState = value
                }.Record();

                alertness = value;
                BackgroundMusicSystem.instance.UpdateMusicIntensity();
            }
        }
    }

    /// <summary>
    /// The current world state as known to the agent.
    /// </summary>
    [HideInInspector] public Dictionary<string, int> WorldStateMemory;

    /// <summary>
    /// Last position where the agent last became aware of something of interest (player, or a distraction).
    /// </summary>
    [HideInInspector] public Vector3 LastStimulusPosition = Vector3.zero;

    /// <summary>
    /// Time when the agent last became aware of something of interest.
    /// </summary>
    float lastStimulusTime = -1f;

    /// <summary>
    /// Time when the agent last detected the target.
    /// </summary>
    float lastDetectTime = -1f;

    /// <summary>
    /// How important the last position of interest was. If other stimulus arrive with lower priority, they are ignored.
    /// </summary>
    int lastStimulusPriority = 0;

    /// <summary>
    /// The last AgentSense that triggered the stimulus.
    /// </summary>
    public AgentSense lastStimulusSense { private set; get; }


    // Uhh
    [Header("Parameters")]

    /// <summary>
    /// Distance within which the agent considers to be feasible for melee attacks.
    /// </summary>
    public float MeleeDistance = 1.5f;

    public float MoveSpeedCalm = 1.5f;

    public float MoveSpeedCautious = 2f;

    public float MoveSpeedAlert = 3f;

    public float MoveSpeedPanic = 3f;

    // Debug
    [Header("Debug")]

    /// <summary>
    /// If set, the agent is not allowed to think.
    /// </summary>
    public bool DisableThinking = false;
    public bool DisableSenses = false;
    public bool DebugOutput = false;
    [SerializeField] GameObject debugCanvas;

    PositionIndicator alertnessIndicator;

    // Other references
    public NavMeshAgent NavMeshAgent { private set; get; }

    public GameObject Target { private set; get; }
    public Animator Animator { private set; get; }

    public Vector3 HomePosition { private set; get; }

    private void Awake()
    {
        State = AgentState.Animate;
        Alertness = AgentAlertness.Calm;
        WorldStateMemory = new Dictionary<string, int>();
    }

    private void Start()
    {
        NavMeshAgent = GetComponentInParent<NavMeshAgent>();
        Animator = NavMeshAgent.GetComponent<Animator>();
        speaker = GetComponent<AgentSpeaker>();
        HomePosition = NavMeshAgent.transform.position;

        // Expects children in this order:
        // - Root Enemy Object
        //   - AgentBrain (this component)
        //     - Goals (contains all goal components)
        //     - Actions (contains all action components)
        var goalsGameObject = transform.Find("Goals");
        GoalSet = new List<Goal>();
        foreach (var goal in goalsGameObject.GetComponentsInChildren<Goal>())
        {
            if (!goal.enabled)
                continue;
            GoalSet.Add(goal);
        }
        GoalSet.Sort(); // Sort by priority
        var actionsGameObject = transform.Find("Actions");
        ActionSet = new List<Action>();
        foreach (var action in actionsGameObject.GetComponentsInChildren<Action>())
        {
            if (!action.enabled)
                continue;
            ActionSet.Add(action);
        }

        var sensesGameObject = transform.Find("Senses");
        senses = new List<AgentSense>();
        foreach (var sense in sensesGameObject.GetComponentsInChildren<AgentSense>())
        {
            if (!sense.enabled)
                continue;
            senses.Add(sense);
        }

        //Debug.Log(GoalSet.ToCommaSeparatedString());
        //Debug.Log(ActionSet.ToCommaSeparatedString());
        //Debug.Log(senses.ToCommaSeparatedString());

        // For now, they will only ever target the player
        //Target = GameObject.FindGameObjectWithTag("Player");
    }

    private void FixedUpdate()
    {
        if (!ShouldThink())
        {
            return;
        }

        EvaluateWorldState();
        SensesThink();

        if (CurrentGoal == null)
        {
            CreateNewGoalPlan();
        }
        if (ActionPlan != null && ActionPlan.Count > 0)
        {
            PlannerThink();
        }

        // handle alertness & stimulus update
        if (lastStimulusTime > 0f)
        {
            if (Time.time - lastStimulusTime > loseInterestTime)
            {
                ForgetStimulus();
            }
        }
        /*
        if (GetWorldState("target_detected") == 0 && GetWorldState("has_stimulus") == 0)
        {
            Alertness = AgentAlertness.Calm;
        }
        */
        if (Alertness == AgentAlertness.Calm)
        {
            NavMeshAgent.speed = MoveSpeedCalm;
            loseInterestTime = LoseInterestTimeCalm;
        }
        else if (Alertness == AgentAlertness.Cautious)
        {
            NavMeshAgent.speed = MoveSpeedCautious;
            loseInterestTime = LoseInterestTimeCautious;
        }
        else if (Alertness == AgentAlertness.Alert)
        {
            NavMeshAgent.speed = MoveSpeedAlert;
            loseInterestTime = LoseInterestTimeAlert;
        } else if (Alertness == AgentAlertness.Panic)
        {
            NavMeshAgent.speed = MoveSpeedPanic;
        }

        WorldStateMemory["alertness"] = (int)Alertness;
        WorldStateMemory["panic"] = Alertness == AgentAlertness.Panic ? 1 : 0;

        /*
        if (debugCanvas != null && debugCanvas.activeInHierarchy)
        {
            debugCanvas.transform.Find("Alertness").GetComponent<TextMeshProUGUI>().text =
                "Alertness: " + Alertness.ToString();
            debugCanvas.transform.Find("Goal").GetComponent<TextMeshProUGUI>().text =
                "Goal: " + (CurrentGoal == null ? "NONE" : CurrentGoal.GetType().ToString());
            debugCanvas.transform.Find("CurrentAction").GetComponent<TextMeshProUGUI>().text =
                "Plan: " + (ActionPlan == null ? "NONE" : ActionPlan.ToLineSeparatedString());
            debugCanvas.transform.Find("WorldState").GetComponent<TextMeshProUGUI>().text =
                WorldStateMemory.ToLineSeparatedString();
        }
        */
    }

    public bool ShouldThink()
    {
        return GameManager.Instance.IsInitialized && Time.timeScale > 0 && !DisableThinking;
    }

    public bool ShouldUseSenses()
    {
        return !DisableSenses && Alertness != AgentAlertness.Panic;
    }

    #region Awareness & Memory
    public int GetWorldState(string state)
    {
        if (!WorldStateMemory.ContainsKey(state))
        {
            return 0;
        }
        return WorldStateMemory[state];
    }

    void EvaluateWorldState()
    {
        WorldStateMemory["alertness"] = (int)Alertness;

        WorldStateMemory["target_exists"] = Target != null ? 1 : 0;

        if (Target != null)
        {
            var dist = Target.transform.position - NavMeshAgent.transform.position;
            WorldStateMemory["target_in_melee_range"] = dist.magnitude <= MeleeDistance ? 1 : 0;

            var player = Target.GetComponent<Player>();
            if (player != null && !player.IsAlive())
            {
                WorldStateMemory["target_alive"] = 0;
            }
            else
            {
                WorldStateMemory["target_alive"] = 1;
            }

            WorldStateMemory["target_exists"] = 1;
        }
        else
        {
            WorldStateMemory["target_alive"] = 0;
            WorldStateMemory["target_exists"] = 0;
        }
    }
    void SensesThink()
    {
        WorldStateMemory["target_detected"] = 0;
        if (ShouldUseSenses())
        {
            foreach (var sense in senses)
            {
                if (sense.IsSenseActive())
                {
                    sense.Think();
                    if (sense.IsSenseDetecting())
                    {
                        DetectTarget(sense, sense.Target);
                    }
                }
            }
        }

        if (GetWorldState("target_detected") == 0 && Time.time - lastDetectTime > loseInterestTime)
        {
            if (Alertness == AgentAlertness.Panic)
            {
                Alertness = AgentAlertness.Calm;
                WorldStateMemory["panic"] = 0;
                ForgetStimulus();
            }
            if (Alertness == AgentAlertness.Alert)
            {
                Alertness = AgentAlertness.Cautious;
                lastDetectTime = Time.time;
            }
            else if (Time.time - lastStimulusTime > loseInterestTime)
            {
                Target = null;
                Alertness = AgentAlertness.Calm;
                lastDetectTime = -1f;
                lastStimulusTime = -1f;
                WorldStateMemory["aggro"] = 0;
            }
        }
    }

    /// <summary>
    /// Called when the sense detects a target. The agent will know the target's exact position.
    /// </summary>
    /// <param name="sense">The sense used to detect the target.</param>
    /// <param name="target">Target to detect.</param>
    public void DetectTarget(AgentSense sense, GameObject target)
    {
        Target = target;
        if (Alertness != AgentAlertness.Alert)
        {
            // this is the first frame being fully alerted
            new Analytics.EnemySensedSomethingEvent()
            {
                EnemyName = transform.parent.name,
                EnemyXPosition = transform.parent.position.x,
                EnemyYPosition = transform.parent.position.y,
                EnemyZPosition = transform.parent.position.z,
                SenseUsed = sense.SenseName
            }.Record();
        }
        Alertness = AgentAlertness.Alert;
        lastDetectTime = Time.time;
        LastStimulusPosition = target.transform.position;
        lastStimulusTime = Time.time;
        lastStimulusSense = sense;
        lastStimulusPriority = STIMULI_PRIORITY_FULL_DETECTION;
        WorldStateMemory["target_detected"] = 1;
    }

    /// <summary>
    /// Prompt
    /// This can be a partial detection, or a distraction not originating from a player.
    /// Puts the agent into cautious state.
    /// </summary>
    /// <param name="sense">The sense used for awareness.</param>
    /// <param name="position">Position where the stimulus happened.</param>
    /// <param name="priority">Importance of the stimulus. Lower priority stimuli will not take precedence.</param>
    public void TriggerStimulus(AgentSense sense, Vector3 position, int priority = 0)
    {
        if (Alertness == AgentAlertness.Alert || Alertness == AgentAlertness.Panic)
        {
            return;
        }
        if (HasStimulus() && priority < lastStimulusPriority)
        {
            return;
        }
        if (Alertness != AgentAlertness.Cautious)
        {
            new Analytics.EnemySensedSomethingEvent()
            {
                EnemyName = transform.parent.name,
                EnemyXPosition = transform.parent.position.x,
                EnemyYPosition = transform.parent.position.y,
                EnemyZPosition = transform.parent.position.z,
                SenseUsed = sense != null ? sense.SenseName : "Unknown",
            }.Record();
        }
        Alertness = AgentAlertness.Cautious;
        LastStimulusPosition = position;
        lastStimulusTime = Time.time;
        lastStimulusSense = sense;
        WorldStateMemory["has_stimulus"] = 1;
    }

    public void ForgetStimulus()
    {
        WorldStateMemory["has_stimulus"] = 0;
        WorldStateMemory["stimulus_seen"] = 0;

        LastStimulusPosition = Vector3.zero;
        lastStimulusTime = -1;
        lastStimulusPriority = 0;
    }
    public bool HasStimulus()
    {
        return lastStimulusTime >= 0;
    }

    public void TriggerPanic(float duration, Vector3 position)
    {
        WorldStateMemory["target_detected"] = 0;
        WorldStateMemory["hibernate"] = 0;
        WorldStateMemory["bored"] = 0;

        Alertness = AgentAlertness.Panic;
        foreach (var sense in senses)
        {
            sense.OnPanic();
        }

        loseInterestTime = duration;
        LastStimulusPosition = position;
        lastDetectTime = Time.time;
        lastStimulusTime = Time.time;
        lastStimulusPriority = STIMULI_PRIORITY_PARTIAL_DETECTION;
    }
    #endregion

    #region Planner
    bool CreateNewGoalPlan()
    {
        //Debug.Log("CreateNewGoalPlan()");
        //Debug.Log("Current world state: " + WorldStateMemory.ToCommaSeparatedString());

        for (int i = GoalSet.Count - 1; i >= 0; i--)
        {
            var goal = GoalSet[i];
            if (goal.IsValid())
            {
                // Don't plan for this if we already fill the requirements
                if (goal.WorldStateDesired.All(x => x.Value == 0 && !WorldStateMemory.ContainsKey(x.Key) || (WorldStateMemory.ContainsKey(x.Key) && WorldStateMemory[x.Key] == x.Value)))
                    continue;

                var plan = FindActionPlanForGoal(goal);

                if (plan != null)
                {
                    CurrentGoal = goal;
                    ActionPlan = plan;

                    if (DebugOutput)
                    {
                        Debug.Log(CurrentGoal.ToString() + " planned: " + ActionPlan.ToCommaSeparatedString());
                    }

                    // Immediately start executing the first action
                    ActionPlan[0].PreExecute();

                    return true;
                }
            }
        }
        return false;
    }
    List<Action> FindActionPlanForGoal(Goal goal)
    {
        // Planner:
        // 1. find valid goal
        // 2. find action that satisfies the goal
        // 3. find action that satisfies previous action
        // 4. repeat until world state is matched
        // 5. on fail, continue down goal list

        //Debug.Log("Creating action plan for goal " + goal.ToString());

        var openActionSet = new List<Action>(ActionSet);

        var stack = new Stack<Action>();
        var actionCost = new Dictionary<Action, float>();
        var desiredWorldState = goal.WorldStateDesired.Dictionary;

        var currentCost = 0f;

        // Check all actions until the desired world state is already satisified by our current state
        while (openActionSet.Count > 0
                    && !desiredWorldState.All(x =>
            (x.Value == 0 && !WorldStateMemory.ContainsKey(x.Key))
            || (WorldStateMemory.ContainsKey(x.Key) && WorldStateMemory[x.Key] == x.Value)))
        {
            // Debug.Log("Current stack: " + stack.ToCommaSeparatedString());
            //Debug.Log("Current desired state: " + desiredWorldState.ToCommaSeparatedString());

            Action bestAction = null;
            foreach (var action in openActionSet)
            {
                if (action.CheckWorldStateOutcome(desiredWorldState))
                {
                    actionCost[action] = action.GetCost();
                    if (bestAction == null || actionCost[action] < actionCost[bestAction])
                    {
                        bestAction = action;
                    }
                    //Debug.Log(action.ToString() + ": cost " + actionCost[action]);
                }
                else
                {
                    //Debug.Log(action.ToString() + ": world state mismatch");
                }
            }

            if (bestAction == null)
            {
                // Can't find any action. boohoo
                if (stack.Count == 0)
                {
                    //Debug.Log(goal.ToString() + " planning failed: no action to satisfy goal");
                    break;
                }

                // Step back to last state
                var lastAction = stack.Pop();
                currentCost -= actionCost[lastAction];


                if (stack.Count == 0)
                {
                    desiredWorldState = goal.WorldStateDesired.Dictionary;
                    //Debug.Log(lastAction.ToString() + " dropped from stack (goal world state)");
                }
                else
                {
                    desiredWorldState = stack.Peek().GetWorldStatePreconditions();
                    //Debug.Log(lastAction.ToString() + " dropped from stack (last action world state)");
                }
            }
            else
            {
                // Advance into this state and try to fill its preconditions
                currentCost += actionCost[bestAction];
                stack.Push(bestAction);
                openActionSet.Remove(bestAction);
                desiredWorldState = bestAction.GetWorldStatePreconditions();

                //Debug.Log(bestAction.ToString() + " added to stack");
            }
        }

        if (stack.Count > 0 && desiredWorldState.All(x =>
            x.Value == 0 && !WorldStateMemory.ContainsKey(x.Key)
            || (WorldStateMemory.ContainsKey(x.Key) && WorldStateMemory[x.Key] == x.Value)))
        {
            //Debug.Log(goal.ToString() + " planned: " + stack.ToCommaSeparatedString());
            return stack.ToList();
        }

        //Debug.Log(goal.ToString() + " planning failed: no valid plan");
        return null;
    }
    void InvalidateCurrentPlan()
    {
        if (ActionPlan != null && ActionPlan.Count > 0)
        {
            ActionPlan[0].OnInvalidated();
        }

        CurrentGoal = null;
        ActionPlan = null;
    }
    void PlannerThink()
    {
        // Check if current action remains valid
        if (!ActionPlan[0].NeverInvalidate)
        {
            if (!ActionPlan[0].IsValid())
            {
                if (DebugOutput)
                {
                    Debug.Log(CurrentGoal.ToString() + ": plan aborted, action " + ActionPlan[0] + " invalid");
                }
                InvalidateCurrentPlan();
                return;
            }
            else if (!CurrentGoal.IsValid())
            {
                if (DebugOutput)
                {
                    Debug.Log(CurrentGoal.ToString() + ": plan aborted, goal " + CurrentGoal + " invalid");
                }
                InvalidateCurrentPlan();
                return;
            }
        }

        // Run our current action. If it returns true that means it needs
        var done = ActionPlan[0].ExecuteAction();

        if (done)
        {
            ActionPlan[0].PostExecute();
            ActionPlan.RemoveAt(0);

            if (ActionPlan.Count == 0)
            {
                // We're all done!
                if (DebugOutput)
                {
                    Debug.Log(CurrentGoal.ToString() + " plan complete");
                }
                InvalidateCurrentPlan();
            }
            else
            {
                if (DebugOutput)
                {
                    Debug.Log(CurrentGoal.ToString() + ": executing next action " + ActionPlan[0]);
                }
                ActionPlan[0].PreExecute();

                // Validate next action has the expected states
                /*
                if (ActionPlan[0].GetWorldStatePreconditions().All(x => GetWorldState(x.Key) == x.Value))
                {
                    // Next action
                    Debug.Log(CurrentGoal.ToString() + ": executing next action " + ActionPlan[0]);
                    ActionPlan[0].PreExecute();
                }
                else
                {
                    Debug.Log(CurrentGoal.ToString() + ": plan aborted, " + ActionPlan[0] + "prerequisites not met");
                    InvalidateCurrentPlan();
                }
                */
            }
        }
    }
    #endregion
}
