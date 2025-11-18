using UnityEngine;

[CreateAssetMenu(menuName = "Card Data")]
public class CardData : ScriptableObject
{
    public int cardId;
    public string cardName;
    public string description;
    public string abilityDescription;
    public Sprite cardImage;
    public int manaCost;
    public int health;
    public int attack;
    public CardType cardType;
    public GameObject unitPrefab;

    // Only used if cardType == Spell
    public SpellEffect spellEffect;
}

public enum CardType { Unit, Spell }
