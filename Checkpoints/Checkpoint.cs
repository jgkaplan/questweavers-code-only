using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(GuidComponent))]
public class Checkpoint : MonoBehaviour
{
    public static UnityEvent<bool, Transform> activateCheckpoint = new(); // (is first activation, this transform)
    public bool hasActivated = false;
    public Animator visualActivationIndicator;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        using (new Handles.DrawingScope(Color.white))
        {
            Handles.ArrowHandleCap(0, transform.position, transform.rotation, 1, EventType.Repaint);
        }
    }
#endif

    void Start()
    {
        if (SaveSystem.HasSaveData() && SaveSystem.saveData.currentCheckpointGUID == GetComponent<GuidComponent>().GetGuid().ToString())
        {
            ActivateVisuals();
        }
        else
        {
            DeactivateVisuals();
        }
        activateCheckpoint.AddListener(TurnOffWhenOtherCheckpointActivated);
    }

    void TurnOffWhenOtherCheckpointActivated(bool firstActivation, Transform otherCheckpoint)
    {
        if (otherCheckpoint.transform != transform)
        {
            DeactivateVisuals();
        }
    }

    public void TriggerCheckpoint()
    {
        activateCheckpoint.Invoke(!hasActivated, transform);
        // StartCoroutine(ActivateVisuals(2.5f));
        hasActivated = true;
    }

    public void ForceTriggerCheckpoint()
    {
        activateCheckpoint.Invoke(false, transform);
    }

    public void ActivateVisuals()
    {
        visualActivationIndicator.SetBool("ShrineActive", true);
    }

    public void DeactivateVisuals()
    {
        visualActivationIndicator.SetBool("ShrineActive", false);
    }
}
