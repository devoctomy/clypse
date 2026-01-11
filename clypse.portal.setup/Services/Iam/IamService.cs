using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.Services.Iam;

/// <summary>
/// Provides functionality for managing AWS IAM policies and roles.
/// </summary>
public class IamService(
    IAmazonIdentityManagementService amazonIdentityManagementService,
    SetupOptions options,
    ILogger<IamService> logger) : IIamService
{
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// Attaches an IAM policy to an IAM role.
    /// </summary>
    /// <param name="roleName">The name of the role to attach the policy to.</param>
    /// <param name="policyArn">The ARN of the policy to attach.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the policy was attached successfully; otherwise, false.</returns>
    public async Task<bool> AttachPolicyToRoleAsync(
        string roleName,
        string policyArn,
        CancellationToken cancellationToken = default)
    {
        var roleNameWithPrefix = $"{options.ResourcePrefix}.{roleName}";
        logger.LogInformation("Attaching policy to role: Role Name = {policyNameWithPrefix}, Policy Arn = {policyArn}", roleNameWithPrefix, policyArn);

        var attachRolePolicyRequest = new AttachRolePolicyRequest
        {
            RoleName = roleNameWithPrefix,
            PolicyArn = policyArn
        };

        var response = await amazonIdentityManagementService.AttachRolePolicyAsync(attachRolePolicyRequest, cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <summary>
    /// Creates a new IAM policy with the specified name and policy document.
    /// </summary>
    /// <param name="name">The name of the policy to create (without the resource prefix).</param>
    /// <param name="policyDocument">The policy document as an object to be serialized to JSON.</param>
    /// <param name="tags">Tags to associate with the policy.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ARN of the created policy.</returns>
    public async Task<string> CreatePolicyAsync(
        string name,
        object policyDocument,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var policyNameWithPrefix = $"{options.ResourcePrefix}.{name}";

        logger.LogInformation("Checking for existing policy: {policyNameWithPrefix}", policyNameWithPrefix);
        var ListPoliciesRequest = new ListPoliciesRequest
        {
            Scope = PolicyScopeType.Local
        };
        var listPoliciesResponse = await amazonIdentityManagementService.ListPoliciesAsync(ListPoliciesRequest, cancellationToken);
        var existingPolicy = listPoliciesResponse.Policies?.FirstOrDefault(p => p.PolicyName == policyNameWithPrefix);
        if (existingPolicy != null)
        {
            logger.LogInformation("Policy already exists: {policyNameWithPrefix}", policyNameWithPrefix);
            return existingPolicy.Arn;
        }

        logger.LogInformation("Existing policy does not exist, creating a new one.");

        var tagSet = tags
            .Select(kv => new Tag { Key = kv.Key, Value = kv.Value })
            .ToList();

        logger.LogInformation("Creating Iam policy: {policyNameWithPrefix}", policyNameWithPrefix);
        var createPolicyRequest = new CreatePolicyRequest
        {
            PolicyName = policyNameWithPrefix,
            PolicyDocument = JsonSerializer.Serialize(policyDocument, _jsonSerializerOptions),
            Tags = tagSet
        };
        var createPolicyResponse = await amazonIdentityManagementService.CreatePolicyAsync(createPolicyRequest, cancellationToken);
        return createPolicyResponse.Policy.Arn;
    }

    /// <summary>
    /// Creates a new IAM role with the specified name.
    /// </summary>
    /// <param name="name">The name of the role to create (without the resource prefix).</param>
    /// <param name="assumeRolePolicyDocument">The assume role policy document as a JSON string.</param>
    /// <param name="tags">Tags to associate with the policy.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The name of the created role.</returns>
    public async Task<string> CreateRoleAsync(
        string name,
        string assumeRolePolicyDocument,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var roleNameWithPrefix = $"{options.ResourcePrefix}.{name}";

        var tagSet = tags
            .Select(kv => new Tag { Key = kv.Key, Value = kv.Value })
            .ToList();

        logger.LogInformation("Creating Iam role: {roleNameWithPrefix}", roleNameWithPrefix);
        var createRoleRequest = new CreateRoleRequest
        {
            RoleName = roleNameWithPrefix,
            AssumeRolePolicyDocument = assumeRolePolicyDocument,
            Tags = tagSet
        };

        var response = await amazonIdentityManagementService.CreateRoleAsync(createRoleRequest, cancellationToken);
        return response.Role.Arn;
    }
}