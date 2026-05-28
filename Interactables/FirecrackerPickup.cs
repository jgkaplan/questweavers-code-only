using UnityEngine;

public class FirecrackerPickup : Interactable
{
    public override bool CanPlayerInteract => true;

    public override bool CanEnemyInteract => false;

    public override InteractionType InteractType => InteractionType.Oneshot;

    public Checkpoint checkpoint;

    public Sprite adviceSprite;
    public string adviceText;
    public string collectableName = "";

    protected override void Interact(Player player)
    {
        FirecrackerAbility firecrackerAbility = player.GetComponent<FirecrackerAbility>();
        if (firecrackerAbility.CanPickUp)
        {
            firecrackerAbility.PickUp();
            checkpoint.TriggerCheckpoint(); // TODO: remove this if we change to individual pickups
            SaveSystem.UnlockFirecrackers();
            new Analytics.ActivatedInteractableEvent()
            {
                InteractableName = name,
                InteractableType = Analytics.ActivatedInteractableEvent.IType.AbilityPickup
            }.Record();

            player.CurrentInteractable = null;
            // TODO: animate player picking up firecracker
            player.animator.SetTrigger("Praying");
            // TODO: add sound when picking up

            // once a firecracker is picked up, it's gone
            gameObject.SetActive(false);
            // Destroy(gameObject);

            if (adviceSprite != null)
            {
                AdvicePanel.instance.DisplayAdvice(adviceSprite, adviceText);
            }
            if (collectableName != null)
            {
                SaveSystem.UnlockCollectable(collectableName);
            }
        }
        // TODO: add sound when can't pick up

    }
}
