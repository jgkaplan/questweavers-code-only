using UnityEngine;

public class CollectablePickup : Interactable
{
    public override bool CanPlayerInteract => true;

    public override bool CanEnemyInteract => false;

    public override InteractionType InteractType => InteractionType.Oneshot;

    public Sprite adviceSprite;
    public string adviceText;
    public string collectableUnlock = "";
    private bool HasBeenActivated = false;

    public void CheckActivation()
    {
        if (SaveSystem.HasCollectable(collectableUnlock))
        {
            HasBeenActivated = true;
            gameObject.SetActive(false);
        }
    }

    protected override void Interact(Player player)
    {
        CheckActivation();
        if (HasBeenActivated) return;
        HasBeenActivated = true;
        new Analytics.ActivatedInteractableEvent()
        {
            InteractableName = name,
            InteractableType = Analytics.ActivatedInteractableEvent.IType.CollectablePickup
        }.Record();

        player.CurrentInteractable = null;
        // TODO: animate player getting ability
        // player.animator.SetTrigger("Praying");

        if (adviceSprite != null)
        {
            AdvicePanel.instance.DisplayAdvice(adviceSprite, adviceText, 0.5f);// TODO: this is a hacky solution. do something different
        }
        if (collectableUnlock != "")
        {
            SaveSystem.UnlockCollectable(collectableUnlock);
        }
        gameObject.SetActive(false);
    }
}
