using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.Interfaces;

namespace MinimalApi.Dominio.Servicos;

public class SecurityService : ISecurityService
{
    public string EncryptPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    Task<OkResult> ISecurityService.EncryptPassword(string password)
    {
        throw new NotImplementedException();
    }
}
