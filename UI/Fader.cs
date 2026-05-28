using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Fader : MonoBehaviour
{
    [SerializeField]
    [Tooltip("How long in seconds it should take to fade completely")]
    private float fadeTime = 1;

    [SerializeField]
    private CanvasGroup fader;

    public void SetTransparent()
    {
        fader.alpha = 0;
        fader.gameObject.SetActive(false);
    }

    public void SetBlack()
    {
        fader.alpha = 1;
        fader.gameObject.SetActive(true);
    }

    public IEnumerator FadeInScene()
    {
        fader.alpha = 1;
        fader.gameObject.SetActive(true);
        while (fader.alpha != 0)
        {
            fader.alpha = Mathf.MoveTowards(fader.alpha, 0, Time.deltaTime / fadeTime);
            yield return new WaitForEndOfFrame();
        }
        fader.gameObject.SetActive(false);
    }

    public async Awaitable FadeInSceneAsync()
    {
        fader.alpha = 1;
        fader.gameObject.SetActive(true);
        while (fader.alpha != 0)
        {
            fader.alpha = Mathf.MoveTowards(fader.alpha, 0, Time.deltaTime / fadeTime);
            await Awaitable.NextFrameAsync();
        }
        fader.gameObject.SetActive(false);
    }

    public IEnumerator FadeToBlack()
    {
        fader.alpha = 0;
        fader.gameObject.SetActive(true);
        while (fader.alpha != 1)
        {
            fader.alpha = Mathf.MoveTowards(fader.alpha, 1, Time.deltaTime / fadeTime);
            yield return new WaitForEndOfFrame();
        }
    }

    public async Awaitable FadeToBlackAsync()
    {
        fader.alpha = 0;
        fader.gameObject.SetActive(true);
        while (fader.alpha != 1)
        {
            fader.alpha = Mathf.MoveTowards(fader.alpha, 1, Time.deltaTime / fadeTime);
            await Awaitable.NextFrameAsync();
        }
    }
}
