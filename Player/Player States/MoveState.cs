using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerAbilityManager))]
public class MoveState : MonobehaviourState
{
    [SerializeField] private bool AllowCrouching = true;
    [SerializeField] private bool AllowSprinting = true;


    [Tooltip("Move speed of the character in m/s")]
    [SerializeField] private float MoveSpeed = 3.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    [SerializeField] private float SprintSpeed = 5.335f;

    [Tooltip("Move speed when casting mist, in m/s")]
    [SerializeField] private float CastingMoveSpeed = 2.0f;

    // [Tooltip("Slowed move speed of the character in m/s, when fully in mist")]
    // public float SlowMoveSpeed = 1.0f;

    // [Tooltip("Slowed sprint speed of the character in m/s, when fully in mist")]
    // public float SlowSprintSpeed = 3.0f;

    [Tooltip("Crouch speed of the character in m/s")]
    [SerializeField] private float CrouchSpeed = 1.0f;
    // [Tooltip("Slowed crouch speed of the character in m/s, when fully in mist")]
    // public float SlowCrouchSpeed = 0.7f;

    [Tooltip("The speed to slow the player to when out of sprint energy")]
    public float TiredSpeed = 0.5f;

    [Tooltip("The multiplier to slow player movement by when in full mist. (Gets lerped to)")]
    public float MistSpeedMultiplier = 0.5f;

    [Tooltip("Transition duration (in seconds) when the player changes velocity or rotation.")]
    [SerializeField] private float Damping = 0.5f;

    // [Tooltip("How fast the character turns to face movement direction")]
    // [Range(0.0f, 0.3f)]
    // [SerializeField] private float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    [SerializeField] private float SpeedChangeRate = 10.0f;

    private PlayerInputActions pInputActions;

    private CharacterController cc;

    private bool sprintToggled = false;

    public bool Strafe { get; set; }

    PlayerAbilityManager abilityManager;

    private Player player;


    public override string StateName => "MoveState";

    override public void Setup()
    {
        abilityManager = GetComponent<PlayerAbilityManager>();
        cc = GetComponent<CharacterController>();
        player = GetComponent<Player>();
    }

    override public void OnEnter()
    {
        pInputActions = player.pInputActions;

        pInputActions.Player.Enable();
        pInputActions.Testing.Enable();

        // pInputActions.Player.Sprint.performed += StartSprinting;
        pInputActions.Player.ToggleSprint.performed += ToggleSprint;
        pInputActions.Player.Crouch.performed += StartCrouching;
        // pInputActions.Player.Sprint.canceled += StopSprinting;
        pInputActions.Player.Crouch.canceled += StopCrouching;

        pInputActions.Player.Interact.performed += TryInteract;

        AttackTrigger.playerHit.AddListener(OnHitByEnemy);

    }

    private void StartSprinting(InputAction.CallbackContext context)
    {
        if (AllowSprinting)
        {
            player.IsSprinting = true;
            player.IsCrouching = false;
        }
    }

    private void StartCrouching(InputAction.CallbackContext context)
    {
        if (AllowCrouching)
        {
            player.IsSprinting = false;
            player.IsCrouching = true;
        }
    }

    private void StopSprinting(InputAction.CallbackContext context)
    {
        player.IsSprinting = false;
    }

    private void StopCrouching(InputAction.CallbackContext context)
    {
        player.IsCrouching = false;
    }

    private void ToggleSprint(InputAction.CallbackContext context)
    {
        sprintToggled = !sprintToggled;
    }

    public override void OnExit()
    {
        pInputActions.Player.Disable();
        pInputActions.Testing.Disable();

        player.currentVelocityXY = Vector3.zero;

        AttackTrigger.playerHit.RemoveListener(OnHitByEnemy);
    }

    override public void OnUpdate()
    {
        CheckSprinting();
        MoveCharacter();
        RotateCharacter();
        player.CameraRotation();
        player.MovePlayer();
        UpdateMistGlowIndicator();
        LogPosition();
    }

    private void UpdateMistGlowIndicator()
    {
        player.playerInMistGlowIndicator.material.SetFloat("_Mist_Glow_Amount", Mathf.MoveTowards(player.playerInMistGlowIndicator.material.GetFloat("_Mist_Glow_Amount"), player.CurrentMistDensity, Time.deltaTime * 2));
    }

    private float positionAnalyticsTimer = 0.0f;
    private void LogPosition()
    {
        positionAnalyticsTimer += Time.deltaTime;
        if (positionAnalyticsTimer >= Analytics.PlayerPositionLogTime)
        {
            positionAnalyticsTimer = 0;
            new Analytics.PlayerPositionEvent()
            {
                PlayerXPosition = transform.position.x,
                PlayerYPosition = transform.position.y,
                PlayerZPosition = transform.position.z
            }.Record();
        }
    }


    private void CheckSprinting()
    {
        bool sprintHeld = pInputActions.Player.Sprint.IsPressed();
        if (sprintHeld) sprintToggled = false;
        if (AllowSprinting && !player.IsBurnedOut && !player.IsCastMode && (sprintToggled || sprintHeld))
        {
            player.IsSprinting = true;
            player.IsCrouching = false;
        }
        else
        {
            player.IsSprinting = false;
        }
    }

    // die
    private void OnHitByEnemy(string enemyName)
    {
        new Analytics.PlayerDiedEvent()
        {
            CauseOfDeath = enemyName,
            PlayerXPosition = transform.position.x,
            PlayerYPosition = transform.position.y,
            PlayerZPosition = transform.position.z
        }.Record();
        fsm.ChangeState<DeadState>();
    }


    void MoveCharacter()
    {
        // In the future we may want to forcibly disable sprint for whatever reason
        /*
        float targetSpeed = MoveSpeed;
        float slowSpeed = SlowMoveSpeed;
        if (player.IsCrouching)
        {
            targetSpeed = CrouchSpeed;
            slowSpeed = SlowCrouchSpeed;
        }
        else if (player.IsSprinting)
        {
            targetSpeed = SprintSpeed;
            slowSpeed = SlowSprintSpeed;
        }

        // slowed by mist
        targetSpeed = Mathf.Lerp(targetSpeed, slowSpeed, MistManager.instance.GetMistDensityAtPoint(transform.position));
        */
        float targetSpeed = MoveSpeed;
        if (player.IsSprinting)
        {
            targetSpeed = SprintSpeed;
            // if sprinting, don't slow in the mist, just increase energy cost
        }
        else
        {
            if (player.IsCrouching)
            {
                targetSpeed = CrouchSpeed;
            }
            else if (player.IsCastMode)
            {
                targetSpeed = CastingMoveSpeed;
            }
            else if (player.IsBurnedOut)
            {
                targetSpeed = TiredSpeed;
            }
            // slowed by mist
            targetSpeed = Mathf.Lerp(targetSpeed, targetSpeed * MistSpeedMultiplier, player.CurrentMistDensity);

        }

        Vector2 moveInputVec = pInputActions.Player.Move.ReadValue<Vector2>(); // already normalized for keyboard/mouse
        if (moveInputVec == Vector2.zero)
        {
            targetSpeed = 0.0f;
            sprintToggled = false; // reset sprint when zeroing stick
        }
        // Vector3 moveVecRaw = new Vector3(moveInputVec.x, 0, moveInputVec.y);

        // Vector3 moveVec = GetInputFrame() * moveVecRaw;

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        Vector3 moveVec = moveInputVec.y * targetSpeed * forward + targetSpeed * moveInputVec.x * right;

        // Vector3 desiredVelocity = moveVec * targetSpeed;

        var damping = Damping;
        if (Vector3.Angle(player.currentVelocityXY, moveVec) < 100)
            player.currentVelocityXY = Vector3.Slerp(
                player.currentVelocityXY, moveVec,
                Damper.Damp(1, damping, Time.deltaTime));
        else
            player.currentVelocityXY += Damper.Damp(
                moveVec - player.currentVelocityXY, damping, Time.deltaTime);


        // TODO: maybe move this to player
        player._animationBlend = Mathf.Lerp(player._animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (player._animationBlend < 0.01f) player._animationBlend = 0f;

        // cc.Move(targetSpeed * Time.deltaTime * moveVec);
        if (player._hasAnimator)
        {
            player.animator.SetFloat(player._animIDMotionSpeed, moveInputVec.magnitude);
        }
    }


    void RotateCharacter()
    {
        // If not strafing, rotate the player to face movement direction
        if (!Strafe && player.currentVelocityXY.sqrMagnitude > 0.001f)
        {
            // var fwd = GetInputFrame() * Vector3.forward;
            var qA = transform.rotation;
            // var qB = Quaternion.LookRotation(
            //     (InputForward == ForwardModes.Player && Vector3.Dot(fwd, currentVelocityXY) < 0)
            //         ? -currentVelocityXY : currentVelocityXY, Vector3.up);
            var qB = Quaternion.LookRotation(player.currentVelocityXY, Vector3.up);
            var damping = Damping;
            transform.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, damping, Time.deltaTime));
        }
    }


    private void TryInteract(InputAction.CallbackContext context)
    {
        if (player.CurrentInteractable != null)
        {
            player.CurrentInteractable.Use(player);
        }
    }

    Quaternion GetInputFrame()
    {
        // Get the raw input frame, depending of forward mode setting
        var frame = Camera.main.transform.rotation; // camera mode
                                                    // or 
                                                    // var frame = transform.rotation; // player mode

        // Map the raw input frame to something that makes sense as a direction for the player
        var playerUp = transform.up;
        var up = frame * Vector3.up;

        return frame;
    }
}
