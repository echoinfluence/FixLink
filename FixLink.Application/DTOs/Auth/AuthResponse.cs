using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Application.DTOs.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IList<string> Roles { get; set; } = new List<string>();

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}