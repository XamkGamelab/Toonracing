using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    enum CameraState
    {
        StartCamera,
        SideviewCamera,
        PlayerOutsideCamera
    }
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera StartCamera;
    [SerializeField] private CinemachineCamera SideviewCamera;
    [SerializeField] private CinemachineCamera PlayerOutsideCamera;
    
    [SerializeField] private CameraState currentCameraState;
    
    // Privates 
    private bool isPlayerSet = false;
    private void Start()
    {
        currentCameraState = CameraState.StartCamera;
        StartCamera.Priority = 10;
        SideviewCamera.Priority = 5;
        PlayerOutsideCamera.Priority = 5;
        if(!PlayerOutsideCamera)
            Debug.LogError("PlayerOutsideCamera not assigned in CameraController");
    }
    void Update()
    {
        if(cinemachineBrain.IsBlending)
            return;
        switch (currentCameraState)
        {
            case CameraState.StartCamera:
                currentCameraState = CameraState.SideviewCamera;
                StartCamera.Priority = 5;
                SideviewCamera.Priority = 10;
                PlayerOutsideCamera.Priority = 5;
                break;
            case CameraState.SideviewCamera:
                if(!isPlayerSet)
                    break;
                currentCameraState = CameraState.PlayerOutsideCamera;
                StartCamera.Priority = 5;
                SideviewCamera.Priority = 5;
                PlayerOutsideCamera.Priority = 10;
                break;
            case CameraState.PlayerOutsideCamera:
                // Do nothing, stay on the PlayerOutsideCamera
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public void SetLocalPlayer(Transform playerTransform)
    {
        if(PlayerOutsideCamera)
            PlayerOutsideCamera.Target.TrackingTarget = playerTransform;
        isPlayerSet = true;
    }
}
