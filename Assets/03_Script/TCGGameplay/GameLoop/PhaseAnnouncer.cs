using UnityEngine;
using DG.Tweening; // Make sure you have DOTween in your project

public class PhaseAnnouncer : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float slideOutDuration = 0.5f;
    [SerializeField] private Ease slideEase = Ease.OutExpo;

    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Vector2 onScreenPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // --- Setup Positions ---
        // Assumes the object starts in its final "on-screen" position in the editor.
        onScreenPosition = rectTransform.anchoredPosition;

        // The "off-screen" position will be just outside the screen bounds.
        float offScreenX = (onScreenPosition.x > 0)
            ? Screen.width + rectTransform.rect.width
            : -Screen.width - rectTransform.rect.width;

        initialPosition = new Vector2(offScreenX, onScreenPosition.y);

        // Start off-screen
        rectTransform.anchoredPosition = initialPosition;
    }

    // This is the public method that will be called to start the animation sequence.
    public void Play()
    {
        // Ensure the object is in the correct starting position before playing.
        rectTransform.anchoredPosition = initialPosition;
        gameObject.SetActive(true);

        // Create a DOTween sequence for the animation.
        Sequence sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(onScreenPosition, slideInDuration).SetEase(slideEase));
        sequence.AppendInterval(holdDuration);
        sequence.Append(rectTransform.DOAnchorPos(initialPosition, slideOutDuration).SetEase(slideEase));
        sequence.OnComplete(() => gameObject.SetActive(false)); // Hide the object when done.
    }
}
