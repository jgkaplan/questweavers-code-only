using UnityEngine;

public class CollectableTextEntry : MonoBehaviour
{
    public string DisplayName;
    [TextArea(minLines: 8, maxLines: 12)] public string Text;
}
