using UnityEngine;

public class RespawningState : MonobehaviourState
{
    [SerializeField] private FMODUnity.StudioEventEmitter respawnSound;
    private Player player;

    public override string StateName => "RespawningState";
    override public void Setup()
    {
        player = GetComponent<Player>();
    }

    override public void OnEnter()
    {
        var cc = GetComponent<CharacterController>();
        player.HardMoveTransformToCheckpoint();
        player.CurrentInteractable = null;

    }

    public void DoRespawnStuff()
    {
        // TODO - play respawn animation
        if (player._hasAnimator)
        {
            player.animator.SetTrigger("PlayerRespawn");
        }
        respawnSound.Play();
        fsm.StartCoroutine(fsm.ChangeStateAfterDelay<MoveState>(0.1f));

    }

    public override void OnExit()
    {
        player.animator.SetBool("Dead", false);
    }
}
