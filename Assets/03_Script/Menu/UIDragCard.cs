using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
IPointerEnterHandler, IPointerExitHandler
{
    [Header("Canvas & Drag")]
    [SerializeField] public Canvas canvas;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [HideInInspector] public bool isDroppedOnZone = false;

    private Vector2 originalAnchoredPosition;
    private Transform originalParent;

    [Header("Return Settings")]
    [SerializeField] private float returnSpeed = 5f;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip pickSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Highlight Khi Hover")]
    [SerializeField] private GameObject highlightEffect;

    private bool isDroppedZone = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canvasGroup.blocksRaycasts && highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (canvasGroup.blocksRaycasts && highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        isDroppedZone = false;

        transform.SetParent(canvas.transform); // Đưa lên cùng Canvas

        if (pickSound)
            audioSource.PlayOneShot(pickSound);

        if (highlightEffect != null)
            highlightEffect.SetActive(true); // Giữ sáng khi đang kéo
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // Không cần move thủ công highlight nếu nó là con của thẻ
        // Nếu bạn dùng Sprite riêng, thì gắn nó làm child là tốt nhất
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (!isDroppedZone)
        {
            StartCoroutine(SmoothReturnWithShake());
        }
        else
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(false); // Tắt sau khi thả thành công
        }
    }

    public void MarkAsDropped(Vector3 dropPosition)
    {
        isDroppedZone = true;
        StartCoroutine(SmoothPlaceIntoZone(dropPosition));

        if (dropSound)
            audioSource.PlayOneShot(dropSound);
    }

    private IEnumerator SmoothReturnWithShake()
    {
        float t = 0f;
        Vector2 start = rectTransform.anchoredPosition;
        float shakeAmount = 5f;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            Vector2 target = Vector2.Lerp(start, originalAnchoredPosition, t);
            Vector2 offset = Random.insideUnitCircle * shakeAmount * (1f - t);
            rectTransform.anchoredPosition = target + offset;
            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
        transform.SetParent(originalParent);

        if (highlightEffect != null)
            highlightEffect.SetActive(false); // Tắt highlight sau khi hoàn tác
    }

    private IEnumerator SmoothPlaceIntoZone(Vector3 worldPos)
    {
        float t = 0f;
        Vector3 start = rectTransform.position;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            rectTransform.position = Vector3.Lerp(start, worldPos, t);
            yield return null;
        }

        rectTransform.position = worldPos;
    }
}