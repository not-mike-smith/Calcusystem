namespace Calcusystem.Core;

public static class Constants
{
    /// <summary>
    /// Sentinel id meaning "mint a fresh identity for this object" — see <see cref="IdBase"/>, which interprets
    /// it. Passing any other non-blank string preserves that id instead, which is what lets a persisted graph
    /// rebuild its references.
    /// </summary>
    public const string CREATE_NEW_ID = "CREATE_NEW";
}
