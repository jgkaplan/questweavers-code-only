using System.Collections;
using UnityEngine;

public class MonobehaviourFSM : MonoBehaviour
{
    public MonoBehaviour initialState;

    public MonobehaviourState currentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Disable the states. We'll update them manually
        foreach (MonobehaviourState s in GetComponents<MonobehaviourState>())
        {
            s.enabled = false;
            s.SetStateMachine(this);
            s.Setup();
        }
    }

    void Start()
    {
        if (initialState != null && currentState == null)
        {
            currentState = GetComponent(initialState.GetType()) as MonobehaviourState;
            currentState.OnEnter(null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == null) return;
        currentState.OnUpdate();
    }

    public void ChangeState<T>() where T : MonobehaviourState
    {
        var nextState = GetComponent<T>();
        ChangeState(nextState);
    }

    public void ChangeState(MonobehaviourState nextState)
    {
        if (currentState != null)
        {
            currentState.OnExit(nextState);
        }
        MonobehaviourState prevState = currentState;
        currentState = nextState;
        nextState.OnEnter(prevState);

    }

    public IEnumerator ChangeStateAfterDelay<T>(float delay) where T : MonobehaviourState
    {
        yield return new WaitForSeconds(delay);
        ChangeState<T>();
    }

    public IEnumerator ChangeStateAfterDelay(MonobehaviourState nextState, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeState(nextState);
    }
}
