using UnityEngine;

public abstract class MonobehaviourState : MonoBehaviour
{
    protected MonobehaviourFSM fsm;

    public virtual string StateName { get => "State"; }

    public void SetStateMachine(MonobehaviourFSM fsm)
    {
        this.fsm = fsm;
    }

    public virtual void Setup() { }
    public virtual void OnEnter(MonobehaviourState previousState) { OnEnter(); }
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }

    public virtual void OnExit(MonobehaviourState nextState) { OnExit(); }
    public virtual void OnExit() { }
}
