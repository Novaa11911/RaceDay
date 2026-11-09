using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part3.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public sealed class RegisterViewModel
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, MinLength(8), StringLength(150)] public string Password { get; set; } = "";
    [Required, StringLength(20)] public string Phone { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
}
