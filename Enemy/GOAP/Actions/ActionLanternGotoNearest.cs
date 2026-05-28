using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public class ActionLanternGotoNearest : Action
    {
        [SerializeField] protected bool preferChangingLanterns = false;
        [SerializeField] protected float desiredDistance = 0.75f;

        protected AgentLanternOccupier occupier;
        protected WispLantern targetLantern => occupier.reservedLantern;

        protected override void Awake()
        {
            base.Awake();
            occupier = brain.GetComponent<AgentLanternOccupier>();
        }

        public override float GetCost()
        {
            return baseCost + (targetLantern == null ? 0 : (targetLantern.transform.position - agent.transform.position).magnitude);
        }
        public override bool IsValid()
        {
            if (brain.GetWorldState("has_lantern") == 1 || targetLantern == null || targetLantern.IsOccupied())
            {
                return false;
            }
            return base.IsValid();
        }

        public override bool ExecuteAction()
        {
            var pos = targetLantern.transform.position;

            agent.SetDestination(pos);
            if ((pos - agent.transform.position).magnitude <= desiredDistance)
            {
                return true;
            }
            return false;
        }

        public override void PostExecute()
        {
            agent.SetDestination(agent.transform.position);
            // brain.WorldStateMemory["near_lantern"] = 1;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            // brain.WorldStateMemory["near_lantern"] = 0;

            var lantern = occupier.FindNearestEmptyLantern(preferChangingLanterns ? 5f : -1f);
            if (lantern != null)
            {
                occupier.ReserveLantern(lantern);
            }
            else
            {
                // Debug.LogWarning("Failed to find empty lantern!");
            }
        }

        public override void OnInvalidated()
        {
            occupier.UnreserveLantern();
            base.OnInvalidated();
        }
    }

}
