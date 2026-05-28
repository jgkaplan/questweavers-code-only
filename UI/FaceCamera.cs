using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera _camera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = transform.position - _camera.transform.position;
        transform.rotation = Quaternion.LookRotation(target, Vector3.up);
    }
}
