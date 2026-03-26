using System.Security.Claims;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Identity;

public sealed class MedFlowUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly ApplicationDbContext _db;

    public MedFlowUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options,
        ApplicationDbContext db)
        : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.TenantId.HasValue)
            identity.AddClaim(new Claim("tenant_id", user.TenantId.Value.ToString()));

        if (await UserManager.IsInRoleAsync(user, "Patient"))
        {
            var patient = await _db.Patients.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && !p.IsDeleted);
            if (patient != null)
                identity.AddClaim(new Claim("patient_id", patient.Id.ToString()));
        }

        return identity;
    }
}
