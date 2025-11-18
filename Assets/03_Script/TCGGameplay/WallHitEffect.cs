using Photon.Pun;
using UnityEngine;
using System.Collections;

public class WallHitEffect : MonoBehaviour
{
    [Tooltip("Which player owns this wall and will receive damage?")]
    public int ownerActorId; // Set this in the inspector

    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float cooldownTime = 2f;

    [Header("Transparency Fade Settings")]
    [SerializeField] private Renderer wallRenderer;
    [SerializeField] private float fadeAlpha = 0.3f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float restoreDuration = 1.0f;

    [Header("Shatter Settings")]
    [SerializeField] private GameObject shatteredWallPrefab;
    [SerializeField] private float explosionForce = 500f;

    // --- NEW: Slow Motion Settings ---
    [Header("Slow Motion on Shatter")]
    [SerializeField] private float slowMotionIntensity = 0.2f; // 20% of normal speed
    [SerializeField] private float slowMotionDuration = 1.5f;  // Lasts for 1.5 seconds

    [Header("SFX")]
    [SerializeField] private AudioSource hitSound;
    //[SerializeField] private AudioSource shatterSound;

    private float lastSpawnTime = -Mathf.Infinity;
    private Coroutine fadeCoroutine;
    private Color originalColor;
    private bool initialized = false;
    public PhotonView photonView { get; private set; }

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        UnitInstance unit = other.GetComponent<UnitInstance>();
        if (Time.time - lastSpawnTime < cooldownTime || unit == null || unit.HasDealtDamage)
        {
            return;
        }

        lastSpawnTime = Time.time;
        unit.HasDealtDamage = true;

        int damage = unit.GetAttack();
        if (damage <= 0) return;

        Vector3 contactPoint = other.ClosestPoint(transform.position);

        GameManager.Instance.photonView.RPC("RPC_TakePlayerDamage", RpcTarget.All, ownerActorId, damage, contactPoint);
        photonView.RPC("RPC_PlayVisualEffects", RpcTarget.All, contactPoint);
        photonView.RPC("RPC_PlayWallHitSound", RpcTarget.All);

        int currentHealth = GameManager.Instance.GetPlayerHealth(ownerActorId);
        if (currentHealth <= 0)
        {
            //photonView.RPC("RPC_PlayShatterSound", RpcTarget.All);
            photonView.RPC("RPC_ShatterWall", RpcTarget.All, contactPoint);
        }
    }

    [PunRPC]
    public void RPC_PlayVisualEffects(Vector3 contactPoint)
    {
        if (hitEffectPrefab != null)
        {
            Vector3 direction = (transform.position - contactPoint).normalized;
            Instantiate(hitEffectPrefab, contactPoint, Quaternion.LookRotation(direction));
        }

        if (wallRenderer != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTransparency());
        }
    }
    [PunRPC]
    void RPC_PlayWallHitSound()
    {
        if (hitSound != null)
        {
            hitSound.Play();
        }
    }
    //[PunRPC]
    //void RPC_PlayShatterSound()
    //{
    //    if (shatterSound != null)
    //    {
    //        shatterSound.Play();
    //    }
    //}
    private IEnumerator FadeTransparency()
    {
        Material mat = wallRenderer.material;
        if (!initialized)
        {
            originalColor = mat.color;
            initialized = true;
        }
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, fadeAlpha, t / fadeDuration);
            mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        t = 0;
        while (t < restoreDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(fadeAlpha, originalColor.a, t / restoreDuration);
            mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        mat.color = originalColor;
    }

    [PunRPC]
    public void RPC_ShatterWall(Vector3 hitPosition)
    {
        // --- THIS IS THE KEY CHANGE ---
        // Trigger the slow-motion effect on all clients
        if (GameManager.Instance != null)
        {
            
            GameManager.Instance.TriggerSlowMotion(slowMotionDuration, slowMotionIntensity);
        }

        if (wallRenderer != null)
            wallRenderer.enabled = false;

        if (shatteredWallPrefab != null)
        {
            GameObject shattered = Instantiate(shatteredWallPrefab, transform.position, transform.rotation);
            foreach (var rb in shattered.GetComponentsInChildren<Rigidbody>())
            {
                Vector3 forceDir = (rb.transform.position - hitPosition).normalized;
                rb.AddForce(forceDir * Random.Range(100, 300));
            }
            // We only destroy the original wall on the MasterClient to avoid issues
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}