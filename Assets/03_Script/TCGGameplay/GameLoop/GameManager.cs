using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using static SpellEffect;
//using System;

public enum GamePhase
{
    None,
    Draw,
    Placement,
    Combat
}

public class GameManager : MonoBehaviourPunCallbacks
{
    #region 📊 Fields & Properties (keep top of class)
    public static GameManager Instance;
    public static bool GameManagerStarted { get; private set; }
    public int CurrentTurnActorId { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentRound => currentRound;

    private int firstPlayerActorId;
    private int secondPlayerActorId;
    private bool phaseEnded = false;
    //private bool gameStarted = false;
    private int currentRound = 1;
    private int pendingUnitSpawns = 0;
    private const int StartingHealth = 30;
    private Coroutine slowMoCoroutine;
    private float lastSlowMoDuration = 1.5f;
    [Header("Game Effects")]
    [SerializeField] private GameObject matrixTransitionPrefab;

    [SerializeField] private Button endPhaseButton;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private AudioSource songAudio;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text turnIndicatorText;
    [SerializeField] private GameObject gameStateUI;

    [Header("MapAnimation")]
    [SerializeField] private GameObject gameMap;
    [SerializeField] private GameObject laneVisualizer;

    private Dictionary<int, int> playerHealths = new Dictionary<int, int>();
    private Dictionary<int, Lane3D[]> playerLanes = new Dictionary<int, Lane3D[]>();
    public Dictionary<int, WallHitEffect> playerWalls = new Dictionary<int, WallHitEffect>();
    
    public event System.Action OnRoundAdvanced;
    #endregion

    #region 🔧 Initialization & Setup
    private void Awake()
    {
        GameManagerStarted = false;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (endPhaseButton != null)
        {
            Debug.Log("[GameManager] Adding listener to endPhaseButton");
            endPhaseButton.onClick.AddListener(OnEndPhaseButtonPressed);
            endPhaseButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[GameManager] endPhaseButton is NOT assigned!");
        }
    }
    private void Start()
    {
        StartCoroutine(WaitForRoomAndPlayers());
    }
    IEnumerator WaitForRoomAndPlayers()
    {
        GameObject roomUI = GameObject.FindWithTag("RoomUi");
        while (roomUI != null && roomUI.activeInHierarchy)
            yield return null;

        while (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2)
            yield return null;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartGameRPC", RpcTarget.All);
            photonView.RPC("TurnOffStateText", RpcTarget.All); // Turn off connection state text
        }
    }
    [PunRPC] private void StartGameRPC()
    {
        Debug.Log("[GameManager] Game started");
        songAudio?.Play();
        //gameStarted = true;

        var players = PhotonNetwork.PlayerList;
        if (players.Length < 2) return;

        InitializePlayers();
        ManaManager.Instance.InitializePlayers();

        if (PhotonNetwork.IsMasterClient )
        {
            int randomIndex = Random.Range(0, 2);
            int first = players[randomIndex].ActorNumber;
            int second = players[1 - randomIndex].ActorNumber;

            photonView.RPC("AssignPlayerOrderRPC", RpcTarget.AllBuffered, first, second);
        }
        UpdateCardGrayStates();
    }
    private void InitializePlayers()
    {
        var players = PhotonNetwork.PlayerList;
        playerHealths.Clear();
        playerLanes.Clear();

        foreach (var player in players)
        {
            playerHealths[player.ActorNumber] = StartingHealth;

            var lanes = FindObjectsOfType<Lane3D>()
                .Where(l => l.PlayerOwnerId == player.ActorNumber)
                .OrderBy(l => l.BoardLaneIndex)
                .ToArray();

            playerLanes[player.ActorNumber] = lanes;

            // Register lanes with BoardManager
            BoardManager.Instance?.RegisterLanesForPlayer(player.ActorNumber, lanes);
        }

        // Update UI health for all players
        foreach (var player in players)
        {
            GameStateUI.Instance?.UpdatePlayerHealthUI(player.ActorNumber, StartingHealth);
        }
    }
    [PunRPC] private void AssignPlayerOrderRPC(int player1Id, int player2Id)
    {
        this.firstPlayerActorId = player1Id;
        this.secondPlayerActorId = player2Id;

        gameStateUI.SetActive(true);
        Debug.Log($"[GameManager] First player: {player1Id}, Second player: {player2Id}");

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetPhase", RpcTarget.All, (int)GamePhase.Draw);
            photonView.RPC("RPC_SetTurn", RpcTarget.AllBuffered, firstPlayerActorId);
            StartCoroutine(SetupPhase());
        }
    }
    #endregion

    #region 🔁 Main Game Loop & Phases
    IEnumerator GameLoop()
    {

        Debug.Log($"[GameManager] Game loop started with first: {firstPlayerActorId}, second: {secondPlayerActorId}");
        currentRound = 1;

        while (true)
        {
            photonView.RPC("RPC_SetRound", RpcTarget.All, currentRound);
            OnRoundAdvanced?.Invoke();
            yield return StartCoroutine(DrawPhase());
            yield return StartCoroutine(PlacementPhase("Player 1", firstPlayerActorId));
            yield return StartCoroutine(PlacementPhase("Player 2", secondPlayerActorId));
            yield return StartCoroutine(CombatPhase());
            currentRound++;
        }
    }
    IEnumerator SetupPhase()
    {
        GameObject roomUI = GameObject.FindWithTag("RoomUi");
        while (roomUI != null && roomUI.activeInHierarchy)
        {
            yield return null;
        }
        Debug.Log("[GameManager] Setup Phase started");
        photonView.RPC("RPC_InitializeDeck", RpcTarget.All);
        photonView.RPC("RPC_InitializeMatrix", RpcTarget.All);
        yield return new WaitForSeconds(4f);
        photonView.RPC("RPC_MapAnimation", RpcTarget.All); // Trigger map animation
        if (PhotonNetwork.IsMasterClient)
        {
            var player1Lanes = GetLanesForPlayer(firstPlayerActorId);
            var player2Lanes = GetLanesForPlayer(secondPlayerActorId);

            int lanesCount = Mathf.Min(player1Lanes.Length, player2Lanes.Length);
            for (int i = 0; i < lanesCount; i++)
            {
                photonView.RPC("RPC_HighlightLane", RpcTarget.All, i, (int)LaneVisualizerManager.LaneVisualMode.Combat);
                yield return new WaitForSeconds(0.2f);
                photonView.RPC("RPC_ClearLaneHighlights", RpcTarget.All);
            }
        }
        
        
        yield return new WaitForSeconds(1f);
        photonView.RPC("RPC_SetPhase", RpcTarget.All, (int)GamePhase.None); // Or Setup phase enum
        photonView.RPC("SetPhaseTextRPC", RpcTarget.All, "Setting Up...");

        for (int i = 0; i < 4; i++)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                photonView.RPC("RPC_DrawCardForPlayer", player, player.ActorNumber);
                yield return new WaitForSeconds(0.2f);
            }
        }

        StartCoroutine(GameLoop()); // now start main loop
    }
    IEnumerator DrawPhase()
    {
        GameStateUI.Instance.photonView.RPC("RPC_AnnouncePhase", RpcTarget.All, (int)GamePhase.Draw, 0);
        yield return new WaitForSeconds(1f); // small delay for sync / animation
        photonView.RPC("RPC_SetPhase", RpcTarget.All, (int)GamePhase.Draw);
        photonView.RPC("SetPhaseTextRPC", RpcTarget.All, "Draw Phase");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            photonView.RPC("RPC_DrawCardForPlayer", player, player.ActorNumber);
            yield return new WaitForSeconds(0.3f); // small delay for sync / animation
        }
    }

    IEnumerator PlacementPhase(string label, int actorId)
    {
        GameStateUI.Instance.photonView.RPC("RPC_AnnouncePhase", RpcTarget.All, (int)GamePhase.Placement, actorId);
        yield return new WaitForSeconds(1f); // small delay for sync / animation
        Debug.Log($"[GameManager] {label} Placement Phase (ActorID {actorId})");
        photonView.RPC("RPC_SetPhase", RpcTarget.All, (int)GamePhase.Placement);
        photonView.RPC("RPC_SetTurn", RpcTarget.All, actorId);
        photonView.RPC("SetPhaseTextRPC", RpcTarget.All, $"{label} Placement");

        phaseEnded = false;
        photonView.RPC("SetEndPhaseButtonActive", RpcTarget.All, actorId);

        while (!phaseEnded)
            yield return null;

        photonView.RPC("SetEndPhaseButtonActive", RpcTarget.All, 0);
    }
    IEnumerator CombatPhase() //Chỗ này các card đấm nhau
    {
        while (GameManager.Instance.HasPendingUnitSpawns())
        {
            Debug.Log("[CombatPhase] Waiting for unit spawns to finish...");
            yield return null;
        }
        GameStateUI.Instance.photonView.RPC("RPC_AnnouncePhase", RpcTarget.All, (int)GamePhase.Combat, 0);
        yield return new WaitForSeconds(1f); // small delay for sync / animation
        photonView.RPC("RPC_SetPhase", RpcTarget.All, (int)GamePhase.Combat);
        photonView.RPC("SetPhaseTextRPC", RpcTarget.All, "Combat Phase");
        Debug.Log("[GameManager] Combat Phase");

        var player1Lanes = GetLanesForPlayer(firstPlayerActorId);
        var player2Lanes = GetLanesForPlayer(secondPlayerActorId);

        int lanesCount = Mathf.Min(player1Lanes.Length, player2Lanes.Length);

        for (int i = 0; i < lanesCount; i++)
        {
            // 🔸 Highlight current lane
            
            photonView.RPC("RPC_HighlightLane", RpcTarget.All, i, (int)LaneVisualizerManager.LaneVisualMode.Combat);

            var lane1 = player1Lanes[i];
            var lane2 = player2Lanes[i];

            var unit1 = lane1.GetCurrentUnit();
            var unit2 = lane2.GetCurrentUnit();

            if (unit1 != null && unit2 != null)
            {
                yield return StartCoroutine(ResolveUnitCombat(unit1.gameObject, unit2.gameObject));
            }
            else if (unit1 != null && unit2 == null)
            {
                var u1 = unit1.GetComponent<UnitInstance>();
                int attack = u1.GetAttack();
                if (attack > 0)
                {
                    u1.photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "AttackPlayer");
                    yield return new WaitUntil(() => u1.HasDealtDamage);
                    yield return new WaitForSeconds(0.3f);
                    u1.photonView.RPC("RPC_ResetHasDealtDamage", RpcTarget.All);
                }
            }
            else if (unit2 != null && unit1 == null)
            {
                var u2 = unit2.GetComponent<UnitInstance>();
                int attack = u2.GetAttack();
                if (attack > 0)
                {
                    u2.photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "AttackPlayer");
                    yield return new WaitUntil(() => u2.HasDealtDamage);
                    yield return new WaitForSeconds(0.3f);
                    u2.photonView.RPC("RPC_ResetHasDealtDamage", RpcTarget.All);
                }
            }

            // 🔹 Small delay between lanes and clear highlight
            yield return new WaitForSeconds(0.3f);
            photonView.RPC("RPC_ClearLaneHighlights", RpcTarget.All);
        }

        // Final cleanup pass
        //CleanupDeadUnits(player1Lanes);
        //CleanupDeadUnits(player2Lanes);

        yield return new WaitForSeconds(1f); // Final pause before next round
    }
    private IEnumerator ResolveUnitCombat(GameObject unit1Obj, GameObject unit2Obj)
    {
        var unit1 = unit1Obj?.GetComponent<UnitInstance>();
        var unit2 = unit2Obj?.GetComponent<UnitInstance>();

        if (unit1 == null || unit2 == null) yield break;

        if (unit1.GetAttack() > 0)
        {
            unit1.StartAttack(unit2);
            yield return new WaitUntil(() => unit1.IsAttackFinished);
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            unit1.IsAttackFinished = true;
        }

        if (unit2.GetAttack() > 0)
        {
            unit2.StartAttack(unit1);
            yield return new WaitUntil(() => unit2.IsAttackFinished);
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            unit2.IsAttackFinished = true;
        }

        yield return new WaitForSeconds(0.5f);

        if (unit1.GetHealth() <= 0 && PhotonNetwork.IsMasterClient)
            UnitDied(unit1.photonView.ViewID);

        if (unit2.GetHealth() <= 0 && PhotonNetwork.IsMasterClient)
            UnitDied(unit2.photonView.ViewID);
    }
    #endregion

    #region 🔥 Unit Combat Handling
    private void TriggerUnitsTurnStart()
    {
        var allUnits = FindObjectsOfType<UnitInstance>();
        foreach (var unit in allUnits)
        {
            unit.OnTurnStart();
        }
    }
    private void CleanupDeadUnits(Lane3D[] lanes)
    {
        foreach (var lane in lanes)
        {
            var unit = lane.GetCurrentUnit();
            if (unit == null) continue;

            var unitInstance = unit.GetComponent<UnitInstance>();
            if (unitInstance == null) continue;

            if (unitInstance.GetHealth() <= 0)
            {
                PhotonNetwork.Destroy(unit.gameObject);
                lane.RemoveUnit(unit.gameObject);
            }
        }
    }
    public void UnitDied(int viewID)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            //Ham dissolve
            //GetComponent<CardDisappear>().TriggerDisappearSmooth(1f);
            // Ask the owner to destroy the unit
            targetView.RPC("RequestUnitDestroyRPC", targetView.Owner, viewID);
        }
    }
    #endregion

    #region 🧠 Game State Accessors
    public int GetPlayerHealth(int actorId)
    {
        return playerHealths.TryGetValue(actorId, out int hp) ? hp : 0;
    }
    public Lane3D[] GetLanesForPlayer(int actorId)
    {
        return playerLanes.TryGetValue(actorId, out var lanes) ? lanes : new Lane3D[0];
    }
    #endregion

    #region 💥 Player Damage & Defeat
    public void DealDamageToPlayer(int actorId, int damage)
    {
        if (!playerHealths.ContainsKey(actorId)) return;

        playerHealths[actorId] -= damage;
        if (playerHealths[actorId] < 0) playerHealths[actorId] = 0;

        photonView.RPC("UpdatePlayerHealthRPC", RpcTarget.All, actorId, playerHealths[actorId]);

        //if (playerHealths[actorId] == 0)
        //{
        //    photonView.RPC("OnPlayerDefeatedRPC", RpcTarget.All, actorId);
        //}
    }
    [PunRPC]
    public void RPC_TakePlayerDamage(int actorId, int damage, Vector3 hitPosition)
    {
        if (!playerHealths.ContainsKey(actorId)) return;

        DealDamageToPlayer(actorId, damage);

        // Optional: Sync UI
        photonView.RPC("UpdatePlayerHealthRPC", RpcTarget.All, actorId, playerHealths[actorId]);

        // Optional: Check for game over
        if (playerHealths[actorId] <= 0)
        {
            OnPlayerDefeatedRPC(actorId);
        }
    }
    [PunRPC]
    private void UpdatePlayerHealthRPC(int actorId, int newHealth)
    {
        playerHealths[actorId] = newHealth;
        Debug.Log($"[GameManager] Player {actorId} health updated: {newHealth}");

        GameStateUI.Instance?.UpdatePlayerHealthUI(actorId, newHealth);
    }

    [PunRPC]
    private void OnPlayerDefeatedRPC(int actorId)
    {
        Debug.Log($"[GameManager] Player {actorId} defeated!");
        Debug.Log($"[GameManager] Stopping game loop for player {actorId}");
        StopAllCoroutines();

        StartCoroutine(GameOverSequence(actorId));
        Debug.Log($"[GameManager] Player {actorId} health is now 0. Ending game loop.");
    }
    private IEnumerator GameOverSequence(int defeatedActorId)
    {
        // Wait for the duration of the slow-motion effect that was just triggered.
        // If no slow-mo was triggered, this will be 0 and the wait will be instant.
        Debug.Log($"[GameManager] Starting Game Over sequence. Waiting for {lastSlowMoDuration} seconds of slow-motion.");
        Debug.Log($"[GameManager] Current Time Scale: {Time.timeScale} (should be 1.0 if no slow-mo is active)");
        Debug.Log($"[GameManager] Current Time: {Time.time} (should be normal time if no slow-mo is active)");
        yield return new WaitForSecondsRealtime(lastSlowMoDuration);

        // Now that the wait is over, show the game over screen.
        Debug.Log($"[GameManager] Slow-motion finished. Showing Game Over screen.");
        GameStateUI.Instance?.ShowGameOver(defeatedActorId);
        
    }
    public void TriggerSlowMotion(float duration, float intensity)
    {
        // Stop any previous slow-motion effect to prevent conflicts
        if (slowMoCoroutine != null)
        {
            Time.timeScale = 1f; // Reset time immediately
            StopCoroutine(slowMoCoroutine);
        }

        lastSlowMoDuration = duration;
        slowMoCoroutine = StartCoroutine(SlowMotionCoroutine(duration, intensity));
    }

    private IEnumerator SlowMotionCoroutine(float duration, float intensity)
    {
        Debug.Log($"[GameManager] Starting slow motion! Intensity: {intensity}, Duration: {duration}");
        Time.timeScale = intensity;

        // Wait for the specified duration in REAL time, not game time
        yield return new WaitForSecondsRealtime(duration);

        Debug.Log("[GameManager] Restoring normal time.");
        Time.timeScale = 1f;
        slowMoCoroutine = null;
    }

    [PunRPC]
    public void RPC_PlayerSurrender(int surrenderingActorId)
    {
        // This RPC only runs on the MasterClient. It is the authoritative command.
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[GameManager] MasterClient received surrender request from player {surrenderingActorId}.");

        // Find the wall belonging to the player who surrendered.
        WallHitEffect surrenderingWall = FindWallOwnedBy(surrenderingActorId);
        if (surrenderingWall != null)
        {
            Vector3 spawnPosition = surrenderingWall.transform.position;

            // Add to the y-axis value
            spawnPosition.z += 1.0f; // Spawns it 1 unit higher
            // Tell all clients to play the shatter effect.
            // We use the wall's center as the hit position for a dramatic effect.
            surrenderingWall.photonView.RPC("RPC_ShatterWall", RpcTarget.All, spawnPosition);
        }

        // Authoritatively set the player's health to 0.
        if (playerHealths.ContainsKey(surrenderingActorId))
        {
            playerHealths[surrenderingActorId] = 0;
            // Update the UI for all players.
            photonView.RPC("UpdatePlayerHealthRPC", RpcTarget.All, surrenderingActorId, 0);
        }

        // Finally, call the existing defeat RPC to end the game for everyone.
        photonView.RPC("OnPlayerDefeatedRPC", RpcTarget.All, surrenderingActorId);
    }
    #endregion

    #region 🛡️ Lane & Wall Management
    public void RegisterWall(int actorId, WallHitEffect wall)
    {
        playerWalls[actorId] = wall;
    }

    public WallHitEffect FindWallOwnedBy(int actorId)
    {
        return playerWalls.TryGetValue(actorId, out var wall) ? wall : null;
    }
    #endregion

    #region 🎴 Unit Spawning / Playing Cards
    [PunRPC]
    public void RPC_RequestPlayUnit(int cardId, int laneIndex, int requestingPlayerId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        CardData cardData = CardDatabase.Instance.GetCardById(cardId);
        if (cardData == null || cardData.unitPrefab == null)
        {
            Debug.LogWarning($"[GameManager] Invalid card data for cardId {cardId}");
            return;
        }

        // MANA CHECK
        if (!ManaManager.Instance.HasEnoughMana(requestingPlayerId, cardData.manaCost))
        {
            Debug.LogWarning($"[GameManager] Player {requestingPlayerId} doesn't have enough mana for card {cardId}.");
            return;
        }

        // LANE VALIDATION
        Lane3D[] allLanes = FindObjectsOfType<Lane3D>();
        Lane3D lane = null;
        foreach (var l in allLanes)
        {
            if (l.BoardLaneIndex == laneIndex && l.PlayerOwnerId == requestingPlayerId)
            {
                lane = l;
                break;
            }
        }

        if (lane == null)
        {
            Debug.LogWarning($"[GameManager] Invalid lane for player {requestingPlayerId}.");
            return;
        }

        if (!lane.CanAddUnit())
        {
            Debug.LogWarning($"[GameManager] Lane {laneIndex} is already occupied.");
            return;
        }

        // SPEND MANA
        bool spent = ManaManager.Instance.TrySpendMana(requestingPlayerId, cardData.manaCost);
        if (!spent)
        {
            Debug.LogWarning($"[GameManager] Failed to spend mana for player {requestingPlayerId}");
            return;
        }
        photonView.RPC("RPC_ConfirmCardPlay", PhotonNetwork.CurrentRoom.GetPlayer(requestingPlayerId), cardId);
        GameManager.Instance.NotifyUnitSpawnStarted();

        // SPAWN UNIT
        Vector3 spawnPos = lane.GetUnitSpawnPosition();
        Quaternion rot = requestingPlayerId == 1 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        object[] instantiationData = new object[] { cardId, lane.SideId }; // 👈 pass sideId too!

        GameObject unit = PhotonNetwork.Instantiate(
            cardData.unitPrefab.name,
            spawnPos,
            rot,
            0,
            instantiationData);

        bool isOwnerView = requestingPlayerId == lane.PlayerOwnerId;
        lane.photonView.RPC("RPC_AddUnit", RpcTarget.AllBuffered, unit.GetComponent<PhotonView>().ViewID/*, isOwnerView*/);
        


    }
    [PunRPC]
    public void RPC_ConfirmCardPlay(int cardId)
    {
        Debug.Log($"[GameManager] Confirmed play of card {cardId}");
        //HandLayout.Instance?.RepositionCards(); // optional
        GameManager.Instance?.UpdateCardGrayStates(); // 👈 Update after mana is spent
    }
    #endregion

    #region ✨ Spell Effects
    [PunRPC]
    public void RPC_RequestPlaySpell(int cardId, int targetUnitViewID, int requestingPlayerId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        CardData cardData = CardDatabase.Instance.GetCardById(cardId);
        if (cardData == null || cardData.cardType != CardType.Spell) return;

        PhotonView targetView = PhotonView.Find(targetUnitViewID);
        if (targetView == null) return;
        UnitInstance targetUnit = targetView.GetComponent<UnitInstance>();
        if (targetUnit == null) return;

        if (cardData.spellEffect is StatModifierEffect statEffect)
        {
            bool isTargetAnAlly = (targetUnit.OwnerActorId == requestingPlayerId);
            if (statEffect.targetAllegiance == TargetAllegiance.Ally && !isTargetAnAlly) return;
            if (statEffect.targetAllegiance == TargetAllegiance.Enemy && isTargetAnAlly) return;
        }

        if (!ManaManager.Instance.TrySpendMana(requestingPlayerId, cardData.manaCost)) return;

        photonView.RPC("RPC_ConfirmCardPlay", PhotonNetwork.CurrentRoom.GetPlayer(requestingPlayerId), cardId);

        var effect = cardData.spellEffect;
        string resourcePath = "SpellEffects/" + effect.name;
        object[] effectData = effect.GetEffectData();
        photonView.RPC("RPC_ApplySpellEffect", RpcTarget.All, targetUnitViewID, resourcePath, effectData);
    }

    [PunRPC]
    public void RPC_ApplySpellEffect(int targetUnitViewID, string spellEffectResourcePath, object[] effectData)
    {
        PhotonView targetView = PhotonView.Find(targetUnitViewID);
        if (targetView == null) return;
        UnitInstance targetUnit = targetView.GetComponent<UnitInstance>();
        if (targetUnit == null) return;
        SpellEffect effectAsset = Resources.Load<SpellEffect>(spellEffectResourcePath);
        if (effectAsset == null) return;
        SpellEffect effect = Instantiate(effectAsset);
        ApplySpellEffect(targetUnit, effect, effectData);
    }

    public void ApplySpellEffect(UnitInstance initialTarget, SpellEffect effect, object[] effectData)
    {
        if (effect is StatModifierEffect statEffect)
        {
            List<UnitInstance> targets = new List<UnitInstance>();
            switch (statEffect.targetMode)
            {
                case SpellTargetMode.SingleTarget:
                    if (initialTarget != null) targets.Add(initialTarget);
                    break;
                case SpellTargetMode.GlobalAllies:
                    targets.AddRange(BoardManager.Instance.GetAllUnitsForPlayer(initialTarget.OwnerActorId));
                    break;
                case SpellTargetMode.GlobalEnemies:
                    targets.AddRange(BoardManager.Instance.GetAllUnitsForOpponent(initialTarget.OwnerActorId));
                    break;
                case SpellTargetMode.GlobalAll:
                    targets.AddRange(BoardManager.Instance.GetAllUnits());
                    break;
            }
            foreach (var unit in targets)
            {
                if (unit != null && unit.GetComponent<UnitEffectHandler>() != null)
                {
                    statEffect.ApplyEffectFromData(unit, effectData);
                }
            }
        }
    }


    #endregion

    #region 🧠 Turn & Phase Control
    [PunRPC]
    public void RPC_SetTurn(int actorId)
    {
        CurrentTurnActorId = actorId;
        Debug.Log($"[GameManager] CurrentTurnActorId set to {actorId} (Local Actor: {PhotonNetwork.LocalPlayer.ActorNumber})");

        // Refill mana for the current turn player
        ManaManager.Instance.RefillMana(actorId, currentRound);
        UpdateCardGrayStates();
        UpdateTurnIndicator();

    }
    [PunRPC]
    public void RPC_SetPhase(int phaseValue)
    {
        CurrentPhase = (GamePhase)phaseValue;
        Debug.Log($"[GameManager] Phase set to: {CurrentPhase}");

        UpdateCardGrayStates();
    }
    [PunRPC]
    public void RPC_HighlightLane(int laneIndex, int mode)
    {
        if (LaneVisualizerManager.Instance != null)
        {
            LaneVisualizerManager.LaneVisualMode visualMode = (LaneVisualizerManager.LaneVisualMode)mode;
            LaneVisualizerManager.Instance.HighlightLane(laneIndex, visualMode);
        }
        else
        {
            Debug.LogWarning("[GameManager] LaneVisualizerManager.Instance is null in RPC_HighlightLane");
        }
    }
    [PunRPC]
    private void RPC_ClearLaneHighlights()
    {
        LaneVisualizerManager.Instance?.ClearAllLaneHighlights();
    }
    [PunRPC]
    private void RPC_MapAnimation()
    {
        Animator mapAnimator = gameMap.GetComponent<Animator>();
        Animator laneAnimator = laneVisualizer.GetComponent<Animator>();
        if (mapAnimator != null)
        {
            mapAnimator.SetTrigger("SetUp");
        }
    }
    [PunRPC]
    private void RPC_SetRound(int round)
    {
        currentRound = round;
        UpdateRoundText(round);
    }
    void UpdateRoundText(int round)
    {
        if (roundText != null)
        {
            roundText.text = $"Round {round}";
        }
        Debug.Log($"[GameManager] Round {round} started");
    }
    #endregion

    #region 🎴 UI Updating
    private void UpdateCardGrayStates()
    {
        bool isMyTurn = PhotonNetwork.LocalPlayer.ActorNumber == CurrentTurnActorId;
        bool isDrawPhase = CurrentPhase == GamePhase.Draw;
        bool isPlacementPhase = CurrentPhase == GamePhase.Placement;

        int currentMana = ManaManager.Instance.GetCurrentMana(PhotonNetwork.LocalPlayer.ActorNumber);

        foreach (var card in FindObjectsOfType<DraggableCard>())
        {
            if (card.cardData != null)
            {
                bool hasEnoughMana = card.cardData.manaCost <= currentMana;
                bool isPlayableNow = (isMyTurn && isPlacementPhase && hasEnoughMana) || isDrawPhase;
                bool shouldBeGray = !isPlayableNow;

                card.SetGrayOut(shouldBeGray);
            }
        }
    }

    private void UpdateTurnIndicator()
    {
        if (turnIndicatorText == null) return;

        if (PhotonNetwork.LocalPlayer.ActorNumber == CurrentTurnActorId)
        {
            turnIndicatorText.text = "Your Turn";
        }
        else
        {
            turnIndicatorText.text = "Opponent's Turn";
        }
    }
    [PunRPC]
    void SetPhaseTextRPC(string text)
    {
        if (phaseText != null)
        {
            Debug.Log($"[GameManager] Phase text set to: {text}");
            phaseText.text = text;
        }
    }
    [PunRPC]
    public void RPC_InitializeMatrix()
    {
        if (matrixTransitionPrefab != null)
        {
            if (ConnectManager.GameStarted == false) return; // Prevent multiple instantiations (if any glitch occurs
            Instantiate(matrixTransitionPrefab);
            Debug.Log("[GameManager] Matrix transition instantiated.");
        }
    }
    [PunRPC]
    public void RPC_InitializeDeck()
    {
        HandSpawner[] allHandSpawners = FindObjectsOfType<HandSpawner>();
        foreach (var spawner in allHandSpawners)
        {
            if (spawner.photonView.IsMine)
            {
                spawner.InitializeDeck();
                break; // Found our spawner, no need to check others
            }
        }
    }
    #endregion

    #region 🔄 Unit Spawn Coordination
    public void NotifyUnitSpawnStarted()
    {
        pendingUnitSpawns++;
    }

    public void NotifyUnitSpawnFinished()
    {
        pendingUnitSpawns = Mathf.Max(0, pendingUnitSpawns - 1);
    }

    public bool HasPendingUnitSpawns()
    {
        return pendingUnitSpawns > 0;
    }
    #endregion

    #region 🎴 Card Drawing
    [PunRPC]
    private void RPC_DrawCardForPlayer(int targetActorId)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != targetActorId)
            return;

        var handSpawner = FindObjectOfType<HandSpawner>();
        handSpawner?.DrawCard();
    }

    #endregion

    public void OnEndPhaseButtonPressed()
    {
        Debug.Log("[GameManager] OnEndPhaseButtonPressed called");

        if (!endPhaseButton.gameObject.activeSelf) return;

        photonView.RPC("SetPhaseEndedRPC", RpcTarget.MasterClient);
    }
    [PunRPC]
    void SetPhaseEndedRPC()
    {
        Debug.Log("[GameManager] SetPhaseEndedRPC called on MasterClient");
        phaseEnded = true;
        photonView.RPC("HideEndPhaseButtonRPC", RpcTarget.All);
    }

    [PunRPC]
    void HideEndPhaseButtonRPC()
    {
        if (endPhaseButton != null)
            endPhaseButton.gameObject.SetActive(false);
    }
    [PunRPC]
    void SetEndPhaseButtonActive(int actorId)
    {
        if (endPhaseButton == null) return;

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorId)
        {
            endPhaseButton.gameObject.SetActive(true);
        }
        else
        {
            endPhaseButton.gameObject.SetActive(false);
        }
    }
    [PunRPC]
    void TurnOffStateText()
    {
        ConnectManager.Instance.ConnectStateTurnOff();
    }
}
