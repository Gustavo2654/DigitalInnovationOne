using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos;

public class AdministradorServico : IAdministradorServico
{
    private readonly DbContexto _contexto;
    public AdministradorServico(DbContexto contexto)
    {
        _contexto = contexto;
    }

    public Administrador? BuscaPorId(int id)
    {
        return _contexto.Administradores.Where(v => v.Id == id).FirstOrDefault();
    }

    public Administrador? Incluir(Administrador administrador)
    {
        string hashSenha = BCrypt.Net.BCrypt.HashPassword(administrador.Senha);
        administrador.Senha = hashSenha;

        _contexto.Administradores.Add(administrador);
        _contexto.SaveChanges();

        return administrador;
    }

    public Administrador? Login(LoginDTO loginDTO)
    {
        if (loginDTO == null || string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Senha))
            return null;

        var adm = _contexto.Administradores.FirstOrDefault(a => a.Email == loginDTO.Email);
        if (adm == null) return null;

        bool senhaValida = false;
        try
        {
            senhaValida = BCrypt.Net.BCrypt.Verify(loginDTO.Senha, adm.Senha);
        }
        catch
        {
            // Se a senha armazenada não estiver no formato bcrypt
            // compara texto plano e, se bater, re-hash e salva no banco.
            if (loginDTO.Senha == adm.Senha)
            {
                adm.Senha = BCrypt.Net.BCrypt.HashPassword(loginDTO.Senha);
                _contexto.SaveChanges();
                senhaValida = true;
            }
            else
            {
                return null;
            }
        }

        if (!senhaValida) return null;

        return adm;
    }

    public List<Administrador> Todos(int? pagina)
    {
        var query = _contexto.Administradores.AsQueryable();
        int itensPorPagina = 10;
    
        if(pagina != null)
        query = query
        .Skip(((int)pagina - 1) * itensPorPagina)
        .Take(itensPorPagina);
        
        return [.. query];
    }
}