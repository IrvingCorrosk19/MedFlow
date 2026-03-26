using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Identity;

public class MedFlowSignInManager : SignInManager<ApplicationUser>
{
    public MedFlowSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<SignInResult> PasswordSignInAsync(ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        if (!user.IsActive || user.IsLocked)
        {
            Logger.LogWarning("Login bloqueado para {Email}: IsActive={Active}, IsLocked={Locked}", user.Email, user.IsActive, user.IsLocked);
            return SignInResult.LockedOut;
        }

        var result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await UserManager.UpdateAsync(user);
        }

        return result;
    }

    public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
    {
        if (!user.IsActive || user.IsLocked)
            return SignInResult.LockedOut;
        return await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
    }
}
