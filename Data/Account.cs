using System;
using System.ComponentModel.DataAnnotations;

namespace PackBot.Data;

public class Account
{
    [Key]
    public string Username { get; set; } = "";
    // Salt bruges sammen med password hashing for at øge sikkerheden.
    // Dette forhindrer brug af rainbow tables.
    // Password hashing med salt er generel IT-sikkerhed,
    // ikke kursusspecifikt, men relevant for login-systemer.
    // Reference:
    // https://industrial-programming.aydos.de/security.html
    public byte[] Salt { get; set; } = Array.Empty<byte>();
    public byte[] SaltedPasswordHash { get; set; } = Array.Empty<byte>();
    
// Angiver om brugeren har administrator-rettigheder.
    // Bruges i GUI til at låse admin-funktioner.
    public bool IsAdmin { get; set; }
}