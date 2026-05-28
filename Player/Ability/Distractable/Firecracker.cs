using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class Firecracker : MonoBehaviour
{
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float enemyAlertSoundRadius = 5f;
    [SerializeField] private float mistRemovalRadius = 0.3f;
    [SerializeField, Tooltip("How many seconds this distraction will go for")] private float distractionTime = 2.0f;
    [SerializeField] private Renderer wick;

    /// <summary>
    /// Force the enemy to not path nearby
    /// </summary>
    [SerializeField] private bool createNavMeshObstacle = true;
    [SerializeField] private float navMeshObstacleRadius = 5f;

    [SerializeField] private FMODUnity.StudioEventEmitter igniteSound;
    [SerializeField] private FMODUnity.StudioEventEmitter throwSound;

    [SerializeField] private Light explodeLight;
    [SerializeField] private VisualEffect sparks;

    [SerializeField] bool igniteOnEnable = false;

    public Analytics.AbilityUsedEvent abilityUsedEvent;
    private bool doIgnite = false;
    bool exploding = false;
    float lightInitialIntensity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        explodeLight.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (exploding && explodeLight != null)
        {
            explodeLight.intensity = lightInitialIntensity * Random.Range(0.75f, 1.25f);
        }
    }

    private void OnEnable()
    {
        if (igniteOnEnable)
        {
            StartCoroutine(Ignite());
            doIgnite = true;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            doIgnite = true;
        }
    }

    public IEnumerator Ignite()
    {
        throwSound.Play();
        yield return new WaitUntil(() => doIgnite);
        // TODO: initial particle effects
        igniteSound.SetParameter("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(transform.position));
        igniteSound.Play();
        wick.material.SetFloat("_IsLit", 1);
        float timer = 0;
        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;
            igniteSound.SetParameter("LocalMistDensity", MistManager.instance.GetMistDensityAtPoint(transform.position));
            // TODO: update wick visuals and particles based on amount of time remaining
            wick.material.SetFloat("_BurnAmount", timer / explosionDelay);
            yield return null;
        }
        igniteSound.SetParameter("firecracker", 1);
        igniteSound.SetParameter("LocalMistDensity", 0);

        // TODO: explosion particles

        explodeLight.enabled = true;
        lightInitialIntensity = explodeLight.intensity;

        sparks.enabled = true;
        sparks.Play();
        MistManager.instance.RemoveMist(transform.position, mistRemovalRadius);
        AudioHint.Create(transform.position, enemyAlertSoundRadius, duration: 0.1f, AudioHintFlags.Distraction, gameObject);

        // This has the unfortunate side effect of snapping agents outside of the obstacle radius
        GameObject navMeshObstacle = null;
        if (createNavMeshObstacle)
        {
            navMeshObstacle = new GameObject();
            navMeshObstacle.name = "Firecracker NavMeshObstacle";
            navMeshObstacle.transform.position = transform.position;
            navMeshObstacle.AddComponent<NavMeshObstacle>();
            var comp = navMeshObstacle.GetComponent<NavMeshObstacle>();
            comp.shape = NavMeshObstacleShape.Capsule;
            comp.radius = navMeshObstacleRadius;
            comp.height = 2f;
            comp.center = Vector3.up;
            comp.carving = true; // agent will not attempt to path into radius
        }

        if (abilityUsedEvent != null)
        {
            abilityUsedEvent.AbilityXPosition = transform.position.x;
            abilityUsedEvent.AbilityYPosition = transform.position.y;
            abilityUsedEvent.AbilityZPosition = transform.position.z;
            abilityUsedEvent.Record();
        }

        // TODO: visual hint for alerting enemies

        exploding = true;

        yield return new WaitForSeconds(distractionTime);
        sparks.Stop();
        if (navMeshObstacle != null)
        {
            Destroy(navMeshObstacle);
        }
        Destroy(gameObject);
    }
}
