using UnityEngine;
using UnityEngine.UI; // Required for Image
using Photon.Pun;
using System.Collections;
using System.Collections.Generic; // Required for List

// This script should be on the same GameObject as your UnitInstance and PhotonView
[RequireComponent(typeof(PhotonView))]
public class CardDisappear : MonoBehaviourPun
{
    // --- CHANGED: This now references a parent GameObject ---
    [Tooltip("Assign the parent GameObject that holds all the images you want to dissolve (e.g., the main card panel).")]
    [SerializeField] private GameObject imageContainer;

    // This will hold a unique material instance for EACH image
    private List<Material> disappearMatInstances = new List<Material>();
    private GameObject cardBack;

    void Awake()
    {
        if (imageContainer == null)
        {
            // If no container is assigned, default to this GameObject
            imageContainer = this.gameObject;
            Debug.LogWarning("CardDisappear: Image Container not assigned. Defaulting to this object.", this);
        }

        // --- CRITICAL STEP: Find ALL images and create material instances ---
        Image[] allImages = imageContainer.GetComponentsInChildren<Image>();
        cardBack = imageContainer.transform.Find("CardBack")?.gameObject;
        if (allImages.Length == 0)
        {
            Debug.LogError("CardDisappear: No Image components found in the container or its children!", this);
            return;
        }

        foreach (Image img in allImages)
        {
            // Create a unique instance of the material for each image
            Material matInstance = new Material(img.material);
            img.material = matInstance;
            disappearMatInstances.Add(matInstance);
        }
    }

    // This is the RPC that all clients will call to start the effect
    [PunRPC]
    public void RPC_StartDissolve()
    {
        if (TryGetComponent<Collider>(out var collider))
        {
            collider.enabled = false;
        }
        StartCoroutine(DissolveAndDestroy(1.0f));
    }

    private IEnumerator DissolveAndDestroy(float duration)
    {
        float elapsed = 0f;

        if (disappearMatInstances.Count == 0)
        {
            Debug.LogError("Disappear material instances list is empty! Cannot play effect.", this);
            if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
            yield break;
        }
        
        // --- CHANGED: Animate all material instances at once ---
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float dissolveValue = Mathf.Lerp(0f, 1f, elapsed / duration);

            // Loop through all the created material instances and update their dissolve value
            cardBack.gameObject.SetActive(false); // Hide the card back during the dissolve effect
            foreach (Material mat in disappearMatInstances)
            {
                mat.SetFloat("_DissolveAmount", dissolveValue);
            }
            yield return null;
        }

        // After the animation is finished, only the owner destroys the object.
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
