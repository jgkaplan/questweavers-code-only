using UnityEngine;
using UnityEngine.Events;

public class ShrineTrigger : MonoBehaviour
{
    public UnityEvent EnteredShrine;
    public UnityEvent FirstTimeInShrine;
    public UnityEvent DisableStuffIfShrineActive;
    public Checkpoint checkpoint;
    private Animator animator;

    private bool hasEnteredShrineBefore = false;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void CheckActivation()
    {
        if (SaveSystem.HasUnlockedCheckpoint(checkpoint))
        {
            animator.SetBool("HasBeenActivated", true);
            animator.Play("Full Shrine First Activate");
            DisableStuffIfShrineActive.Invoke();
        }
        else
        {
            animator.SetBool("HasBeenActivated", false);
        }
    }
    // async void Start()
    // {
    //     while (SaveSystem.saveData == null)
    //     {
    //         await Awaitable.EndOfFrameAsync();
    //     }
    //     CheckActivation();
    // }

    public void ActivateVisuals()
    {
        animator.SetBool("HasBeenActivated", true);
        GetComponent<FMODUnity.StudioEventEmitter>().Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BackgroundMusicSystem.instance.SetPlayerInShrineParam(true);
            EnteredShrine.Invoke();
            new Analytics.EnteredShrineZoneEvent()
            {
                ShrineName = gameObject.name
            }.Record();

            // checkpoint.TriggerCheckpoint();
            if (!hasEnteredShrineBefore)
            {
                hasEnteredShrineBefore = true;
                FirstTimeInShrine.Invoke();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BackgroundMusicSystem.instance.SetPlayerInShrineParam(false);
        }
    }
}
