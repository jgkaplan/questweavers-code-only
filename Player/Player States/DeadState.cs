
using System.Collections;
using UnityEngine;

public class DeadState : MonobehaviourState
{
    [SerializeField, Tooltip("How long the player should be dead before respawning (in seconds)")]
    private float timeSpentDead = 3.0f;

    [SerializeField] private FMODUnity.StudioEventEmitter deathSound;

    private WaitForSeconds respawnTimer;
    private Player player;

    public override string StateName => "DeadState";

    override public void Setup()
    {
        respawnTimer = new WaitForSeconds(timeSpentDead);
        player = GetComponent<Player>();
    }

    override public void OnEnter()
    {
        if (player._hasAnimator)
        {
            // TODO - make character look dead
            player.animator.SetTrigger("PlayerDie");
            player.animator.SetBool("Dead", true);
        }
        deathSound.Play();
        player.IsSprinting = false;
        fsm.StartCoroutine(ReloadSceneSoon());
    }

    public override void OnUpdate()
    {
        player.MovePlayer();
    }

    IEnumerator ReloadSceneSoon()
    {
        yield return respawnTimer;
        fsm.ChangeState<LoadingState>();
        GameManager.Instance.ResetScene();
    }

    public override void OnExit()
    {
        // if (_hasAnimator)
        // {
        //     // TODO - end the dead state
        // }
    }
}
