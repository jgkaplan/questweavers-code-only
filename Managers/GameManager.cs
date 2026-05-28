using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Tooltip("The how much time slows down when aiming.")]
    public float bulletTimeTimeScale = 0.2f;

    [SerializeField] EventReference pauseSoundEffect;
    [SerializeField] EventReference unpauseSoundEffect;

    public GameObject CollectionMenu;

    public InputActionReference pauseInput;
    public InputActionReference aimInput; // used here to adjust the timescale when unpausing
    public InputActionReference resetInput; // TODO: Delete this. Used for testing only
    public InputActionReference backInput;

    public UnityEvent pauseEvent;
    public UnityEvent unpauseEvent;
    public UnityEvent finishedLoading;
    public UnityEvent initialized;
    public UnityEvent DoReset;

    public static GameManager Instance;

    public bool IsInitialized { get; private set; }

    public Fader fader;

    public GameObject inGameUI;
    public Analytics analyticsManager;

    private MonobehaviourFSM fsm;

    private List<WispLantern> lanterns;
    private Dictionary<string, List<WispLantern>> lanternsByTag;

    private bool paused = false;
    public bool IsPaused => paused;
    public bool IsInGameState = false;

    void OnEnable()
    {
        pauseInput.action.performed += TogglePause;
        backInput.action.performed += OnBack;
        // resetInput.action.performed += ResetButtonPressed;
        // resetInput.action.Enable();
    }

    void OnDisable()
    {
        pauseInput.action.performed -= TogglePause;
        backInput.action.performed -= OnBack;
        // resetInput.action.performed -= ResetButtonPressed;
        // resetInput.action.Disable();
    }

    void Awake()
    {
        fsm = GetComponent<MonobehaviourFSM>();
        Instance = this;
        lanterns = new List<WispLantern>();
        lanternsByTag = new();
    }

    public void CacheLanterns()
    {
        lanterns.Clear();
        lanternsByTag.Clear();
        foreach (var lantern in FindObjectsByType<WispLantern>(FindObjectsSortMode.None))
        {
            lanterns.Add(lantern);
            if (!lanternsByTag.TryGetValue(lantern.LanternTag, out List<WispLantern> l_list))
            {
                l_list = new();
                lanternsByTag.Add(lantern.LanternTag, l_list);
            }
            l_list.Add(lantern);
            // lanternsByTag.Add(lantern.LanternTag, lantern)
        }
    }

    void HideCollectedCollectables()
    {
        foreach (var collectable in FindObjectsByType<CollectablePickup>(FindObjectsSortMode.None))
        {
            collectable.CheckActivation();
        }
    }

    void HideLockedShrines()
    {
        foreach (var shrine in FindObjectsByType<ShrineTrigger>(FindObjectsSortMode.None))
        {
            shrine.CheckActivation();
        }
    }
    public WispLantern[] GetWispLanterns()
    {
        return lanterns.ToArray();
    }

    public List<WispLantern> GetWispLanternsByTag(string lanternTag)
    {
        return lanternsByTag.GetValueOrDefault(lanternTag);
    }

    // public void ResetScene()
    // {
    //     StartCoroutine(SceneReloader());
    // }

    // IEnumerator SceneReloader()
    // {
    //     yield return fader.FadeToBlack();
    //     AdditiveSceneManager.instance.ReloadGameScene();
    // }

    // This coroutine starts the game after things have loaded
    // IEnumerator StartGame()
    // {
    //     yield return new WaitUntil(() => CheckpointSystem.Loaded != CheckpointSystem.CheckpointLoadingState.NotLoaded);
    //     unpauseEvent.Invoke(); // lock the cursor and enable character input
    //     finishedLoading.Invoke();
    //     yield return fader.FadeInScene();
    // }

    public async Awaitable InitializeManagerAsync()
    {
        // Await spawnpoint to exist due to async loading
        while (GameObject.FindGameObjectWithTag("SpawnPoint") == null)
        {
            await Awaitable.NextFrameAsync();
        }
        Transform t = GameObject.FindGameObjectWithTag("SpawnPoint").transform;
        SaveSystem.SetCheckpoint(t);
    }

    /*

public async void StartGame()
{
    Analytics.StartNewSession();
    IsInitialized = false;
    fsm.ChangeState<LoadingLevelState>();
    await SaveSystem.TryLoadSave();
    await AdditiveSceneManager.instance.LoadFirstLevel();
    if (SaveSystem.Loaded == SaveSystem.SaveLoadingState.NeedNewSave)
    {
        await InitializeManagerAsync();
    }
    initialized.Invoke();
    fsm.ChangeState<GameplayState>();
    unpauseEvent.Invoke(); // lock the cursor and enable character input
    await fader.FadeInSceneAsync();
    new Analytics.LevelStartedEvent().Record();
    finishedLoading.Invoke();
    IsInitialized = true;
    // StartCoroutine(fader.FadeInScene());
}

public async void ResetScene()
{
    IsInitialized = false;
    await fader.FadeToBlackAsync();
    fsm.ChangeState<GameplayState>();
    DoReset.Invoke();
    await AdditiveSceneManager.instance.ReloadGameScene();
    unpauseEvent.Invoke();
    if (SaveSystem.Loaded == SaveSystem.SaveLoadingState.NeedNewSave)
    {
        await InitializeManagerAsync();
    }
    initialized.Invoke();
    await fader.FadeInSceneAsync();
    finishedLoading.Invoke();
    IsInitialized = true;
}
*/

    public void StartGame()
    {
        SaveSystem.Loaded = SaveSystem.SaveLoadingState.NotLoaded; // TODO: remove this if we ever have save files back
        SaveSystem.NewSave();
        ResetScene(true, true);
    }
    public async void ResetScene(bool startGame = false, bool loadStartingScene = false)
    {
        IsInitialized = false;
        fsm.ChangeState<LoadingLevelState>();
        if (startGame)
        {
            analyticsManager.StartNewSession();
            await SaveSystem.TryLoadSave();
            if (loadStartingScene)
            {
                await AdditiveSceneManager.instance.LoadFirstLevel();
            }
        }
        else
        {
            await AdditiveSceneManager.instance.ReloadGameScene();
        }
        DoReset.Invoke();
        if (SaveSystem.Loaded == SaveSystem.SaveLoadingState.NeedNewSave)
        {
            await InitializeManagerAsync();
        }
        CacheLanterns();
        HideCollectedCollectables();
        HideLockedShrines();
        initialized.Invoke();
        fsm.ChangeState<GameplayState>();
        unpauseEvent.Invoke(); // lock the cursor and enable character input
        await fader.FadeInSceneAsync();
        if (startGame)
        {
            new Analytics.LevelStartedEvent().Record();
        }
        finishedLoading.Invoke();
        IsInitialized = true;
    }

    // TODO: delete this in final build after testing
    private void ResetButtonPressed(InputAction.CallbackContext context)
    {
        ResetButtonPressed();
    }

    public void ResetButtonPressed()
    {
        SaveSystem.NewSave();
        fader.SetBlack();
        DoReset.Invoke();
        StartGame();
    }


    private void TogglePause(InputAction.CallbackContext context)
    {
        // HACK

        // if (GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().IsInState(typeof(PromptState)))
        if (AdvicePanel.instance.isShowingAdvice)
        {
            AdvicePanel.instance.CloseAdvice();
            return;
        }
        TogglePause();
    }

    public void TogglePause()
    {
        paused = !paused;
        if (paused)
        {
            Time.timeScale = 0.0f;
            FMODUnity.RuntimeManager.PlayOneShot(pauseSoundEffect);
            new Analytics.GamePausedEvent().Record();
            pauseEvent.Invoke();
        }
        else
        {
            // if (aimInput.action.IsPressed())
            // {
            //     Time.timeScale = bulletTimeTimeScale;
            // }
            // else
            // {
            //     Time.timeScale = 1.0f;
            // }
            Time.timeScale = 1.0f;
            FMODUnity.RuntimeManager.PlayOneShot(unpauseSoundEffect);
            new Analytics.GameUnpausedEvent().Record();
            unpauseEvent.Invoke();
        }
        CursorLockManager.CheckShouldLockCursor();
    }
    public void BackToMenu()
    {
        Player.instance.OnGoToMenu();
        fsm.ChangeState<MainMenuState>();
    }

    public void OnBack(InputAction.CallbackContext context)
    {
        if (!paused)
        {
            return;
        }

        if (CollectionMenu.activeSelf)
        {
            CollectionMenu.transform.Find("Button Back").GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            TogglePause();
        }

    }
}
