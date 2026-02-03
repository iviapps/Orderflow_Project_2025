using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Orderflow.Identity.Data.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    // Google OAuth
    [MaxLength(100)]
    public string? GoogleId { get; set; }

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
}