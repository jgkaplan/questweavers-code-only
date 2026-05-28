using UnityEngine;

public class MainMenuState : MonobehaviourState
{
    GameManager gm;

    public override void Setup()
    {
        gm = GetComponent<GameManager>();
    }

    public override async void OnEnter(MonobehaviourState prev)
    {
        gm.inGameUI.SetActive(false);
        gm.IsInGameState = false;
        CursorLockManager.CheckShouldLockCursor();
        if (prev != null && prev != this)
        {
            gm.analyticsManager.EndSession();
            await AdditiveSceneManager.instance.LoadScene(AdditiveSceneManager.instance.mainMenuScene);
        }
        SaveSystem.NewSave(); // TODO: remove this in the final build
    }
}
