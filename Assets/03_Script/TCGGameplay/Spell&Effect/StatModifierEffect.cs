using UnityEngine;
using System.Collections.Generic;

// This enum defines who the spell can target.
public enum TargetAllegiance
{
    Ally,
    Enemy,
    Any
}

[CreateAssetMenu(menuName = "Spell Effects/Stat Modifier")]
public class StatModifierEffect : SpellEffect
{
    [Header("Targeting Rules")]
    public SpellTargetMode targetMode = SpellTargetMode.SingleTarget;
    // Use this in the Inspector to define if the spell hits allies or enemies.
    public TargetAllegiance targetAllegiance = TargetAllegiance.Ally;

    [Header("Effect Details")]
    public int attackValue;
    public StatModificationMode attackMode;

    public int healthValue;
    public StatModificationMode healthMode;

    public int durationTurns = 0;

    public bool requireConditions = false;
    public List<StatCondition> conditions;

    // The actual application logic doesn't need to change.
    #region Existing Methods
    public override void ApplyEffect(UnitInstance target)
    {
        if (target == null) return;

        if (requireConditions && conditions.Exists(c => !c.IsMet(target)))
        {
            Debug.Log("[StatModifierEffect] Conditions not met. Effect not applied.");
            return;
        }

        var handler = target.GetComponent<UnitEffectHandler>();
        if (handler == null)
        {
            Debug.LogWarning("[StatModifierEffect] No UnitEffectHandler found!");
            return;
        }

        int atkChange = 0;
        int hpChange = 0;

        if (attackMode == StatModificationMode.Add)
            atkChange = attackValue;
        else if (attackMode == StatModificationMode.Set)
            atkChange = attackValue - target.currentAttack;

        if (healthMode == StatModificationMode.Add)
            hpChange = healthValue;
        else if (healthMode == StatModificationMode.Set)
            hpChange = healthValue - target.currentHealth;

        handler.ApplyTimedStatEffect(atkChange, hpChange, durationTurns);
    }

    public override object[] GetEffectData()
    {
        return new object[]
        {
            attackValue, (int)attackMode,
            healthValue, (int)healthMode,
            durationTurns,
            requireConditions
        };
    }
    #endregion
}

public enum StatType { Attack, Health }
public enum ComparisonType { GreaterThan, LessThan, EqualTo, GreaterOrEqual, LessOrEqual }
public enum StatModificationMode
{
    Add,    // currentAttack += X
    Set     // currentAttack = X
}
[System.Serializable]
public class StatCondition
{
    public StatType stat;
    public ComparisonType comparison;
    public int value;

    public bool IsMet(UnitInstance unit)
    {
        int statValue = stat == StatType.Attack ? unit.currentAttack : unit.currentHealth;

        return comparison switch
        {
            ComparisonType.GreaterThan => statValue > value,
            ComparisonType.LessThan => statValue < value,
            ComparisonType.EqualTo => statValue == value,
            ComparisonType.GreaterOrEqual => statValue >= value,
            ComparisonType.LessOrEqual => statValue <= value,
            _ => false
        };
    }
}