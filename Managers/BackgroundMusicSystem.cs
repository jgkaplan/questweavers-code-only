using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(FMODUnity.StudioEventEmitter))]
public class BackgroundMusicSystem : MonoBehaviour
{
    [SerializeField, Range(0, 1), Tooltip("The current intensity of the music, from 0 (unnoticed) to 1 (pursuit)")]
    private float intensity = 0;
    [SerializeField, Range(0, 1), Tooltip("The target intensity of the music, from 0 (unnoticed) to 1 (pursuit)")]
    private float targetIntensity = 0;
    [SerializeField, Tooltip("Time in seconds to fade between different music intensities")] private float intensityFadeTime = 0.5f;
    private bool isInShrine = false;

    [SerializeField, ParamRef] private string intensityParameter;
    [SerializeField, ParamRef] private string playerMistDensityParameter;
    [SerializeField, ParamRef] private string pausedParameter;
    [SerializeField, ParamRef] private string inShrineParameter;
    [SerializeField] private bool enableBackgroundMusic = true;
    private FMOD.Studio.PARAMETER_ID _intensityId;
    private FMOD.Studio.PARAMETER_ID _densityId;
    private FMOD.Studio.PARAMETER_ID _pausedId;
    private FMOD.Studio.PARAMETER_ID _inShrineId;
    private FMODUnity.StudioEventEmitter soundSource;

    public static BackgroundMusicSystem instance;

    void OnEnable()
    {
        Player.PlayerMistDensityChanged.AddListener(ChangeListenerDensity);
    }

    void OnDisable()
    {
        Player.PlayerMistDensityChanged.RemoveListener(ChangeListenerDensity);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        soundSource = GetComponent<FMODUnity.StudioEventEmitter>();
        FMOD.Studio.PARAMETER_DESCRIPTION param_description;
        FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(intensityParameter, out param_description);
        _intensityId = param_description.id;
        FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(playerMistDensityParameter, out param_description);
        _densityId = param_description.id;
        FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(pausedParameter, out param_description);
        _pausedId = param_description.id;
        FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(inShrineParameter, out param_description);
        _inShrineId = param_description.id;
    }

    public void DoReset()
    {
        intensity = 0;
        targetIntensity = 0;
        FMODUnity.RuntimeManager.StudioSystem.setParameterByID(_intensityId, intensity);
        SetPlayerInShrineParam(false);
    }

    // Update is called once per frame
    void Update()
    {
        // UpdateMusicIntensity();// todo: switch this to only when intensity changes
        intensity = Mathf.MoveTowards(intensity, targetIntensity, Time.deltaTime * intensityFadeTime);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByID(_intensityId, intensity);
    }

    public void UpdateMusicIntensity()
    {
        AgentAlertness maxAlertness = AgentAlertness.Calm;
        foreach (var enemy in FindObjectsByType<AgentBrain>(FindObjectsSortMode.None))
        {
            // Hack: Treat panic state as if it's suspicious in terms of music intensity
            var enemyAlertness = enemy.Alertness == AgentAlertness.Panic ? AgentAlertness.Cautious : enemy.Alertness;
            maxAlertness = enemyAlertness > maxAlertness ? enemyAlertness : maxAlertness;
        }
        targetIntensity = (float)maxAlertness / (float)AgentAlertness.Alert;
    }

    void ChangeListenerDensity(float newDensity)
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByID(_densityId, newDensity);
    }
    public void ChangePauseParam(bool paused)
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByIDWithLabel(_pausedId, paused ? "Paused" : "Unpaused");
        FMODUnity.RuntimeManager.GetBus("bus:/In World").setPaused(paused);
    }

    public void SetPlayerInShrineParam(bool inShrine)
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByID(_inShrineId, inShrine ? 1 : 0);
    }

    public struct ParameterMap
    {
        public string parameter_name;
        public float value;
    }

    /// <summary>
    /// Play a one shot 3D sound, as affected by the mist in the world
    /// </summary>
    /// <param name="soundEvent">The sound to play</param>
    /// <param name="position">The world position the sound should be played in</param>
    /// <param name="extraParams">Optionally, a list of parameter names to values to set</param>
    public static void PlayOneShotSound(EventReference soundEvent, Vector3 position, ParameterMap[] extraParams = null)
    {
        FMOD.Studio.EventInstance eventInstance = InstantiateOneShotSound(soundEvent, position);
        // TPDO: also use movement speed as a parameter

        // eventInstance.setParameterByNameWithLabel(PLAYER_STEP_TYPE_PARAMETER, stepType);
        if (extraParams != null)
        {
            foreach (var param in extraParams)
            {
                eventInstance.setParameterByName(param.parameter_name, param.value);
            }
        }


        eventInstance.start();
        eventInstance.release();
    }

    /// <summary>
    /// Instantiate a one shot 3d sound, affected by the mist in the world.
    /// This is useful if you have to do weird parameter setting logic.
    /// This does not play or release the instance, and must be manually played with
    /// eventInstance.start();
    /// eventInstance.release();
    /// 
    /// </summary>
    /// <param name="soundEvent"></param>
    /// <param name="position"></param>
    /// <returns>An instantiation of the event.</returns>
    public static FMOD.Studio.EventInstance InstantiateOneShotSound(EventReference soundEvent, Vector3 position)
    {
        FMOD.Studio.EventInstance eventInstance = FMODUnity.RuntimeManager.CreateInstance(soundEvent);

        eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
        if (MistManager.instance != null)
        {
            eventInstance.setParameterByName("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(position));
        }
        return eventInstance;
    }

    // Using these until I know if we have different music for the menu
    public void StartGameMusic()
    {
        targetIntensity = 0;
        intensity = 0;
        UpdateMusicIntensity();
        if (enableBackgroundMusic)
        {
            soundSource.Play();
        }
    }

    public void StopGameMusic()
    {
        soundSource.AllowFadeout = true;
        soundSource.Stop();
    }
}
