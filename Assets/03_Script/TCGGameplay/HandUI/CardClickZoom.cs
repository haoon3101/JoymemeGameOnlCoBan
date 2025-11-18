using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardClickZoom : MonoBehaviour, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    public int savedSiblingIndex { get; private set; } = -1;
    private Coroutine scaleRoutine;

    [SerializeField] private float zoomScale = 1.15f;
    [SerializeField] private float scaleSpeed = 8f;

    private static CardClickZoom currentlyZoomedCard = null;
    private DraggableCard draggableCard;

    private void Awake()
    {
        draggableCard = GetComponent<DraggableCard>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (currentlyZoomedCard != null && currentlyZoomedCard != this)
            {
                currentlyZoomedCard.ResetZoom();
            }

            if (currentlyZoomedCard == this)
            {
                ResetZoom();
            }
            else
            {
                // ✅ Save before reordering
                savedSiblingIndex = transform.GetSiblingIndex();
                transform.SetAsLastSibling();

                StartScaleAnimation(originalScale * zoomScale);
                currentlyZoomedCard = this;
            }
        }
    }

    public void ResetZoom()
    {
        StartScaleAnimation(originalScale);

        // ✅ Restore correct sibling index
        if (savedSiblingIndex >= 0)
        {
            transform.SetSiblingIndex(savedSiblingIndex);
            savedSiblingIndex = -1;
        }

        if (currentlyZoomedCard == this)
            currentlyZoomedCard = null;
    }

    private void StartScaleAnimation(Vector3 target)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleTo(target));
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.01f)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        rectTransform.localScale = targetScale;
    }
}

