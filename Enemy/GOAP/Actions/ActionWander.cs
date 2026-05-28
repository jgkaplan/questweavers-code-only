using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public class ActionWander : Action
    {
        [SerializeField] float boredomMin = 10f;
        [SerializeField] float boredomMax = 15f;
        [SerializeField] float range = 10f;

        /// <summary>
        /// If agent is too far from home position, they will roam back near where they started.
        /// </summary>
        [SerializeField] bool leash = true;
        [SerializeField] float leashRange = 30f;

        float boredomTime = 0f;
        float repathTime = 0f;

        Vector3 wanderPosition = Vector3.zero;

        public override bool IsValid()
        {
            return base.IsValid();
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {

        }

        public override bool ExecuteAction()
        {
            boredomTime -= Time.fixedDeltaTime;
            repathTime -= Time.fixedDeltaTime;
            if (boredomTime <= 0 || agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid || agent.remainingDistance <= 1f)
            {
                return true;
            } else if (repathTime <= 0 && brain.Alertness == AgentAlertness.Panic && (agent.transform.position - brain.LastStimulusPosition).magnitude <= 5f)
            {
                // Immediately trigger a re-path
                RandomPosition();
                repathTime = boredomMin;
            }
            return false;
        }

        public override void PostExecute()
        {
            brain.WorldStateMemory["hibernate"] = 0;
        }

        public override void PreExecute()
        {
            boredomTime = Mathf.Lerp(boredomMin, boredomMax, Random.value);
            repathTime = boredomMin;
            brain.State = AgentState.Animate;
            RandomPosition();
        }

        void RandomPosition()
        {
            if (leash && Vector3.Distance(agent.transform.position, brain.HomePosition) > leashRange)
            {
                wanderPosition = brain.HomePosition + Vector3.forward * Random.Range(-range, range) + Vector3.right * Random.Range(-range, range);
            }
            else
            {
                wanderPosition = agent.transform.position + Vector3.forward * Random.Range(-range, range) + Vector3.right * Random.Range(-range, range);
            }

            // In panic state, push the wander position away from the stimulus
            if (brain.Alertness == AgentAlertness.Panic)
            {
                wanderPosition += (agent.transform.position - brain.LastStimulusPosition).normalized * Random.Range(0, range);
            }

            agent.SetDestination(wanderPosition);
        }
    }

}
