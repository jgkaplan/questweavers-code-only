using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public abstract class Action : MonoBehaviour
    {
        protected virtual float baseCost { get => 1f; }
        [SerializeField] UDictionary<string, int> WorldStatePreconditions;
        [SerializeField] UDictionary<string, int> WorldStateOutcome;

        protected AgentBrain brain;
        protected NavMeshAgent agent { get => brain.NavMeshAgent; }

        /// <summary>
        /// If set, AgentBrain will never try to invalidate this action while it's being performed.
        /// </summary>
        public virtual bool NeverInvalidate { get => false; }

        protected virtual void Awake()
        {
            brain = GetComponentInParent<AgentBrain>();
        }

        public virtual Dictionary<string, int> GetProceduralPreconditions(Dictionary<string, int> worldState)
        {
            return null;
        }

        public Dictionary<string, int> GetWorldStatePreconditions(bool ignoreProcedural = false)
        {
            var state = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> kvp in WorldStatePreconditions.Dictionary)
            {
                state[kvp.Key] = kvp.Value;
            }
            if (!ignoreProcedural)
            {
                var proceduralStates = GetProceduralPreconditions(state);
                if (proceduralStates != null)
                {
                    foreach (KeyValuePair<string, int> kvp in proceduralStates)
                    {
                        state[kvp.Key] = kvp.Value;
                    }
                }
            }
            return state;
        }

        public bool CheckWorldStatePreconditions(Dictionary<string, int> state)
        {
            var preconds = GetWorldStatePreconditions();
            foreach (KeyValuePair<string, int> pair in preconds)
            {
                if ((!state.ContainsKey(pair.Key) && pair.Value != 0)
                    || (state.ContainsKey(pair.Key) && state[pair.Key] != pair.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CheckWorldStateOutcome(Dictionary<string, int> state)
        {
            foreach (KeyValuePair<string, int> pair in state)
            {
                if ((!WorldStateOutcome.ContainsKey(pair.Key) || WorldStateOutcome[pair.Key] != pair.Value)
                    && !(brain.WorldStateMemory.ContainsKey(pair.Key) && brain.WorldStateMemory[pair.Key] == pair.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual float GetCost()
        {
            return baseCost;
        }
        /// <summary>
        /// Check whether the action is still valid while executing it.
        /// </summary>
        /// <returns>Return false to invalidate the current plan.</returns>
        public virtual bool IsValid()
        {
            if (!CheckWorldStatePreconditions(brain.WorldStateMemory))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Called every FixedUpdate() while the action is active.
        /// </summary>
        /// <returns>Return true to declare the action is complete.</returns>
        public abstract bool ExecuteAction();

        /// <summary>
        /// Called once just before the action becomes the active action.
        /// </summary>
        /// <returns></returns>
        public abstract void PreExecute();

        /// <summary>
        /// Called once after the action is executed successfully. Does not run if the action failed to complete.
        /// </summary>
        /// <returns></returns>
        public abstract void PostExecute();

        /// <summary>
        /// Called once after the action is invalidated (such as when IsValid fails).
        /// </summary>
        public virtual void OnInvalidated()
        {

        }

        // Extension: Sustainable/open-ended tasks?

        // Extension: Adaptability?

    }

}
