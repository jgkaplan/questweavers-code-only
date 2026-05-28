using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VolumetricFogAndMist2;

public class FireBlocker : MonoBehaviour
{
    bool fireActive = true;
    public AnimationCurve pushStrengthCurve;
    [SerializeField] float strength = 1.5f;
    [SerializeField] GameObject ExtinguishEffectPrefab;

    [SerializeField] FMODUnity.EventReference fireExtinguishSound;
    [SerializeField] bool extinguishOverTime = false;

    private FMODUnity.StudioEventEmitter firePlayer;
    private NavMeshObstacle obstacle;

    private void Start()
    {
        // var fogTransparentObject = GetComponent<FogTransparentObject>();
        // if (fogTransparentObject != null)
        // {
        //     fogTransparentObject.fogVolume = MistManager.instance.GetZoneAtPoint(transform.position).fogZone;
        // }
        firePlayer = GetComponent<FMODUnity.StudioEventEmitter>();
        obstacle = GetComponent<NavMeshObstacle>();
    }

    private void OnTriggerEnter(Collider col)
    {
        var charController = col.GetComponent<CharacterController>();
        if (charController != null)
        {
            if (Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 direction, out float distance))
            {
                direction.y = 0;
                var v = charController.velocity;
                v.y = 0;
                var force = Vector3.Dot(direction, v);

                if (col.GetComponent<Player>())
                {
                    // Force face the fire so animation moves player back
                    col.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    col.GetComponent<Player>().animator.SetTrigger("Stagger");
                }
            }
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (fireActive)
            PushRigidBodies(col);
    }

    private void PushRigidBodies(Collider col)
    {
        // https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

        var charController = col.GetComponent<CharacterController>();
        if (charController != null)
        {
            if (Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 direction, out float distance))
            {
                direction.y = 0;
                var v = charController.velocity;
                v.y = 0;
                var force = Vector3.Dot(direction, v);
                charController.Move(-direction * (distance * strength + pushStrengthCurve.Evaluate(distance) * force) * Time.fixedDeltaTime);
            }
        }
        else
        {
            // make sure we hit a non kinematic rigidbody
            Rigidbody body = col.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            if (Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 direction, out float distance))
            {
                direction.y = 0;
                body.AddForce(-direction * pushStrengthCurve.Evaluate(distance) * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }
    }

    public bool IsFireActive()
    {
        return fireActive;
    }

    public void SetFireState(bool state)
    {
        if (fireActive == state) return;
        fireActive = state;
        foreach (var col in gameObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = state;
        }
        if (obstacle != null)
        {
            obstacle.enabled = state;
        }
        foreach (var fx in gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            if (fireActive)
            {
                if (fx.gameObject.name == "Fire")
                {
                    fx.gameObject.SetActive(true);

                    var sizeOverLifetime = fx.sizeOverLifetime;
                    sizeOverLifetime.xMultiplier = 1f;
                    sizeOverLifetime.yMultiplier = 1f;
                }
                fx.Play();
            }
            else
            {
                if (fx.gameObject.name == "Fire")
                {
                    var sizeOverLifetime = fx.sizeOverLifetime;
                    sizeOverLifetime.xMultiplier = 0f;
                    sizeOverLifetime.yMultiplier = 0f;
                    if (!extinguishOverTime)
                    {
                        fx.gameObject.SetActive(false);
                    }
                }
                fx.Stop();
            }
        }
        if (!fireActive && ExtinguishEffectPrefab != null)
        {
            var obj = Instantiate(ExtinguishEffectPrefab);
            obj.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
        if (!fireActive)
        {
            firePlayer.Stop();
            BackgroundMusicSystem.PlayOneShotSound(fireExtinguishSound, transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var col in GetComponentsInChildren<CapsuleCollider>())
        {
            Gizmos.color = col.isTrigger ? Color.yellow : Color.red;
            GizmosExtensions.DrawWireCircle(transform.position, col.radius, rotation: transform.rotation);
        }
    }
}