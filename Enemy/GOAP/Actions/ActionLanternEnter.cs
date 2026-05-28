using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GOAP
{
    public class ActionLanternEnter : Action
    {
        [SerializeField] FMODUnity.EventReference sound;

        bool animFinished = false;
        UnityEvent animFinishedEvent;

        public override bool NeverInvalidate => true;

        AgentLanternOccupier occupier;
        WispLantern targetLantern => occupier.reservedLantern;

        protected override void Awake()
        {
            base.Awake();
            animFinishedEvent = new UnityEvent();
            animFinishedEvent.AddListener(OnAnimEnd);
            occupier = brain.GetComponent<AgentLanternOccupier>();
        }

        public override bool IsValid()
        {
            if (targetLantern == null)
            {
                return false;
            }
            return base.IsValid();
        }

        public override bool ExecuteAction()
        {
            return animFinished;
        }

        public override void PostExecute()
        {
            // occupier.EnterLantern(targetLantern);
            // brain.WorldStateMemory["near_lantern"] = 0;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            animFinished = false;
            agent.SetDestination(agent.transform.position);

            if (targetLantern != null)
            {
                agent.GetComponent<AgentActions>().AddEventBinding("EnterLantern", animFinishedEvent);
                occupier.EnterLantern(targetLantern);
                /*
                targetLantern.animator.ResetTrigger("EnterLantern");
                targetLantern.animator.SetTrigger("EnterLantern");
                brain.Animator.ResetTrigger("EnterLantern");
                brain.Animator.SetTrigger("EnterLantern");
                */
            }


            if (!sound.IsNull)
            {
                BackgroundMusicSystem.PlayOneShotSound(sound, agent.transform.position);
            }
        }

        public override void OnInvalidated()
        {
            occupier.UnreserveLantern();
            // brain.WorldStateMemory["near_lantern"] = 0;
            base.OnInvalidated();
        }

        public void OnAnimEnd()
        {
            animFinished = true;
        }
    }

}
