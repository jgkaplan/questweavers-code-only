using UnityEngine;

namespace GOAP
{
    public class GoalGuard : Goal
    {
        public override int Priority { get => 10; }

        public override bool IsValid()
        {
            return base.IsValid() && brain.Alertness != AgentAlertness.Alert;
        }
    }
}