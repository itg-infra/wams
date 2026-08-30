namespace WAMS.Application.Interfaces.Common;

public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hashedPassword);
}
