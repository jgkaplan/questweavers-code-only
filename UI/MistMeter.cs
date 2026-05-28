using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MistMeter : MonoBehaviour
{
    [SerializeField] private float FadeInOutTime = 0.5f;
    [SerializeField, Tooltip("How much of the bar should change in a second. A value of 1 means the bar can go from empty to full in 1 second.")] private float meterChangeRate = 1f;
    [SerializeField] private GameObject castModeIcon;
    [SerializeField] private GameObject removeModeIcon;
    [SerializeField] private Image fullnessImage;

    private CanvasGroup canvasGroup;
    private Material material;
    private Coroutine fullnessCo;
    private int fadeState = 0;

    void Awake()
    {
        material = fullnessImage.material;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        removeModeIcon.SetActive(false);
        castModeIcon.SetActive(true);
    }

    void OnEnable()
    {
        PlayerAbilityManager.MistResourceChanged.AddListener(SetFullness);
        PlayerAbilityManager.CurrentAbilityChanged.AddListener(OnAbilityChanged);
    }

    private void OnAbilityChanged(PlayerAbility currentAbility)
    {
        if (currentAbility.AbilityName == "Create Mist")
        {
            castModeIcon.SetActive(true);
            removeModeIcon.SetActive(false);
        }
        else
        {
            castModeIcon.SetActive(false);
            removeModeIcon.SetActive(true);
        }
    }

    void OnDisable()
    {
        PlayerAbilityManager.MistResourceChanged.RemoveListener(SetFullness);
        PlayerAbilityManager.CurrentAbilityChanged.RemoveListener(OnAbilityChanged);
    }

    public async void Show()
    {
        fadeState = 1;
        while (fadeState == 1 && canvasGroup.alpha != 1)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, Time.deltaTime / FadeInOutTime);
            await Awaitable.NextFrameAsync();
        }
        if (fadeState == 1)
        {
            fadeState = 0;
        }
    }

    public async void Hide()
    {
        fadeState = -1;
        while (fadeState == -1 && canvasGroup.alpha != 0)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, Time.deltaTime / FadeInOutTime);
            await Awaitable.NextFrameAsync();
        }
        if (fadeState == -1)
        {
            fadeState = 0;
        }
    }

    void SetFullness(float value)
    {
        if (fullnessCo != null)
        {
            StopCoroutine(fullnessCo);
        }
        fullnessCo = StartCoroutine(SetFullnessCoroutine(value));
    }

    IEnumerator SetFullnessCoroutine(float value)
    {
        float current;
        do
        {

            current = material.GetFloat("_Fullness");
            material.SetFloat("_Fullness", Mathf.MoveTowards(current, value, Time.deltaTime * meterChangeRate));
            yield return new WaitForEndOfFrame();
        } while (!Mathf.Approximately(current, value));
        fullnessCo = null;
    }
}
