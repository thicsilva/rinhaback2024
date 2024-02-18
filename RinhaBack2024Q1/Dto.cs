using System.Text.Json.Serialization;

namespace RinhaBack2024Q1;

public class Saldo
{
    public int Total { get; set; }
    [JsonPropertyName("data_extrato")] 
    public DateTime DataExtrato { get; set; }
    public int Limite { get; set; }
}

public class Transacao
{
    public Transacao(int id, int idCliente, int valor, char tipo, string descricao, DateTime realizadaEm)
    {
        Id = id;
        IdCliente = idCliente;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        RealizadaEm = realizadaEm;
    }
    
    public int Id { get; init; }
    public int IdCliente { get; init; }
    public int Valor { get; init; }
    public char Tipo { get; init; }
    public string Descricao { get; init; }
    [JsonPropertyName("realizada_em")]
    public DateTime RealizadaEm { get; init; }

}

public class Extrato
{
    public Saldo? Saldo { get; set; }

    [JsonPropertyName("ultimas_transacoes")]
    public IEnumerable<Transacao>? Transacoes { get; set; }
}

public record TransacaoRequestDto(int Valor, char Tipo, string Descricao);

public record TransacaoResponseDto(int Limite, int Saldo);