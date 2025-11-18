using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    private Dictionary<int, Lane3D[]> playerLanes = new(); // ActorId → Lanes[]
    private Dictionary<Lane3D, UnitInstance> laneToUnit = new(); // Cache units on lanes

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterLanesForPlayer(int actorId, Lane3D[] lanes)
    {
        playerLanes[actorId] = lanes;
        foreach (var lane in lanes)
        {
            if (!laneToUnit.ContainsKey(lane))
                laneToUnit[lane] = null;
        }
    }

    public void UpdateLaneUnit(Lane3D lane, UnitInstance unit)
    {
        if (laneToUnit.ContainsKey(lane))
            laneToUnit[lane] = unit;
    }

    public Lane3D[] GetLanesForPlayer(int actorId)
    {
        return playerLanes.TryGetValue(actorId, out var lanes) ? lanes : null;
    }

    public List<UnitInstance> GetAllUnits()
    {
        return laneToUnit.Values.Where(u => u != null).ToList();
    }
    public UnitInstance GetUnitAtLaneForOpponent(int actorId, int laneIndex)
    {
        return GetAllUnits()
            .FirstOrDefault(u => u.OwnerActorId != actorId && u.LaneIndex == laneIndex);
    }

    public List<UnitInstance> GetAllUnitsForPlayer(int actorId)
    {
        var units = FindObjectsOfType<UnitInstance>();
        var filtered = units.Where(u => u.OwnerActorId == actorId).ToList();

        Debug.Log($"[BoardManager] GetAllUnitsForPlayer({actorId}) found {filtered.Count} units");

        foreach (var u in filtered)
            Debug.Log($" - {u.name} (owner: {u.OwnerActorId})");

        return filtered;
    }

    public List<UnitInstance> GetAllUnitsForOpponent(int actorId)
    {
        var units = FindObjectsOfType<UnitInstance>();
        var filtered = units.Where(u => u.OwnerActorId != actorId).ToList();

        Debug.Log($"[BoardManager] GetAllUnitsForOpponent({actorId}) found {filtered.Count} units");

        foreach (var u in filtered)
            Debug.Log($" - {u.name} (owner: {u.OwnerActorId})");

        return filtered;
    }


    public Lane3D GetLaneOfUnit(UnitInstance unit)
    {
        return laneToUnit.FirstOrDefault(kvp => kvp.Value == unit).Key;
    }

    public UnitInstance GetLeftNeighbor(UnitInstance unit)
    {
        var lane = GetLaneOfUnit(unit);
        if (lane == null) return null;

        var lanes = playerLanes[lane.PlayerOwnerId];
        int index = lane.BoardLaneIndex;

        return lanes.FirstOrDefault(l => l.BoardLaneIndex == index - 1)?.GetCurrentUnit();
    }

    public UnitInstance GetRightNeighbor(UnitInstance unit)
    {
        var lane = GetLaneOfUnit(unit);
        if (lane == null) return null;

        var lanes = playerLanes[lane.PlayerOwnerId];
        int index = lane.BoardLaneIndex;

        return lanes.FirstOrDefault(l => l.BoardLaneIndex == index + 1)?.GetCurrentUnit();
    }
    public void SetUnitAt(int laneIndex, UnitInstance unit)
    {
        var lane = laneToUnit.Keys.FirstOrDefault(l => l.BoardLaneIndex == laneIndex);
        if (lane != null)
        {
            laneToUnit[lane] = unit;
        }
        else
        {
            Debug.LogWarning($"[BoardManager] Lane with index {laneIndex} not found when trying to set unit.");
        }
    }

    public void ClearUnitAt(int laneIndex)
    {
        var lane = laneToUnit.Keys.FirstOrDefault(l => l.BoardLaneIndex == laneIndex);
        if (lane != null)
        {
            laneToUnit[lane] = null;
        }
        else
        {
            Debug.LogWarning($"[BoardManager] Lane with index {laneIndex} not found when trying to clear unit.");
        }
    }

}
