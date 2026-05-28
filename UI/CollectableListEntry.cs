using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectableListEntry : MonoBehaviour
{
    [SerializeField] protected TMPro.TextMeshProUGUI nameField;
    private CollectableEntry entry;
    protected Image page;
    protected TextMeshProUGUI textDisplay;

    public void Setup(CollectableEntry entry, Image page, TextMeshProUGUI textDisplay)
    {
        this.entry = entry;
        this.page = page;
        this.textDisplay = textDisplay;
        nameField.text = entry.DisplayName;
    }

    public void DisplayEntry()
    {
        page.sprite = entry.Sprite;
        page.gameObject.SetActive(true);
        textDisplay.text = "";
        textDisplay.gameObject.SetActive(false);
    }
}
