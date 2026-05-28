using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class FinishLevel : MonoBehaviour
{
    private bool hasBeatenLevel = false;

    [SerializeField] private PlayableDirector finalCutscene;

    void Start()
    {
        var timelineAsset = finalCutscene.playableAsset as TimelineAsset;
        if (timelineAsset == null) return;
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            if (track.name == "Player Signal Track")
            {
                finalCutscene.SetGenericBinding(track, Player.instance.GetComponent<SignalReceiver>());
            }
            else if (track.name == "Player Camera Track")
            {
                finalCutscene.SetGenericBinding(track, Player.instance.playerCameraRig.transform.parent.GetComponentInChildren<CinemachineBrain>());
            }
            else if (track.name == "FMOD Listener Signal Track")
            {
                finalCutscene.SetGenericBinding(track, Camera.main.GetComponent<SignalReceiver>());
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasBeatenLevel && other.CompareTag("Player"))
        {
            hasBeatenLevel = true;
            new Analytics.LevelEndedEvent().Record();
            finalCutscene.Play();
        }
    }
}
