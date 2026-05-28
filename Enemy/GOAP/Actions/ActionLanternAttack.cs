using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GOAP
{
    public class ActionLanternAttack : Action
    {
        [SerializeField] FMODUnity.EventReference sound;

        bool animFinished = false;
        UnityEvent animFinishedEvent;

        AgentLanternOccupier occupier;
        public override bool NeverInvalidate => true;

        protected override void Awake()
        {
            base.Awake();
            animFinishedEvent = new UnityEvent();
            animFinishedEvent.AddListener(OnAnimEnd);
            occupier = brain.GetComponent<AgentLanternOccupier>();
        }

        public override bool IsValid()
        {
            return base.IsValid();
        }

        public override bool ExecuteAction()
        {
            return animFinished;
        }

        public override void PostExecute()
        {
            occupier.ExitLantern();
            brain.WorldStateMemory["bored"] = 0;
            brain.WorldStateMemory["near_lantern"] = 0;
            brain.WorldStateMemory["lantern_attack"] = 0;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            animFinished = false;
            agent.SetDestination(agent.transform.position);

            brain.WorldStateMemory["lantern_attack"] = 1;
            agent.GetComponent<AgentActions>().AddEventBinding("WispAttack", animFinishedEvent);
            occupier.currentLantern.animator.ResetTrigger("Attack");
            occupier.currentLantern.animator.SetTrigger("Attack");
            brain.Animator.ResetTrigger("Attack");
            brain.Animator.SetTrigger("Attack");

            if (!sound.IsNull)
            {
                BackgroundMusicSystem.PlayOneShotSound(sound, agent.transform.position);
            }
        }

        public override void OnInvalidated()
        {
            base.OnInvalidated();
            occupier.ExitLantern();
            brain.WorldStateMemory["bored"] = 0;
            brain.WorldStateMemory["near_lantern"] = 0;
            brain.WorldStateMemory["lantern_attack"] = 0;
        }

        public void OnAnimEnd()
        {
            animFinished = true;
        }
    }

}
