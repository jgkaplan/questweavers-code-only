using UnityEngine;

public class LoadingLevelState : MonobehaviourState
{
    [SerializeField] private BackgroundMusicSystem backgroundMusicSystem;
    GameManager gm;

    public override void Setup()
    {
        gm = GetComponent<GameManager>();
    }
    public override void OnEnter()
    {
        gm.inGameUI.SetActive(true);
        gm.fader.SetBlack();
        gm.IsInGameState = true;
        CursorLockManager.CheckShouldLockCursor();
    }

}
