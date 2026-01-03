using System.Threading;

namespace clypse.portal.setup.Services.Iam;

/// <summary>
/// Defines operations for managing AWS IAM policies and roles.
/// </summary>
public interface IIamService
{
    /// <summary>
    /// Creates a new IAM policy with the specified name and policy document.
    /// </summary>
    /// <param name="name">The name of the policy to create (without the resource prefix).</param>
    /// <param name="policyDocument">The policy document as an object to be serialized to JSON.</param>
    /// <param name="tags">Tags to associate with the policy.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ARN of the created policy.</returns>
    public Task<string> CreatePolicyAsync(
        string name,
        object policyDocument,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new IAM role with the specified name.
    /// </summary>
    /// <param name="name">The name of the role to create (without the resource prefix).</param>
    /// <param name="tags">Tags to associate with the role.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The name of the created role.</returns>
    public Task<string> CreateRoleAsync(
        string name,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches an IAM policy to an IAM role.
    /// </summary>
    /// <param name="roleName">The name of the role to attach the policy to.</param>
    /// <param name="policyArn">The ARN of the policy to attach.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the policy was attached successfully; otherwise, false.</returns>
    public Task<bool> AttachPolicyToRoleAsync(
        string roleName,
        string policyArn,
        CancellationToken cancellationToken = default);
}
