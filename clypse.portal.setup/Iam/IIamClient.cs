using System.Threading;

namespace clypse.portal.setup.Iam;

public interface IIamClient
{
    public Task<string> CreatePolicyAsync(
        string name,
        object policyDocument,
        CancellationToken cancellationToken = default);

    public Task<string> CreateRoleAsync(
        string name,
        CancellationToken cancellationToken = default);

    public Task<bool> AttachPolicyToRoleAsync(
        string roleName,
        string policyArn,
        CancellationToken cancellationToken = default);
}
