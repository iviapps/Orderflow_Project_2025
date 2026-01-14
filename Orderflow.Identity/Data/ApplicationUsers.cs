using Microsoft.AspNetCore.Identity;

namespace Orderflow.Identity.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Google OAuth
    public string? GoogleId { get; set; }
    public string? ProfilePictureUrl { get; set; }
}