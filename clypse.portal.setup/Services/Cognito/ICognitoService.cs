namespace clypse.portal.setup.Services.Cognito;

/// <summary>
/// Defines operations for managing AWS Cognito identity pools, user pools, and users.
/// </summary>
public interface ICognitoService
{
    /// <summary>
    /// Creates a new Cognito identity pool with the specified name.
    /// </summary>
    /// <param name="name">The name of the identity pool to create (without the resource prefix).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the created identity pool.</returns>
    public Task<string> CreateIdentityPoolAsync(
        string name,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Cognito user pool with the specified name.
    /// </summary>
    /// <param name="name">The name of the user pool to create (without the resource prefix).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the created user pool.</returns>
    public Task<string> CreateUserPoolAsync(
        string name,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new client for the specified Cognito user pool.
    /// </summary>
    /// <param name="accountId">The AWS account ID.</param>
    /// <param name="name">The name of the user pool client to create (without the resource prefix).</param>
    /// <param name="userPoolId">The ID of the user pool where the client will be created.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The client ID of the created user pool client. Or null if the operation failed.</returns>
    public Task<string?> CreateUserPoolClientAsync(
        string accountId,
        string name,
        string userPoolId,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user in the specified Cognito user pool.
    /// </summary>
    /// <param name="email">The email address of the user to create.</param>
    /// <param name="userPoolId">The ID of the user pool where the user will be created.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the user was created successfully; otherwise, false.</returns>
    public Task<bool> CreateUserAsync(
        string email,
        string userPoolId,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    public Task<bool> SetIdentityPoolAuthenticatedRoleAsync(
        string identityPoolId,
        string roleArn,
        CancellationToken cancellationToken = default);
}
