using UnityEngine;

namespace GOAP
{
    public class GoalOccupyLantern : Goal
    {
        public override int Priority { get => 150; }
        public override bool IsValid()
        {
            return base.IsValid() && brain.GetWorldState("has_lantern") == 0;
        }

    }
}