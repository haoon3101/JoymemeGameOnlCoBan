using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;

public class GameStateUI : MonoBehaviourPunCallbacks
{
    public static GameStateUI Instance;
    [SerializeField] private AudioSource turnSFX;
    [SerializeField] private AudioSource turnFightSFX;
    [SerializeField] private AudioSource battleSong;

    [Header("Player Health UI")]
    [SerializeField] private TMP_Text yourHealthText;
    [SerializeField] private TMP_Text opponentHealthText;

    [Header("Player Mana UI")]
    [SerializeField] private TMP_Text yourManaText;
    [SerializeField] private TMP_Text opponentManaText;
    [SerializeField] private List<Image> yourManaCells;
    [SerializeField] private Color filledColor = Color.blue;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Phase Announcers")]
    [SerializeField] private Animator phaseAnimator;

    [Header("Game Over UI")]
    [SerializeField] private GameObject winPanelPrefab;
    [SerializeField] private GameObject lossPanelPrefab;
    [SerializeField] private GameObject endGameClick;
    [Tooltip("The main canvas to spawn the Game Over panels onto.")]
    [SerializeField] private Transform mainCanvas;


    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    // --- NEW: Add a reference to your Surrender button ---
    [SerializeField] private Button surrenderButton;


    public PhotonView photonView { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        photonView = GetComponent<PhotonView>();
        if (phaseAnimator == null) phaseAnimator = GetComponent<Animator>();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // --- NEW: Add a listener for the surrender button ---
        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(OnSurrenderButtonPressed);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    // --- NEW: Method to handle the surrender button click ---
    public void OnSurrenderButtonPressed()
    {
        // Send an RPC to the GameManager, telling it that this player wants to surrender.
        if (GameManager.Instance != null)
        {
            Debug.Log("Surrender button clicked. Sending request to GameManager.");
            GameManager.Instance.photonView.RPC("RPC_PlayerSurrender", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);

            // Optionally, disable the button immediately to prevent spamming
            surrenderButton.interactable = false;
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            
        }
    }

    public void ShowGameOver(int defeatedPlayerActorId)
    {
        bool localPlayerWon = (defeatedPlayerActorId != PhotonNetwork.LocalPlayer.ActorNumber);
        if (mainCanvas == null) return;

        if (localPlayerWon)
        {
            if (winPanelPrefab != null)
            {
                Instantiate(winPanelPrefab, mainCanvas);
                battleSong.Pause();
            }
        }
        else
        {
            if (lossPanelPrefab != null)
            {
                Instantiate(lossPanelPrefab, mainCanvas);
                battleSong.Pause();
            }
        }
        if (endGameClick != null)
        {
            endGameClick.SetActive(true);
            Transform endButTransform = endGameClick.transform;
            endButTransform.SetAsLastSibling(); // Ensure the settings panel is on top
        }
    }

    [PunRPC]
    public void RPC_AnnouncePhase(int phaseValue, int currentTurnActorId)
    {
        if (phaseAnimator == null) return;
        GamePhase phase = (GamePhase)phaseValue;

        switch (phase)
        {
            case GamePhase.Draw:
                phaseAnimator.SetTrigger("DrawTurn");
                turnSFX.Play();
                break;
            case GamePhase.Placement:
                if (currentTurnActorId == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    phaseAnimator.SetTrigger("YourTurn");
                    turnSFX.Play();
                }
                else
                {
                    phaseAnimator.SetTrigger("OpponentTurn");
                    turnSFX.Play();
                }
                break;
            case GamePhase.Combat:
                phaseAnimator.SetTrigger("CombatTurn");
                turnFightSFX.Play();
                break;
        }
    }

    #region Existing UI Methods
    public void UpdatePlayerHealthUI(int actorId, int health)
    {
        if (yourHealthText == null || opponentHealthText == null) return;
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorId)
            yourHealthText.text = $"{health}";
        else
            opponentHealthText.text = $"{health}";
    }

    public void UpdateManaUI(int actorId, int amount)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorId)
        {
            yourManaText.text = $"{amount}";
            UpdateManaBar(amount);
        }
        else
        {
            opponentManaText.text = $"{amount}";
        }
    }

    private void UpdateManaBar(int amount)
    {
        if (yourManaCells == null || yourManaCells.Count == 0) return;
        for (int i = 0; i < yourManaCells.Count; i++)
        {
            if (yourManaCells[i] != null)
                yourManaCells[i].color = i < amount ? filledColor : emptyColor;
        }
    }
    #endregion
}
