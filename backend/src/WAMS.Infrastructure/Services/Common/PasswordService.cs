namespace WAMS.Infrastructure.Services.Common;

using WAMS.Application.Interfaces.Common;

public class PasswordService : IPasswordHasher
{
    public string Hash(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    public bool Verify(string plainPassword, string hashedPassword)
        => BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
}
