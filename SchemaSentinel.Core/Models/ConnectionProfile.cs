using System.Text;

namespace SchemaSentinel.Core.Models;

public enum AuthType { WindowsAuth, SqlAuth, EntraMfa }

public class ConnectionProfile
{
    public string ProfileName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public AuthType AuthType { get; set; } = AuthType.WindowsAuth;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int ConnectionTimeout { get; set; } = 30;
    public bool TrustServerCertificate { get; set; } = false;
    public bool Encrypt { get; set; } = true;

    public string BuildConnectionString()
    {
        var sb = new StringBuilder();
        sb.Append($"Server={ServerName};Database={DatabaseName};");
        sb.Append($"Connect Timeout={ConnectionTimeout};");
        sb.Append($"Encrypt={Encrypt};TrustServerCertificate={TrustServerCertificate};");

        if (AuthType == AuthType.WindowsAuth)
            sb.Append("Integrated Security=true;");
        else if (AuthType == AuthType.SqlAuth)
            sb.Append($"User Id={Username};Password={Password};");
        else if (AuthType == AuthType.EntraMfa)
        {
            sb.Append("Authentication=Active Directory Interactive;");
            if (!string.IsNullOrWhiteSpace(Username))
                sb.Append($"User Id={Username};");
        }

        return sb.ToString();
    }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(ProfileName)
            ? $"{ServerName} / {DatabaseName}"
            : ProfileName;
}
