using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDeck", menuName = "TCG/Deck Data")]
public class DeckData : ScriptableObject
{
    // It's good practice to have an explicit ID, even if we load by name.
    public string deckID;
    public List<CardData> cards;
}