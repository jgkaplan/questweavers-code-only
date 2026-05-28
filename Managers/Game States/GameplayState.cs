using UnityEngine;

public class GameplayState : MonobehaviourState
{
    [SerializeField] private BackgroundMusicSystem backgroundMusicSystem;
    GameManager gm;

    public override void Setup()
    {
        gm = GetComponent<GameManager>();
    }
    public override void OnEnter()
    {
        gm.pauseInput.action.Enable();
        gm.inGameUI.SetActive(true);
        gm.fader.SetBlack();
        gm.IsInGameState = true;
        CursorLockManager.CheckShouldLockCursor();
        backgroundMusicSystem.StartGameMusic();
    }

    public override void OnExit()
    {
        gm.pauseInput.action.Disable();
        gm.inGameUI.SetActive(false);
        gm.fader.SetTransparent();
        backgroundMusicSystem.StopGameMusic();
    }

}
