
using Microsoft.AspNetCore.Mvc;

namespace MinimalApi.Dominio.Interfaces
{
    public interface ISecurityService
    {
        public Task<OkResult> EncryptPassword(string password);
    }
}