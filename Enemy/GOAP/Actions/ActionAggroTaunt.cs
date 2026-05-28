using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GOAP
{
    public class ActionAggroTaunt : Action
    {
        [SerializeField] float shareDetectionHintRadius = 20f;
        [SerializeField] float skipAnimationRadius = 1f;
        [SerializeField] FMODUnity.EventReference tauntSound;

        bool animFinished = false;
        UnityEvent animFinishedEvent;

        protected override void Awake()
        {
            base.Awake();
            animFinishedEvent = new UnityEvent();
            animFinishedEvent.AddListener(OnAnimEnd);
        }

        public override bool ExecuteAction()
        {
            return animFinished;
        }

        public override void PostExecute()
        {
            brain.WorldStateMemory["aggro"] = 1;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            animFinished = false;
            if (brain.Target != null && (brain.Target.transform.position - agent.transform.position).magnitude <= skipAnimationRadius)
            {
                animFinished = true;
                brain.WorldStateMemory["aggro"] = 1;
            }
            else
            {
                agent.SetDestination(agent.transform.position);
                agent.GetComponent<AgentActions>().TriggerAggroTaunt(animFinishedEvent);
                BackgroundMusicSystem.PlayOneShotSound(tauntSound, agent.transform.position);
                AudioHint.Create(transform.position, shareDetectionHintRadius, 1f, AudioHintFlags.ShareDetection, brain.gameObject);
            }
        }

        public void OnAnimEnd()
        {
            animFinished = true;
        }
    }

}
