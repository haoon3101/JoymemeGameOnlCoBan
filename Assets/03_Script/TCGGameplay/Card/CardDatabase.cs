using UnityEngine;
using System.Collections.Generic;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance { get; private set; }

    [SerializeField] private CardData[] allCards;

    private Dictionary<int, CardData> cardDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);

            cardDict = new Dictionary<int, CardData>();
            foreach (var card in allCards)
            {
                if (!cardDict.ContainsKey(card.cardId))
                    cardDict.Add(card.cardId, card);
                else
                    Debug.LogWarning($"Duplicate cardId {card.cardId} found in CardDatabase");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public CardData GetCardById(int cardId)
    {
        if (cardDict.TryGetValue(cardId, out var cardData))
            return cardData;
        Debug.LogError($"CardDatabase: No card found with ID {cardId}");
        return null;
    }
}
