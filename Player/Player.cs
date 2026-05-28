using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MonobehaviourFSM))]
public class Player : MonoBehaviour
{
    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("Layers to include in ground detection via Raycasts.")]
    [SerializeField] private LayerMask GroundLayers = 1;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Header("Noise")]
    [Tooltip("Range of noise made when walking")]
    public float AudioHintRadiusWalk = 1.5f;

    [Tooltip("Range of noise made when sprinting")]
    public float AudioHintRadiusSprint = 5f;

    [Tooltip("Range of noise made when crouch walking")]
    public float AudioHintRadiusCrouch = 0.5f;

    [Tooltip("Range of noise made when landing from a fall")]
    public float AudioHintRadiusLand = 5f;
    [SerializeField] private FMODUnity.EventReference footstepSound;
    [SerializeField] private FMODUnity.EventReference landingSound;
    const string PLAYER_STEP_TYPE_PARAMETER = "PlayerStepType";
    public FMODUnity.StudioEventEmitter prayStartSound;
    public FMODUnity.StudioEventEmitter prayStopSound;

    [Header("Other")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] public Renderer playerInMistGlowIndicator;
    [SerializeField] private Material screenspaceEffectInMist;
    private MonobehaviourFSM fsm;
    private MonobehaviourState beforePauseState;

    [Header("Sprint Energy")]
    [SerializeField, Tooltip("Total energy we have for sprinting before exhausting")] private float maxSprintEnergy = 3;
    [SerializeField, Tooltip("Energy drain per secon while sprinting")] private float sprintEnergyDrainPerSecond = 1;
    [SerializeField, Tooltip("Energy drain multiplier when in mist")] private float sprintMistEnergyDrainMultiplier = 1.75f;
    [SerializeField, Tooltip("Sprint energy restored per second")] private float sprintRestorePerSecond = 0.5f;
    [SerializeField, Tooltip("How long should we wait before starting sprint energy recovery")] private float sprintRecoveryDelay = 0.5f;
    [SerializeField, Tooltip("Sprint energy restored per second when burned out")] private float sprintBurnedOutRestorePerSecond = 1f;
    private float currentSprintEnergy = 3;
    private float sprintRecoveryDelayTimer = 0f;
    private bool burnedOut = false;
    public float CurrentSprintPercentRemaining => currentSprintEnergy / maxSprintEnergy;
    public bool IsBurnedOut
    {
        get => burnedOut;
    }

    private Interactable _interactable;
    public Interactable CurrentInteractable
    {
        get
        {
            return _interactable;
        }
        set
        {
            interactionPrompt.SetActive(value != null);
            _interactable = value;
        }
    }

    public static Player instance;
    // Animator params
    [HideInInspector] public Animator animator;
    private CharacterController cc;
    [HideInInspector] public bool _hasAnimator;
    [HideInInspector] public int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    [HideInInspector] public int _animIDMotionSpeed;
    [HideInInspector] public float _animationBlend;
    // Movement params
    [HideInInspector] public float _verticalVelocity;
    [HideInInspector] public Vector3 currentVelocityXY = Vector3.zero;
    private float _terminalVelocity = 53.0f;
    private float _fallTimeoutDelta;
    [HideInInspector] public bool IsSprinting = false;
    [HideInInspector] public bool IsCrouching = false;
    public bool IsCastMode => abilityManager.IsAiming;

    [Header("Cameras")]
    public PlayerCameraRig playerCameraRig;
    public Transform playerAimCore;
    public Transform freeCameraRoot;
    public Transform headCameraRoot;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private float _headYaw = 0f;
    private float _headPitch = 0f;

    private float _currentMistDensity = 0;
    public float CurrentMistDensity
    {
        get => _currentMistDensity; protected set
        {
            if (Mathf.Approximately(value, _currentMistDensity))
            {
                PlayerMistDensityChanged.Invoke(value);
            }
            _currentMistDensity = value;
        }
    }

    public PlayerInputActions pInputActions { get; private set; }
    private PlayerAbilityManager abilityManager;
    private FirecrackerAbility firecrackerAbility;

    public static UnityEvent<float> PlayerMistDensityChanged = new();


    void Awake()
    {
        AssignAnimationIDs();

        _hasAnimator = TryGetComponent(out animator);
        cc = GetComponent<CharacterController>();
        fsm = GetComponent<MonobehaviourFSM>();
        abilityManager = GetComponent<PlayerAbilityManager>();
        firecrackerAbility = GetComponent<FirecrackerAbility>();

        pInputActions = new();
        instance = this;
    }
    public void Start()
    {
        interactionPrompt.SetActive(false);

        pInputActions.Testing.AimCamera.performed += (e) => playerCameraRig.Mode = PlayerCameraRig.CameraMode.Aim;
        pInputActions.Testing.CloseCamera.performed += (e) => playerCameraRig.Mode = PlayerCameraRig.CameraMode.ThirdPerson;
        pInputActions.Testing.FreeCamera.performed += (e) => playerCameraRig.Mode = PlayerCameraRig.CameraMode.Freelook;
        pInputActions.Testing.FirstPersonCamera.performed += (e) => playerCameraRig.Mode = PlayerCameraRig.CameraMode.FirstPerson;

        pInputActions.Player.Aim.performed += abilityManager.OnInputAim;
        pInputActions.Player.Aim.canceled += abilityManager.OnInputAim;

        pInputActions.Player.UseFirecracker.performed += firecrackerAbility.OnInputAim;
        pInputActions.Player.UseFirecracker.canceled += firecrackerAbility.OnInputAim;

        pInputActions.Player.CastMist.performed += abilityManager.OnInputCast;
        pInputActions.Player.CastMist.canceled += abilityManager.OnInputCast;

        pInputActions.Testing.SwapAbilities.performed += (e) => abilityManager.CycleAbility();
        // pInputActions.Testing.SwapAbilities.performed += (e) => { if (t) { animator.SetTrigger("PlayerDie"); } else { animator.SetTrigger("PlayerRespawn"); } t = !t; };

        pInputActions.Camera.Enable();
    }



    public bool IsAlive()
    {
        // return fsm.currentState.GetType().Name == typeof(MoveState).Name;
        return fsm.currentState.StateName == "MoveState";
        /*
        return fsm.currentState.GetType().Name switch {
            typeof(MoveState).Name => true,
            _ => false
        };
        */
    }

    public bool IsInState(Type stateType)
    {
        return fsm.currentState.GetType() == stateType;
    }

    public void OnPause()
    {
        beforePauseState = fsm.currentState;
        fsm.ChangeState<PausedState>();
        //SendMessage("OnGamePause", true);
    }

    public void OnUnpause()
    {
        if (fsm.currentState == null || fsm.currentState.GetType() != typeof(PausedState)) return; // start of game unpause
        fsm.ChangeState(beforePauseState);
        beforePauseState = null;
        //SendMessage("OnGamePause", false);
    }

    public void OnReset()
    {
        CurrentInteractable = null;
        HardMoveTransformToCheckpoint();
        animator.ResetControllerState();
        fsm.ChangeState<LoadingState>();
    }

    public void OnSceneLoaded()
    {
        fsm.ChangeState<RespawningState>();
    }

    public void StartPraying()
    {
        animator.SetBool("Praying", true);
        prayStartSound.Play();
        //fsm.ChangeState<PrayState>();
    }

    public void StopPraying()
    {
        animator.SetBool("Praying", false);
        prayStopSound.Play();
    }

    public void OnGoToMenu()
    {
        fsm.ChangeState<LoadingState>();
    }

    public void OnEnterPromptState()
    {
        Debug.Log("Called from somewhere");
        /*
        if (fsm.currentState.GetType() != typeof(MoveState))
        {
            Debug.LogError("Entering advice state from unexpected state " + fsm.currentState.GetType().ToString());
            return;
        }
        fsm.ChangeState<PromptState>();
        */
    }

    public void OnExitPromptState()
    {
        /*
        if (fsm.currentState.GetType() != typeof(PromptState))
        {
            Debug.LogError("Exiting advice state from unexpected state " + fsm.currentState.GetType().ToString());
            return;
        }
        fsm.ChangeState<MoveState>();
        */
    }

    void Update()
    {
        // Doing these here so they work across all states

        // MovePlayer();
        screenspaceEffectInMist.SetFloat("_Amount", Mathf.MoveTowards(screenspaceEffectInMist.GetFloat("_Amount"), 1 - Mathf.Pow(1 - CurrentMistDensity, 4), Time.deltaTime));
        RecoverSprintEnergy();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void RecoverSprintEnergy()
    {
        if (!IsSprinting || pInputActions.Player.Move.ReadValue<Vector2>() == Vector2.zero)
        {
            if (sprintRecoveryDelayTimer < sprintRecoveryDelay)
            {
                sprintRecoveryDelayTimer += Time.deltaTime;
            }
            else
            {
                currentSprintEnergy = Mathf.MoveTowards(currentSprintEnergy, maxSprintEnergy, Time.deltaTime * (burnedOut ? sprintBurnedOutRestorePerSecond : sprintRestorePerSecond));
                if (Mathf.Approximately(currentSprintEnergy, maxSprintEnergy))
                {
                    burnedOut = false;
                }
            }
        }
    }

    public void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        CurrentMistDensity = MistManager.instance.GetMistDensityAtPoint(transform.position);
        // update animator if using character
        if (_hasAnimator)
        {
            animator.SetBool(_animIDGrounded, Grounded);
            animator.SetFloat("Mist", CurrentMistDensity);
        }
    }

    // Call in update to handle player movement
    public void MovePlayer()
    {
        GroundedCheck();
        DoGravity();
        cc.Move(currentVelocityXY * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        if (IsSprinting && pInputActions.Player.Move.ReadValue<Vector2>() != Vector2.zero)
        {
            sprintRecoveryDelayTimer = 0;
            float energyDrain = Mathf.Lerp(sprintEnergyDrainPerSecond, sprintEnergyDrainPerSecond * sprintMistEnergyDrainMultiplier, CurrentMistDensity);
            currentSprintEnergy = Mathf.MoveTowards(currentSprintEnergy, 0, Time.deltaTime * energyDrain);
            if (Mathf.Approximately(currentSprintEnergy, 0))
            {
                burnedOut = true;
                IsSprinting = false;
            }
        }
        // _animationBlend = Mathf.Lerp(_animationBlend, currentVelocityXY.magnitude, Time.deltaTime * SpeedChangeRate);
        // if (_animationBlend < 0.01f) _animationBlend = 0f;

        if (_hasAnimator)
        {
            animator.SetFloat(_animIDSpeed, _animationBlend);

            animator.SetFloat("SpeedForward", currentVelocityXY.x);
            animator.SetFloat("SpeedSide", currentVelocityXY.z);
        }
    }

    public void DoGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                animator.SetBool(_animIDJump, false);
                animator.SetBool(_animIDFreeFall, false);
            }
            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
        }
        else
        {
            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    animator.SetBool(_animIDFreeFall, true);
                }
            }
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }


    public void HardMoveTransform(Vector3 pos, Quaternion rot)
    {
        Vector3 oldPlayerPos = transform.position;
        Vector3 oldAimCorePos = playerAimCore.position;
        Vector3 oldFreeCamCorePos = freeCameraRoot.position;
        Vector3 oldHeadCamCorePos = headCameraRoot.position;
        cc.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        playerAimCore.localEulerAngles = Vector3.zero;
        cc.enabled = true;
        CinemachineCore.OnTargetObjectWarped(transform, transform.position - oldPlayerPos);
        CinemachineCore.OnTargetObjectWarped(playerAimCore, playerAimCore.position - oldAimCorePos);
        CinemachineCore.OnTargetObjectWarped(freeCameraRoot, freeCameraRoot.position - oldFreeCamCorePos);
        CinemachineCore.OnTargetObjectWarped(headCameraRoot, headCameraRoot.position - oldHeadCamCorePos);
        _cinemachineTargetPitch = playerAimCore.rotation.eulerAngles.x;
        _cinemachineTargetYaw = playerAimCore.rotation.eulerAngles.y;
    }

    public void HardMoveTransformToCheckpoint()
    {
        HardMoveTransform(SaveSystem.saveData.currentCheckpointPosition, SaveSystem.saveData.currentCheckpointRotation);
    }

    public void Die(string reason = "unspecified reason")
    {
        new Analytics.PlayerDiedEvent()
        {
            CauseOfDeath = reason,
            PlayerXPosition = transform.position.x,
            PlayerYPosition = transform.position.y,
            PlayerZPosition = transform.position.z
        }.Record();
        fsm.ChangeState<DeadState>();
    }


    public void CameraRotation()
    {
        Vector3 look = pInputActions.Camera.Look.ReadValue<Vector2>();
        // if there is an input and camera position is not fixed
        float deltaTimeMultiplier = Time.deltaTime;
        if (look.sqrMagnitude >= 0.01f)
        {
            //Don't multiply mouse input by Time.deltaTime;
            // float deltaTimeMultiplier = 1.0f; // IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += -look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        playerAimCore.rotation = Quaternion.Euler(_cinemachineTargetPitch,
            _cinemachineTargetYaw, 0.0f);


        _headPitch = Mathf.MoveTowardsAngle(_headPitch, (playerAimCore.localRotation.eulerAngles.x + 180) % 360 - 180, 360 * deltaTimeMultiplier);
        _headYaw = Mathf.Clamp(Mathf.MoveTowardsAngle(_headYaw, (playerAimCore.localRotation.eulerAngles.y + 180) % 360 - 180, 360 * deltaTimeMultiplier), -60, 60);

        //Debug.Log(_headPitch + " " + _headYaw);

        animator.SetFloat("HeadPitch", _headPitch);
        animator.SetFloat("HeadYaw", _headYaw);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }


    private void OnFootstep(AnimationEvent animationEvent)
    {
        float radius = AudioHintRadiusWalk;
        string stepType = "Normal";
        if (IsSprinting)
        {
            radius = AudioHintRadiusSprint;
            stepType = "Sprinting";
        }
        else if (IsCrouching)
        {
            radius = AudioHintRadiusCrouch;
            stepType = "Crouching";
        }

        AudioHint.Create(transform.TransformPoint(cc.center), radius, 0.1f, AudioHintFlags.Suspicious, gameObject);

        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            FMOD.Studio.EventInstance footstepInstance = BackgroundMusicSystem.InstantiateOneShotSound(footstepSound, transform.position);

            footstepInstance.setParameterByNameWithLabel(PLAYER_STEP_TYPE_PARAMETER, stepType);

            footstepInstance.start();
            footstepInstance.release();
            //    if (FootstepAudioClips.Length > 0)
            //    {
            //        var index = Random.Range(0, FootstepAudioClips.Length);
            //        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            //    }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioHint.Create(transform.TransformPoint(cc.center), AudioHintRadiusLand, 0.5f, AudioHintFlags.Suspicious, gameObject);
            BackgroundMusicSystem.PlayOneShotSound(landingSound, transform.TransformPoint(cc.center));
            // AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }

    private void OnAnimationTriggerReset(AnimationEvent animationEvent)
    {
        if (animationEvent.stringParameter != "")
        {
            animator.ResetTrigger(animationEvent.stringParameter);
        }
    }

    /// <summary>
    /// Despite the name, this sets to PrayState and is intended to be called by the praying animation.
    /// </summary>
    /// <param name="animationEvent"></param>
    private void PlayerStateMoveToIdle(AnimationEvent animationEvent)
    {
        if (fsm.currentState.GetType() == typeof(MoveState))
        {
            fsm.ChangeState<PrayState>();
        }
    }

    /// <summary>
    /// Despite the name, this goes from PrayState to MoveState and is intended to be called by the praying animation.
    /// </summary>
    /// <param name="animationEvent"></param>
    private void PlayerStateIdleToMove(AnimationEvent animationEvent)
    {
        if (fsm.currentState.GetType() == typeof(PrayState))
        {
            fsm.ChangeState<MoveState>();
        }
    }

    public void SetStrafe(bool state)
    {
        if (fsm.currentState.GetType() == typeof(MoveState))
        {
            var moveState = fsm.currentState as MoveState;
            moveState.Strafe = state;
        }
    }

    public void DebugTeleportToShrine(string name)
    {
        var parent = GameObject.Find("Good Stuff");
        var shrine = parent.transform.Find(name);
        var checkpoint = shrine.GetComponentInChildren<Checkpoint>();

        // checkpoint.ForceTriggerCheckpoint();
        HardMoveTransform(checkpoint.transform.position, checkpoint.transform.rotation);
    }
}
