using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Dominio.Interfaces;

public interface IVeiculoServico
{
    List<Veiculo> Todos(int? pagina = 1, string? nome = null, string? marca = null);
    Veiculo? BuscaPorId(int id);
    void Incluir(Veiculo voidculo);
    void Atualizar(Veiculo veiculo);
    void Apagar(Veiculo veiculo);
    }