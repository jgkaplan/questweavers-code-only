using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    [SerializeField] GameObject entryPrefab;
    [SerializeField] GameObject textEntryPrefab;
    [SerializeField] Transform collectableParent;
    [SerializeField] Transform listParent;
    [SerializeField] Image collectablePage;
    [SerializeField] TextMeshProUGUI collectableTextDisplay;

    Dictionary<string, CollectableListEntry> entries;

    private void Start()
    {
        entries = new();

        foreach (var entry in collectableParent.GetComponentsInChildren<CollectableEntry>())
        {
            var newEntry = Instantiate(entryPrefab, listParent);
            var comp = newEntry.GetComponent<CollectableListEntry>();

            comp.Setup(entry, collectablePage, collectableTextDisplay);

            entries[entry.name] = comp;
        }
        foreach (var entry in collectableParent.GetComponentsInChildren<CollectableTextEntry>())
        {
            var newEntry = Instantiate(textEntryPrefab, listParent);
            var comp = newEntry.GetComponent<CollectableTextListEntry>();

            comp.Setup(entry, collectablePage, collectableTextDisplay);

            entries[entry.name] = comp;
        }
    }

    public void RefreshEntries()
    {
        foreach (var key in entries.Keys)
        {
            entries[key].gameObject.SetActive(SaveSystem.HasCollectable(key));
        }
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void ContinueButton()
    {
        GameManager.Instance.TogglePause();
    }

    public void ResetButton()
    {
        GameManager.Instance.TogglePause();
        Player.instance.Die("got stuck");
        // GameManager.Instance.ResetButtonPressed();
    }

    public void BackToMenuButton()
    {
        GameManager.Instance.TogglePause();
        GameManager.Instance.BackToMenu();
    }
}
