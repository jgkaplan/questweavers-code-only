
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// The runtime instance of a the TextTrack. It is responsible for blending and setting the final data
// on the Text binding
public class TextTrackMixerBehaviour : PlayableBehaviour
{
    //     Color m_DefaultColor;
    //     float m_DefaultFontSize;
    string m_DefaultText;

    TextCutscenePlayer m_TrackBinding;

    // Called every frame that the timeline is evaluated. ProcessFrame is invoked after its' inputs.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        SetDefaults(playerData as TextCutscenePlayer);
        if (m_TrackBinding == null)
            return;

        int inputCount = playable.GetInputCount();

        Color blendedColor = Color.clear;
        float blendedFontSize = 0f;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        string text = m_DefaultText;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<TextPlayableBehaviour> inputPlayable = (ScriptPlayable<TextPlayableBehaviour>)playable.GetInput(i);
            TextPlayableBehaviour input = inputPlayable.GetBehaviour();

            // blendedColor += input.color * inputWeight;
            // blendedFontSize += input.fontSize * inputWeight;
            totalWeight += inputWeight;
            // canvasGroup.alpha = totalWeight;
            // use the text with the highest weight
            if (inputWeight > greatestWeight)
            {
                text = input.text;
                greatestWeight = inputWeight;
            }
        }

        // blend to the default values
        // m_TrackBinding.color = Color.Lerp(m_DefaultColor, blendedColor, totalWeight);
        // m_TrackBinding.fontSize = Mathf.RoundToInt(Mathf.Lerp(m_DefaultFontSize, blendedFontSize, totalWeight));
        m_TrackBinding.canvasGroup.alpha = totalWeight;
        m_TrackBinding.textDisplay.text = text;
    }

    // Invoked when the playable graph is destroyed, typically when PlayableDirector.Stop is called or the timeline
    // is complete.
    public override void OnPlayableDestroy(Playable playable)
    {
        RestoreDefaults();
    }

    void SetDefaults(TextCutscenePlayer text)
    {
        if (text == m_TrackBinding)
            return;

        RestoreDefaults();

        m_TrackBinding = text;
        // canvasGroup = text.gameObject.GetComponent<CanvasGroup>();
        if (m_TrackBinding != null)
        {
            // m_DefaultColor = m_TrackBinding.color;
            // m_DefaultFontSize = m_TrackBinding.fontSize;
            m_DefaultText = m_TrackBinding.textDisplay.text;
        }
    }

    void RestoreDefaults()
    {
        if (m_TrackBinding == null)
            return;

        // m_TrackBinding.color = m_DefaultColor;
        // m_TrackBinding.fontSize = m_DefaultFontSize;
        m_TrackBinding.textDisplay.text = m_DefaultText;
    }
}
