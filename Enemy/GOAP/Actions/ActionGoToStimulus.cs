using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public class ActionGoToStimulus : Action
    {
        protected override float baseCost { get => 1.5f; }

        [SerializeField] float desiredDistance = 0.75f;
        [SerializeField] float stayTime = 3f;

        float stayTimeLeft;

        public override float GetCost()
        {
            return baseCost + (brain.LastStimulusPosition - agent.transform.position).magnitude;
        }

        public override bool IsValid()
        {
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid || brain.GetWorldState("target_detected") == 1)
            {
                return false;
            }
            return true;
        }

        public override bool ExecuteAction()
        {
            agent.GetComponent<AgentLocomotion>().SetDestination(brain.LastStimulusPosition);

            if ((brain.LastStimulusPosition - agent.transform.position).magnitude <= desiredDistance)
            {
                agent.GetComponent<AgentLocomotion>().SetDestination(agent.transform.position);
                stayTimeLeft -= Time.fixedDeltaTime;
                if (stayTimeLeft <= 0)
                {
                    return true;
                }
            } else
            {
                agent.GetComponent<AgentLocomotion>().SetDestination(brain.LastStimulusPosition);
            }
            return false;
        }

        public override void PostExecute()
        {
            brain.ForgetStimulus();
            brain.Animator.SetBool("Looking", false);
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            brain.Animator.SetBool("Looking", true);
            stayTimeLeft = stayTime;
        }

        public override void OnInvalidated()
        {
            base.OnInvalidated();
            brain.Animator.SetBool("Looking", false);
        }
    }

}
