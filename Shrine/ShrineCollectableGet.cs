using UnityEngine;

public class ShrineCollectableGet : MonoBehaviour
{
    public Sprite adviceSprite;
    public string adviceText;
    public string collectableUnlock = "";

    public void Activate()
    {
        if (adviceSprite != null)
        {
            AdvicePanel.instance.DisplayAdvice(adviceSprite, adviceText);
        }
        if (collectableUnlock != "")
        {
            SaveSystem.UnlockCollectable(collectableUnlock);
        }
    }
}
