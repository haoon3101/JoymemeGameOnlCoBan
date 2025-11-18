using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class HandLayout : MonoBehaviour
{
    [SerializeField] private float cardSpacing = 100f;
    [SerializeField] private float cardOverlap = -60f;
    [field: SerializeField] public float animationDuration { get; private set; } = 0.2f;

    private bool isRepositioning;
    public bool IsRepositioning => isRepositioning;

    public void RepositionCards()
    {
        if (isRepositioning) return;
        isRepositioning = true;

        List<RectTransform> cards = new();
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i).GetComponent<RectTransform>();
            if (child != null) cards.Add(child);
        }

        int count = cards.Count;
        if (count == 0)
        {
            isRepositioning = false;
            return;
        }

        float totalWidth = cardSpacing * (count - 1);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            float x = startX + i * cardSpacing;
            Vector2 targetPos = new(x, 0);

            card.DOAnchorPos(targetPos, animationDuration).SetEase(Ease.OutCubic);
        }

        DOVirtual.DelayedCall(animationDuration, () => isRepositioning = false);
    }
}
