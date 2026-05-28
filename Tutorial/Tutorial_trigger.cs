using UnityEngine;

public class Tutorial_trigger : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private TutorialPackage package;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (Tutorial_manager.Instance == null)
        {
            Debug.LogWarning("Tutorial_trigger: No TutorialOverlayManager found in scene.");
            return;
        }

        Debug.Log("Player entered tutorial zone");

        Tutorial_manager.Instance.RequestPackage(package);
    }
    /*
        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            if (Tutorial_manager.Instance == null)
            {
                Debug.LogWarning("Tutorial_trigger: No TutorialOverlayManager found in scene.");
                return;
            }
            Tutorial_manager.Instance.PlayerInTutorialZone();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            if (Tutorial_manager.Instance == null)
            {
                Debug.LogWarning("Tutorial_trigger: No TutorialOverlayManager found in scene.");
                return;
            }

            Debug.Log("Player left tutorial zone");

            Tutorial_manager.Instance.PlayerLeavingTutorialZone();
        }
    */
}