using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class FirecrackerAbility : MonoBehaviour
{
    [Header("Resources")]
    private int _currentAmount = 0;
    public int CurrentAmount
    {
        get => _currentAmount;
        private set
        {
            _currentAmount = value;
            FirecrackerResourceChanged.Invoke(_currentAmount);
        }
    }
    [SerializeField] private int maxFirecrackerResource = 3;
    [SerializeField] private float castCooldown = 0.2f;
    [SerializeField] private Firecracker firecrackerPrefab;
    [SerializeField] public bool abilityActive = false;
    private float cooldownTimer = 0f;
    private bool onCooldown = false;

    [Header("Throwing")]
    [SerializeField] private float throwForce = 3.0f;
    [SerializeField] private Vector3 throwDirectionModifier;
    private Vector3 throwDirection;
    [SerializeField] private LineRenderer arcVisualizer;
    [SerializeField, Tooltip("How many points should be on the line"), Range(10, 1000)] private int linePoints = 250;
    [SerializeField, Tooltip("How many seconds of the flight trajectory should be computed by the line")] private float timeBetweenPoints = 0.01f;
    [SerializeField] private Transform releasePosition;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float aimingTimeScale = 1f;

    [SerializeField] PlayerCameraRig playerCameraRig;
    Vector3 castDirection2D = Vector3.zero;
    PlayerCameraRig.CameraMode lastCameraMode;

    public static UnityEvent<int> FirecrackerResourceChanged = new();

    private bool isAiming = false;
    public bool CanPickUp
    {
        get => CurrentAmount < maxFirecrackerResource;
    }

    // TODOs for Firecracker
    // - make player able to pick up
    //      - set throw direction based on camera angle
    //      - input mappings
    //      - line renderer for arc
    // - animation for throwing
    //      - release in arc
    //      - play sound on release
    // - play particle effects
    //      - ignite on landing
    // - particle effects while counting down
    //      - sounds when counting down
    // - particle effects on explosion
    //      - sounds on explosion
    //      - remove mist
    //      - audio hint enemies
    // - visual hint for enemies
    //      - destroy when done
    // - add to checkpoint / save system (resource count)
    // - reset current amount to 0
    // - fix throwing on pause
    //      - fix audio playing on pause
    // - fix issue when aiming this and casting mist at the same time

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Checkpoint.activateCheckpoint.AddListener((_, _) => CurrentAmount = abilityActive ? maxFirecrackerResource : 0);
        GameManager.Instance.DoReset.AddListener(() => CurrentAmount = abilityActive ? maxFirecrackerResource : 0);
    }

    public void PickUp()
    {
        abilityActive = true;
        CurrentAmount = 3;
        // currentAmount += 1;
    }

    public void Throw()
    {
        Firecracker firecracker = Instantiate(firecrackerPrefab, releasePosition.position, Quaternion.identity);

        Rigidbody rb = firecracker.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        firecracker.abilityUsedEvent = new Analytics.AbilityUsedEvent()
        {
            AbilityName = "Firecracker",
            PlayerXPosition = transform.position.x,
            PlayerYPosition = transform.position.y,
            PlayerZPosition = transform.position.z,
            StartingResourceLevel = CurrentAmount,
            FinalResourceLevel = CurrentAmount - 1
        };
        CurrentAmount -= 1;
        StartCoroutine(firecracker.Ignite());
        GetComponent<Animator>().ResetTrigger("ThrowTrigger");
        GetComponent<Animator>().SetTrigger("ThrowTrigger");
    }

    // Update is called once per frame
    void Update()
    {
        if (onCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= castCooldown)
            {
                onCooldown = false;
                cooldownTimer = 0;
            }
        }
        if (isAiming)
        {
            Time.timeScale = aimingTimeScale;
            UpdateCastTarget();
            // CurrentAbility.AimThink(CastIndicator);

            var qB = Quaternion.LookRotation(castDirection2D, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, qB, Damper.Damp(1, 0.5f, Time.deltaTime));
            DrawArc();
        }
        else
        {
            arcVisualizer.enabled = false;
        }
    }

    public void LoadFromSave()
    {
        abilityActive = SaveSystem.saveData.hasFirecrackersUnlocked;
        if (abilityActive)
        {
            CurrentAmount = maxFirecrackerResource;
        }
    }

    private void DrawArc()
    {
        arcVisualizer.enabled = true;
        arcVisualizer.positionCount = linePoints;
        Vector3 startPosition = releasePosition.position;
        Vector3 startVelocity = throwForce * throwDirection / firecrackerPrefab.GetComponent<Rigidbody>().mass;
        arcVisualizer.SetPosition(0, startPosition);
        Vector3 lastArcPosition = startPosition;
        for (int i = 1; i < linePoints; i++)
        {
            float time = timeBetweenPoints * i;
            Vector3 nextPoint = startPosition + time * startVelocity;
            nextPoint.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2f * time * time); // d = v*t + 1/2 * a * t^2
            // Check for collisions
            if (Physics.Raycast(lastArcPosition, (nextPoint - lastArcPosition).normalized, out RaycastHit hit, (nextPoint - lastArcPosition).magnitude, collisionMask))
            {
                arcVisualizer.SetPosition(i, hit.point);
                arcVisualizer.positionCount = i + 1;
                break;
            }

            arcVisualizer.SetPosition(i, nextPoint);
            lastArcPosition = nextPoint;
        }
    }

    void StartAiming()
    {
        if (isAiming)
        {
            return;
        }

        lastCameraMode = playerCameraRig.Mode;

        if (playerCameraRig.Mode != PlayerCameraRig.CameraMode.FirstPerson)
        {
            playerCameraRig.Mode = PlayerCameraRig.CameraMode.Aim;
        }

        UpdateCastTarget();

        // var animator = GetComponent<Animator>();
        // animator.SetInteger("AbilityState", CurrentAbility.AbilityAnimatorIndex);
        GetComponent<Animator>().SetBool("Throwing", true);
        GetComponent<Player>().SetStrafe(true);

        isAiming = true;
    }

    public void StopAiming()
    {
        if (!isAiming)
        {
            return;
        }

        Throw();
        playerCameraRig.Mode = lastCameraMode;

        isAiming = false;
        Time.timeScale = 1.0f;

        // var animator = GetComponent<Animator>();
        // animator.SetInteger("AbilityState", 0);
        GetComponent<Animator>().SetBool("Throwing", false);

        GetComponent<Player>().SetStrafe(false);
    }

    void UpdateCastTarget()
    {
        if (playerCameraRig.Mode == PlayerCameraRig.CameraMode.Aim)
        {
            // CinemachineThirdPersonAim has its own internal variables for distance and collision filter.
            // We update this when we start aiming, assuming the player cannot change abilities while aiming.
            CinemachineThirdPersonAim aimer = playerCameraRig.AimCamera.GetComponent<CinemachineThirdPersonAim>();
            // Player.instance.playerAimCore.rotation.eulerAngles;
            Vector3 worldPos = aimer.AimTarget;
            // throwDirection = (aimer.AimTarget - releasePosition.position).normalized;
            throwDirection = (worldPos - Camera.main.transform.position + throwDirectionModifier).normalized;
            // throwDirection = Player.instance.playerAimCore.rotation.eulerAngles;
            // throwDirection = Camera.main.transform.rotation.eulerAngles;
        }
        else
        {
            // throwDirection = Camera.main.transform.rotation.eulerAngles;
        }
        // castDirection = (worldPos - Camera.main.transform.position).normalized;
        castDirection2D = new Vector3(throwDirection.x, 0, throwDirection.z);
    }

    public void OnInputAim(InputAction.CallbackContext context)
    {
        // Start aiming and create indicator
        if (!isAiming && context.performed && abilityActive && CurrentAmount > 0 && !onCooldown)
        {
            StartAiming();
        }

        // Release aiming (and casting if that was happening)
        if (isAiming && context.canceled)
        {
            StopAiming();
        }
    }
}
