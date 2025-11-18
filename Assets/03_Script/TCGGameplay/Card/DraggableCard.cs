using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;
using DG.Tweening;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    private CanvasGroup canvasGroup;
    public Transform originalParent { get; private set; }
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private RectTransform rectTransform;
    private bool isGrayedOut = false;
    public bool HasBeenDropped { get; private set; } = false;
    private Vector2 pointerOffset;
    public int uiCardInstanceID { get; private set; }

    public void MarkAsDropped()
    {
        HasBeenDropped = true;
    }
    void Awake()
    {
        uiCardInstanceID = GetInstanceID();
    }
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            Debug.LogError($"[{name}] CanvasGroup missing!");
        if (rectTransform == null)
            Debug.LogError($"[{name}] RectTransform missing!");
    }

    public void SetGrayOut(bool grayOut)
    {
        isGrayedOut = grayOut;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isGrayedOut ? 0.5f : 1f;
            canvasGroup.interactable = !isGrayedOut;
        }
    }
    public bool IsGrayedOut()
    {
        return isGrayedOut;
    }
    //public bool IsValidDropTarget(DropZone zone)
    //{
    //    // Basic game state checks
    //    if (GameManager.Instance.CurrentTurnActorId != PhotonNetwork.LocalPlayer.ActorNumber ||
    //        GameManager.Instance.CurrentPhase != GamePhase.Placement ||
    //        !ManaManager.Instance.HasEnoughMana(PhotonNetwork.LocalPlayer.ActorNumber, cardData.manaCost))
    //    {
    //        return false;
    //    }

    //    // Card and target specific checks
    //    if (cardData.cardType == CardType.Unit)
    //    {
    //        // Can only play units on your own empty lanes
    //        return !zone.isOpponentZone && zone.GetCurrentUnitInLane() == null;
    //    }else
    //    if (cardData.cardType == CardType.Spell)
    //    {
    //        // --- THIS IS THE KEY CHANGE ---
    //        // Get the live data instantly from the DropZone
    //        UnitData targetData = zone.GetCurrentUnitData();

    //        // Check if there's a unit there (OwnerActorId will be -1 if not)
    //        if (targetData.OwnerActorId == -1) return false;

    //        // Now, check the spell's conditions against the live data
    //        if (cardData.spellEffect is StatModifierEffect statEffect)
    //        {
    //            // Check allegiance
    //            bool isTargetAnAlly = (targetData.OwnerActorId == PhotonNetwork.LocalPlayer.ActorNumber);
    //            if (statEffect.targetAllegiance == TargetAllegiance.Ally && !isTargetAnAlly) return false;
    //            if (statEffect.targetAllegiance == TargetAllegiance.Enemy && isTargetAnAlly) return false;

    //            // Check stat conditions
    //            if (statEffect.requireConditions)
    //            {
    //                foreach (var condition in statEffect.conditions)
    //                {
    //                    // Use the live data from the struct for the check
    //                    int statValue = (condition.stat == StatType.Attack) ? targetData.CurrentAttack : targetData.CurrentHealth;

    //                    // This is a simplified version of your IsMet logic
    //                    bool conditionMet = condition.comparison switch
    //                    {
    //                        ComparisonType.GreaterThan => statValue > condition.value,
    //                        ComparisonType.LessThan => statValue < condition.value,
    //                        // ... add other comparison types
    //                        _ => false
    //                    };

    //                    if (!conditionMet) return false; // If any condition fails, the drop is invalid
    //                }
    //            }
    //        }
    //        return true;
    //    }
    //    return false; // Fallback
    //}
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null) return;

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        CardClickZoom zoom = GetComponent<CardClickZoom>();
        if (zoom != null && zoom.savedSiblingIndex >= 0)
            originalSiblingIndex = zoom.savedSiblingIndex;
        else
            originalSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(originalParent.parent); // move to canvas root
        canvasGroup.blocksRaycasts = false;

        // Calculate offset from mouse position to the rectTransform center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pointerOffset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Apply offset so the card doesn’t snap to center
            rectTransform.localPosition = localPoint - pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If a DropZone did not successfully handle the drop and set this flag,
        // then the drop was invalid and we must snap back.
        if (!HasBeenDropped)
        {
            Debug.Log($"[{name}] Invalid drop, snapping back");

            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);

            // 👇 Smoothly animate back to original position
            HandLayout handLayout = originalParent.GetComponent<HandLayout>();
            if (handLayout != null)
            {
                SnapBackToOriginal(rectTransform.anchoredPosition, originalPosition, handLayout.animationDuration, () =>
                {
                    handLayout.RepositionCards(); // Align after animation
                });
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
            }
        }
        // If HasBeenDropped is true, the DropZone is handling the card's destruction,
        // so we do nothing here.
    }
    //public void OnEndDrag(PointerEventData eventData)
    //{
    //    DropZone dropZone = eventData.pointerEnter?.GetComponent<DropZone>();

    //    if (canvasGroup != null)
    //        canvasGroup.blocksRaycasts = true;
    //    if (IsValidDropTarget(dropZone) == false && cardData.cardType == CardType.Spell)
    //    {
    //        Debug.Log($"[{name}] Invalid drop, snapping back to original position.");
    //        transform.SetParent(originalParent);
    //        transform.SetSiblingIndex(originalSiblingIndex);

    //        // 👇 Smoothly animate back to original position
    //        HandLayout handLayout = originalParent.GetComponent<HandLayout>();
    //        if (handLayout != null)
    //        {
    //            SnapBackToOriginal(rectTransform.anchoredPosition, originalPosition, handLayout.animationDuration, () =>
    //            {
    //                handLayout.RepositionCards(); // Align after animation
    //            });
    //        }
    //        else
    //        {
    //            rectTransform.anchoredPosition = originalPosition;
    //        }
    //        return;
    //    }

    //    if (HasBeenDropped)
    //    {
    //        Debug.Log($"[{name}] Card was successfully dropped. Skipping snap-back.");
    //        StartCoroutine(FadeOutAndDestroy(this));
    //        return;
    //    }
        

    //    if (dropZone != null && dropZone.CanPlayCard(cardData) && ManaManager.Instance.HasEnoughMana(PhotonNetwork.LocalPlayer.ActorNumber, cardData.manaCost) && IsValidDropTarget(dropZone) == true)
    //    {
    //        Debug.Log($"[{name}] Dropped on valid drop zone: {dropZone.name}");
    //        Debug.Log($"[{name}] Original Parent: {originalParent.name}, Sibling Index: {originalSiblingIndex}");
    //        Debug.Log("Card Spell Effect: " + cardData.spellEffect?.name);

    //        Debug.Log("Card Spell Target Allegiance: " + cardData.spellEffect +
    //                  ", Card Type: " + cardData.cardType +
    //                  ", Mana Cost: " + cardData.manaCost);
    //        dropZone.OnDrop(eventData); 
    //        // ✅ Only realign after card is played
    //        HandLayout handLayout = originalParent.GetComponent<HandLayout>();
    //        handLayout?.RepositionCards();
    //    }else if(IsValidDropTarget(dropZone) == false)
    //    {
    //        Debug.Log($"[{name}] Invalid drop, snapping back");

    //        transform.SetParent(originalParent);
    //        transform.SetSiblingIndex(originalSiblingIndex);

    //        // 👇 Smoothly animate back to original position
    //        HandLayout handLayout = originalParent.GetComponent<HandLayout>();
    //        if (handLayout != null)
    //        {
    //            SnapBackToOriginal(rectTransform.anchoredPosition, originalPosition, handLayout.animationDuration, () =>
    //            {
    //                handLayout.RepositionCards(); // Align after animation
    //            });
    //        }
    //        else
    //        {
    //            rectTransform.anchoredPosition = originalPosition;
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log($"[{name}] Invalid drop, snapping back");

    //        transform.SetParent(originalParent);
    //        transform.SetSiblingIndex(originalSiblingIndex);

    //        // 👇 Smoothly animate back to original position
    //        HandLayout handLayout = originalParent.GetComponent<HandLayout>();
    //        if (handLayout != null)
    //        {
    //            SnapBackToOriginal(rectTransform.anchoredPosition, originalPosition, handLayout.animationDuration, () =>
    //            {
    //                handLayout.RepositionCards(); // Align after animation
    //            });
    //        }
    //        else
    //        {
    //            rectTransform.anchoredPosition = originalPosition;
    //        }
    //    }

    //}
    private void SnapBackToOriginal(Vector2 from, Vector2 to, float duration, System.Action onComplete)
    {
        rectTransform.DOAnchorPos(to, duration).SetEase(Ease.OutCubic).OnComplete(() => onComplete?.Invoke());
    }


    public void ResetToHand()
    {
        if (originalParent == null) return;

        // Move back to hand visually
        StartCoroutine(SmoothReturnToHand());
    }

    private IEnumerator SmoothReturnToHand()
    {
        Vector3 worldPos = transform.position;
        transform.SetParent(originalParent, true);
        transform.position = worldPos;

        yield return null;

        RectTransform handSlot = GetComponent<RectTransform>();
        HandLayout handLayout = originalParent.GetComponent<HandLayout>();
        if (handLayout == null || handSlot == null) yield break;

        Vector2 targetPos = handSlot.anchoredPosition;
        Quaternion targetRot = handSlot.localRotation;

        handSlot.DOAnchorPos(targetPos, handLayout.animationDuration).SetEase(Ease.OutCubic);
        handSlot.DOLocalRotateQuaternion(targetRot, handLayout.animationDuration).SetEase(Ease.OutCubic)
            .OnComplete(() => handLayout.RepositionCards());
    }


    public IEnumerator FadeOutAndDestroy(DraggableCard card, float duration = 0.3f)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            Destroy(card.gameObject);
            yield break;
        }

        cg.DOFade(0f, duration).OnComplete(() =>
        {
            if (card != null && card.gameObject != null)
                Destroy(card.gameObject);
        });

        yield return new WaitForSeconds(duration);
    }


}
