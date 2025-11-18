using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class CubeInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Camera mainCamera;
    private Vector3 offset;
    private float yPlaneHeight;
    private PhotonView photonView;

    // For interpolation
    private Vector3 networkPosition;
    private float lerpSpeed = 10f;

    void Start()
    {
        mainCamera = Camera.main;
        yPlaneHeight = transform.position.y;
        photonView = GetComponent<PhotonView>();

        networkPosition = transform.position;

        if (!photonView.IsMine)
        {
            // Don't disable the script, just disable input.
            // This allows it to still interpolate.
        }
    }

    void Update()
    {
        // Interpolate for remote players
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * lerpSpeed);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!photonView.IsMine) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, yPlaneHeight, 0));
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            offset = transform.position - worldPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!photonView.IsMine) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, yPlaneHeight, 0));
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            transform.position = worldPoint + offset;

            // Sync position via RPC
            photonView.RPC("SyncPosition", RpcTarget.Others, transform.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!photonView.IsMine) return;
    }

    [PunRPC]
    void SyncPosition(Vector3 position)
    {
        networkPosition = position;
    }
}
