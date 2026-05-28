using UnityEngine;

public class FirecrackerUI : MonoBehaviour
{
    [SerializeField] private GameObject[] uiCounters;
    void Awake()
    {
        OnFirecrackerResourceChanged(0);
        FirecrackerAbility.FirecrackerResourceChanged.AddListener(OnFirecrackerResourceChanged);
    }

    void OnFirecrackerResourceChanged(int newNumberOfFirecrackers)
    {
        for (int i = 0; i < uiCounters.Length; i++)
        {
            uiCounters[i].SetActive(i < newNumberOfFirecrackers);
        }
    }
}
