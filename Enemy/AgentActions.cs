using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class AgentActions : MonoBehaviour
{
    [SerializeField] AttackTrigger trigger;
    [SerializeField] ParticleSystem attackParticle;

    [Header("Sound Effects")]
    [SerializeField] StudioEventEmitter attackSoundSource;
    [SerializeField] bool attackSoundPlayOnHit = false;
    [Tooltip("Range of noise made when attacking")]
    public float AudioHintRadiusAttack = 5f;

    [Header("Mist Removal")]
    [SerializeField] bool AttackRemovesMist = false;
    [SerializeField] float MistRemoveRadius = 8f;

    // references
    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private bool _hasAnimator;

    Dictionary<string, UnityEvent> eventBindings = new Dictionary<string, UnityEvent>();

    UnityEvent aggroFinishEvent;

    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _navMeshAgent = GetComponent<NavMeshAgent>();

        if (trigger != null)
        {
            trigger.attackActive = false;
        }

    }

    public void TriggerAttack()
    {
        if (_hasAnimator)
        {
            //_animator.SetBool("Attacking", true);
            _animator.SetTrigger("Attack");
            // trigger.attackActive = true;

        }

        if (!attackSoundPlayOnHit)
        {
            if (attackSoundSource != null && !attackSoundSource.IsPlaying())
            {
                attackSoundSource.SetParameter("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(attackSoundSource.transform.position));
                attackSoundSource.Play();
            }
            AudioHint.Create(attackSoundSource.transform.position, AudioHintRadiusAttack, 0.5f, AudioHintFlags.Suspicious | AudioHintFlags.IgnoreMist, gameObject);
        }
    }

    public void AddEventBinding(string eventName, UnityEvent eventBinding)
    {
        eventBindings[eventName] = eventBinding;
    }

    public void OnEventBindingTrigger(string eventName)
    {
        if (eventBindings.ContainsKey(eventName))
        {
            eventBindings[eventName].Invoke();
        }
    }

    public void TriggerAggroTaunt(UnityEvent finishEvent)
    {
        if (_hasAnimator)
        {
            _animator.SetTrigger("Aggro");
            aggroFinishEvent = finishEvent;
        }
    }

    void OnAttackActive()
    {
        if (_hasAnimator)
        {
            _animator.SetBool("Attacking", true);
            if (trigger != null)
            {
                trigger.attackActive = true;
            }
            if (attackParticle != null)
            {
                attackParticle.Play();
            }
            if (AttackRemovesMist)
            {
                MistManager.instance.RemoveMist(transform.position, MistRemoveRadius, 0f);
            }
            if (attackSoundPlayOnHit)
            {
                if (attackSoundSource != null && !attackSoundSource.IsPlaying())
                {
                    attackSoundSource.SetParameter("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(attackSoundSource.transform.position));
                    attackSoundSource.Play();
                }
                AudioHint.Create(transform.position, AudioHintRadiusAttack, 0.5f, AudioHintFlags.Suspicious | AudioHintFlags.IgnoreMist, gameObject);
            }
        }
    }

    void OnAttackEnd()
    {
        if (_hasAnimator)
        {
            _animator.SetBool("Attacking", false);
            _animator.ResetTrigger("Attack");
            if (trigger != null)
            {
                trigger.attackActive = false;
            }
            if (attackParticle != null)
            {
                attackParticle.Stop();
            }
        }
    }

    void OnAggroTauntEnd()
    {
        _animator.ResetTrigger("Aggro");

        if (aggroFinishEvent != null)
        {
            aggroFinishEvent.Invoke();
            aggroFinishEvent = null;
        }
    }
}
