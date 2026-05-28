using System;
using UnityEngine;
using UnityEngine.AI;

public abstract class AgentSense : MonoBehaviour
{
    protected AgentBrain brain;
    protected NavMeshAgent agent { get => brain.NavMeshAgent; }
    public GameObject Target { get; protected set; }

    public abstract string SenseName { get; }

    public virtual void Awake()
    {
        brain = GetComponentInParent<AgentBrain>();
    }

    public virtual void Start()
    {

    }

    public virtual bool IsSenseActive()
    {
        return true;
    }

    /// <summary>
    /// Whether the sense is currently tracking the target.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsSenseDetecting()
    {
        return false;
    }

    public virtual void Think()
    {

    }

    /// <summary>
    /// Called once when AgentBrain is entering panic.
    /// </summary>
    public virtual void OnPanic()
    {

    }
}
