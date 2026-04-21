using SchemaSentinel.Core.Models;
using SchemaSentinel.Data;

namespace SchemaSentinel.UI.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private readonly SqlConnectionService _connectionService = new();

    private string _profileName = string.Empty;
    private string _serverName = string.Empty;
    private string _databaseName = string.Empty;
    private AuthType _authType = AuthType.WindowsAuth;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _tenantId = string.Empty;
    private int _connectionTimeout = 30;
    private bool _trustServerCertificate = false;
    private bool _encrypt = true;
    private string _connectionStatus = string.Empty;
    private bool _isConnected;
    private bool _isTesting;

    public string Label { get; init; } = "Connection";

    public string ProfileName { get => _profileName; set => SetField(ref _profileName, value); }
    public string ServerName { get => _serverName; set => SetField(ref _serverName, value); }
    public string DatabaseName { get => _databaseName; set => SetField(ref _databaseName, value); }
    public AuthType AuthType
    {
        get => _authType;
        set
        {
            SetField(ref _authType, value);
            OnPropertyChanged(nameof(IsSqlAuth));
            OnPropertyChanged(nameof(IsEntraMfa));
            OnPropertyChanged(nameof(ShowUsernameField));
            OnPropertyChanged(nameof(ShowPasswordField));
            OnPropertyChanged(nameof(ShowTenantField));
        }
    }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string Password { get => _password; set => SetField(ref _password, value); }
    public string TenantId { get => _tenantId; set => SetField(ref _tenantId, value); }
    public int ConnectionTimeout { get => _connectionTimeout; set => SetField(ref _connectionTimeout, value); }
    public bool TrustServerCertificate { get => _trustServerCertificate; set => SetField(ref _trustServerCertificate, value); }
    public bool Encrypt { get => _encrypt; set => SetField(ref _encrypt, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetField(ref _connectionStatus, value); }
    public bool IsConnected { get => _isConnected; set => SetField(ref _isConnected, value); }
    public bool IsTesting { get => _isTesting; set => SetField(ref _isTesting, value); }

    public bool IsSqlAuth         => AuthType == AuthType.SqlAuth;
    public bool IsEntraMfa        => AuthType == AuthType.EntraMfa;
    public bool ShowUsernameField => AuthType == AuthType.SqlAuth || AuthType == AuthType.EntraMfa;
    public bool ShowPasswordField => AuthType == AuthType.SqlAuth;
    public bool ShowTenantField   => AuthType == AuthType.EntraMfa;

    public bool IsWindowsAuth
    {
        get => AuthType == AuthType.WindowsAuth;
        set { if (value) AuthType = AuthType.WindowsAuth; }
    }

    public bool IsSqlAuthentication
    {
        get => AuthType == AuthType.SqlAuth;
        set { if (value) AuthType = AuthType.SqlAuth; }
    }

    public bool IsEntraMfaAuthentication
    {
        get => AuthType == AuthType.EntraMfa;
        set { if (value) AuthType = AuthType.EntraMfa; }
    }

    public AsyncRelayCommand TestConnectionCommand { get; }

    public ConnectionViewModel()
    {
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
    }

    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        IsTesting = true;
        IsConnected = false;
        ConnectionStatus = AuthType == AuthType.EntraMfa
            ? "Browser opening for sign-in..."
            : "Testing...";

        var (success, error) = await _connectionService.TestConnectionAsync(ToProfile(), cancellationToken);

        IsConnected = success;
        ConnectionStatus = success ? "Connected successfully." : $"Failed: {error}";
        IsTesting = false;
    }

    public ConnectionProfile ToProfile() => new()
    {
        ProfileName = ProfileName,
        ServerName = ServerName,
        DatabaseName = DatabaseName,
        AuthType = AuthType,
        Username = Username,
        Password = Password,
        TenantId = string.IsNullOrWhiteSpace(TenantId) ? null : TenantId.Trim(),
        ConnectionTimeout = ConnectionTimeout,
        TrustServerCertificate = TrustServerCertificate,
        Encrypt = Encrypt
    };

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(ServerName) &&
        !string.IsNullOrWhiteSpace(DatabaseName) &&
        (AuthType != AuthType.SqlAuth || !string.IsNullOrWhiteSpace(Username));
}
