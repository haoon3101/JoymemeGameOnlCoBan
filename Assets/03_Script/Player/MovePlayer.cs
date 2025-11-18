using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviourPun
{
    [SerializeField] private float speedPlayer = 5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            // Di chuyển của Player riêng (nếu là chủ sở hữu)
            float verticalMove = Input.GetAxis("Vertical");
            transform.Translate(Vector3.forward * verticalMove * speedPlayer * Time.deltaTime);
            float horizontalMove = Input.GetAxis("Horizontal");
            transform.Translate(Vector3.right * horizontalMove * speedPlayer * Time.deltaTime);

            // Gửi vị trí qua mạng
            photonView.RPC("SyncPosition", RpcTarget.OthersBuffered, transform.position);
        }
    }

    [PunRPC]
    void SyncPosition(Vector3 position)
    {
        // Nhận vị trí từ client khác và áp dụng vào đối tượng
        transform.position = position;
    }
}
