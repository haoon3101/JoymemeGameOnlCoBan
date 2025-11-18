using UnityEngine;
using UnityEngine.EventSystems;

// This ensures the script is always on the same object as a DraggableCard
[RequireComponent(typeof(DraggableCard))]
public class InspectableCard : MonoBehaviour, IPointerClickHandler
{
    private DraggableCard draggableCard;
    [SerializeField] private AudioSource checkCardSFX;

    private void Awake()
    {
        // Get a reference to the DraggableCard script on this same object
        draggableCard = GetComponent<DraggableCard>();
        Debug.Assert(draggableCard != null, "InspectableCard requires a DraggableCard component on the same GameObject.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if the click was a right-click
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // We only want to inspect if we are not currently dragging a card
            if (draggableCard.cardData != null && UnitInspector.Instance != null)
            {
                // Tell the inspector to show the details for this card's data.
                // The second parameter (UnitInstance) is null because this card is
                // in the hand, not a live unit on the field.
                Debug.Log($"InspectableCard: Inspecting card {draggableCard.cardData.name}");
                UnitInspector.Instance.ShowUnitDetails(draggableCard.cardData, null);
                checkCardSFX.Play();
            }
            
        }
    }
}