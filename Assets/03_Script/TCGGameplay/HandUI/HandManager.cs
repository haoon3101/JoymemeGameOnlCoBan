using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    private List<GameObject> cardsInHand = new List<GameObject>();

    public IReadOnlyList<GameObject> CardsInHand => cardsInHand;

    public void AddCardToHand(GameObject cardGO)
    {
        cardsInHand.Add(cardGO);
    }

    public void RemoveCardFromHand(GameObject cardGO)
    {
        if (cardsInHand.Contains(cardGO))
        {
            cardsInHand.Remove(cardGO);
        }
    }

    public void ClearHand()
    {
        cardsInHand.Clear();
    }

    public int GetCardCount()
    {
        return cardsInHand.Count;
    }
}
