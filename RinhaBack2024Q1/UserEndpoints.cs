namespace RinhaBack2024Q1;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clientes/{id}/extrato", async (int id, Database database) =>
        {
            if (id is < 1 or > 5)
                return Results.NotFound();

            var (found, extract) = await database.GetExtract(id);

            return !found ? Results.NotFound() : Results.Ok(extract);
        });

        app.MapPost("/clientes/{id}/transacoes", async (int id, TransacaoRequestDto? request, Database database) =>
        {
            if (id is < 1 or > 5)
                return Results.NotFound();
            if (request is null ||
                request.Tipo != 'd' && request.Tipo != 'c' ||
                request.Valor <= 0 ||
                string.IsNullOrEmpty(request.Descricao) || request.Descricao.Length > 10)
                return Results.UnprocessableEntity();
            var (saldo,limite) = await database.AddTransacao(id, request.Valor, request.Tipo, request.Descricao);
            if (saldo == -1 && limite == -1)
                return Results.UnprocessableEntity();
            
            return Results.Ok(new TransacaoResponseDto(limite, saldo));
        }).DisableRequestTimeout();
    }
}