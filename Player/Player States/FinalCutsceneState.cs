using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(CharacterController))]
public class FinalCutsceneState : MonobehaviourState
{

    public override string StateName => "FinalCutsceneState";

    private Player player;
    private CharacterController cc;
    private bool reachedDestination = false;

    public Transform pathfindLocation;
    [Tooltip("Transition duration (in seconds) when the player changes velocity or rotation.")]
    [SerializeField] private float Damping = 0.5f;
    [Tooltip("Sprint speed of the character in m/s")]
    [SerializeField] private float SprintSpeed = 5.335f;
    [Tooltip("Acceleration and deceleration")]
    [SerializeField] private float SpeedChangeRate = 10.0f;
    public GameObject gameUICanvas;

    public override void Setup()
    {
        player = GetComponent<Player>();
        cc = GetComponent<CharacterController>();
    }
    public override void OnEnter()
    {
        player.IsSprinting = false;
        gameUICanvas.SetActive(false);
    }

    public override void OnUpdate()
    {
        if (reachedDestination)
        {
            player.currentVelocityXY = Vector3.Slerp(
                player.currentVelocityXY, Vector3.zero,
                Damper.Damp(1, Damping, Time.deltaTime));
            player._animationBlend = Mathf.Lerp(player._animationBlend, 0, Time.deltaTime * SpeedChangeRate);
            if (player._animationBlend < 0.01f) player._animationBlend = 0f;
            player.animator.SetFloat("SpeedForward", player.currentVelocityXY.x);
            player.animator.SetFloat("SpeedSide", player.currentVelocityXY.z);
            if (player._hasAnimator)
            {
                player.animator.SetFloat(player._animIDMotionSpeed, 0);
                player.animator.SetFloat(player._animIDSpeed, player._animationBlend);

                player.animator.SetFloat("SpeedForward", player.currentVelocityXY.x);
                player.animator.SetFloat("SpeedSide", player.currentVelocityXY.z);
            }
            return;
        }
        Vector3 targetDir = (pathfindLocation.position - transform.position);
        targetDir.y = 0;
        Vector3 moveVec = targetDir.normalized * SprintSpeed;

        if (Vector3.Angle(player.currentVelocityXY, moveVec) < 100)
            player.currentVelocityXY = Vector3.Slerp(
                player.currentVelocityXY, moveVec,
                Damper.Damp(1, Damping, Time.deltaTime));
        else
            player.currentVelocityXY += Damper.Damp(
                moveVec - player.currentVelocityXY, Damping, Time.deltaTime);


        // TODO: maybe move this to player
        player._animationBlend = Mathf.Lerp(player._animationBlend, SprintSpeed, Time.deltaTime * SpeedChangeRate);
        if (player._animationBlend < 0.01f) player._animationBlend = 0f;

        // cc.Move(targetSpeed * Time.deltaTime * moveVec);
        if (player._hasAnimator)
        {
            player.animator.SetFloat(player._animIDMotionSpeed, 1);
        }
        player.GroundedCheck();
        player.DoGravity();
        cc.Move(player.currentVelocityXY * Time.deltaTime + new Vector3(0.0f, player._verticalVelocity, 0.0f) * Time.deltaTime);

        if (player._hasAnimator)
        {
            player.animator.SetFloat(player._animIDSpeed, player._animationBlend);

            player.animator.SetFloat("SpeedForward", player.currentVelocityXY.x);
            player.animator.SetFloat("SpeedSide", player.currentVelocityXY.z);
        }
        Vector3 currentDistance = (pathfindLocation.position - transform.position);
        currentDistance.y = 0;
        if (currentDistance.sqrMagnitude <= 1f)
        {
            player.currentVelocityXY = Vector2.zero;
            reachedDestination = true;
        }
    }

    void RotateCharacter()
    {
        // If not strafing, rotate the player to face movement direction
        if (player.currentVelocityXY.sqrMagnitude > 0.001f)
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
}
