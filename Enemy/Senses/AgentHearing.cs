using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class AgentHearing : AgentSense
{
    /// <summary>
    /// Size of audio collider that will interact with audio colliders. Effectively an additive modifier to range.
    /// </summary>
    [SerializeField] float HearingRadius = 0.5f;

    [SerializeField] bool CreateStimulus = true;
    [SerializeField] bool ReceiveSharedDetection = true;

    /// <summary>
    /// When hearing a sound with Distraction tag, this enemy will be forced into a fleeing state.
    /// If the enemy has a LanternOccupier, they are forced out of the lantern.
    /// </summary>
    [SerializeField] bool DistractionPanic = true;
    /// <summary>
    /// Distraction flee will trigger if the sound source is within this distance.
    /// </summary>
    [SerializeField] float DistractionPanicRange = 20f;
    /// <summary>
    /// Distraction will trigger panic for this long.
    /// </summary>
    [SerializeField] float DistractionPanicDuration = 8f;

    public override string SenseName => "Hearing";
    private Collider[] heardColliders;

    public override void Start()
    {
        heardColliders = new Collider[10];
    }

    void OnEnable()
    {
        AudioHint.AudioHintEmitted += MaybeHeardSound;
    }

    void OnDestroy()
    {
        AudioHint.AudioHintEmitted -= MaybeHeardSound;
    }

    void MaybeHeardSound(AudioHintData data)
    {
        if (!isActiveAndEnabled) return;
        float distance = Vector3.Distance(data.Origin, brain.transform.position);
        if (distance > data.Radius) return; // TODO: should we consider HearingRadius?

        if (CreateStimulus && brain.Alertness != AgentAlertness.Alert && (data.Flags & AudioHintFlags.Suspicious) == AudioHintFlags.Suspicious)
        {

            var prio = (int)Mathf.Lerp(AgentBrain.STIMULI_PRIORITY_AUDIO_MINIMUM, AgentBrain.STIMULI_PRIORITY_AUDIO_MAXIMUM, Mathf.InverseLerp(1, 0, distance / 50f));
            if ((data.Flags & AudioHintFlags.Distraction) == AudioHintFlags.Distraction)
            {
                prio = AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION;
            }
            brain.TriggerStimulus(this, data.Origin, prio);
        }

        if (ReceiveSharedDetection && brain.Alertness != AgentAlertness.Alert && (data.Flags & AudioHintFlags.ShareDetection) == AudioHintFlags.ShareDetection)
        {
            var sourceBrain = data.Source.GetComponent<AgentBrain>();

            var pos = data.Origin;
            if (sourceBrain != null && sourceBrain.Target != null)
            {
                pos = sourceBrain.Target.transform.position;
                brain.WorldStateMemory["aggro"] = 1;
                //brain.DetectTarget(this, sourceBrain.Target);
            }


            brain.TriggerStimulus(this, pos, AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION);
        }

        // Hearing distraction forces out wisps and causes enemies to panic
        if (DistractionPanic && (data.Flags & AudioHintFlags.Distraction) == AudioHintFlags.Distraction)
        {
            if (distance <= DistractionPanicRange)
            {
                brain.TriggerPanic(DistractionPanicDuration, data.Origin);
            }
        }
    }

    /*
    public override void Think()
    {
        int collisions = Physics.OverlapSphereNonAlloc(transform.position, HearingRadius, heardColliders, LayerMask.GetMask("Audio"), QueryTriggerInteraction.Collide);
        for (int i = 0; i < collisions; i++)
        {
            Collider col = heardColliders[i];
            var audioHint = col.GetComponent<AudioHint>();
            if (audioHint == null || !audioHint.enabled)
                continue;

            if (CreateStimulus && brain.Alertness != AgentAlertness.Alert && (audioHint.data.Flags & AudioHintFlags.Suspicious) == AudioHintFlags.Suspicious)
            {
                var dist = (audioHint.transform.position - brain.transform.position).magnitude;

                var prio = (int)Mathf.Lerp(AgentBrain.STIMULI_PRIORITY_AUDIO_MINIMUM, AgentBrain.STIMULI_PRIORITY_AUDIO_MAXIMUM, Mathf.InverseLerp(1, 0, dist / 50f));
                if ((audioHint.data.Flags & AudioHintFlags.Distraction) == AudioHintFlags.Distraction)
                {
                    prio = AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION;
                }
                brain.TriggerStimulus(this, audioHint.transform.position, prio);

                // is this good?
                // audioHint.enabled = false;
            }

            if (ReceiveSharedDetection && brain.Alertness != AgentAlertness.Alert && (audioHint.data.Flags & AudioHintFlags.ShareDetection) == AudioHintFlags.ShareDetection)
            {
                var sourceBrain = audioHint.data.Source.GetComponent<AgentBrain>();

                var pos = audioHint.transform.position;
                if (sourceBrain != null && sourceBrain.Target != null)
                {
                    pos = sourceBrain.Target.transform.position;
                    brain.WorldStateMemory["aggro"] = 1;
                    //brain.DetectTarget(this, sourceBrain.Target);
                }


                brain.TriggerStimulus(this, pos, AgentBrain.STIMULI_PRIORITY_PARTIAL_DETECTION);
            }

            // Hearing distraction forces out wisps and causes enemies to panic
            if (DistractionPanic && (audioHint.data.Flags & AudioHintFlags.Distraction) == AudioHintFlags.Distraction)
            {
                var dist = (audioHint.transform.position - brain.transform.position).magnitude;
                if (dist <= DistractionPanicRange)
                {
                    brain.TriggerPanic(DistractionPanicDuration, audioHint.transform.position);
                }
            }
        }
    }
    */
}
