using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AdvicePanel : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI textLabel;
    [SerializeField] Button closeButton;
    [SerializeField] GameObject backdrop;

    [HideInInspector] public static AdvicePanel instance;

    public bool isShowingAdvice = false;

    void Awake()
    {
        instance = this;
        backdrop.SetActive(false);
    }

    public async void DisplayAdvice(Sprite sprite, string contents = "", float delay = 0)
    {
        await Awaitable.WaitForSecondsAsync(delay);
        DisplayAdvice(sprite, contents);
    }

    public void DisplayAdvice(Sprite sprite, string contents = "")
    {
        image.sprite = sprite;
        textLabel.text = contents;
        EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        isShowingAdvice = true;
        backdrop.SetActive(true);
        Time.timeScale = 0;
        CursorLockManager.CheckShouldLockCursor();
    }

    public void CloseAdvice()
    {
        isShowingAdvice = false;
        backdrop.SetActive(false);
        Time.timeScale = 1;
        CursorLockManager.CheckShouldLockCursor();
    }
}
