using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandSpawner : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Transform handPanel;
    [SerializeField] private GameObject cardUIPrefab;
    [SerializeField] private DropZone[] dropZones;
    [SerializeField] private HandManager handManager;
    [SerializeField] private AudioSource songGame;
    [SerializeField] private AudioSource handSpawnCardSound;

    [Header("Opponent Interaction")]
    [SerializeField] private DropZone[] opponentDropZones;

    private DeckData startingDeck;
    private List<CardData> remainingDeck;

    public void InitializeDeck()
    {
        if (!photonView.IsMine) return;

        var owner = photonView.Owner;

        if (owner.CustomProperties.TryGetValue(ChooseDeck.DECK_CHOICE_PROPERTY, out object deckNameObject))
        {
            string deckName = deckNameObject as string;
            string resourcePath = "Decks/" + deckName;
            startingDeck = Resources.Load<DeckData>(resourcePath);
        }

        if (startingDeck == null)
        {
            Debug.LogWarning($"[HandSpawner] No deck choice found for player {owner.NickName}. Loading a default deck.");
            startingDeck = Resources.Load<DeckData>("Decks/memeDeck");
        }

        if (startingDeck == null)
        {
            Debug.LogError($"[HandSpawner] FAILED to load any deck for player {owner.NickName}!");
            return;
        }

        remainingDeck = new List<CardData>(startingDeck.cards);
        ShuffleDeck(remainingDeck);
    }

    void LinkDropZonesToLanes()
    {
        Lane3D[] allLanes = FindObjectsOfType<Lane3D>();
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        int opponentId = (myId == 1) ? 2 : 1;
        bool isSecondPlayer = myId != 1;

        // --- Link Local Player's DropZones ---
        foreach (DropZone dz in dropZones)
        {
            // This logic is correct: Player 1's view is direct, Player 2's is flipped.
            int boardIndex = isSecondPlayer ? 4 - dz.localLaneIndex : dz.localLaneIndex;
            Lane3D myLane = allLanes.FirstOrDefault(l => l.PlayerOwnerId == myId && l.BoardLaneIndex == boardIndex);
            if (myLane != null)
            {
                dz.LinkToLane(myLane);
                myLane.LinkToDropZone(dz);
            }
        }

        // --- Link Opponent's DropZones ---
        foreach (DropZone dz in opponentDropZones)
        {
            // --- THIS IS THE FIX ---
            // The mapping for the opponent's lanes depends on who is looking.
            // MasterClient (P1) sees the opponent's lanes directly (0 -> 0).
            // NormalClient (P2) sees the opponent's lanes flipped (0 -> 4).
            int boardIndex = isSecondPlayer ? 4 - dz.localLaneIndex : dz.localLaneIndex;

            Lane3D opponentLane = allLanes.FirstOrDefault(l => l.PlayerOwnerId == opponentId && l.BoardLaneIndex == boardIndex);
            if (opponentLane != null)
            {
                dz.LinkToLane(opponentLane);
                opponentLane.LinkToDropZone(dz);
                Debug.Log($"[HandSpawner] Linked OPPONENT DropZone (UI index {dz.localLaneIndex}) to Lane3D (Board index {opponentLane.BoardLaneIndex}) for Player {opponentId}");
            }
        }
    }

    // --- The rest of your script remains the same ---
    #region Existing Methods
    private void Awake() { }
    IEnumerator Start()
    {
        GameObject roomUI = GameObject.FindWithTag("RoomUi");
        while (roomUI != null && roomUI.activeInHierarchy)
        {
            yield return null;
        }

        if (photonView.IsMine)
        {
            StartCoroutine(DelayedLayout());
            LinkDropZonesToLanes();
        }
        songGame.Play();
    }

    private IEnumerator DelayedLayout()
    {
        yield return null;
        GetComponent<HandLayout>()?.RepositionCards();
    }
    public void DrawCard(bool repositionAfter = true)
    {
        if (!photonView.IsMine || remainingDeck == null || remainingDeck.Count == 0)
            return;

        CardData card = remainingDeck[0];
        remainingDeck.RemoveAt(0);

        GameObject cardGO = Instantiate(cardUIPrefab, handPanel);
        CardUI cardUI = cardGO.GetComponent<CardUI>();
        cardUI.Setup(card);

        DraggableCard draggableCard = cardGO.GetComponent<DraggableCard>();
        if (draggableCard != null)
            draggableCard.cardData = card;
        handSpawnCardSound.Play();
        handManager?.AddCardToHand(cardGO);

        if (OpponentHandUI.Instance != null && draggableCard != null)
        {
            OpponentHandUI.Instance.photonView.RPC("RPC_AddCardToOpponentHand", RpcTarget.Others, draggableCard.uiCardInstanceID);
        }

        if (repositionAfter)
            GetComponent<HandLayout>()?.RepositionCards();
    }

    void ShuffleDeck(List<CardData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }
    #endregion
}
