
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// Runtime representation of a TextClip.
// The Serializable attribute is required to be animated by timeline, and used as a template.
[Serializable]
public class TextPlayableBehaviour : PlayableBehaviour
{
    [Tooltip("The text to display")]
    public string text = "";

    public AnimationCurve fadeIn = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve fadeOut = AnimationCurve.Linear(0, 1, 1, 0);

    private TextCutscenePlayer _textPlayer;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var binding = playerData as TextCutscenePlayer;
        if (binding == null)
            return;
        _textPlayer = binding;
        float clipDuration = (float)playable.GetDuration();
        float time = (float)playable.GetTime();
        float targetAlpha = 1f;
        float fadeOutStartTime = clipDuration - fadeOut[fadeOut.length - 1].time;
        if (time < fadeIn[fadeIn.length - 1].time)
        {
            targetAlpha = fadeIn.Evaluate(time);
        }
        else if (time > fadeOutStartTime)
        {
            targetAlpha = fadeOut.Evaluate(time - fadeOutStartTime);
        }
        // blend to the default values
        // m_TrackBinding.color = Color.Lerp(m_DefaultColor, blendedColor, totalWeight);
        // m_TrackBinding.fontSize = Mathf.RoundToInt(Mathf.Lerp(m_DefaultFontSize, blendedFontSize, totalWeight));
        binding.canvasGroup.alpha = targetAlpha;
        binding.textDisplay.text = text;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        base.OnPlayableDestroy(playable);
        _textPlayer.canvasGroup.alpha = 0;
    }


}