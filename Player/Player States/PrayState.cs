
public class PrayState : MonobehaviourState
{

    public override string StateName => "PrayState";
    private Player player;

    public override void Setup()
    {
        base.Setup();
        player = GetComponent<Player>();
    }
    // public override void OnEnter()
    // {
    // base.OnEnter();
    // fsm.StartCoroutine(fsm.ChangeStateAfterDelay<MoveState>(6));
    // }
}
