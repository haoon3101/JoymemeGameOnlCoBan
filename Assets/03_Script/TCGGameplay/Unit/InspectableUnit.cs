using UnityEngine;
using UnityEngine.EventSystems;

// This script goes on the 3D unit prefabs on the board
public class InspectableUnit : MonoBehaviour, IPointerClickHandler
{
    private UnitInstance unitInstance;
    private CardData cardData;

    private void Awake()
    {
        unitInstance = GetComponent<UnitInstance>();
    }

    // This is called when the UnitInstance is initialized
    public void SetCardData(CardData data)
    {
        cardData = data;
    }

    // When the unit on the board is clicked...
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("InspectableUnit: CardData is not set!");
            return;
        }

        // ...find the inspector and tell it to show this unit's details.
        UnitInspector.Instance?.ShowUnitDetails(cardData, unitInstance);
    }
}
