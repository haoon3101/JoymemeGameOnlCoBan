public struct UnitData
{
    public int CurrentAttack;
    public int CurrentHealth;
    public int OwnerActorId;
    // Add any other stats you might need for conditions in the future

    // A constructor to make it easy to create
    public UnitData(UnitInstance unit)
    {
        if (unit != null)
        {
            CurrentAttack = unit.currentAttack;
            CurrentHealth = unit.currentHealth;
            OwnerActorId = unit.OwnerActorId;
        }
        else
        {
            CurrentAttack = 0;
            CurrentHealth = 0;
            OwnerActorId = -1; // Use an invalid ID to signify no unit
        }
    }
}