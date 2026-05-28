using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Flags]
public enum AudioHintFlags
{
    None = 0,
    Suspicious = 1, // Enemies that hear this audio hint get a stimulus
    ShareDetection = 2, // Source is an enemy who is trying to share detection/alert state to other unaware enemies.
    Distraction = 4, // This is a distraction caused by a firecracker. Some enemies will flee from it.
    IgnoreMist = 8, // Unaffected by mist
}

public struct AudioHintData
{
    public Vector3 Origin;
    public float Radius;
    /// <summary>
    /// Hint lasts this amount of time before being removed. Negative value results in infinite duration.
    /// </summary>
    public float Duration;
    public AudioHintFlags Flags;
    /// <summary>
    /// The player, enemy or object that caused the sound.
    /// </summary>
    public GameObject Source;
}

public class AudioHint : MonoBehaviour
{
    public static event Action<AudioHintData> AudioHintEmitted;
    /*
    public AudioHintData data;
    float durationLeft;
    new SphereCollider collider;
    private void Start()
    {
        durationLeft = data.Duration;
        collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        collider.radius = data.Radius;
        transform.position = data.Origin;
    }

    public void FixedUpdate()
    {
        var density = MistManager.instance.GetMistDensityAtPoint(transform.position);
        if ((data.Flags & AudioHintFlags.IgnoreMist) == AudioHintFlags.IgnoreMist) // TODO: hmm this seems backwards
        {
            collider.radius = data.Radius * Mathf.Lerp(1f, 0.5f, density);
        }

        if (data.Duration >= 0)
        {
            durationLeft -= Time.fixedDeltaTime;
            if (durationLeft <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
    */

    public static void Create(Vector3 origin, float radius, float duration = 0.1f, AudioHintFlags flags = AudioHintFlags.Suspicious, GameObject source = null)
    {
        AudioHintData data = new() { Flags = flags, Origin = origin, Duration = duration, Radius = radius, Source = source };
        if ((data.Flags & AudioHintFlags.IgnoreMist) == 0)
        {
            // Affected by mist
            var density = MistManager.instance.GetMistDensityAtPoint(origin);
            data.Radius = Mathf.Lerp(data.Radius, data.Radius / 2, density);
        }
        AudioHintEmitted.Invoke(data);
    }

    /*
        public static AudioHint Create(AudioHintData hintData)
        {
            var obj = new GameObject("AudioHint", new Type[] { typeof(AudioHint) });
            obj.layer = LayerMask.NameToLayer("Audio");
            var audioHint = obj.GetComponent<AudioHint>();
            audioHint.data = hintData;
            obj.transform.position = hintData.Origin;
            return audioHint;
        }
        public static AudioHint Create(Vector3 origin, float radius, float duration = 0.1f, AudioHintFlags flags = AudioHintFlags.Suspicious, GameObject source = null)
        {
            return Create(new AudioHintData { Flags = flags, Origin = origin, Duration = duration, Radius = radius, Source = source });
        }

        */
}
