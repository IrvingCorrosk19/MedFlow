using Microsoft.AspNetCore.Identity;

namespace MedFlow.Infrastructure.Identity;

public class ApplicationUserRole : IdentityUserRole<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
