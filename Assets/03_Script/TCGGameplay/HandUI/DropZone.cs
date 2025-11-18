using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using System.Collections;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public int localLaneIndex;
    [Tooltip("Check this box for the opponent's drop zones.")]
    public bool isOpponentZone = false;

    private Lane3D correspondingLane;
    private UnitInstance currentUnitInLane;
    private UnitData currentUnitData;

    public UnitInstance GetCurrentUnitInLane() => currentUnitInLane;
    public UnitData GetCurrentUnitData() => currentUnitData;

    public void RegisterUnitData(UnitData data)
    {
        currentUnitData = data;
        Debug.Log($"DropZone {localLaneIndex} updated unit data. ATK: {data.CurrentAttack}, HP: {data.CurrentHealth}");
    }
    public void RegisterUnit(UnitInstance unit)
    {
        currentUnitInLane = unit;
        currentUnitData = new UnitData(unit);
    }
    public void ClearUnit()
    {
        currentUnitInLane = null;
        currentUnitData = new UnitData(null);
    }
    public void LinkToLane(Lane3D lane) => correspondingLane = lane;

    // --- THIS IS NOW THE MAIN VALIDATION LOGIC ---
    public void OnDrop(PointerEventData eventData)
    {
        DraggableCard card = eventData.pointerDrag?.GetComponent<DraggableCard>();
        if (card == null || card.cardData == null || card.HasBeenDropped)
        {
            return;
        }

        // Perform ALL client-side validation here, instantly.
        if (IsValidDrop(card))
        {
            // If the drop is valid:
            // 1. Mark the card so it knows not to snap back.
            card.MarkAsDropped();

            if (OpponentHandUI.Instance != null)
            {
                OpponentHandUI.Instance.photonView.RPC("RPC_RemoveCardFromOpponentHand", RpcTarget.Others, card.uiCardInstanceID);
            }
            // 2. Send the appropriate RPC request to the server.
            if (card.cardData.cardType == CardType.Unit)
            {
                PhotonView.Get(GameManager.Instance).RPC("RPC_RequestPlayUnit", RpcTarget.MasterClient,
                    card.cardData.cardId, correspondingLane.BoardLaneIndex, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else if (card.cardData.cardType == CardType.Spell)
            {
                PhotonView.Get(GameManager.Instance).RPC("RPC_RequestPlaySpell", RpcTarget.MasterClient,
                    card.cardData.cardId, currentUnitInLane.photonView.ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
            }

            // 3. Tell the card to fade out and destroy itself from the hand.
            StartCoroutine(card.FadeOutAndDestroy(card));
        }
        // If IsValidDrop is false, we do nothing. The card's OnEndDrag will see it wasn't
        // marked as dropped and will automatically handle the snap back.
    }

    private bool IsValidDrop(DraggableCard card)
    {
        // Basic game state checks
        if (GameManager.Instance.CurrentTurnActorId != PhotonNetwork.LocalPlayer.ActorNumber ||
            GameManager.Instance.CurrentPhase != GamePhase.Placement ||
            !ManaManager.Instance.HasEnoughMana(PhotonNetwork.LocalPlayer.ActorNumber, card.cardData.manaCost))
        {
            return false;
        }

        // Card and target specific checks
        if (card.cardData.cardType == CardType.Unit)
        {
            return !isOpponentZone && currentUnitInLane == null;
        }
        else if (card.cardData.cardType == CardType.Spell)
        {
            if (currentUnitInLane == null) return false;

            if (card.cardData.spellEffect is StatModifierEffect statEffect)
            {
                bool isTargetAnAlly = (currentUnitInLane.OwnerActorId == PhotonNetwork.LocalPlayer.ActorNumber);
                if (statEffect.targetAllegiance == TargetAllegiance.Ally && !isTargetAnAlly) return false;
                if (statEffect.targetAllegiance == TargetAllegiance.Enemy && isTargetAnAlly) return false;

                if (statEffect.requireConditions)
                {
                    foreach (var condition in statEffect.conditions)
                    {
                        int statValue = (condition.stat == StatType.Attack) ? currentUnitData.CurrentAttack : currentUnitData.CurrentHealth;
                        bool conditionMet = condition.comparison switch
                        {
                            ComparisonType.GreaterThan => statValue > condition.value,
                            ComparisonType.LessThan => statValue < condition.value,
                            ComparisonType.EqualTo => statValue == condition.value,
                            ComparisonType.GreaterOrEqual => statValue >= condition.value,
                            ComparisonType.LessOrEqual => statValue <= condition.value,
                            _ => false
                        };
                        if (!conditionMet) return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentUnitInLane != null && eventData.pointerDrag == null)
        {
            CardData unitCardData = CardDatabase.Instance.GetCardById(currentUnitInLane.cardId);
            if (unitCardData != null)
            {
                UnitInspector.Instance?.ShowUnitDetails(unitCardData, currentUnitInLane);
            }
        }
    }
}
