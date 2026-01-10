namespace clypse.portal.setup.Services.Security;

public interface ISecurityTokenService
{
    public Task<string> GetAccountIdAsync(CancellationToken cancellationToken = default);
}
