using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace GOAP
{

    [ExecuteInEditMode]
    public class ActionPatrol : Action
    {
        public PatrolNode[] patrolNodes;

        int nodeIndex = 0;
        float timeAtNode = 0f;

        protected override void Awake()
        {
            base.Awake();
        }

        public override bool ExecuteAction()
        {

            var node = patrolNodes[nodeIndex];
            if (node == null)
            {
                Debug.LogWarning("Agent had invalid patrol node!");
                return true;
            }
            // uh oh
            /*
            if (node.position == Vector3.zero)
            {
                node.position = brain.HomePosition;
            }
            */

            if (Vector3.Distance(agent.transform.position, node.position) <= agent.radius + agent.stoppingDistance)
            {
                timeAtNode += Time.fixedDeltaTime;
                if (timeAtNode >= node.stopDuration && patrolNodes.Length > 1)
                {
                    nodeIndex = (nodeIndex + 1) % patrolNodes.Length;
                    timeAtNode = 0f;
                    agent.GetComponent<AgentLocomotion>().SetDestination(patrolNodes[nodeIndex].position);
                }
                else
                {
                    agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, node.rotation, Time.fixedDeltaTime * Mathf.Max(60f, agent.angularSpeed));
                }
            }
            else
            {
                timeAtNode = 0;
                agent.GetComponent<AgentLocomotion>().SetDestination(node.position);
            }

            return false;
        }

        public override void PostExecute()
        {
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            nodeIndex = PatrolNode.GetClosestNodeIndex(patrolNodes, agent.transform.position);
            agent.GetComponent<AgentLocomotion>().SetDestination(patrolNodes[nodeIndex].position);
            timeAtNode = 0;

        }
    }

}
