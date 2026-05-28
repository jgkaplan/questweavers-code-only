using UnityEngine;

public class NoiseMakerInteractable : Interactable
{
    public override bool CanPlayerInteract => true;
    public override bool CanEnemyInteract => false;
    public override InteractionType InteractType => InteractionType.Oneshot;
    protected override float cooldown => 3f;

    [SerializeField] float soundRange = 25f;
    [SerializeField] FMODUnity.StudioEventEmitter fmodEventEmitter;

    protected override void Interact(Player player)
    {
        if (fmodEventEmitter != null)
        {
            fmodEventEmitter.Play();
        }
        AudioHint.Create(transform.position, soundRange, 1.5f, AudioHintFlags.Suspicious | AudioHintFlags.Distraction, gameObject);
        player.animator.SetTrigger("Interact");

    }
}
