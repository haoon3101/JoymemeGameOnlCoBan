using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfoUI : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image cardSprite;

    // --- NEW: Direct object references for the type icons ---
    [Header("Type Icon Objects")]
    [Tooltip("The GameObject that contains the 'Unit' type icon.")]
    [SerializeField] private GameObject unitTypeObject;
    [Tooltip("The GameObject that contains the 'Spell' type icon.")]
    [SerializeField] private GameObject spellTypeObject;

    public void Setup(CardData data, UnitInstance liveUnit)
    {
        if (data == null) return;

        nameText.text = data.cardName;
        descriptionText.text = data.abilityDescription;
        loreText.text = data.description;
        costText.text = data.manaCost.ToString();
        cardSprite.sprite = data.cardImage;

        // --- NEW: Simple check to show the correct icon object ---
        if (unitTypeObject != null && spellTypeObject != null)
        {
            // Show the 'Unit' icon and hide the 'Spell' icon if it's a unit card
            unitTypeObject.SetActive(data.cardType == CardType.Unit);
            // Show the 'Spell' icon and hide the 'Unit' icon if it's a spell card
            spellTypeObject.SetActive(data.cardType == CardType.Spell);
        }

        if (liveUnit != null)
        {
            atkText.text = liveUnit.currentAttack.ToString();
            healthText.text = liveUnit.currentHealth.ToString();
        }
        else
        {
            if (data.cardType == CardType.Unit)
            {
                atkText.text = data.attack.ToString();
                healthText.text = data.health.ToString();
            }
            else
            {
                atkText.text = "";
                healthText.text = "";
            }
        }
    }
}
