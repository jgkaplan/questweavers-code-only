using UnityEngine;

public class ShrineTextUnlock : MonoBehaviour
{
    public string collectableUnlock = "";

    public void Activate()
    {
        if (collectableUnlock != "")
        {
            SaveSystem.UnlockCollectable(collectableUnlock);
        }
    }
}
