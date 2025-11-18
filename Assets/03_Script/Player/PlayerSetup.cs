using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviourPun
{
    //public GameObject playerCamera;
    [SerializeField] private GameObject handUI;

    void Start()
    {
        //playerCamera.GetComponent<Camera>();
        if (!photonView.IsMine)
        {
            // Disable other players' camera and UI
            //playerCamera.SetActive(false);
            handUI.SetActive(false);
        }
        else
        {
            // Ensure local camera and UI are active
            //playerCamera.SetActive(true);
            handUI.SetActive(true);
        }
    }
}
