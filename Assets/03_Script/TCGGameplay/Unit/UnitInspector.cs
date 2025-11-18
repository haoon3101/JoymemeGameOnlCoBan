using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitInspector : MonoBehaviour
{
    public static UnitInspector Instance;

    [Header("UI References")]
    [SerializeField] private GameObject inspectorPanel;
    // --- CHANGED: This now references your new, more detailed UI script ---
    [SerializeField] private CardInfoUI cardDisplay;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inspectorPanel.SetActive(false);
        closeButton.onClick.AddListener(HideInspector);
    }

    // This method is called by units on the field when they are clicked
    public void ShowUnitDetails(CardData cardData, UnitInstance unitInstance)
    {
        if (cardData == null) return;

        // --- CHANGED: Call the new Setup method on CardInfoUI ---
        // This will populate all the detailed fields, including the live stats from the unit.
        cardDisplay.Setup(cardData, unitInstance);

        inspectorPanel.SetActive(true);
    }

    public void HideInspector()
    {
        inspectorPanel.SetActive(false);
    }
}
