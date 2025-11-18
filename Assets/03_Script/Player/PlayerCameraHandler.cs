using Photon.Pun;
using UnityEngine;
using Cinemachine;

public class PlayerCameraHandler : MonoBehaviourPun
{
    [SerializeField] private CinemachineVirtualCamera virtualCam;

    void Start()
    {
        if (!photonView.IsMine)
        {
            virtualCam.gameObject.SetActive(false);
            
        }
        else
        {
            virtualCam.gameObject.SetActive(true);
            
        }

        if (photonView.IsMine)
        {
            // Assign the camera only for the local player
            Canvas myCanvas = GetComponentInChildren<Canvas>();
            Camera uiCam = GetComponentInChildren<Camera>(); // or use Camera.main if shared
            myCanvas.worldCamera = uiCam;
            myCanvas.gameObject.SetActive(true);
        }
        else
        {
            // Disable the other players' Canvas (not needed locally)
            Canvas otherCanvas = GetComponentInChildren<Canvas>();
            otherCanvas.gameObject.SetActive(false);
        }
    }
}
