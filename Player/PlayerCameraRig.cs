using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// This is a custom camera manager that selects between an aiming camera child and a
/// non-aiming camera child, depending on the value of some user input.
///
/// The Aiming child is expected to have ThirdPersonFollow and ThirdPersonAim components,
/// and to have a player as its Follow target.  The player is expected to have a
/// SimplePlayerAimController behaviour on one of its children, to decouple aiminag and
/// player rotation.
/// </summary>
[ExecuteAlways]
public class PlayerCameraRig : CinemachineCameraManagerBase
{

    public enum CameraMode
    {
        Freelook, Aim, ThirdPerson, FirstPerson
    }

    [SerializeField]
    private CameraMode _mode = CameraMode.Freelook;

    public CameraMode Mode
    {
        get
        {
            return _mode;
        }
        set
        {
            switch (_mode)
            {
                case CameraMode.Freelook:
                    {
                        break;
                    }
                case CameraMode.Aim:
                    {
                        // lookPos = FreeCamera.GetComponent<CinemachineThirdPersonAim>().AimTarget;
                        break;
                    }
                case CameraMode.ThirdPerson:
                    {
                        // FreeCamera.
                        break;
                    }
                case CameraMode.FirstPerson:
                    {
                        break;
                    }
            }
            _mode = value;
        }
    }

    [HideInInspector] public CinemachineVirtualCameraBase AimCamera;
    [HideInInspector] public CinemachineVirtualCameraBase FreeCamera;
    [HideInInspector] public CinemachineVirtualCameraBase ThirdPersonCamera;
    [HideInInspector] public CinemachineVirtualCameraBase FirstPerson;

    protected override void Start()
    {
        base.Start();

        for (int i = 0; i < ChildCameras.Count; ++i)
        {
            var cam = ChildCameras[i];
            if (!cam.isActiveAndEnabled)
                continue;
            if (cam.Name == "Aim Camera")
            {
                AimCamera = cam;
            }
            else if (cam.Name == "Free Camera")
            {
                FreeCamera = cam;
            }
            else if (cam.Name == "Third Person Camera")
            {
                ThirdPersonCamera = cam;
            }
            else if (cam.Name == "First Person Camera")
            {
                FirstPerson = cam;
            }

        }
    }

    protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
    {
        switch (Mode)
        {
            case CameraMode.Freelook:
                {
                    return FreeCamera;
                }
            case CameraMode.Aim:
                {
                    return AimCamera;
                }
            case CameraMode.ThirdPerson:
                {
                    return ThirdPersonCamera;
                }
            case CameraMode.FirstPerson:
                {
                    return FirstPerson;
                }
            default:
                {
                    return FreeCamera;
                }
        }
    }
}
