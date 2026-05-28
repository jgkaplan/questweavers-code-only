using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public class ActionLookAtStimulus : Action
    {
        protected override float baseCost { get => 1.5f; }

        [SerializeField] float turnRate = 60f;
        [SerializeField] float lookTime = 1f;
        float lookStartTime = -1f;

        public override float GetCost()
        {
            return baseCost + (brain.LastStimulusPosition - agent.transform.position).magnitude;
        }

        public override bool ExecuteAction()
        {
            var dir = brain.LastStimulusPosition - agent.transform.position;
            dir.y = 0f;
            agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, Quaternion.LookRotation(dir), Time.fixedDeltaTime * turnRate);

            if (lookStartTime < 0 && Vector3.Angle(dir, agent.transform.forward) <= 1f)
            {
                lookStartTime = Time.time;
                brain.Animator.SetBool("Looking", true);
            }
            else if (lookStartTime >= 0 && Time.time - lookStartTime > lookTime)
            {
                return true;
            }
            return false;
        }

        public override void PostExecute()
        {
            brain.WorldStateMemory["stimulus_seen"] = 1;
            brain.Animator.SetBool("Looking", false);
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            agent.SetDestination(agent.transform.position);
            lookStartTime = -1;
        }

        public override void OnInvalidated()
        {
            base.OnInvalidated();
            brain.Animator.SetBool("Looking", false);
        }
    }

}
