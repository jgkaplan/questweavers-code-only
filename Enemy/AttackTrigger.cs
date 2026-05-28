using UnityEngine;
using UnityEngine.Events;

public class AttackTrigger : MonoBehaviour
{
    [SerializeField] bool requireLineofSightCheck = false;
    [SerializeField] float maxDistance = -1f;
    [SerializeField] LayerMask lineOfSightMask;

    [HideInInspector] public bool attackActive = false;
    public static UnityEvent<string> playerHit = new();

    void TriggerCheck(Collider other)
    {
        if (attackActive && other.CompareTag("Player"))
        {
            var los = false;
            var diff = other.transform.position - transform.position;

            if (requireLineofSightCheck)
            {
                RaycastHit hit;
                if (!Physics.Raycast(transform.position, diff, out hit, diff.magnitude, lineOfSightMask, QueryTriggerInteraction.Ignore))
                {
                    los = true;
                }
            }
            else
            {
                los = true;
            }

            if (los && (maxDistance < 0 || diff.magnitude <= maxDistance))
            {
                playerHit.Invoke(transform.parent.name);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerCheck(other);
    }
    private void OnTriggerEnter(Collider other)
    {
        TriggerCheck(other);
    }
}
