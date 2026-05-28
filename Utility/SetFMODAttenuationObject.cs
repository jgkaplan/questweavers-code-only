using UnityEngine;

[RequireComponent(typeof(FMODUnity.StudioListener))]
public class SetCameraAttenuationObject : MonoBehaviour
{
    private FMODUnity.StudioListener listener;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        listener = GetComponent<FMODUnity.StudioListener>();
    }

    public void SetAttenuationObject(GameObject g)
    {
        listener.AttenuationObject = g;
    }
}
