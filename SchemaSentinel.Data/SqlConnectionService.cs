using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Data;

public class SqlConnectionService
{
    public async Task<(bool Success, string? Error)> TestConnectionAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (host, port) = ParseHostAndPort(profile.ServerName);
        var tcpOk = await CheckTcpAsync(host, port, cancellationToken);
        if (!tcpOk)
        {
            return (false,
                $"Cannot reach {host} on port {port}.\n\n" +
                "Possible causes:\n" +
                "  • Not connected to VPN / corporate network\n" +
                "  • Azure SQL MI public endpoint not enabled\n" +
                "  • Firewall blocking the port\n\n" +
                "For Azure SQL MI public endpoint use:\n" +
                "  server.public.<zone>.database.windows.net,3342");
        }

        try
        {
            await using var connection = await CreateAndOpenConnectionAsync(profile, cancellationToken);
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            return (false, "Cancelled.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<SqlConnection> CreateAndOpenConnectionAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(profile.BuildConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<bool> CheckTcpAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await tcp.ConnectAsync(host, port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (string Host, int Port) ParseHostAndPort(string serverName)
    {
        var parts = serverName.Trim().Split(',');
        var host = parts[0].Trim();
        var port = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var p) ? p : 1433;
        return (host, port);
    }
}
