using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TextCutscenePlayer : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI textDisplay;
    public CanvasGroup canvasGroup;

    public static TextCutscenePlayer instance;

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        canvasGroup.alpha = 0;
        // Test();
    }
}
