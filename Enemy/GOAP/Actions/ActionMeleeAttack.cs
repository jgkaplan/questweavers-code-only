using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public class ActionMeleeAttack : Action
    {

        [SerializeField] float attackTurnRate = 120f;
        [SerializeField] float attackAngleTolerance = 100f;

        public override bool IsValid()
        {
            return base.IsValid() && brain.Target != null && (!brain.Target.GetComponent<Player>() || brain.Target.GetComponent<Player>().IsAlive());
        }

        public override bool ExecuteAction()
        {
            var dir = brain.Target.transform.position - brain.transform.position;
            dir.y = 0f;
            agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, Quaternion.LookRotation(dir), Time.fixedDeltaTime * attackTurnRate);
            
            if (!agent.GetComponent<Animator>().GetBool("Attacking"))
            {
                /*
                if (Vector3.Angle(dir, brain.transform.forward) <= attackAngleTolerance)
                {
                    agent.GetComponent<AgentActions>().TriggerAttack();
                } else
                {
                    return true;
                }
                */
                return true;
            }
            return false;
        }

        public override void PostExecute()
        {
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            agent.GetComponent<AgentActions>().TriggerAttack();
        }
    }

}
