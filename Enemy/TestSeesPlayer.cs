using UnityEngine;

public class TestSeesPlayer : MonoBehaviour
{
    public float detectionRange = 10f;
    public float detectionAngle = 60;

    public float sphereCastRadius = 0.1f;
    public GameObject player;

    private Material defaultMaterial;
    public Material detectionMaterial;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultMaterial = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > detectionRange)
        {
            OnNotDetectingPlayer();
            return;
        }

        Vector3 directionToPlayer = player.transform.position - transform.position;
        if (Vector3.Angle(directionToPlayer, transform.forward) > detectionAngle / 2)
        {
            OnNotDetectingPlayer();
            return;
        }

        if (Physics.SphereCast(transform.position, sphereCastRadius, directionToPlayer, out RaycastHit hit, detectionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.transform.gameObject == player)
            {

                OnDetectPlayer();
                return;
            }
            else
            {
                OnNotDetectingPlayer();
                return;
            }
        }
        OnNotDetectingPlayer();
        return;

    }

    void OnDetectPlayer()
    {
        GetComponent<Renderer>().material = detectionMaterial;
    }

    void OnNotDetectingPlayer()
    {
        GetComponent<Renderer>().material = defaultMaterial;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {

        Gizmos.DrawLine(transform.position, transform.position + transform.forward * detectionRange);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, detectionAngle, detectionRange, 0, 1);
    }
#endif
}
