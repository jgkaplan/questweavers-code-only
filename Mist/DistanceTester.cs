using UnityEngine;
using VolumetricFogAndMist2;

public class DistanceTester : MonoBehaviour
{
    public Transform start;
    public Transform end;

    public float stepSize = 0.1f;
    public MistZone mistZone;
    public Color zeroColor;
    public Color fullColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float density = mistZone.GetDensityBetweenPoints(start.position, end.position, stepSize);
        // Debug.Log($"{density} {distance} {density / distance}");
        Debug.DrawLine(start.position, end.position, Color.Lerp(zeroColor, fullColor, density / 10));
    }
}
