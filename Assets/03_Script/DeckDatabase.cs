using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// This allows you to create an instance of this database in your project assets
[CreateAssetMenu(fileName = "DeckDatabase", menuName = "Game/Deck Database")]
public class DeckDatabase : ScriptableObject
{
    [SerializeField] private List<DeckData> allDecks;

    // This method allows other scripts to find a deck by its name
    public DeckData GetDeckByName(string deckName)
    {
        if (string.IsNullOrEmpty(deckName)) return null;

        // Find the first deck in the list where the asset name matches the requested name
        return allDecks.FirstOrDefault(deck => deck.name == deckName);
    }
}