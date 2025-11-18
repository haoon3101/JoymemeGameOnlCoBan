using System.Collections.Generic;
using UnityEngine;

public class CardPrefabDatabase : MonoBehaviour
{
    [System.Serializable]
    public struct CardPrefabPair
    {
        public CardData cardData;
        public GameObject prefab;
    }

    public List<CardPrefabPair> prefabPairs;
    private Dictionary<CardData, GameObject> prefabDictionary;

    void Awake()
    {
        prefabDictionary = new Dictionary<CardData, GameObject>();
        foreach (var pair in prefabPairs)
        {
            if (pair.cardData != null && pair.prefab != null)
                prefabDictionary[pair.cardData] = pair.prefab;
        }
    }

    public GameObject GetPrefabForCard(CardData card)
    {
        if (card == null) return null;

        if (prefabDictionary.TryGetValue(card, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning("No prefab found for card: " + card.cardName);
        return null;
    }
}
