using Unity.Cinemachine;
using UnityEngine;

public class CinemachineIgnoreLayers : CinemachineExtension
{
    public LayerMask ignoreLayersMask;

    private LayerMask defaultMask;


    override protected void Awake()
    {
        base.Awake();
        defaultMask = Camera.main.cullingMask;
        CinemachineCore.CameraActivatedEvent.AddListener(OnActivate);
        CinemachineCore.CameraDeactivatedEvent.AddListener(OnDeactivate);
    }

    private void OnDeactivate(ICinemachineMixer mixer, ICinemachineCamera cam)
    {
        if (cam.Name == ComponentOwner.Name)
        {
            CinemachineCore.FindPotentialTargetBrain(ComponentOwner).OutputCamera.cullingMask = defaultMask;
        }
    }

    private void OnActivate(ICinemachineCamera.ActivationEventParams args)
    {
        if (args.IncomingCamera.Name == ComponentOwner.Name)
        {
            Camera c = CinemachineCore.FindPotentialTargetBrain(ComponentOwner).OutputCamera;
            defaultMask = c.cullingMask;
            c.cullingMask = defaultMask & ~ignoreLayersMask;
        }
    }

}
