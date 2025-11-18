using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("Core UI Elements")]
    [SerializeField] private Image cardArt;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;

    // --- NEW: Add references to your frame objects ---
    [Header("Card Frames")]
    [Tooltip("The GameObject for the Unit card frame.")]
    [SerializeField] private GameObject unitFrameObject;
    [Tooltip("The GameObject for the Spell card frame.")]
    [SerializeField] private GameObject spellFrameObject;

    public void Setup(CardData data)
    {
        if (data == null) return;

        cardArt.sprite = data.cardImage;
        nameText.text = data.cardName;
        descText.text = data.description;
        manaText.text = data.manaCost.ToString();

        // --- NEW: Logic to enable the correct frame ---
        if (unitFrameObject != null && spellFrameObject != null)
        {
            // If the card is a Unit, show the unit frame and hide the spell frame.
            unitFrameObject.SetActive(data.cardType == CardType.Unit);
            // If the card is a Spell, show the spell frame and hide the unit frame.
            spellFrameObject.SetActive(data.cardType == CardType.Spell);
        }

        if (data.cardType == CardType.Unit)
        {
            attackText.text = data.attack.ToString();
            healthText.text = data.health.ToString();
        }
        else // For Spell cards
        {
            attackText.text = "";
            healthText.text = "";
        }
    }
}
