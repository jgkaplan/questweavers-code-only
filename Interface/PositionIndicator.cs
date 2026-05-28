using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PositionIndicator : MonoBehaviour
{
    [SerializeField] GameObject toggle;
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            return;

        var pos = Camera.main.WorldToScreenPoint(target.position);
        //pos.x = Mathf.Clamp(pos.x, 0, Screen.width - GetComponent<RectTransform>().rect.size.x);
        //pos.y = Mathf.Clamp(pos.y, 0, Screen.height - GetComponent<RectTransform>().rect.size.y);
        if (RectTransformUtility.RectangleContainsScreenPoint(GetComponentInParent<Canvas>().GetComponent<RectTransform>(), pos)
            && Vector3.Dot(Camera.main.transform.forward, (target.position - Camera.main.transform.position).normalized) >= -0.5f)
        {
            toggle.SetActive(true);
            transform.position = pos;
        }
        else
        {
            toggle.SetActive(false);
            //Vector3 direction = (target.position - Camera.main.transform.position).normalized;
            //var ang = -DirectionalIndicator.AngleSigned(Camera.main.transform.forward, direction, Camera.main.transform.up);
            //transform.position = Quaternion.AngleAxis(ang, Vector3.forward) * (Vector3.up * 512f) + new Vector3(Screen.width / 2f, Screen.height / 2f);
        }
    }
}
