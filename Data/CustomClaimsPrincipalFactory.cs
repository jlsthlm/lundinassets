using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using LundinAssets.Models;

namespace LundinAssets.Data;

public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public CustomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("FullName", user.FullName));
        identity.AddClaim(new Claim("Initials", user.Initials));
        if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            identity.AddClaim(new Claim("ProfilePhoto", user.ProfilePhotoPath));
        return identity;
    }
}
