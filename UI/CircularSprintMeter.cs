using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CircularSprintMeter : MonoBehaviour
{
    [SerializeField] private Player player;
    private Material indicatorMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        indicatorMaterial = GetComponent<Image>().material;
    }

    void Update()
    {
        indicatorMaterial.SetFloat("_Progress", player.CurrentSprintPercentRemaining);
        indicatorMaterial.SetFloat("_IsRefilling", player.IsBurnedOut ? 1 : 0);
    }
}
