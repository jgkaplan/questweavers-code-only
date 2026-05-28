using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    public abstract bool CanPlayerInteract { get; }
    public abstract bool CanEnemyInteract { get; }

    public InteractionState InteractState { get; private set; }
    public abstract InteractionType InteractType { get; }

    protected virtual bool defaultState => false;
    protected virtual float cooldown => 0.1f;

    Player interactingPlayer;

    float cooldownLeft = 0;

    public enum InteractionState
    {
        Off,
        On,
        Held,
    }

    public enum InteractionType
    {
        Oneshot,
        Toggle,
        Continuous
    }

    private void Start()
    {
        if (defaultState)
        {
            InteractState = InteractionState.On;
        }
    }

    /// <summary>
    /// Called to use the interactable. Behavior depends on interaction type.
    /// </summary>
    /// <param name="player">The player who made this interaction.</param>
    /// <exception cref="System.NotImplementedException">InteractionType.Continuous is currently unimplemented.</exception>
    public void Use(Player player)
    {
        if (!IsInteractable())
        {
            return;
        }

        cooldownLeft = cooldown;

        if (InteractType == InteractionType.Oneshot)
        {
            Interact(player);
        }
        else if (InteractType == InteractionType.Toggle)
        {
            if (InteractState == InteractionState.On)
            {
                InteractState = InteractionState.Off;
                Deactivate(player);
            }
            else
            {
                InteractState = InteractionState.On;
                Activate(player);
            }
        }
        else if (InteractType == InteractionType.Continuous)
        {
            throw new System.NotImplementedException();
        }
    }

    /// <summary>
    /// Called once when used for Oneshot interactables
    /// </summary>
    protected virtual void Interact(Player player) { }

    /// <summary>
    /// Called once when switching to On state for Toggle interactables
    /// </summary>
    protected virtual void Activate(Player player) { }

    /// <summary>
    /// Called once when switching to Off state for Toggle interactables
    /// </summary>
    protected virtual void Deactivate(Player player) { }

    public virtual bool IsInteractable()
    {
        return cooldownLeft <= 0;
    }

    void OnTriggerStay(Collider other)
    {
        if (interactingPlayer == null && CanPlayerInteract && IsInteractable() && other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            player.CurrentInteractable = this;
            interactingPlayer = player;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (CanPlayerInteract && other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player.CurrentInteractable == this)
            {
                player.CurrentInteractable = null;
            }
            interactingPlayer = null;
        }
    }

    protected virtual void Update()
    {
        if (interactingPlayer != null && !IsInteractable())
        {
            interactingPlayer.CurrentInteractable = null;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (cooldownLeft > 0)
        {
            cooldownLeft -= Time.fixedDeltaTime;
        }
    }
}
