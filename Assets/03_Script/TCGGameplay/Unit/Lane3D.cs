using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Collections;

public class Lane3D : MonoBehaviourPun
{
    [SerializeField] public int playerOwnerId;
    [SerializeField] public int boardLaneIndex;
    [SerializeField] private int sideId;

    [SerializeField] private GameObject currentUnit;
    private UnitInstance currentUnitInstance;
    [SerializeField] private AudioSource addCard;

    // --- NEW: Reference to the corresponding UI DropZone ---
    private DropZone correspondingDropZone;

    public int SideId => sideId;
    public int PlayerOwnerId => playerOwnerId;
    public int BoardLaneIndex => boardLaneIndex;

    // --- NEW: Method to establish the link from the HandSpawner ---
    public void LinkToDropZone(DropZone dz)
    {
        correspondingDropZone = dz;
    }

    public bool CanAddUnit()
    {
        return currentUnit == null;
    }
    public void UpdateUnitDataOnDropZone()
    {
        if (currentUnitInstance != null && correspondingDropZone != null)
        {
            // Create a new data snapshot and send it to the DropZone
            UnitData data = new UnitData(currentUnit.GetComponent<UnitInstance>());
            correspondingDropZone.RegisterUnitData(data);
            Debug.Log($"Lane3D: Updated DropZone with unit data. ATK: {data.CurrentAttack}, HP: {data.CurrentHealth}");
        }
    }
    public UnitInstance GetCurrentUnit()
    {
        if (currentUnit == null) return null;
        return currentUnit.GetComponent<UnitInstance>();
    }

    public void RemoveUnit(GameObject unit)
    {
        if (currentUnit == unit)
        {
            currentUnit = null;
            BoardManager.Instance?.ClearUnitAt(boardLaneIndex);

            // --- NEW: Notify the DropZone that the unit has been removed ---
            correspondingDropZone?.ClearUnit();
        }
    }

    public Vector3 GetUnitSpawnPosition() => transform.position;

    [PunRPC]
    public void RPC_AddUnit(int unitViewID) // isOwnerView is not needed here
    {
        StartCoroutine(DelayedAddUnit(unitViewID));
    }
    private void OnUnitStatsUpdated(UnitData data)
    {
        Debug.Log($"Lane3D: Unit stats updated. ATK: {data.CurrentAttack}, HP: {data.CurrentHealth}");
        // When we receive an update from the unit, pass it along to the DropZone.
        if (correspondingDropZone != null)
        {
            correspondingDropZone.RegisterUnitData(data);
        }
    }
    private IEnumerator DelayedAddUnit(int unitViewID)
    {
        PhotonView unitPV = null;
        float timer = 0f;
        while (unitPV == null && timer < 3f)
        {
            unitPV = PhotonView.Find(unitViewID);
            timer += Time.deltaTime;
            yield return null;
        }

        if (unitPV == null)
        {
            Debug.LogError($"Lane3D: Could not find PhotonView with ID {unitViewID}.");
            yield break;
        }

        GameObject unit = unitPV.gameObject;
        UnitInstance unitInstance = unit.GetComponent<UnitInstance>();
        if (unitInstance != null)
        {
            unitInstance.SetSide(sideId);
            unitInstance.SetPlayerOwnerId(playerOwnerId);
            unitInstance.SetLaneIndex(boardLaneIndex);

            // --- NEW: Notify the DropZone about the new unit ---
            correspondingDropZone?.RegisterUnit(unitInstance);
            //NOTICE THÍ SHIT AND DONT TOUCH IT???????BECAUSE THIS SHIT IS FOR SPELL UNIT DATA CHECK AND UPDATE SO DONT TOUCH IT
            currentUnitInstance = unitInstance;
            Debug.LogWarning("Lane3D: Replacing existing unit instance without cleaning up first.");
            // --- CHANGED: Subscribe to the unit's event ---
            currentUnitInstance.OnStatsChanged.AddListener(OnUnitStatsUpdated);
        }
        
        currentUnit = unit;
        //if (currentUnitInstance != null)
        //{
            

        //    // Do the initial registration
        //    if (correspondingDropZone != null)
        //    {
        //        correspondingDropZone.RegisterUnit(currentUnitInstance);
        //        correspondingDropZone.RegisterUnitData(new UnitData(currentUnitInstance));
        //    }
        //}
        //else
        //{
        //                currentUnitInstance = unitInstance;
        //    // --- CHANGED: Subscribe to the unit's event ---
        //    currentUnitInstance.OnStatsChanged.AddListener(OnUnitStatsUpdated);
        //}
        if (unitInstance != null)
        {
            currentUnit.GetComponent<UnitInstance>().SetLane(this);
            Debug.LogError($"Lane3D: Replacing existing unit {currentUnit} with new unit {unitInstance.photonView.ViewID}.");
        }
        unit.transform.SetParent(transform, false);
        unit.transform.localPosition = Vector3.zero;
        unit.transform.localScale = Vector3.one * 1.2f;
        unitInstance?.photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Entry");
        if (addCard != null) addCard.Play();

        BoardManager.Instance?.SetUnitAt(boardLaneIndex, unitInstance);
        GameManager.Instance.NotifyUnitSpawnFinished();
    }
}
