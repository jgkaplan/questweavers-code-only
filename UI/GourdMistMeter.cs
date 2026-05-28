using UnityEngine;

public class GourdMistMeter : MonoBehaviour
{
    [SerializeField] private PlayerAbilityManager playerAbilityManager;
    [SerializeField] private Renderer[] gems;

    void Start()
    {
        PlayerAbilityManager.MistResourceChanged.AddListener(UpdateResourceAmount);
        PlayerAbilityManager.CurrentAbilityChanged.AddListener(ChangeCastMode);
    }

    // void OnDisable()
    // {
    //     PlayerAbilityManager.MistResourceChanged.RemoveListener(UpdateResourceAmount);
    //     PlayerAbilityManager.CurrentAbilityChanged.RemoveListener(ChangeCastMode);
    // }

    void UpdateResourceAmount(float newAmount)
    {
        bool setZero = false;
        for (int i = 0; i < gems.Length; i++)
        {
            if (setZero)
            {
                gems[i].material.SetFloat("_Amount", 0);
                continue;
            }
            else if (newAmount >= (i + 1.0f) / gems.Length)
            {
                gems[i].material.SetFloat("_Amount", 1);
            }
            else
            {
                gems[i].material.SetFloat("_Amount", newAmount * gems.Length - i);
                setZero = true;
            }
        }
    }

    void ChangeCastMode(PlayerAbility newAbility)
    {
        if (newAbility.AbilityName == "Remove Mist")
        {
            SetRemoveMode();
        }
        else
        {
            SetCreateMode();
        }
    }

    void SetRemoveMode()
    {
        foreach (Renderer gem in gems)
        {
            gem.material.SetFloat("_IsRemoveMode", 1);
        }
    }

    void SetCreateMode()
    {
        foreach (Renderer gem in gems)
        {
            gem.material.SetFloat("_IsRemoveMode", 0);
        }
    }
}
