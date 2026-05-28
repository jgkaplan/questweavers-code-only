using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectableTextListEntry : CollectableListEntry
{
    private CollectableTextEntry textEntry;

    public void Setup(CollectableTextEntry entry, Image page, TextMeshProUGUI textDisplay)
    {
        this.textEntry = entry;
        this.page = page;
        this.textDisplay = textDisplay;
        nameField.text = entry.DisplayName;
    }

    public new void DisplayEntry()
    {
        page.sprite = null;
        page.gameObject.SetActive(false);
        textDisplay.text = textEntry.Text;
        textDisplay.gameObject.SetActive(true);
    }
}
