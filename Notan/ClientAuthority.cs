namespace Notan;

public enum ClientAuthority
{
    /// <summary>
    /// Only the server has authority.
    /// </summary>
    None,
    /// <summary>
    /// Authenticated clients can have authority.
    /// </summary>
    Authenticated,
    /// <summary>
    /// Any client can have authority, even if they are unauthenticated.
    /// </summary>
    Unauthenticated,
}
