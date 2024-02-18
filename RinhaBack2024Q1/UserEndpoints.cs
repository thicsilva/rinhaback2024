using System.Data;
using MySqlConnector;

namespace RinhaBack2024Q1;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clientes/{id}/extrato", (int id, IConfiguration configuration) =>
        {
            if (id is < 1 or > 5)
                return Results.NotFound();

            var sql = "select saldo, limite from clientes where id=@id";
            using var connection = new MySqlConnection(configuration.GetConnectionString("rinha"));
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            connection.Open();
            var reader = command.ExecuteReader();
            if (!reader.Read())
                return Results.NotFound();
            var saldo = reader.GetInt32(0);
            var limite = reader.GetInt32(1);
            connection.Close();

            List<Transacao> transacoes = [];
            sql = "select id, cliente_id, valor, tipo, descricao, realizada_em  from transacoes where cliente_id=@id order by realizada_em desc limit 10";
            command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            connection.Open();
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                Transacao transacao = new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetChar(3),
                    reader.GetString(4), reader.GetDateTime(5));
                transacoes.Add(transacao);
            }
            connection.Close();
            command.Dispose();

            return Results.Ok(new Extrato()
            {
                Saldo = new Saldo() { Limite = limite, Total = saldo, DataExtrato = DateTime.UtcNow },
                Transacoes = transacoes
            });
        });

        app.MapPost("/clientes/{id}/transacoes", (int id, TransacaoRequestDto? request, IConfiguration configuration) =>
        {
            if (id is < 1 or > 5)
                return Results.NotFound();
            if (request is null ||
                request.Tipo != 'd' && request.Tipo != 'c' ||
                request.Valor <= 0 ||
                string.IsNullOrEmpty(request.Descricao) || request.Descricao.Length > 10)
                return Results.UnprocessableEntity();
            using var connection = new MySqlConnection(configuration.GetConnectionString("rinha"));
            using var command = new MySqlCommand("SalvarTransacoes", connection);
            var outSaldo = new MySqlParameter("@o_saldo", MySqlDbType.Int32)
                { Direction = ParameterDirection.Output };
            var outLimite = new MySqlParameter("@o_limite", MySqlDbType.Int32)
                { Direction = ParameterDirection.Output };
            command.Parameters.AddWithValue("@p_cliente_id", id);
            command.Parameters.AddWithValue("@p_valor", request.Valor);
            command.Parameters.AddWithValue("@p_tipo", request.Tipo);
            command.Parameters.AddWithValue("@p_descricao", request.Descricao);
            command.Parameters.Add(outSaldo);
            command.Parameters.Add(outLimite);
            command.CommandType = CommandType.StoredProcedure;
            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                var saldo = (int)outSaldo.Value!;
                var limite = (int)outLimite.Value!;
                if (saldo == -1 && limite == -1)
                    return Results.UnprocessableEntity();
                return Results.Ok(new TransacaoResponseDto(limite, saldo));
            }
            catch (Exception e)
            {
                return Results.UnprocessableEntity();
            }
        }).DisableRequestTimeout();
    }
}