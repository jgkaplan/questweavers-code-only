using UnityEngine;

public class ResetAndClearChildren : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnReset()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
