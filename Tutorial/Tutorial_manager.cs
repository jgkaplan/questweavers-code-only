using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialImageEvent
{
    [Header("References")]
    public string imageId;
    public Image overlay;

    [Header("Timing")]
    [Min(0f)] public float startDelay = 0f;
    [Min(0f)] public float fadeInDuration = 0.25f;

    [Header("Early Hide")]
    public bool hideEarly = false;
    [Min(0f)] public float hideTime = 0f;
    [Min(0f)] public float fadeOutDuration = 0.25f;

    [Header("Carry-Over")]
    public bool persistAcross = false;
    public string stopPersisting = "";
}

[Serializable]
public class TutorialPackage
{
    public string packageId;
    public List<TutorialImageEvent> images = new();
    public string collectableUnlock;
}

public class Tutorial_manager : MonoBehaviour
{
    [SerializeField] private Transform overlayRoot;
    [SerializeField] private bool useUnscaledTime = true;

    private TutorialPackage currentPackage;
    private TutorialPackage requestedNextPackage;

    private bool isTransitioning = false;
    private int activePackageToken = 0;

    private readonly List<Coroutine> pendingSpawnRoutines = new();
    private readonly List<ActiveOverlay> activeOverlays = new();

    private static Tutorial_manager instance;
    public static Tutorial_manager Instance => instance;

    private readonly TutorialPackage empty = new() { packageId = "__empty" };
    private bool playerInZone = false; // True if the player is currently in a tutorial zone

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (overlayRoot == null)
        {
            Debug.LogError("Tutorial_manager: Overlay Root is not assigned.");
            return;
        }

        ResetAllSceneOverlays();
    }

    public void RequestPackage(TutorialPackage package)
    {
        if (package == null)
            return;

        if (currentPackage != null && currentPackage.packageId == package.packageId && !isTransitioning)
            return;

        if (requestedNextPackage != null && requestedNextPackage.packageId == package.packageId)
            return;

        if (currentPackage == null && !isTransitioning)
        {
            StartPackage(package);
            return;
        }

        requestedNextPackage = package;

        if (!isTransitioning)
        {
            StartCoroutine(TransitionToRequestedPackage());
        }
    }

    public void PlayerInTutorialZone()
    {
        playerInZone = true;
    }

    public async void PlayerLeavingTutorialZone()
    {
        playerInZone = false;
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        if (!playerInZone) RequestPackage(empty);
    }

    private void StartPackage(TutorialPackage package)
    {
        currentPackage = package;
        activePackageToken++;

        StopPendingSpawnRoutines();

        if (currentPackage == null || currentPackage.images == null)
            return;

        for (int i = 0; i < currentPackage.images.Count; i++)
        {
            TutorialImageEvent imageEvent = currentPackage.images[i];
            Coroutine routine = StartCoroutine(ShowImageAfterDelay(activePackageToken, currentPackage, imageEvent));
            pendingSpawnRoutines.Add(routine);
        }
        if (package.collectableUnlock != null && package.collectableUnlock != "" && !SaveSystem.HasCollectable(package.collectableUnlock))
        {
            SaveSystem.UnlockCollectable(package.collectableUnlock);
        }
    }

    private IEnumerator ShowImageAfterDelay(int packageToken, TutorialPackage ownerPackage, TutorialImageEvent imageEvent)
    {
        if (imageEvent == null || imageEvent.overlay == null)
            yield break;

        if (imageEvent.startDelay > 0f)
            yield return WaitSeconds(imageEvent.startDelay);

        if (packageToken != activePackageToken)
            yield break;

        if (currentPackage == null || ownerPackage == null || currentPackage.packageId != ownerPackage.packageId)
            yield break;

        ActivateSceneOverlay(ownerPackage, imageEvent);
    }

    private void ActivateSceneOverlay(TutorialPackage ownerPackage, TutorialImageEvent imageEvent)
    {
        Image target = imageEvent.overlay;
        if (target == null)
            return;

        ActiveOverlay existing = FindActiveOverlay(target);

        if (existing != null)
        {
            return;
        }

        target.gameObject.SetActive(true);
        SetImageAlpha(target, 0f);
        target.transform.SetAsLastSibling();

        ActiveOverlay overlay = new ActiveOverlay
        {
            ownerPackageId = ownerPackage.packageId,
            definition = imageEvent,
            image = target
        };

        activeOverlays.Add(overlay);
        overlay.lifetimeRoutine = StartCoroutine(RunOverlayLifetime(overlay));
    }

    private IEnumerator RunOverlayLifetime(ActiveOverlay overlay)
    {
        yield return FadeImageAlpha(overlay.image, 0f, 1f, overlay.definition.fadeInDuration);

        if (overlay.definition.hideEarly)
        {
            float waitAfterSpawn = Mathf.Max(0f, overlay.definition.hideTime - overlay.definition.startDelay);

            if (waitAfterSpawn > 0f)
                yield return WaitSeconds(waitAfterSpawn);

            if (overlay != null && overlay.image != null)
            {
                yield return FadeOutAndDisableOverlay(overlay, overlay.definition.fadeOutDuration);
            }
        }
    }

    private IEnumerator TransitionToRequestedPackage()
    {
        isTransitioning = true;

        StopPendingSpawnRoutines();

        TutorialPackage nextPackage = requestedNextPackage;
        if (nextPackage == null)
        {
            isTransitioning = false;
            yield break;
        }

        List<ActiveOverlay> overlaysToFadeOut = new();

        for (int i = 0; i < activeOverlays.Count; i++)
        {
            ActiveOverlay overlay = activeOverlays[i];
            if (overlay == null || overlay.image == null)
                continue;

            bool shouldPersist = ShouldPersistIntoNextPackage(overlay, nextPackage.packageId);

            if (!shouldPersist)
            {
                overlaysToFadeOut.Add(overlay);
            }
        }

        for (int i = 0; i < overlaysToFadeOut.Count; i++)
        {
            ActiveOverlay overlay = overlaysToFadeOut[i];

            if (overlay.lifetimeRoutine != null)
                StopCoroutine(overlay.lifetimeRoutine);

            overlay.lifetimeRoutine = StartCoroutine(FadeOutAndDisableOverlay(overlay, overlay.definition.fadeOutDuration));
        }

        while (AnyOverlayStillActive(overlaysToFadeOut))
        {
            yield return null;
        }

        activePackageToken++;
        currentPackage = null;

        TutorialPackage packageToStart = requestedNextPackage;
        requestedNextPackage = null;

        isTransitioning = false;

        if (packageToStart != null)
        {
            StartPackage(packageToStart);
        }
    }

    private bool ShouldPersistIntoNextPackage(ActiveOverlay overlay, string nextPackageId)
    {
        if (overlay == null || overlay.definition == null)
            return false;

        if (!overlay.definition.persistAcross)
            return false;

        if (string.IsNullOrWhiteSpace(overlay.definition.stopPersisting))
            return true;

        return !string.Equals(
            overlay.definition.stopPersisting,
            nextPackageId,
            StringComparison.Ordinal
        );
    }

    private IEnumerator FadeOutAndDisableOverlay(ActiveOverlay overlay, float duration)
    {
        if (overlay == null || overlay.image == null)
            yield break;

        float startAlpha = overlay.image.color.a;
        yield return FadeImageAlpha(overlay.image, startAlpha, 0f, duration);

        if (overlay.image != null)
        {
            overlay.image.gameObject.SetActive(false);
        }

        if (activeOverlays.Contains(overlay))
        {
            activeOverlays.Remove(overlay);
        }
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        if (duration <= 0f)
        {
            SetImageAlpha(image, to);
            yield break;
        }

        float elapsed = 0f;
        SetImageAlpha(image, from);

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetImageAlpha(image, to);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += DeltaTime();
            yield return null;
        }
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void StopPendingSpawnRoutines()
    {
        for (int i = 0; i < pendingSpawnRoutines.Count; i++)
        {
            if (pendingSpawnRoutines[i] != null)
                StopCoroutine(pendingSpawnRoutines[i]);
        }

        pendingSpawnRoutines.Clear();
    }

    private bool AnyOverlayStillActive(List<ActiveOverlay> overlays)
    {
        for (int i = 0; i < overlays.Count; i++)
        {
            if (activeOverlays.Contains(overlays[i]))
                return true;
        }

        return false;
    }

    private ActiveOverlay FindActiveOverlay(Image target)
    {
        for (int i = 0; i < activeOverlays.Count; i++)
        {
            if (activeOverlays[i].image == target)
                return activeOverlays[i];
        }

        return null;
    }

    private void ResetAllSceneOverlays()
    {
        Image[] allImages = overlayRoot.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < allImages.Length; i++)
        {
            SetImageAlpha(allImages[i], 0f);
            allImages[i].gameObject.SetActive(false);
        }
    }

    private class ActiveOverlay
    {
        public string ownerPackageId;
        public TutorialImageEvent definition;
        public Image image;
        public Coroutine lifetimeRoutine;
    }
}