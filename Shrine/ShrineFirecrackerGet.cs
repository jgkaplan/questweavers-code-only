using UnityEngine;

public class ShrineFirecrackerGet : MonoBehaviour
{
    public void Activate()
    {
        FirecrackerAbility firecrackerAbility = Player.instance.GetComponent<FirecrackerAbility>();
        if (firecrackerAbility.CanPickUp)
        {
            firecrackerAbility.PickUp();
            SaveSystem.UnlockFirecrackers();
            new Analytics.ActivatedInteractableEvent()
            {
                InteractableName = "Firecracker Pickup",
                InteractableType = Analytics.ActivatedInteractableEvent.IType.AbilityPickup
            }.Record();
        }
    }
}
