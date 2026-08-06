namespace GAS
{
    /// <summary>
    /// Identifies generic non-payload ability events replicated between client and server.
    /// </summary>
    public enum GameplayAbilityGenericReplicatedEvent
    {
        GenericConfirm = 0,
        GenericCancel,
        InputPressed,
        InputReleased,
        GenericSignalFromClient,
        GenericSignalFromServer,
        GameCustom1,
        GameCustom2,
        GameCustom3,
        GameCustom4,
        GameCustom5,
        GameCustom6,
        Max
    }
}