using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ManaManager : MonoBehaviourPun
{
    public static ManaManager Instance;

    private Dictionary<int, int> playerMana = new Dictionary<int, int>();
    private const int MaxMana = 10;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializePlayers()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerMana[player.ActorNumber] = 1;
            photonView.RPC("RPC_SetMana", RpcTarget.All, player.ActorNumber, 1);
        }
    }
    public void RefillMana(int actorId, int turnNumber)
    {
        int newMana = Mathf.Min(turnNumber, MaxMana);
        playerMana[actorId] = newMana;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetMana", RpcTarget.All, actorId, newMana);
        }

        Debug.Log($"[ManaManager] Player {actorId} mana refilled to {newMana}");
    }

    public bool HasEnoughMana(int actorId, int cost)
    {
        return playerMana.TryGetValue(actorId, out int mana) && mana >= cost;
    }

    public bool TrySpendMana(int actorId, int cost)
    {
        if (HasEnoughMana(actorId, cost))
        {
            playerMana[actorId] -= cost;

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SetMana", RpcTarget.All, actorId, playerMana[actorId]);
            }

            Debug.Log($"[ManaManager] Player {actorId} spent {cost} mana, remaining: {playerMana[actorId]}");
            return true;
        }
        return false;
    }

    public int GetCurrentMana(int actorId)
    {
        return playerMana.TryGetValue(actorId, out int mana) ? mana : 0;
    }

    [PunRPC]
    private void RPC_SetMana(int actorId, int amount)
    {
        playerMana[actorId] = amount;
        GameStateUI.Instance?.UpdateManaUI(actorId, amount);
    }
}
