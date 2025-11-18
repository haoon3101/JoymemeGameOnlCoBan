using System.Collections.Generic;
using UnityEngine;

public abstract class SpellEffect : ScriptableObject
{
    // This enum is still needed by StatModifierEffect to define its area of effect.
    public enum SpellTargetMode
    {
        SingleTarget,
        GlobalAllies,
        GlobalEnemies,
        GlobalAll
    }

    // This is the core method that every specific spell (like StatModifierEffect)
    // must implement to define its actual logic (e.g., deal damage, heal, etc.).
    public abstract void ApplyEffect(UnitInstance target);

    // These methods are used to package the spell's data (like damage amount)
    // to be sent over the network.
    public virtual object[] GetEffectData() => null;
    public virtual void ApplyEffectFromData(UnitInstance target, object[] data) => ApplyEffect(target);
}
