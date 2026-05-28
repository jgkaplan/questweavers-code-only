using UnityEngine;
using UnityEngine.UI;

public class DirectionalIndicator : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Image imageIcon;

    [SerializeField] Color colorMin = Color.white;
    [SerializeField] Color colorMax = Color.red;
    [SerializeField] Color colorFlash = Color.orange;

    [SerializeField] AnimationCurve colorCurve;
    [SerializeField] AnimationCurve alphaCurve;

    public Transform target;
    public Transform player;

    float intensity = 0f;
    AgentAlertness alertness = AgentAlertness.Calm;

    public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
    {
        return Mathf.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    }

    public void SetIntensity(float f)
    {
        intensity = f;
    }

    public void SetAlertness(AgentAlertness alertness)
    {
        this.alertness = alertness;
    }

    public void SetIcon(Sprite sprite)
    {
        imageIcon.gameObject.SetActive(sprite != null);
        imageIcon.sprite = sprite;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null || player == null)
            return;
        var rt = GetComponent<RectTransform>();
        Vector3 direction = (target.position - player.position).normalized;
        Vector3 correction = new Vector3(0, 0, -AngleSigned(Camera.main.transform.forward, direction, Camera.main.transform.up));

        if (intensity >= 1f || alertness == AgentAlertness.Alert)
        {
            image.color = colorFlash;
            rt.sizeDelta = new Vector2(256, 256);
            //image.color = Color.Lerp(colorFlash, colorMax, Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(-1, 1, Mathf.Sin(Time.time * 10f))));
        }
        else if (alertness == AgentAlertness.Cautious)
        {
            image.color = colorMax;
            rt.sizeDelta = new Vector2(256, 256);
        }
        else
        {
            Color indicatorColor = Color.Lerp(colorMin, colorMax, colorCurve.Evaluate(intensity));
            indicatorColor.a = alphaCurve.Evaluate(intensity);
            image.color = indicatorColor;
            // .WithAlpha(alphaCurve.Evaluate(intensity)); commented out because of error;
            rt.sizeDelta = new Vector2(Mathf.Lerp(128, 256, alphaCurve.Evaluate(intensity)), 256);
        }

        rt.localRotation = Quaternion.Euler(correction);
        imageIcon.transform.localRotation = Quaternion.Euler(-correction);
        imageIcon.color = image.color;
    }
}
