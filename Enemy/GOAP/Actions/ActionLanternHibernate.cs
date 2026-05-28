using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace GOAP
{
    public class ActionLanternHibernate : Action
    {
        float boredomTime = 0f;

        AgentLanternOccupier occupier;
        WispLantern targetLantern => occupier.reservedLantern;

        public override bool IsValid()
        {

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
            return brain.GetWorldState("hibernate") == 0; // hibernation state removal is handled by AgentProximitySense
        }

        public override void PostExecute()
        {
            brain.WorldStateMemory["hibernate"] = 0;
        }

        public override void PreExecute()
        {
        }
    }

}
