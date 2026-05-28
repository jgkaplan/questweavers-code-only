
using UnityEngine;

public class IdleState : MonobehaviourState
{

    public override string StateName => "IdleState";
    private Player player;
    public override void Setup()
    {
        player = GetComponent<Player>();
    }
    public override void OnUpdate()
    {
        player.CameraRotation();
        player.MovePlayer();
    }
}
