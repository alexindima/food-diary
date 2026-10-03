namespace FoodDiary.Persistence.Abstractions;

/// <summary>Reviewed phases of the shared save. Peers within a phase retain their enlistment order.</summary>
public static class PersistenceSaveOrder {
    public const int AccountPrincipals = -100;
    public const int SharedRecords = 0;
    public const int FullModelComposition = 1;
    public const int ModuleOwners = 100;
}
