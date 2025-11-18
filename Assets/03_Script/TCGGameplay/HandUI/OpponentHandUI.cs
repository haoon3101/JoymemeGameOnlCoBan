using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class OpponentHandUI : MonoBehaviourPunCallbacks
{
    public static OpponentHandUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject cardBackPrefab;
    [SerializeField] private Transform handPanel;
    [Tooltip("The position where new cards will appear from before animating into the hand.")]
    [SerializeField] private Transform cardSpawnPoint;

    [Header("Layout Settings")]
    [SerializeField] private float cardSpacing = 40f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease animationEase = Ease.OutCubic;

    private Dictionary<int, GameObject> cardsInOpponentHand = new Dictionary<int, GameObject>();
    private List<int> cardOrder = new List<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [PunRPC]
    public void RPC_AddCardToOpponentHand(int cardInstanceID)
    {
        if (cardBackPrefab == null || handPanel == null) return;

        Vector3 spawnPos = cardSpawnPoint != null ? cardSpawnPoint.position : handPanel.position;

        GameObject cardBack = Instantiate(cardBackPrefab, spawnPos, Quaternion.identity, handPanel);

        cardsInOpponentHand[cardInstanceID] = cardBack;
        cardOrder.Add(cardInstanceID);

        RepositionCards();
    }

    [PunRPC]
    public void RPC_RemoveCardFromOpponentHand(int cardInstanceID)
    {
        if (cardsInOpponentHand.TryGetValue(cardInstanceID, out GameObject cardToRemove))
        {
            cardsInOpponentHand.Remove(cardInstanceID);
            cardOrder.Remove(cardInstanceID);

            cardToRemove.GetComponent<CanvasGroup>()?.DOFade(0, 0.2f).OnComplete(() => {
                Destroy(cardToRemove);
            });

            RepositionCards();
        }
    }

    private void RepositionCards()
    {
        int cardCount = cardOrder.Count;
        if (cardCount == 0) return;

        // --- THIS IS THE NEW LOGIC ---
        // Instead of centering, we anchor the hand from a fixed point (x=0)
        // and push existing cards to the left.

        for (int i = 0; i < cardCount; i++)
        {
            int currentCardId = cardOrder[i];
            GameObject cardObject = cardsInOpponentHand[currentCardId];

            RectTransform rt = cardObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                // The last card in the list (the newest one) will be at x=0.
                // Each previous card is shifted to the left by cardSpacing.
                float targetX = (i - (cardCount - 1)) * cardSpacing;
                Vector2 targetPos = new Vector2(targetX, 0);

                // This ensures the newest card (last in the list) is always on top.
                rt.SetSiblingIndex(i);

                rt.DOKill();
                rt.DOAnchorPos(targetPos, animationDuration).SetEase(animationEase);
            }
        }
    }
}
