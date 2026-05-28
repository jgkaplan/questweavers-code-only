using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Incense : MonoBehaviour
{
    [SerializeField] private List<GameObject> glowObjects;
    [SerializeField] private List<VisualEffect> particles;

    private static event Action<Incense> AnIncenseTurnedOn;

    void Start()
    {
        UnlightIncense();
    }

    void OnEnable()
    {
        AnIncenseTurnedOn += OnOtherIncenseTurnedOn;
    }

    void OnDisable()
    {
        AnIncenseTurnedOn -= OnOtherIncenseTurnedOn;
    }

    void OnOtherIncenseTurnedOn(Incense other)
    {
        if (other == this) return;
        UnlightIncense();
    }
    public void LightIncense()
    {
        foreach (var o in glowObjects)
        {
            o.SetActive(true);
        }
        foreach (var p in particles)
        {
            p.Play();
        }
        AnIncenseTurnedOn.Invoke(this);
    }

    public void UnlightIncense()
    {
        foreach (var o in glowObjects)
        {
            o.SetActive(false);
        }
        foreach (var p in particles)
        {
            p.Stop();
        }
    }
}
