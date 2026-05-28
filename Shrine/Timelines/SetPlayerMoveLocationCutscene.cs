using UnityEngine;

public class SetPlayerMoveLocationCutscene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetPlayerMoveLocation();
    }

    public void SetPlayerMoveLocation()
    {
        Player.instance.GetComponent<FinalCutsceneState>().pathfindLocation = transform;
    }
}
