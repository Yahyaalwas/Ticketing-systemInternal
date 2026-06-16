using ITS.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ITS.Infrastructure.Services;

public sealed class TicketSequenceService(IConfiguration configuration) : ITicketSequenceService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

    public async Task<int> NextNumberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        // Atomic UPDATE ... OUTPUT guarantees no two concurrent requests get the same number.
        // ROWLOCK hint scopes contention to the single project row rather than the whole table.
        const string sql = """
            UPDATE config.ProjectSequences WITH (ROWLOCK)
            SET CurrentNumber = CurrentNumber + 1,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.CurrentNumber
            WHERE ProjectId = @ProjectId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProjectId", projectId);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null or DBNull)
            throw new InvalidOperationException($"Project sequence not found for project {projectId}. Ensure ProjectSequences is seeded when a project is created.");

        return Convert.ToInt32(result);
    }
}
