using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public abstract class Goal : MonoBehaviour, IComparer<Goal>, System.IComparable<Goal>
    {
        public string Name { get; protected set; }

        /// <summary>
        /// The importance of this goal relative to other goals.
        /// Higher priority goals are evaluated first.
        /// </summary>
        public virtual int Priority { get => 100; }

        /// <summary>
        /// If not set, being in Panic alertness makes this goal invalid.
        /// </summary>
        public bool AllowedWhileInPanic = false;


        public UDictionary<string, int> WorldStateDesired;

        protected AgentBrain brain;
        protected NavMeshAgent agent { get => brain.NavMeshAgent; }

        protected virtual void Awake()
        {
            brain = GetComponentInParent<AgentBrain>();
        }

        public virtual bool IsValid()
        {
            if (!AllowedWhileInPanic && brain.Alertness == AgentAlertness.Panic)
            {
                return false;
            }
            return true;
        }

        public int Compare(Goal x, Goal y)
        {
            return x.Priority.CompareTo(y.Priority);
        }

        public int CompareTo(Goal other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}