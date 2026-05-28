using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AdditiveSceneManager : MonoBehaviour
{
    public SceneReference initialGameLevelScene;
    public SceneReference mainMenuScene;

    [SerializeField] private SceneReference managerScene; // This scene. Used for name checks

    public static AdditiveSceneManager instance;
    private int currentScene;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    async void Awake()
    {
        instance = this;

        // for development purposes, if there's more than just the current scene loaded don't load a new one
        int numberOfCurrentScenes = SceneManager.sceneCount;
        // Debug.Log("Current scenes #: " + SceneManager.loadedSceneCount);
        // Debug.Log("Current scene name: " + SceneManager.GetActiveScene().name);

        // await SaveSystem.TryLoadSave();

        if (numberOfCurrentScenes > 1)
        {
            for (int i = 0; i < numberOfCurrentScenes; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.buildIndex != managerScene.BuildIndex)
                {
                    // make this one the active scene and don't load a new one
                    currentScene = s.buildIndex;
                }
            }
        }
        else
        {
            currentScene = mainMenuScene.BuildIndex;
            await SceneManager.LoadSceneAsync(mainMenuScene.BuildIndex, LoadSceneMode.Additive);
        }
#if !UNITY_EDITOR
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(currentScene));
#endif
    }

    void Start()
    {
        if (currentScene != mainMenuScene.BuildIndex)
        {
            GameManager.Instance.ResetScene(startGame: true, loadStartingScene: false);
        }
    }

    public async Awaitable LoadFirstLevel()
    {
        await LoadScene(initialGameLevelScene.BuildIndex);
    }

    public async Awaitable LoadScene(SceneReference newScene)
    {
        await LoadScene(newScene.BuildIndex);
    }

    public async Awaitable LoadScene(int newSceneBuildIndex)
    {
        MistManager.instance.UnsetActiveMistZones();
        await SceneManager.UnloadSceneAsync(currentScene);
        await SceneManager.LoadSceneAsync(newSceneBuildIndex, LoadSceneMode.Additive);
        currentScene = newSceneBuildIndex;
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(newSceneBuildIndex));
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        Debug.Log("Loaded Scene: " + s.name);
    }

    async public Awaitable ReloadGameScene()
    {
        // Debug.Log(SceneManager.GetSceneByBuildIndex(currentScene).name);
        // await Awaitable.EndOfFrameAsync();
        await LoadScene(currentScene);
    }
}
