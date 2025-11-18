using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class ChooseDeck : MonoBehaviourPunCallbacks
{
    [Header("Deck Identifiers")]
    // IMPORTANT: These must match the file names of your DeckData assets in the Resources/Decks folder.
    [SerializeField] private string memeDeckName = "memeDeck";
    [SerializeField] private string minecraftDeckName = "minecraftDeck";

    [Header("UI References")]
    [SerializeField] private Button memeDeckBtn;
    [SerializeField] private Button minecraftDeckBtn;
    [SerializeField] private Button readyBtn; // NEW: Assign your "Ready" or "Confirm" button here
    [SerializeField] private TextMeshProUGUI feedbackText;

    public const string DECK_CHOICE_PROPERTY = "deckName";
    public const string PLAYER_READY_PROPERTY = "isReady";

    private string locallySelectedDeck; // Stores the choice before it's confirmed

    private void Start()
    {
        // At the start, players can choose a deck, but cannot be ready yet.
        SetDeckButtonsInteractable(true);
        readyBtn.interactable = false;
        if (feedbackText != null) feedbackText.text = "Choose your deck";

        memeDeckBtn.onClick.AddListener(() => OnDeckButtonClicked(memeDeckName));
        minecraftDeckBtn.onClick.AddListener(() => OnDeckButtonClicked(minecraftDeckName));
        readyBtn.onClick.AddListener(OnReadyButtonClicked);
    }

    // Step 1: Player clicks a deck button. This is a local action.
    public void OnDeckButtonClicked(string selectedDeckName)
    {
        if (string.IsNullOrEmpty(selectedDeckName)) return;

        // Store the choice locally
        locallySelectedDeck = selectedDeckName;
        Debug.Log($"Locally selected deck: {locallySelectedDeck}");

        // Update UI and enable the ready button so they can confirm
        if (feedbackText != null) feedbackText.text = $"Selected:\n{locallySelectedDeck}";
        readyBtn.interactable = true;
    }

    // Step 2: Player clicks the "Ready" button to confirm their choice and notify others.
    public void OnReadyButtonClicked()
    {
        if (string.IsNullOrEmpty(locallySelectedDeck))
        {
            Debug.LogError("Ready button clicked but no deck was selected locally!");
            return;
        }

        // Create a Hashtable to send both properties to the network at once
        Hashtable playerProps = new Hashtable
        {
            { DECK_CHOICE_PROPERTY, locallySelectedDeck },
            { PLAYER_READY_PROPERTY, true }
        };

        // Set the custom properties for the local player. This syncs to all other players.
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        Debug.Log($"Player is Ready. Deck choice '{locallySelectedDeck}' sent to Photon.");

        // Disable all buttons to prevent the player from changing their mind
        SetDeckButtonsInteractable(false);
        readyBtn.interactable = false;
        if (feedbackText != null) feedbackText.text = "Waiting for opponent...";
    }

    private void SetDeckButtonsInteractable(bool isInteractable)
    {
        memeDeckBtn.interactable = isInteractable;
        minecraftDeckBtn.interactable = isInteractable;
    }

    // When a player leaves a room, reset their UI state
    public override void OnLeftRoom()
    {
        SetDeckButtonsInteractable(true);
        readyBtn.interactable = false;
        locallySelectedDeck = null;
        if (feedbackText != null) feedbackText.text = "Choose your deck";
    }
}
