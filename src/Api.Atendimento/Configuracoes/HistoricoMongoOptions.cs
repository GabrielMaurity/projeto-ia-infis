using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Local do histórico de atendimentos no MongoDB. A conexão fica em
/// ConnectionStrings:HistoricoAtendimentos.
/// </summary>
public class HistoricoMongoOptions
{
    public const string Secao = "HistoricoMongo";

    [Required]
    public string Banco { get; set; } = "bd-historico-atendimentos";

    [Required]
    public string Colecao { get; set; } = "atendimentos";
}
