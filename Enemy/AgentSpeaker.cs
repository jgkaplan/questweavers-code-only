using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;

public enum VoicelineType
{
    None = -1,
    Ambient = 0, // Has cooldown.
    Dialogue = 1, // Spoken lines. No cooldown, but can be overridden by barks.
    Bark = 2, // Short vocal reactions. Overrides speech, has cooldown.
    Critical = 3, // No cooldown, highest priority.
}

public class AgentSpeaker : MonoBehaviour
{
    Dictionary<string, float> voicelineCooldowns = new Dictionary<string, float>();

    VoicelineType activeVoicelineType = VoicelineType.None;
    EventInstance activeVoiceline;

    private void Start()
    {
    }

    private void Update()
    {
        if (activeVoiceline.isValid())
        {
            PLAYBACK_STATE playbackState;
            activeVoiceline.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                activeVoiceline.release();
                activeVoiceline.clearHandle();
            }
        }
    }

    private void OnDestroy()
    {
        StopPlayback(STOP_MODE.IMMEDIATE);
    }

    /// <summary>
    /// Whether a voiceline is currently being played.
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        if (!activeVoiceline.isValid())
        {
            return false;
        }
        PLAYBACK_STATE playbackState;
        activeVoiceline.getPlaybackState(out playbackState);
        return playbackState == PLAYBACK_STATE.PLAYING;
    }

    public void StopPlayback(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        if (activeVoiceline.isValid())
        {
            activeVoiceline.stop(stopMode);
            activeVoiceline.release();
            activeVoiceline.clearHandle();
        }
    }

    /// <summary>
    /// Whether the provided voiceline type shoud override the current line.
    /// </summary>
    /// <param name="voicelineType">Voiceline type.</param>
    /// <returns>True if should override.</returns>
    public bool ShouldOverride(VoicelineType voicelineType)
    {
        return !activeVoiceline.isValid() || voicelineType > activeVoicelineType;
    }

    /// <summary>
    /// Makes the agent speak a line, if possible.
    /// </summary>
    /// <param name="path">String path to FMOD event.</param>
    /// <param name="voicelineType">The voiceline type.</param>
    /// <returns>Whether the line is actually played.</returns>
    public bool SpeakLine(string path, VoicelineType voicelineType)
    {
        /*
        if (!ShouldOverride(voicelineType))
        {
            return false;
        }

        if (IsPlaying())
        {
            StopPlayback(STOP_MODE.IMMEDIATE);
        }

        activeVoiceline = FMODUnity.RuntimeManager.CreateInstance(FMODUnity.EventReference.Find(path));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(activeVoiceline, gameObject);
        activeVoicelineType = voicelineType;
        activeVoiceline.start();
        */

        return true;
    }

    public void OnGamePause(bool pause)
    {
        /*
        if (pause && fmodEmitter.IsPlaying())
        {
            fmodEmitter.EventInstance.setPaused(true);
        } else if (!pause && fmodEmitter.EventInstance.isValid())
        {
            fmodEmitter.EventInstance.setPaused(false);
        }
        */
    }
}
