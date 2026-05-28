using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class ShrinePrayInteractable : Interactable
{
    public override bool CanPlayerInteract => true;

    public override bool CanEnemyInteract => false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public override InteractionType InteractType => InteractionType.Oneshot;

    public Checkpoint checkpoint;
    [Header("Cutscene")]
    // public PlayableDirector cutscenePlayer;
    public PlayableDirector firstTimeCutscene;
    public PlayableDirector normalCutscene;
    private bool isFirstTime = true;

    void CheckIfPreviousActivation()
    {
        if (SaveSystem.HasUnlockedCheckpoint(checkpoint))
        {
            isFirstTime = false;
        }
        else
        {
            isFirstTime = true;
        }
        Debug.Log("First time:" + isFirstTime.ToString());
    }

    void Start()
    {
        InitializeTimelineAsset(normalCutscene);
        InitializeTimelineAsset(firstTimeCutscene);
    }

    void InitializeTimelineAsset(PlayableDirector director)
    {
        if (director == null) return;
        var timelineAsset = director.playableAsset as TimelineAsset;
        if (timelineAsset == null) return;
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            if (track.name == "Text Cutscene Track")
            {
                // bind text to this
                director.SetGenericBinding(track, TextCutscenePlayer.instance);
            }
            else if (track.name == "Player Signal Track")
            {
                director.SetGenericBinding(track, Player.instance.GetComponent<SignalReceiver>());
            }
            else if (track.name == "Player Camera Track")
            {
                director.SetGenericBinding(track, Player.instance.playerCameraRig.transform.parent.GetComponentInChildren<CinemachineBrain>());
            }
            else if (track.name == "FMOD Listener Signal Track")
            {
                director.SetGenericBinding(track, Camera.main.GetComponent<SignalReceiver>());
            }
        }
    }
    protected override void Interact(Player player)
    {
        CheckIfPreviousActivation();
        // PlayerAbilityManager abilityManager = player.GetComponent<PlayerAbilityManager>();
        // checkpoint.TriggerCheckpoint();

        new Analytics.ActivatedInteractableEvent()
        {
            InteractableName = name,
            InteractableType = Analytics.ActivatedInteractableEvent.IType.Shrine
        }.Record();
        player.CurrentInteractable = null;
        if (firstTimeCutscene != null && isFirstTime)
        {
            Debug.Log("Playing first time cutscene");
            isFirstTime = false;
            firstTimeCutscene.Play();
        }
        else
        {
            Debug.Log("Playing normal cutscene");
            normalCutscene.Play();
        }
        // player.StartPraying();
        // GetComponent<FMODUnity.StudioEventEmitter>().Play();
        // shrineActivated.Invoke();
    }
}
