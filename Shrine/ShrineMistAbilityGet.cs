using UnityEngine;

public class ShrineMistAbilityGet : MonoBehaviour
{
    public string[] abilitiesToUnlock = { "Create Mist", "Remove Mist" };

    public void Activate()
    {
        PlayerAbilityManager abilityManager = Player.instance.GetComponent<PlayerAbilityManager>();
        foreach (string ability in abilitiesToUnlock)
        {
            if (!abilityManager.HasAbility(ability))
            {
                abilityManager.AddAbilityFromPreset(ability);
                new Analytics.ActivatedInteractableEvent()
                {
                    InteractableName = "Mist Ability Pickup",
                    InteractableType = Analytics.ActivatedInteractableEvent.IType.AbilityPickup
                }.Record();
            }
        }
    }
}
