using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class StartMenu : MonoBehaviour
{
    //[SerializeField] string MainSceneName = "Main";
    //[SerializeField] string MapSceneName = "Halves First Pass";
    [SerializeField] int MainSceneIndex = 0;
    [SerializeField] int MapSceneIndex = 2;

    [Header("Start Video")] // test on playing video right after clicking "start" button
    [SerializeField] private GameObject startVideoObject;
    [SerializeField] private VideoPlayer startVideoPlayer;
    [SerializeField] private GameObject activeButton;
    private bool hasStarted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (startVideoObject != null)
        {
            startVideoObject.SetActive(false);
        }

        if (startVideoPlayer != null)
        {
            startVideoPlayer.playOnAwake = false;
            startVideoPlayer.isLooping = false;
            startVideoPlayer.loopPointReached += OnStartVideoFinished;
        }
    }

    private void OnEnable()
    {
        if (activeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(activeButton);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Unload the video
    private void OnDestroy()
    {
        if (startVideoPlayer != null)
        {
            startVideoPlayer.loopPointReached -= OnStartVideoFinished;
        }
    }

    public IEnumerator LoadScene()
    {

        var sceneMain = SceneManager.LoadSceneAsync(MainSceneIndex, LoadSceneMode.Additive);
        sceneMain.allowSceneActivation = false;

        while (!sceneMain.isDone)
        {
            if (sceneMain.progress >= 0.9f)
            {
                break;
            }

            yield return null;
        }

        var sceneMap = SceneManager.LoadSceneAsync(MapSceneIndex, LoadSceneMode.Additive);
        sceneMap.allowSceneActivation = false;

        while (!sceneMap.isDone)
        {
            if (sceneMap.progress >= 0.9f)
            {
                break;
            }

            yield return null;
        }

        sceneMain.allowSceneActivation = true;
        sceneMap.allowSceneActivation = true;

        while (!sceneMap.isDone || !sceneMain.isDone)
        {
            yield return null;
        }

        // Only when the scene is loaded we can unload the orginally active screen
        var asyncUnload = SceneManager.UnloadSceneAsync("StartMenu");

        while (!asyncUnload.isDone)
        {
            if (asyncUnload.progress >= 0.9f)
            {
                UnityEngine.Debug.Log("Unloading...");
                break;
            }

            yield return null;
        }
    }

    public void StartGame()
    {
        // StartCoroutine(LoadScene());
        // this.enabled = false;

        // Updated to play video first right before loading next scene
        if (hasStarted)
            return;

        hasStarted = true;

        if (startVideoObject != null)
        {
            startVideoObject.SetActive(true);
        }

        if (startVideoPlayer != null)
        {
            startVideoPlayer.Play();
        }
        else
        {
            GameManager.Instance.StartGame();
        }
    }

    public void OpenInstructions()
    {

    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }

    /*
    private void OnStartVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(LoadScene());
    }*/

    private void OnStartVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(BeginGameAfterVideo());
    }

    private IEnumerator BeginGameAfterVideo()
    {
        if (startVideoPlayer != null)
        {
            startVideoPlayer.Stop();
        }

        if (startVideoObject != null)
        {
            startVideoObject.SetActive(false);
        }

        yield return null;

        GameManager.Instance.StartGame();
    }
}
