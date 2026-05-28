using System.Linq;
using FMOD.Studio;
using FMODUnity;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Windows;

public class AgentLocomotion : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public EventReference EnemyLandingAudio;
    public EventReference EnemyFootstepsAudio;

    [Space(10)]
    [Tooltip("The height the character can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Sound")]
    public FMODUnity.StudioEventEmitter movementSound;

    [Header("Debug")]
    [Tooltip("Always follow this transform")]
    public Transform DebugFollowTransform;


    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    // references
    private Animator _animator;
    private CharacterController _controller;
    private NavMeshAgent _navMeshAgent;
    private AgentBrain _brain;

    private const float _threshold = 0.01f;
    private bool _hasAnimator;
    Vector2 _smoothDeltaPosition = Vector2.zero;
    Vector2 _velocity = Vector2.zero;

    private BackgroundMusicSystem.ParameterMap[] footstepParameterMap = { new() { parameter_name = "BasicGuardStep", value = 1 } };
    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _brain = GetComponentInChildren<AgentBrain>();
        _navMeshAgent.updatePosition = false;
        _navMeshAgent.autoTraverseOffMeshLink = false;

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        movementSound.Play();
    }
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        /*
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
        */
        // _navMeshAgent.updatePosition = !Grounded;

    }

    public void SetDestination(Vector3 position)
    {
        _navMeshAgent.destination = position;
    }

    private void Update()
    {
        if (DebugFollowTransform != null)
        {
            SetDestination(DebugFollowTransform.position);
        }

        _animator.SetInteger("Alertness", (int)_brain.Alertness);


        //GroundedCheck();
        if (!_navMeshAgent.isOnOffMeshLink) // && _brain.State == AgentState.Goto
        {
            //_navMeshAgent.isStopped = false;
            Move();
        }
        HeadLook();
        UpdateMoveSound();
    }

    void UpdateMoveSound()
    {
        if (_brain.Alertness == AgentAlertness.Alert)
        {
            float intensity = Mathf.InverseLerp(0, _brain.MoveSpeedAlert, _navMeshAgent.velocity.magnitude);
            movementSound.SetParameter("BasicGuardAgroMovement", intensity);
            movementSound.SetParameter("BasicGuardNormalMovement", intensity);
        }
        else
        {
            float intensity = Mathf.InverseLerp(0, _brain.MoveSpeedCautious, _navMeshAgent.velocity.magnitude);
            movementSound.SetParameter("BasicGuardAgroMovement", 0);
            movementSound.SetParameter("BasicGuardNormalMovement", intensity);
        }

    }

    private void HeadLook()
    {
        LookAt lookAt = GetComponent<LookAt>();
        if (lookAt)
            lookAt.lookAtTargetPosition = _navMeshAgent.steeringTarget + transform.forward;
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = MoveSpeed; //_input.sprint ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }



        Vector3 worldDeltaPosition = _navMeshAgent.nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        _smoothDeltaPosition = Vector2.Lerp(_smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            _velocity = _smoothDeltaPosition / Time.deltaTime;

        _animationBlend = Mathf.Lerp(_animationBlend, _velocity.magnitude, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        _animator.SetFloat(_animIDSpeed, _animationBlend);
        _animator.SetFloat(_animIDMotionSpeed, 1f);

        // GetComponent<LookAt>().lookAtTargetPosition = agent.steeringTarget + transform.forward;

        // normalise input direction
        /*
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, 1f);
        }
        */
    }

    void OnAnimatorMove()
    {
        // Update position to agent position
        if (_navMeshAgent != null && !_navMeshAgent.isOnOffMeshLink)
        {
            transform.position = _navMeshAgent.nextPosition;
        }

        //Vector3 position = _animator.rootPosition;
        //position.y = _navMeshAgent.nextPosition.y;
        //transform.position = position;

    }
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            BackgroundMusicSystem.PlayOneShotSound(EnemyFootstepsAudio, transform.position, footstepParameterMap);
        }
    }
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            BackgroundMusicSystem.PlayOneShotSound(EnemyLandingAudio, transform.TransformPoint(_controller.center));
        }
    }
}
