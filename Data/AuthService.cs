using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PackBot.Data;

// Denne service håndterer login, brugeroprettelse og adgangskontrol.
// Strukturen (service-klasse + async DB-kald) følger undervisningen i
// Industrial Programming:
// https://industrial-programming.aydos.de/security/protecting-credential-database-against-exploitation.html
public class AuthService
{
    // Indstillinger til password hashing (PBKDF2)
    // PBKDF2 er en standard metode til sikker password-håndtering.
    // Reference:
    // https://learn.microsoft.com/dotnet/api/system.security.cryptography.rfc2898derivebytes
    private const int SaltLen = 16;
    private const int KeyLen = 32;
    private const int Iterations = 200_000;

    // Opretter en default admin-bruger, hvis databasen er tom.
    // Bruges ved første start af programmet.
    public async Task EnsureAdminSeedAsync()
    {
        await using var db = new PackBotDbContext();

        // Find admin hvis den allerede findes
        var admin = await db.Accounts.FirstOrDefaultAsync(a => a.Username == "admin");

        if (admin is null)
        {
            // Opret admin første gang
            var (salt, hash) = HashPassword("admin123");
            db.Accounts.Add(new Account
            {
                Username = "admin",
                Salt = salt,
                SaltedPasswordHash = hash,
                IsAdmin = true
            });
            await db.SaveChangesAsync();
            return;
        }

        // Hvis admin findes men ikke er admin → gør den til admin
        if (!admin.IsAdmin)
        {
            admin.IsAdmin = true;
            await db.SaveChangesAsync();
        }
    }


    // Tjekker om et brugernavn allerede findes i databasen
    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var db = new PackBotDbContext();
        return await db.Accounts.AnyAsync(a => a.Username == username);
    }

    // Opretter en ny bruger
    public async Task RegisterAsync(string username, string password, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username missing");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password missing");

        await using var db = new PackBotDbContext();

        if (await db.Accounts.AnyAsync(a => a.Username == username))
            throw new InvalidOperationException("User already exists");

        var (salt, hash) = HashPassword(password);

        db.Accounts.Add(new Account
        {
            Username = username.Trim(),
            Salt = salt,
            SaltedPasswordHash = hash,
            IsAdmin = isAdmin
        });

        await db.SaveChangesAsync();
    }

    // Logger en bruger ind og returnerer om login lykkes,
    // samt om brugeren er admin
    public async Task<(bool ok, bool isAdmin)> LoginAsync(string username, string password)
    {
        await using var db = new PackBotDbContext();

        var acc = await db.Accounts
            .AsNoTracking()   // Kun læsning, ingen tracking nødvendig
            .FirstOrDefaultAsync(a => a.Username == username);

        if (acc is null)
            return (false, false);

        var ok = PasswordCorrect(password, acc.Salt, acc.SaltedPasswordHash);
        return (ok, ok && acc.IsAdmin);
    }

    // -------- Password hashing helpers --------
    // Denne del er baseret på .NET cryptography og generel IT-sikkerhed.
    // Delvist udarbejdet med hjælp fra ChatGPT og derefter tilpasset projektet.

    private static (byte[] salt, byte[] hash) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLen);
        var hash = PBKDF2(password, salt);
        return (salt, hash);
    }

    private static bool PasswordCorrect(string password, byte[] salt, byte[] expectedHash)
    {
        var actual = PBKDF2(password, salt);

        // FixedTimeEquals forhindrer timing attacks
        return CryptographicOperations.FixedTimeEquals(actual, expectedHash);
    }

    private static byte[] PBKDF2(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeyLen
        );
    }
}
