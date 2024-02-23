using System.Data;
using System.Runtime.CompilerServices;
using MySqlConnector;

namespace RinhaBack2024Q1;

public class Database(IConfiguration configuration) : IAsyncDisposable
{
    private bool _disposed;
    private readonly MySqlCommand _insertCommand = CreateInsertCommand();

    private static MySqlCommand CreateInsertCommand()
    {
        return new MySqlCommand("SalvarTransacoes")
        {
            Parameters =
            {
                new MySqlParameter("@p_cliente_id", MySqlDbType.Int32),
                new MySqlParameter("@p_valor", MySqlDbType.Int32),
                new MySqlParameter("@p_tipo", MySqlDbType.VarChar),
                new MySqlParameter("@p_descricao", MySqlDbType.VarChar),
                new MySqlParameter("@o_saldo", MySqlDbType.Int32)
                    { Direction = ParameterDirection.Output },
                new MySqlParameter("@o_limite", MySqlDbType.Int32)
                    { Direction = ParameterDirection.Output }
            },
            CommandType = CommandType.StoredProcedure
        };
    }

    private readonly MySqlCommand _getCustomerCommand = GetCustomerCommand();

    private static MySqlCommand GetCustomerCommand()
    {
        return new MySqlCommand("select saldo, limite from clientes where id=@id")
        {
            Parameters = { new MySqlParameter("@id", MySqlDbType.Int32) }
        };
    }

    private readonly MySqlCommand _getTransactionsCommand = GetTransactionsCommand();

    private static MySqlCommand GetTransactionsCommand()
    {
        return new MySqlCommand(
            "select id, cliente_id, valor, tipo, descricao, realizada_em from transacoes where cliente_id=@id order by realizada_em desc limit 10")
        {
            Parameters = { new MySqlParameter("@id", MySqlDbType.Int32) }
        };
    }

    private readonly MySqlDataSource _dataSource =
        new MySqlDataSourceBuilder(configuration.GetConnectionString("rinha")).Build();

    private MySqlConnection NewConnection() => _dataSource.OpenConnection();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<(int saldo, int limite)> AddTransacao(int idCliente, int valor, char tipo, string descricao)
    {
        await using var command = _insertCommand.Clone();
        command.Parameters[0].Value = idCliente;
        command.Parameters[1].Value = valor;
        command.Parameters[2].Value = tipo;
        command.Parameters[3].Value = descricao;
        await using var connection = NewConnection();
        command.Connection = connection;
        await command.ExecuteNonQueryAsync();
        var saldo = (int)command.Parameters[4].Value!;
        var limite = (int)command.Parameters[5].Value!;
        return (saldo, limite);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<(bool found, Extrato? extrato)> GetExtract(int idCliente)
    {
        await using var connection = NewConnection();
        var (success, saldo) = await GetSaldo(idCliente, connection);
        if (!success)
            return (false, null);

        var transacoes = await GetTransactions(idCliente, connection);
        var extrato = new Extrato() { Saldo = saldo, Transacoes = transacoes };
        return (success, extrato);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task<IEnumerable<Transacao>?> GetTransactions(int idCliente, MySqlConnection connection)
    {
        await using var command = _getTransactionsCommand.Clone();
        command.Connection = connection;
        command.Parameters[0].Value = idCliente;
        var transacoes = new List<Transacao>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var transacao = new Transacao(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetChar(3),
                reader.GetString(4), reader.GetDateTime(5));
            transacoes.Add(transacao);
        }

        return transacoes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task<(bool success, Saldo? saldo)> GetSaldo(int idCliente, MySqlConnection connection)
    {
        await using var command = _getCustomerCommand.Clone();
        command.Connection = connection;
        command.Parameters[0].Value = idCliente;
        await using var reader = await command.ExecuteReaderAsync();
        var success = await reader.ReadAsync();
        if (!success)
            return (false, null);
        
        var saldo = new Saldo()
            { Total = reader.GetInt32(0), Limite = reader.GetInt32(1), DataExtrato = DateTime.UtcNow };
        return (true, saldo);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await _insertCommand.DisposeAsync();
        await _getCustomerCommand.DisposeAsync();
        await _getTransactionsCommand.DisposeAsync();
    }
}