using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitEffectHandler : MonoBehaviour
{
    private UnitInstance unit;
    private List<ActiveEffect> activeEffects = new();

    public class ActiveEffect
    {
        public int atkChange;
        public int hpChange;
        public int turnsRemaining;

        public ActiveEffect(int atk, int hp, int duration)
        {
            atkChange = atk;
            hpChange = hp;
            turnsRemaining = duration;
        }
    }

    private void Awake()
    {
        unit = GetComponent<UnitInstance>();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
    }

    private void OnRoundAdvanced()
    {
        // Only MasterClient controls expiration logic
        if (!PhotonNetwork.IsMasterClient) return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].turnsRemaining--;

            if (activeEffects[i].turnsRemaining <= 0)
            {
                // Revert stats locally
                unit.ModifyStats(-activeEffects[i].atkChange, -activeEffects[i].hpChange);

                // Broadcast to others
                PhotonView pv = unit.photonView;
                pv.RPC("RPC_RevertStats", RpcTarget.Others, pv.ViewID, -activeEffects[i].atkChange, -activeEffects[i].hpChange);

                activeEffects.RemoveAt(i);
            }
        }
    }
    public void SetStats(int atk, int hp, int duration)
    {
        int atkDiff = atk - unit.currentAttack;
        int hpDiff = hp - unit.currentHealth;

        unit.ModifyStats(atkDiff, hpDiff);
        activeEffects.Add(new ActiveEffect(atkDiff, hpDiff, duration));
    }


    public void ApplyEffect(SpellEffect effect)
    {
        if (effect is StatModifierEffect timedEffect)
        {
            int atkChange = 0;
            int hpChange = 0;

            if (timedEffect.attackMode == StatModificationMode.Add)
                atkChange = timedEffect.attackValue;
            else if (timedEffect.attackMode == StatModificationMode.Set)
                atkChange = timedEffect.attackValue - unit.currentAttack;

            if (timedEffect.healthMode == StatModificationMode.Add)
                hpChange = timedEffect.healthValue;
            else if (timedEffect.healthMode == StatModificationMode.Set)
                hpChange = timedEffect.healthValue - unit.currentHealth;

            unit.ModifyStats(atkChange, hpChange);
            activeEffects.Add(new ActiveEffect(atkChange, hpChange, timedEffect.durationTurns));
        }
        else
        {
            effect.ApplyEffect(unit);
        }
    }

    [PunRPC]
    public void RPC_RevertStats(int viewID, int atk, int hp)
    {
        if (PhotonView.Find(viewID)?.GetComponent<UnitInstance>() is UnitInstance target)
        {
            target.ModifyStats(atk, hp); // Will update visuals too
        }
    }

    public void ApplyTimedStatEffect(int atk, int hp, int turns)
    {
        unit.ModifyStats(atk, hp);
        activeEffects.Add(new ActiveEffect(atk, hp, turns));
    }

}

