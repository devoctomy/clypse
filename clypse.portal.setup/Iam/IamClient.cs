using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.Iam;

public class IamClient(
    IAmazonIdentityManagementService amazonIdentityManagementService,
    AwsServiceOptions options,
    ILogger<IamClient> logger) : IIamClient
{
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public async Task<bool> AttachPolicyToRoleAsync(
        string roleName,
        string policyArn,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attaching policy to role: Role Name = {policyNameWithPrefix}, Policy Arn = {policyArn}", roleName, policyArn);

        var attachRolePolicyRequest = new AttachRolePolicyRequest
        {
            RoleName = roleName,
            PolicyArn = policyArn
        };

        var response = await amazonIdentityManagementService.AttachRolePolicyAsync(attachRolePolicyRequest, cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<string> CreatePolicyAsync(
        string name,
        object policyDocument,
        CancellationToken cancellationToken = default)
    {
        var policyNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating Iam policy: {policyNameWithPrefix}", policyNameWithPrefix);

        var policy = new CreatePolicyRequest
        {
            PolicyName = policyNameWithPrefix,
            PolicyDocument = JsonSerializer.Serialize(policyDocument, _jsonSerializerOptions)
        };

        var response = await amazonIdentityManagementService.CreatePolicyAsync(policy, cancellationToken);
        return response.Policy.Arn;
    }

    public async Task<string> CreateRoleAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var roleNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating Iam role: {roleNameWithPrefix}", roleNameWithPrefix);

        var createRoleRequest = new CreateRoleRequest
        {
            RoleName = roleNameWithPrefix,
        };

        var response = await amazonIdentityManagementService.CreateRoleAsync(createRoleRequest, cancellationToken);
        return response.Role.RoleName;
    }
}