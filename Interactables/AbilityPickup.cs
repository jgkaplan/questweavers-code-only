using UnityEngine;

public class AbilityPickup : Interactable
{
    public override bool CanPlayerInteract => true;

    public override bool CanEnemyInteract => false;

    public override InteractionType InteractType => InteractionType.Oneshot;

    public string abilityName;
    public Checkpoint checkpoint;

    public Sprite adviceSprite;
    public string adviceText;
    public string collectableName = "";

    protected override void Interact(Player player)
    {
        PlayerAbilityManager abilityManager = player.GetComponent<PlayerAbilityManager>();
        checkpoint.TriggerCheckpoint();
        abilityManager.AddAbilityFromPreset(abilityName);
        new Analytics.ActivatedInteractableEvent()
        {
            InteractableName = name,
            InteractableType = Analytics.ActivatedInteractableEvent.IType.AbilityPickup
        }.Record();
        player.CurrentInteractable = null;
        // TODO: animate player getting ability
        player.animator.SetTrigger("Praying");

        if (adviceSprite != null)
        {
            AdvicePanel.instance.DisplayAdvice(adviceSprite, adviceText);
        }
        if (collectableName != null)
        {
            SaveSystem.UnlockCollectable(collectableName);
        }

        // once an ability is picked up, it's gone
        gameObject.SetActive(false);
        // Destroy(gameObject);
    }
}
