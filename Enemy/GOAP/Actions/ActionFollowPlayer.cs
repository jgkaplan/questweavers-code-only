using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public class ActionFollowPlayer : Action
    {
        [SerializeField] float desiredDistance = 0.75f;

        public override float GetCost()
        {
            return baseCost + (brain.Target == null ? 0 :(brain.Target.transform.position - agent.transform.position).magnitude);
        }
        public override bool IsValid()
        {
            if (brain.Target == null || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }
            return base.IsValid();
        }

        public override bool ExecuteAction()
        {
            var pos = brain.Target.transform.position;

            agent.GetComponent<AgentLocomotion>().SetDestination(pos);
            if ((pos - agent.transform.position).magnitude <= desiredDistance)
            {
                return true;
            }
            return false;
        }

        public override void PostExecute()
        {
            agent.GetComponent<AgentLocomotion>().SetDestination(agent.transform.position);
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            brain.Alertness = AgentAlertness.Alert;
        }
    }

}
