using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class CardSpawnerButton : MonoBehaviourPun
{
    [SerializeField] private Transform handPanel;
    [SerializeField] private GameObject cardUIPrefab;
    [SerializeField] private List<CardData> availableCards;

    public void SpawnRandomCard()
    {
        if (!photonView.IsMine) return;

        CardData randomCard = availableCards[Random.Range(0, availableCards.Count)];
        GameObject newCard = Instantiate(cardUIPrefab, handPanel);
        newCard.GetComponent<CardUI>().Setup(randomCard);
        newCard.GetComponent<DraggableCard>().cardData = randomCard;
    }
}
