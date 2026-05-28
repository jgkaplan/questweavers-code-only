using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveData saveData;

    public static SaveLoadingState Loaded = SaveLoadingState.NotLoaded;

#if UNITY_EDITOR
    // [SerializeField, Tooltip("Turn this off when testing in editor if you want to restart")] private bool useSaveFile = true;
    private const bool UseSaveFile = false; // Enable this when we want to have save file support in editor
#else
    private const bool UseSaveFile = false; // Enable this when we want to have save file support in build
#endif

    private static string persistentDataPath;

    const string SAVE_DATA_PATH = "savedata.json";

    private static CancellationTokenSource cts;


    public enum SaveLoadingState
    {
        LoadedFile,
        NewSaveCreated,
        NeedNewSave,
        NotLoaded
    }

    void OnEnable()
    {
        Checkpoint.activateCheckpoint.AddListener(OnActivateCheckpoint);
        MistZone.mistFOWTextureChange.AddListener(OnMistTextureChanged);
    }

    void OnDisable()
    {
        Checkpoint.activateCheckpoint.RemoveListener(OnActivateCheckpoint);
        MistZone.mistFOWTextureChange.RemoveListener(OnMistTextureChanged);
    }

    void OnDestroy()
    {
        CancelInProgressSave();
    }

    private void OnActivateCheckpoint(bool _isFirstTime, Transform checkpoint)
    {
        SetCheckpoint(checkpoint);
    }

    void Reset()
    {
        string savePath = Path.Join(persistentDataPath, SAVE_DATA_PATH);
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    static void CancelInProgressSave()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    static void NewCancellationToken()
    {
        cts ??= new CancellationTokenSource();
    }

    public static bool HasSaveData()
    {
        switch (Loaded)
        {
            case SaveLoadingState.LoadedFile:
                {
                    return true;
                }
            case SaveLoadingState.NewSaveCreated:
                {
                    return true;
                }
            case SaveLoadingState.NeedNewSave:
                {
                    return false;
                }
            case SaveLoadingState.NotLoaded:
                {
                    string path = Path.Join(persistentDataPath, SAVE_DATA_PATH);
                    return File.Exists(path);
                }
        }
        return false;
    }

    public static async Awaitable TryLoadSave()
    {
        persistentDataPath = Application.persistentDataPath;
        if (Loaded == SaveLoadingState.NotLoaded)
        {
            if (UseSaveFile)
            {
                saveData = await Load();
                if (saveData != null)
                {
                    Loaded = SaveLoadingState.LoadedFile;
                }
                else
                {
                    NewSave();
                }
            }
            else
            {
                NewSave();
            }
        }
        await Awaitable.EndOfFrameAsync();
    }

    public static void SetCheckpoint(Transform newCheckpoint)
    {
        saveData.currentCheckpointPosition = newCheckpoint.transform.position;
        saveData.currentCheckpointRotation = newCheckpoint.transform.rotation;
        string guid = newCheckpoint.GetComponent<GuidComponent>().GetGuid().ToString();
        saveData.currentCheckpointGUID = guid;
        if (!saveData.previouslyUnlockedCheckpoints.Contains(guid))
        {
            saveData.previouslyUnlockedCheckpoints.Add(guid);
        }
        if (Loaded == SaveLoadingState.NeedNewSave)
        {
            Loaded = SaveLoadingState.NewSaveCreated;
        }
        if (UseSaveFile)
        {
            CancelInProgressSave();
            NewCancellationToken();
            Save(cts.Token);
        }
    }

    public static bool HasUnlockedCheckpoint(Checkpoint checkpoint)
    {
        return saveData.previouslyUnlockedCheckpoints.Contains(checkpoint.GetComponent<GuidComponent>().GetGuid().ToString());
    }

    public static void GetAbility(PlayerAbility ability)
    {
        if (!saveData.unlockedAbilities.Contains(ability.AbilityName))
        {
            saveData.unlockedAbilities.Add(ability.AbilityName);
        }
        if (UseSaveFile)
        {
            CancelInProgressSave();
            NewCancellationToken();
            Save(cts.Token);
        }
    }

    public static void UnlockFirecrackers()
    {
        saveData.hasFirecrackersUnlocked = true;
        if (UseSaveFile)
        {
            CancelInProgressSave();
            NewCancellationToken();
            Save(cts.Token);
        }
    }

    public static void OnMistTextureChanged(Guid guid, int textureIndex)
    {
        if (saveData == null) return; //TODO: this might cause bugs. be careful
        saveData.mistZoneTextureIndexes.Add(guid.ToString(), textureIndex);
    }

    public static void UnlockCollectable(string name)
    {
        saveData.unlockedCollectables.Add(name);
        if (UseSaveFile)
        {
            CancelInProgressSave();
            NewCancellationToken();
            Save(cts.Token);
        }
    }

    public static bool HasCollectable(string name)
    {
        return saveData.unlockedCollectables.Contains(name);
    }

    /// <summary>
    /// Reset save to defaults
    /// </summary>
    public static void NewSave()
    {
        Loaded = SaveLoadingState.NeedNewSave;
        saveData = new();
    }

    public static async void Save(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested) return;
            // await Awaitable.BackgroundThreadAsync();
            /*
            SaveDataWriteable sdw = new()
            {
                currentCheckpointPosition = new(saveData.currentCheckpointPosition),
                currentCheckpointRotation = new(saveData.currentCheckpointRotation),
                unlockedAbilities = saveData.unlockedAbilities,
                mistZoneTextureIndexes = saveData.mistZoneTextureIndexes
            };
            var jsonString2 = JsonUtility.ToJson(sdw);
            var jsonString3 = JsonUtility.ToJson(saveData);
            Debug.Log($"Normal: {jsonString3}");
            Debug.Log($"Normal Serializable: {jsonString2}");
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(sdw);
            Debug.Log(jsonString);
            return;
            */
            var jsonString = JsonUtility.ToJson(saveData);
            string savePath = Path.Join(persistentDataPath, SAVE_DATA_PATH);
            if (cancellationToken.IsCancellationRequested) return;
            await File.WriteAllTextAsync(savePath, jsonString, cancellationToken);
            // TODO: add save icon
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Save was cancelled");
        }
    }

    static async Awaitable<SaveData> Load()
    {
        string savePath = Path.Join(persistentDataPath, SAVE_DATA_PATH);
        if (File.Exists(savePath))
        {
            string jsonString = await File.ReadAllTextAsync(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(jsonString);
            return data;
        }
        else
        {
            Debug.Log("[Save System]: Save file not found.");
            return null;
        }
    }
}

[Serializable]
public class SaveData
{
    public Vector3 currentCheckpointPosition;
    public Quaternion currentCheckpointRotation;
    public string currentCheckpointGUID;
    public List<string> unlockedAbilities;
    public bool hasFirecrackersUnlocked;
    public UDictionary<string, int> mistZoneTextureIndexes;
    public List<string> unlockedCollectables;
    public List<string> previouslyUnlockedCheckpoints;

    public SaveData()
    {
        currentCheckpointPosition = Vector3.zero;
        currentCheckpointRotation = Quaternion.identity;
        currentCheckpointGUID = Guid.Empty.ToString();
        unlockedAbilities = new();
        hasFirecrackersUnlocked = false;
        mistZoneTextureIndexes = new();
        unlockedCollectables = new();
        previouslyUnlockedCheckpoints = new();
    }
}

[Serializable]
public class SaveDataWriteable
{
    public CustomVector3 currentCheckpointPosition;
    public CustomQuaternion currentCheckpointRotation;
    public List<string> unlockedAbilities;
    public bool hasFirecrackersUnlocked;
    public UDictionary<string, int> mistZoneTextureIndexes;
    public List<string> unlockedCollectables;
}

[Serializable]
public class CustomVector3
{
    public readonly float x;
    public readonly float y;
    public readonly float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public CustomVector3(Vector3 from)
    {
        x = from.x;
        y = from.y;
        z = from.z;
    }
}

[Serializable]
public class CustomQuaternion
{
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly float w;

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public CustomQuaternion(Quaternion from)
    {
        x = from.x;
        y = from.y;
        z = from.z;
        w = from.w;
    }
}