using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;


public class UnitInstance : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public UnityEvent<UnitData> OnStatsChanged = new UnityEvent<UnitData>();

    [SerializeField] private Image cardImage;      // UI Image for artwork (on world-space canvas)
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private AudioSource hitSound;
    [SerializeField] private GameObject hitVFX; // Sound played when attacking, if needed 

    public int OwnerActorId;
    public int LaneIndex { get; private set; } // Index in the lane array, used for combat targeting
    private Animator animator;

    private int sideId;
    public int cardId { get; private set; }
    public int currentHealth { get; private set; }
    public int currentAttack { get; private set; }
    private int currentMana;
    
    public bool GotDamageResponse { get; set; } = false;
    public bool HasImpacted { get; set; } = false;
    public bool HasDealtDamage = false;

    private UnitInstance targetUnit; // assigned during ResolveUnitCombat
    private Lane3D currentLane;

    public void SetCombatTarget(UnitInstance target)
    {
        targetUnit = target;
    }
    public void SetPlayerOwnerId(int actorId)
    {
        OwnerActorId = actorId;
        Debug.Log($"[UnitInstance] OwnerActorId set to {actorId}");
    }
    public void SetLaneIndex(int laneIndex)
    {
        LaneIndex = laneIndex;
    }
    public void SetLane(Lane3D lane)
    {
        currentLane = lane;
    }
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    // Called automatically when Photon instantiates this prefab
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length >= 2)
        {
            cardId = (int)instantiationData[0];
            sideId = (int)instantiationData[1]; // ✅ assign immediately
            InitializeVisuals();
        }
        else
        {
            Debug.LogError("UnitInstance: Instantiation data missing sideId!");
        }
    }
    public bool IsAttackFinished { get; set; } = false;
    private bool hasCollided = false;

    public void StartAttack(UnitInstance target)
    {
        SetCombatTarget(target);
        IsAttackFinished = false;
        hasCollided = false;

        // Trigger animation
        photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Attack");

        // You could also move the object forward using animation, DOTween, or NavMesh
        // Or simply move it manually here if you want
    }

    // Called when this unit's collider hits its target
    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided || !PhotonNetwork.IsMasterClient) return;

        UnitInstance otherUnit = other.GetComponent<UnitInstance>();
        if (otherUnit != null && otherUnit == targetUnit)
        {
            hasCollided = true;

            int damage = GetAttack();
            int newHealth = Mathf.Max(0, otherUnit.GetHealth() - damage);
            

            // Broadcast to everyone
            otherUnit.photonView.RPC("RPC_PlayCardHitEffect", RpcTarget.All);
            otherUnit.photonView.RPC("RPC_PlayCardHitSound", RpcTarget.All);
            otherUnit.photonView.RPC("RPC_SetHealth", RpcTarget.All, newHealth);
            otherUnit.photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Damaged");
        
            StartCoroutine(FinishAttackAfterDelay(1f));
        }
    }

    [PunRPC]
    public void Initialize(int actorId)
    {
        this.OwnerActorId = actorId;
    }
    private IEnumerator FinishAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsAttackFinished = true;
    }

    private IEnumerator DelayedDeath(UnitInstance unit)
    {
        yield return new WaitForSeconds(0.5f); // Let animation play first
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.UnitDied(unit.photonView.ViewID);
        }
    }


    //[PunRPC]
    //public void RPC_PlayDamagedReaction()
    //{
    //    Debug.Log($"[{name}] Playing Damaged reaction!");

    //    RPC_PlayAnimation("Damaged");
    //    HasPlayedDamaged = true;
    //}
    [PunRPC]
    public void RPC_TakeDamageWithAnimation(int damage, int attackerViewId)
    {
        StartCoroutine(TakeDamageAndNotify(damage, attackerViewId));
    }


    [PunRPC]
    public void RPC_DamageResponse()
    {
        GotDamageResponse = true;
    }
    [PunRPC]
    public void RPC_PlayCardHitEffect()
    {
        // Create a new position based on the current one
        Vector3 spawnPosition = transform.position;

        // Add to the y-axis value
        spawnPosition.y += 1.0f; // Spawns it 1 unit higher

        // Instantiate the effect at the new, higher position
        Instantiate(hitVFX, spawnPosition, Quaternion.identity);
    }
    [PunRPC]
    public void RPC_PlayCardHitSound()
    {
        hitSound.Play();
    }
    private void BroadcastStatChange()
    {
        UnitData newData = new UnitData(this);
        OnStatsChanged.Invoke(newData);
        Debug.Log($"[UnitInstance {cardId}] Invoked OnStatsChanged. New Stats - ATK:{newData.CurrentAttack}, HP:{newData.CurrentHealth}");
    }
    public void ModifyStats(int atk, int hp)
    {
        currentAttack = Mathf.Max(currentAttack + atk, 0);
        currentHealth = Mathf.Max(currentHealth + hp, 0);
        UpdateHealth(currentHealth);
        UpdateAttack(currentAttack);
        Debug.Log($"[UnitInstance] Modified stats: Attack = {currentAttack}, Health = {currentHealth}");
        BroadcastStatChange();

        if (currentHealth <= 0)
        {
            Debug.Log($"[UnitInstance] Unit died → calling UnitDied");
            GameManager.Instance.UnitDied(photonView.ViewID);
        }
    }
    private class TempStatModifier
    {
        public int attackDelta;
        public int healthDelta;
        public int turnsRemaining;
    }

    private List<TempStatModifier> tempModifiers = new List<TempStatModifier>();

    public void ApplyTemporaryStatModifier(int atk, int hp, int duration)
    {
        ModifyStats(atk, hp);
        tempModifiers.Add(new TempStatModifier
        {
            attackDelta = atk,
            healthDelta = hp,
            turnsRemaining = duration
        });
    }

    public void OnTurnStart()
    {
        for (int i = tempModifiers.Count - 1; i >= 0; i--)
        {
            tempModifiers[i].turnsRemaining--;
            if (tempModifiers[i].turnsRemaining <= 0)
            {
                ModifyStats(-tempModifiers[i].attackDelta, -tempModifiers[i].healthDelta);
                tempModifiers.RemoveAt(i);
            }
        }
    }

    private IEnumerator TakeDamageAndNotify(int damage, int attackerViewId)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[UnitInstance] Playing trigger 'Damaged2' on side {sideId}");
        RPC_PlayAnimation("Damaged");

        yield return new WaitForSeconds(0.5f);

        // Send signal back to attacker
        PhotonView attackerPV = PhotonView.Find(attackerViewId);
        if (attackerPV != null)
        {
            attackerPV.RPC("RPC_DamageResponse", RpcTarget.All);
        }

        if (currentHealth <= 0)
        {
            GameManager.Instance.UnitDied(photonView.ViewID);
        }
    }



    [PunRPC]
    public void RPC_ApplyCombatResult(int newHealth)
    {
        Debug.Log($"[UnitInstance] RPC_ApplyCombatResult: New health = {newHealth}");

        currentHealth = Mathf.Max(newHealth, 0);
        UpdateHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log($"[UnitInstance] Unit died → calling UnitDied");
            GameManager.Instance.UnitDied(photonView.ViewID);
        }
    }

    // Use this to initialize or refresh visuals locally and set current stats
    private void InitializeVisuals()
    {
        CardData cardData = CardDatabase.Instance.GetCardById(cardId);
        if (cardData == null)
        {
            // ... (your existing error log)
            return;
        }

        GetComponent<InspectableUnit>()?.SetCardData(cardData);

        currentHealth = cardData.health;
        currentAttack = cardData.attack;
        currentMana = cardData.manaCost;
        //More data in future
        if (cardImage != null && cardData.cardImage != null)
            cardImage.sprite = cardData.cardImage;

        UpdateHealth(currentHealth);
        UpdateAttack(currentAttack);
        UpdateMana(currentMana);

    }
    public void SetSide(int id) => sideId = id;
    public int GetSide() => sideId;
    [PunRPC]
    public void RPC_PlayAnimation(string type)
    {
        string trigger = "";

        switch (type)
        {
            case "Attack":
                trigger = sideId == 0 ? "Attack1" : "Attack2";
                break;
            case "AttackPlayer":
                trigger = sideId == 0 ? "P1UnitAtk" : "P2UnitAtk";
                break;
            case "Damaged":
                trigger = sideId == 0 ? "Damaged1" : "Damaged2";
                break;
            case "Die":
                trigger = "Die";
                break;
            case "Entry":
                trigger = sideId == 0 ? "Entry1" : "Entry2";
                break;
        }

        Debug.Log($"[UnitInstance] Playing trigger '{trigger}' on side {sideId}");
        animator?.SetTrigger(trigger);
    }
    [PunRPC]
    public void RPC_ResetHasDealtDamage()
    {
        HasDealtDamage = false;
    }

    [PunRPC]
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // Clamp to zero

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            GameManager.Instance.UnitDied(photonView.ViewID);
        }
        UpdateHealth(currentHealth);
    }
    [PunRPC]
    public void RequestUnitDestroyRPC(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            if (!pv.IsMine)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    pv.TransferOwnership(PhotonNetwork.LocalPlayer);
                }
                else
                {
                    Debug.LogWarning("Cannot destroy object: not owner and not MasterClient.");
                }
            }

            photonView.RPC("RPC_StartDissolve", RpcTarget.All);
            //yield return new WaitForSeconds(1f); // Wait for any animations to finish
            //PhotonNetwork.Destroy(pv.gameObject);
        }
    }

    public int GetHealth() => currentHealth;
    public int GetAttack() => currentAttack;
    

    [PunRPC]
    public void RPC_SetHealth(int newHealth)
    {
        currentHealth = Mathf.Max(newHealth, 0);
        UpdateHealth(newHealth);

        // --- CHANGED: Invoke the event with the new data ---
        BroadcastStatChange();
    }


    public void UpdateHealth(int newHealth)
    {
        if (healthText != null)
            healthText.text = Mathf.Max(newHealth, 0).ToString(); // Visual clamping
    }

    public void UpdateAttack(int newAttack)
    {
        if (attackText != null)
            attackText.text = Mathf.Max(newAttack, 0).ToString(); // Clamp for UI
    }

    public void UpdateMana(int newMana)
    {
        if (manaText != null)
            manaText.text = Mathf.Max(newMana, 0).ToString(); // Clamp for UI
    }
}
