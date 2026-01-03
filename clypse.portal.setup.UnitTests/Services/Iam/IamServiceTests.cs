using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using clypse.portal.setup.Services.Iam;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Iam;

public class IamServiceTests
{
    [Fact]
    public async Task GivenNameAndPolicyDocument_AndPolicyNotLreadyExists_WhenCreatePolicy_ThenCreatesPolicy()
    {
        // Arrange
        var mockAmazonIam = new Mock<IAmazonIdentityManagementService>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new IamService(
            mockAmazonIam.Object,
            options,
            Mock.Of<ILogger<IamService>>());
        var policyName = "test-policy";
        var expectedPolicyName = "test-prefix.test-policy";
        var expectedPolicyArn = "arn:aws:iam::123456789012:policy/test-prefix.test-policy";
        var policyDocument = new
        {
            Version = "2012-10-17",
            Statement = new[]
            {
                new
                {
                    Effect = "Allow",
                    Action = "s3:GetObject",
                    Resource = "*"
                }
            }
        };

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockAmazonIam.Setup(iam => iam.ListPoliciesAsync(
                It.IsAny<ListPoliciesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListPoliciesResponse
            {
                Policies = []
            });

        mockAmazonIam
            .Setup(iam => iam.CreatePolicyAsync(
                It.Is<CreatePolicyRequest>(req =>
                    req.PolicyName == expectedPolicyName &&
                    TagsMatch(req.Tags, tags)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePolicyResponse
            {
                Policy = new ManagedPolicy
                {
                    Arn = expectedPolicyArn
                }
            });
        
        // Act
        var policyArn = await sut.CreatePolicyAsync(policyName, policyDocument, tags);

        // Assert
        Assert.Equal(expectedPolicyArn, policyArn);
        mockAmazonIam.Verify(iam => iam.CreatePolicyAsync(
            It.Is<CreatePolicyRequest>(req => req.PolicyName == expectedPolicyName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenName_WhenCreateRole_ThenCreatesRole()
    {
        // Arrange
        var mockAmazonIam = new Mock<IAmazonIdentityManagementService>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new IamService(
            mockAmazonIam.Object,
            options,
            Mock.Of<ILogger<IamService>>());
        var roleName = "test-role";
        var expectedRoleName = "test-prefix.test-role";

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockAmazonIam
            .Setup(iam => iam.CreateRoleAsync(
                It.Is<CreateRoleRequest>(req =>
                    req.RoleName == expectedRoleName &&
                    TagsMatch(req.Tags, tags))))
            .ReturnsAsync(new CreateRoleResponse
            {
                Role = new Role
                {
                    RoleName = expectedRoleName
                }
            });
        
        // Act
        var returnedRoleName = await sut.CreateRoleAsync(roleName, tags);

        // Assert
        Assert.Equal(expectedRoleName, returnedRoleName);
        mockAmazonIam.Verify(iam => iam.CreateRoleAsync(
            It.Is<CreateRoleRequest>(req => req.RoleName == expectedRoleName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenRoleNameAndPolicyArn_WhenAttachPolicyToRole_ThenAttachesPolicy()
    {
        // Arrange
        var mockAmazonIam = new Mock<IAmazonIdentityManagementService>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new IamService(
            mockAmazonIam.Object,
            options,
            Mock.Of<ILogger<IamService>>());
        var roleName = "test-role";
        var policyArn = "arn:aws:iam::123456789012:policy/test-policy";
        var expectedRoleName = "test-prefix.test-role";

        mockAmazonIam
            .Setup(iam => iam.AttachRolePolicyAsync(
                It.Is<AttachRolePolicyRequest>(req => 
                    req.RoleName == expectedRoleName && 
                    req.PolicyArn == policyArn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachRolePolicyResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });
        
        // Act
        var success = await sut.AttachPolicyToRoleAsync(roleName, policyArn);

        // Assert
        Assert.True(success);
        mockAmazonIam.Verify(iam => iam.AttachRolePolicyAsync(
            It.Is<AttachRolePolicyRequest>(req => 
                req.RoleName == expectedRoleName && 
                req.PolicyArn == policyArn),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private bool TagsMatch(
        List<Tag> awsTags,
        Dictionary<string, string> expectedTags)
    {
        if (awsTags.Count != expectedTags.Count)
            return false;
        foreach (var tag in awsTags)
        {
            if (!expectedTags.TryGetValue(tag.Key, out var expectedValue) ||
                tag.Value != expectedValue)
            {
                return false;
            }
        }
        return true;
    }
}
