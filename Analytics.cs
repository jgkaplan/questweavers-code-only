using System;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.UnityConsent;

public class Analytics : MonoBehaviour
{

    public static readonly float PlayerPositionLogTime = 0.5f; // How often the player position should be recorded

    public static bool useAnalytics = false;
    [SerializeField] private TextMeshProUGUI guidText;

    [SerializeField] private TextMeshProUGUI dateTimeText;
    [SerializeField] private TextMeshProUGUI versionText;

    public async void Awake()
    {
#if UNITY_EDITOR
        useAnalytics = false;
#else
        useAnalytics = true;
#endif
        versionText.text = $"Version {Application.version}";
        await Initialize();
    }

    void Update()
    {
        if (dateTimeText.isActiveAndEnabled)
        {
            dateTimeText.text = $"{DateTime.UtcNow}, UTC";
        }
    }

    public static Guid currentUserGUID;

    public async Awaitable Initialize()
    {
        if (!useAnalytics) return;
        var options = new InitializationOptions();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        options.SetEnvironmentName("development");
#elif PLAYTEST_BUILD
            options.SetEnvironmentName("testing");
#else
            options.SetEnvironmentName("production"); // Default to "production" for release builds
#endif

        try
        {

            await UnityServices.InitializeAsync(options);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Starts a new game session, changing the user id so that it shows up differently in the analytics logs
    /// </summary>
    public void StartNewSession()
    {
        if (!useAnalytics) return;
        // #if PLAYTEST_BUILD
        //             currentUserGUID = Guid.NewGuid();
        //             UnityServices.ExternalUserId = currentUserGUID.ToString();
        // #else
        //         currentUserGUID = Guid.Empty;
        //         UnityServices.ExternalUserId = String.Empty; // set this to use installation ID instead of custom
        // #endif
        currentUserGUID = Guid.NewGuid();
#if PLAYTEST_BUILD
        UnityServices.ExternalUserId = currentUserGUID.ToString();
#endif
        guidText.text = $"GUID: {currentUserGUID}";
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = ConsentStatus.Granted,
            AdsIntent = ConsentStatus.Denied
        });
    }

    public void EndSession()
    {
        if (!useAnalytics) return;
        AnalyticsService.Instance.Flush();
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = ConsentStatus.Denied,
            AdsIntent = ConsentStatus.Denied
        });
        currentUserGUID = Guid.Empty;
        guidText.text = $"GUID: None";
    }

    // TODO: invite teammates
    // TODO: copy events to other environments
    // TODO: figure out what we want to track for player ability usage (aiming? continuous casting?)

    public void RecordEvent(Unity.Services.Analytics.Event e)
    {
        if (!useAnalytics) return;
        AnalyticsService.Instance.RecordEvent(e);
    }

    public class QuestweaversAnalyticsEvent : Unity.Services.Analytics.Event
    {
        public QuestweaversAnalyticsEvent(string name) : base(name)
        {
            CurrentPlayerGUID = currentUserGUID.ToString();
        }

        /// <summary>
        /// Records this event, sending it to Unity Analytics
        /// </summary>
        public void Record()
        {
            if (!useAnalytics) return;
            AnalyticsService.Instance.RecordEvent(this);
        }

        public string CurrentPlayerGUID { set { SetParameter("currentUserGUID", value); } }
    }


    public class PlayerDiedEvent : QuestweaversAnalyticsEvent
    {
        public PlayerDiedEvent() : base("playerDiedEvent") { }

        // public DateTime TimeOfDeath { set { SetParameter("timeOfDeath", value); } }
        public float PlayerXPosition { set { SetParameter("playerXPosition", value); } }
        public float PlayerYPosition { set { SetParameter("playerYPosition", value); } }
        public float PlayerZPosition { set { SetParameter("playerZPosition", value); } }
        public string CauseOfDeath { set { SetParameter("causeOfDeath", value); } }
    }

    public class LevelStartedEvent : QuestweaversAnalyticsEvent
    {
        public LevelStartedEvent() : base("levelStartedEvent") { }
    }

    public class LevelEndedEvent : QuestweaversAnalyticsEvent
    {
        public LevelEndedEvent() : base("levelEndedEvent") { }
    }

    public class GamePausedEvent : QuestweaversAnalyticsEvent
    {
        public GamePausedEvent() : base("gamePaused") { }
    }

    public class GameUnpausedEvent : QuestweaversAnalyticsEvent
    {
        public GameUnpausedEvent() : base("gameUnpaused") { }
    }

    public class EnteredShrineZoneEvent : QuestweaversAnalyticsEvent
    {
        public EnteredShrineZoneEvent() : base("enteredShrineZoneEvent") { }
        public string ShrineName { set { SetParameter("ShrineName", value); } }
    }

    public class ActivatedInteractableEvent : QuestweaversAnalyticsEvent
    {
        public enum IType
        {
            Shrine,
            AbilityPickup,
            CollectablePickup
        }

        public string ITypeToString(IType t)
        {
            return t switch
            {
                IType.Shrine => "Shrine",
                IType.AbilityPickup => "AbilityPickup",
                IType.CollectablePickup => "CollectablePickup",
                _ => "error: undefined type"
            };
        }
        public ActivatedInteractableEvent() : base("activatedInteractableEvent") { }
        public string InteractableName { set { SetParameter("interactableName", value); } }
        public IType InteractableType { set { SetParameter("interactableType", ITypeToString(value)); } }
    }

    public class AbilityUsedEvent : QuestweaversAnalyticsEvent
    {
        public AbilityUsedEvent() : base("abilityUsedEvent") { }

        public string AbilityName { set { SetParameter("abilityName", value); } }
        public float PlayerXPosition { set { SetParameter("playerXPosition", value); } }
        public float PlayerYPosition { set { SetParameter("playerYPosition", value); } }
        public float PlayerZPosition { set { SetParameter("playerZPosition", value); } }
        public float AbilityXPosition { set { SetParameter("abilityXPosition", value); } }
        public float AbilityYPosition { set { SetParameter("abilityYPosition", value); } }
        public float AbilityZPosition { set { SetParameter("abilityZPosition", value); } }
        public float StartingResourceLevel { set { SetParameter("startingResourceLevel", value); } }
        public float FinalResourceLevel { set { SetParameter("finalResourceLevel", value); } }
    }

    // To be logged twice a second
    public class PlayerPositionEvent : QuestweaversAnalyticsEvent
    {
        public PlayerPositionEvent() : base("playerPositionEvent") { }
        public float PlayerXPosition { set { SetParameter("playerXPosition", value); } }
        public float PlayerYPosition { set { SetParameter("playerYPosition", value); } }
        public float PlayerZPosition { set { SetParameter("playerZPosition", value); } }
    }

    public class EnemyDetectionStateChangedEvent : QuestweaversAnalyticsEvent
    {
        public EnemyDetectionStateChangedEvent() : base("enemyDetectionStateChangedEvent") { }

        string AgentAlertnessToString(AgentAlertness alertness)
        {
            return alertness switch
            {
                AgentAlertness.Calm => "Calm",
                AgentAlertness.Cautious => "Cautious",
                AgentAlertness.Alert => "Alert",
                _ => "error: invalid",
            };
        }

        public string EnemyName { set { SetParameter("enemyName", value); } }
        // public string AggroSource { set { SetParameter("aggroSource", value); } } // would be nice, but seems hard to determine
        public float EnemyXPosition { set { SetParameter("enemyXPosition", value); } }
        public float EnemyYPosition { set { SetParameter("enemyYPosition", value); } }
        public float EnemyZPosition { set { SetParameter("enemyZPosition", value); } }
        public AgentAlertness PreviousDetectionState { set { SetParameter("previousDetectionState", AgentAlertnessToString(value)); } }
        public AgentAlertness NewDetectionState { set { SetParameter("newDetectionState", AgentAlertnessToString(value)); } }
    }

    public class EnemySensedSomethingEvent : QuestweaversAnalyticsEvent
    {
        public EnemySensedSomethingEvent() : base("enemySensedSomethingEvent") { }
        public string EnemyName { set { SetParameter("enemyName", value); } }
        public string SenseUsed { set { SetParameter("senseUsed", value); } }
        public float EnemyXPosition { set { SetParameter("enemyXPosition", value); } }
        public float EnemyYPosition { set { SetParameter("enemyYPosition", value); } }
        public float EnemyZPosition { set { SetParameter("enemyZPosition", value); } }
    }

}
