using UnityEngine;

public class MistBlocker : MonoBehaviour
{
    [SerializeField] ParticleSystem effect;
    [SerializeField] GameObject forceField;
    [SerializeField] Collider mistCollider;

    bool blockerActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (forceField != null)
        {
            forceField.SetActive(false);
        }
    }

    public void SetBlockerState(bool state)
    {
        if (blockerActive == state)
        {
            return;
        }
        blockerActive = state;

        if (blockerActive)
        {
            effect.Play();
            forceField.SetActive(false);
            mistCollider.enabled = true;
        }
        else
        {
            effect.Stop();
            forceField.SetActive(true);
            mistCollider.enabled = false;
        }
    }
}
