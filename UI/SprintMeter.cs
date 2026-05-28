using UnityEngine;
using UnityEngine.UI;

public class SprintMeter : MonoBehaviour
{
    [SerializeField] private Image innerBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        innerBar.transform.localScale = new(Player.instance.CurrentSprintPercentRemaining, 1, 1);
        innerBar.color = Player.instance.IsBurnedOut ? Color.red : Color.yellow;
    }
}
