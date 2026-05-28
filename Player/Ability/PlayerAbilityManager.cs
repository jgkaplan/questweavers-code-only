using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Cinemachine.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerAbilityManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] List<PlayerAbility> abilities;
    [SerializeField] PlayerCameraRig playerCameraRig;
    [SerializeField] private float aimingTimeScale = 0.3f;
    [SerializeField] private MistMeter mistMeter;
    [SerializeField] private GameObject gourd;
    #endregion

    #region Public Fields
    public PlayerAbility CurrentAbility
    {
        get
        {
            return abilities.Count > abilityIndex ? abilities[abilityIndex] : null;
        }
        set
        {
            abilityIndex = abilities.IndexOf(value);
            CurrentAbilityChanged.Invoke(abilities[abilityIndex]);
        }
    }

    public Transform AbilityPresets;

    /// <summary>
    /// Whether the player is currently in the aiming state.
    /// </summary>
    public bool IsAiming { get; private set; }

    /// <summary>
    /// Whether the ability is currently being casted. Only set for Continuous abilities.
    /// </summary>
    public bool IsCasting { get; private set; }

    /// <summary>
    /// Temporary object used to indicate cast position and other information while aiming.
    /// </summary>
    public GameObject CastIndicator { get; private set; }

    /// <summary>
    /// The resource the player has for using mist abilities. This is 
    /// </summary>
    public float MistResource
    {
        get => _mistResource;
        set
        {
            _mistResource = Mathf.Clamp(value, 0, MaxMist);
            MistResourceChanged.Invoke(_mistResource / MaxMist);
        }
    }

    public float MaxMist = 100;

    #endregion

    #region Private Fields
    int abilityIndex = 0;

    bool castAllowed = false;
    Vector3 castPosition = Vector3.zero;
    Vector3 castDirection = Vector3.zero;
    Vector3 castDirection2D = Vector3.zero;
    float lastCastTime = 0f;
    private float _mistResource = 100;
    PlayerCameraRig.CameraMode lastCameraMode;
    Animator animator;

    // Input variables
    float aimInput = 0f;
    float castInput = 0f;

    private Analytics.AbilityUsedEvent analyticsAbilityUsed;
    #endregion

    #region Events
    // Event that fires whenever the amount of mist resource changes
    // It passes in the new amount of mist we have, in range [0,1]
    public static UnityEvent<float> MistResourceChanged = new();
    public static UnityEvent<PlayerAbility> CurrentAbilityChanged = new();
    #endregion

    #region Start and Update
    private void Start()
    {
        Checkpoint.activateCheckpoint.AddListener((_, _) => MistResource = MaxMist);
        animator = GetComponent<Animator>();
    }

    public void DoReset()
    {
        MistResource = MaxMist;
        // mistMeter.Hide();
        if (abilities.Count > 0)
        {
            mistMeter.Show();
            SelectAbility(abilities.FindIndex(a => a.AbilityName == "Create Mist"));
            gourd.SetActive(true);
        }
        else
        {
            mistMeter.Hide();
            gourd.SetActive(false);
        }
    }
    private void Update()
    {
        if (IsAiming)
        {
            Time.timeScale = aimingTimeScale;
            UpdateCastTarget();
            CurrentAbility.AimThink(CastIndicator);

            var qB = Quaternion.LookRotation(castDirection2D, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, qB, Damper.Damp(1, 0.5f, Time.deltaTime));

        }
        animator.SetBool("Aiming", IsAiming);
    }

    void FixedUpdate()
    {
        // Handle continuous casting
        if (IsAiming && IsCasting && CurrentAbility.CastMode == AbilityCastMode.Continuous)
        {
            if (CanCastCurrentAbility() && castInput > 0)
            {
                if (Time.time - lastCastTime >= CurrentAbility.CastCooldown)
                {
                    CurrentAbility.Cast(castPosition, castDirection);
                    ConsumeCastResources();
                }
            }
            else
            {
                FinishContinuousCastAndLog();
            }
        }
    }

    void FinishContinuousCastAndLog()
    {
        CurrentAbility.OnCastStop(castPosition, castDirection);
        analyticsAbilityUsed.FinalResourceLevel = MistResource;
        analyticsAbilityUsed.Record();
        IsCasting = false;
        lastCastTime = Time.time;
    }
    #endregion

    #region Targetting
    /// <summary>
    /// Updates the current ability's target and normal, and checks if this position is valid.
    /// </summary>
    void UpdateCastTarget()
    {
        if (CurrentAbility == null)
        {
            return;
        }

        if (CurrentAbility.TargetMode == AbilityTargetMode.Self)
        {
            castPosition = transform.position;
            castDirection = Vector3.up;
        }
        else if (CurrentAbility.TargetMode == AbilityTargetMode.Raycast)
        {
            bool collided = false;
            RaycastHit hit;
            Vector3 worldPos = Vector3.zero;
            if (playerCameraRig.Mode == PlayerCameraRig.CameraMode.Aim)
            {
                // CinemachineThirdPersonAim has its own internal variables for distance and collision filter.
                // We update this when we start aiming, assuming the player cannot change abilities while aiming.
                CinemachineThirdPersonAim aimer = playerCameraRig.AimCamera.GetComponent<CinemachineThirdPersonAim>();
                worldPos = aimer.AimTarget;
            }
            else if (playerCameraRig.Mode == PlayerCameraRig.CameraMode.FirstPerson)
            {
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                collided = RuntimeUtility.RaycastIgnoreTag(ray,
                    out hit, CurrentAbility.RaycastDistance, CurrentAbility.RaycastMask, "Player");
                worldPos = collided ? hit.point : Camera.main.transform.position + Camera.main.transform.forward * CurrentAbility.RaycastDistance;
            }

            if (CurrentAbility.RaycastGrounded)
            {
                // FIXME: This solution will allow players to cast grounded abilities through walls by aiming above them!
                RaycastHit hit2;
                if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out hit2, Mathf.Infinity, CurrentAbility.RaycastMask, QueryTriggerInteraction.Ignore))
                {
                    worldPos = hit2.point;
                }
            }

            castPosition = worldPos;
            castDirection = (worldPos - Camera.main.transform.position).normalized;
        }

        castDirection2D = new Vector3(castDirection.x, 0, castDirection.z);

        CurrentAbility.CorrectCastTarget(ref castPosition, ref castDirection);

        castAllowed = CurrentAbility.CanCast(castPosition, castDirection);

        if (CastIndicator != null)
        {
            CastIndicator.transform.position = castPosition;
            CastIndicator.transform.rotation = Quaternion.LookRotation(castDirection, Vector3.up);
            // does indicator normal matter?
        }
    }

    /// <summary>
    /// Checks all conditions and costs for casting the current ability.
    /// </summary>
    /// <returns>Whether the current ability can be cast.</returns>
    public bool CanCastCurrentAbility()
    {
        return CurrentAbility != null && castAllowed
            && ((CurrentAbility.CastMode == AbilityCastMode.Continuous && IsCasting)
            || (Time.time - lastCastTime >= CurrentAbility.CastCooldown))
            && (Mathf.Infinity > GetActualCastCost()); // TODO implement cost.
    }
    #endregion

    #region Aiming and Casting
    void StartAiming(PlayerAbility newAbility = null)
    {
        if (IsAiming)
        {
            return;
        }

        if (newAbility != null && abilities.Contains(newAbility))
        {
            CurrentAbility = newAbility;
        }

        // mistMeter.Show();

        lastCameraMode = playerCameraRig.Mode;

        if (playerCameraRig.Mode != PlayerCameraRig.CameraMode.FirstPerson)
        {
            playerCameraRig.Mode = PlayerCameraRig.CameraMode.Aim;
        }

        // CinemachineThirdPersonAim has its own internal variables for distance and collision filter.
        // We update this when we start aiming, assuming the player cannot change abilities while aiming.
        CinemachineThirdPersonAim aimer = playerCameraRig.AimCamera.GetComponent<CinemachineThirdPersonAim>();
        if (aimer != null)
        {
            aimer.AimDistance = CurrentAbility.RaycastDistance;
            aimer.AimCollisionFilter = CurrentAbility.RaycastMask;
        }

        if (CurrentAbility.IndicatorPrefab != null)
        {
            CastIndicator = Instantiate(CurrentAbility.IndicatorPrefab);
        }
        UpdateCastTarget();
        CurrentAbility.OnAimStart(CastIndicator);

        var animator = GetComponent<Animator>();
        animator.SetInteger("AbilityState", CurrentAbility.AbilityAnimatorIndex);

        GetComponent<Player>().SetStrafe(true);

        IsAiming = true;
    }

    public void StopAiming()
    {
        if (!IsAiming)
        {
            return;
        }

        if (IsCasting)
        {
            if (CurrentAbility.CastMode == AbilityCastMode.Continuous)
            {
                FinishContinuousCastAndLog();
            }
            else
            {
                CurrentAbility.OnCastStop(castPosition, castDirection);
                IsCasting = false;
            }
        }
        // mistMeter.Hide();
        CurrentAbility.OnAimStop();

        if (CastIndicator != null)
        {
            Destroy(CastIndicator);
        }

        playerCameraRig.Mode = lastCameraMode;

        IsAiming = false;
        Time.timeScale = 1.0f;

        var animator = GetComponent<Animator>();
        animator.SetInteger("AbilityState", 0);

        GetComponent<Player>().SetStrafe(false);
    }

    /// <summary>
    /// Calculates the current casting cost. For continuous abilities, this returns the cost per tick.
    /// </summary>
    /// <returns>Cost of casting.</returns>
    float GetActualCastCost()
    {
        if (CurrentAbility == null)
        {
            return Mathf.Infinity;
        }
        var cost = CurrentAbility.GetCastCost();
        // if (CurrentAbility.CastMode == AbilityCastMode.Continuous)
        // {
        //     cost *= CurrentAbility.CastCooldown;
        // }
        return cost;
    }

    /// <summary>
    /// Consumes the cast cost.
    /// </summary>
    void ConsumeCastResources()
    {
        if (CurrentAbility == null)
        {
            return;
        }

        var cost = GetActualCastCost();
        MistResource -= cost;
    }
    #endregion

    #region Ability Management
    public void AddAbility(PlayerAbility ability)
    {
        if (abilities.Contains(ability))
        {
            return;
        }
        abilities.Add(ability);
        mistMeter.Show();
        gourd.SetActive(true);
        SaveSystem.GetAbility(ability);
    }

    public void AddAbilityFromPreset(string abilityName)
    {
        foreach (Transform child in AbilityPresets)
        {
            if (child.TryGetComponent(out PlayerAbility ability))
            {
                if (ability.AbilityName == abilityName)
                {
                    AddAbility(ability);
                    return;
                }
            }
        }
        Debug.LogWarning("Didn't find ability with that name");
    }

    public void LoadAbilitiesFromSave()
    {
        List<string> unlockedAbilities = SaveSystem.saveData.unlockedAbilities;
        abilities.Clear();
        foreach (Transform child in AbilityPresets)
        {
            if (child.TryGetComponent(out PlayerAbility ability))
            {
                if (unlockedAbilities.Contains(ability.AbilityName))
                {
                    abilities.Add(ability);
                }
            }
        }

        foreach (AbilityPickup pickup in FindObjectsByType<AbilityPickup>(FindObjectsSortMode.None))
        {
            if (unlockedAbilities.Contains(pickup.abilityName))
            {
                pickup.gameObject.SetActive(false);
            }
        }
    }

    public void RemoveAbility(PlayerAbility ability)
    {
        abilities.Remove(ability);
        // TODO handle abilityIndex shifting because of this!
        // TODO handle removing the active ability
    }

    public bool SelectAbility(int index)
    {
        if (index < abilities.Count && index >= 0 && abilities[index] != null)
        {
            // TODO: Handle changing abilities while currently aiming!
            abilityIndex = index;
            CurrentAbilityChanged.Invoke(abilities[abilityIndex]);
            return true;
        }
        return false;
    }

    public void CycleAbility()
    {
        if (abilities.Count > 0)
        {
            StopAiming();
            abilityIndex = (abilityIndex + 1) % abilities.Count;
            CurrentAbilityChanged.Invoke(abilities[abilityIndex]);
            if (aimInput > 0f)
            {
                StartAiming();
            }
        }
    }

    public bool HasAbility(string abilityName)
    {
        return abilities.Exists(a => a.AbilityName == abilityName);
    }
    #endregion

    #region Input Handling
    public void OnInputAim(InputAction.CallbackContext context)
    {
        aimInput = context.action.ReadValue<float>();

        // Start aiming and create indicator
        if (!IsAiming && CurrentAbility != null && aimInput > 0f)
        {
            StartAiming();
        }

        // Release aiming (and casting if that was happening)
        if (IsAiming && aimInput <= 0f)
        {
            StopAiming();
        }
    }


    public void OnInputCast(InputAction.CallbackContext context)
    {
        castInput = context.action.ReadValue<float>();

        if (!IsAiming)
        {
            return;
        }

        if (castInput > 0 && !IsCasting && CurrentAbility.CastMode == AbilityCastMode.Continuous && CanCastCurrentAbility())
        {
            // Continuous cast release is handled in FixedUpdate
            CurrentAbility.OnCastStart(castPosition, castDirection);
            analyticsAbilityUsed = new()
            {
                AbilityName = CurrentAbility.AbilityName,
                PlayerXPosition = transform.position.x,
                PlayerYPosition = transform.position.y,
                PlayerZPosition = transform.position.z,
                AbilityXPosition = castPosition.x,
                AbilityYPosition = castPosition.y,
                AbilityZPosition = castPosition.z,
                StartingResourceLevel = MistResource // TODO: this doesn't work with non-mist things
            };
            IsCasting = true;
        }

        if (castInput > 0 && CurrentAbility.CastMode == AbilityCastMode.Instant && CanCastCurrentAbility())
        {
            // TODO: I'm not sure if instant casting feels better on press or on release
            CurrentAbility.Cast(castPosition, castDirection);
            analyticsAbilityUsed = new()
            {
                AbilityName = CurrentAbility.AbilityName,
                PlayerXPosition = transform.position.x,
                PlayerYPosition = transform.position.y,
                PlayerZPosition = transform.position.z,
                AbilityXPosition = castPosition.x,
                AbilityYPosition = castPosition.y,
                AbilityZPosition = castPosition.z,
                StartingResourceLevel = MistResource // TODO: this doesn't work with non-mist things
            };
            ConsumeCastResources();
            analyticsAbilityUsed.FinalResourceLevel = MistResource;
            analyticsAbilityUsed.Record();
            lastCastTime = Time.time;
        }
    }
    #endregion
}
