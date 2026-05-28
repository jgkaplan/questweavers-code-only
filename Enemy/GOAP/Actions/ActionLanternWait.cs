using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace GOAP
{
    public class ActionLanternWait : Action
    {
        [SerializeField] float boredomMin = 20f;
        [SerializeField] float boredomMax = 45f;

        float boredomTime = 0f;

        AgentLanternOccupier occupier;
        WispLantern targetLantern => occupier.reservedLantern;

        public override bool IsValid()
        {
            if (brain.GetWorldState("target_proximity") == 1)
            {
                return false;
            }
            return base.IsValid();
        }

        protected override void Awake()
        {
            base.Awake();
            occupier = brain.GetComponent<AgentLanternOccupier>();
        }

        private void Start()
        {
        }

        public override bool ExecuteAction()
        {
            if (boredomTime > 0 || brain.GetWorldState("target_los") == 1) // TODO: Ensure we're not detecting player and player isn't nearby in sight when we slip away
            {
                boredomTime -= Time.fixedDeltaTime;
                return false;
            }
            return true;
        }

        public override void PostExecute()
        {
            brain.WorldStateMemory["bored"] = 1;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Animate;
            boredomTime = Mathf.Lerp(boredomMin, boredomMax, UnityEngine.Random.value);
        }
    }

}
