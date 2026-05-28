using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum OffMeshLinkMoveMethod
{
    Teleport,
    NormalSpeed,
    Parabola
}

[RequireComponent(typeof(NavMeshAgent))]
public class AgentLinkMover : MonoBehaviour
{
    public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Parabola;
    public float ParabolaDelay = 0.1f;
    Animator animator;
    IEnumerator Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.autoTraverseOffMeshLink = false;
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                /*
                while (animator.GetBool("Attacking"))
                {
                    yield return null;
                }
                */

                if (method == OffMeshLinkMoveMethod.NormalSpeed)
                    yield return StartCoroutine(NormalSpeed(agent));
                else if (method == OffMeshLinkMoveMethod.Parabola)
                {
                    var diff = agent.currentOffMeshLinkData.endPos - agent.currentOffMeshLinkData.startPos;
                    var dist = diff.magnitude;
                    var vDist = diff.y;
                    diff.y = 0f;
                    var hDist = diff.magnitude;
                    var dir = diff.normalized;
                    agent.updateRotation = false;


                    while (Vector3.Dot(dir, transform.forward) < 0.99f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * agent.angularSpeed);
                        yield return null;
                    }

                    var addTime = 0f;
                    var addVert = 0.1f;
                    animator.SetBool("Grounded", false);
                    if (vDist < 0f && !Physics.Raycast(agent.currentOffMeshLinkData.startPos + Vector3.up * 1f, agent.currentOffMeshLinkData.endPos - agent.currentOffMeshLinkData.startPos, dist, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
                    {
                        addTime = 0.05f;
                        animator.SetBool("FreeFall", true);
                    }
                    else
                    {
                        animator.SetBool("Jump", true);
                        addTime = 0.3f;
                        addVert = Mathf.Max(0f, vDist * -1f);
                    }
                    yield return StartCoroutine(Parabola(agent, animator, addVert + Mathf.Clamp(vDist / 12f, 0f, 1f), Mathf.Clamp(hDist / 5f + Mathf.Abs(vDist / 12f), 0f, 2f), addTime));
                }
                agent.CompleteOffMeshLink();
            }
            yield return null;
        }
    }

    IEnumerator NormalSpeed(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        while (agent.transform.position != endPos)
        {
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Parabola(NavMeshAgent agent, Animator animator, float height, float duration, float addDuration = 0f)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
        animator.SetBool("Jump", false);
        animator.SetBool("Grounded", true);
        animator.SetBool("FreeFall", false);
        yield return new WaitForSeconds(addDuration);
        agent.updateRotation = true;
    }
}
