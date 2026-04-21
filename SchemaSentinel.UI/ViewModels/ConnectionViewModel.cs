using System.Collections.ObjectModel;
using SchemaSentinel.Core.Models;
using SchemaSentinel.Data;

namespace SchemaSentinel.UI.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private readonly SqlConnectionService _connectionService = new();
    private readonly ConnectionProfileStore _store;

    private string _profileName = string.Empty;
    private string _serverName = string.Empty;
    private string _databaseName = string.Empty;
    private AuthType _authType = AuthType.WindowsAuth;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _connectionTimeout = 30;
    private bool _trustServerCertificate = false;
    private bool _encrypt = true;
    private string _connectionStatus = string.Empty;
    private bool _isConnected;
    private bool _isTesting;
    private bool _isExpanded = true;
    private ConnectionProfile? _selectedSavedProfile;

    public string Label { get; init; } = "Connection";

    public string ProfileName { get => _profileName; set => SetField(ref _profileName, value); }
    public string ServerName
    {
        get => _serverName;
        set { SetField(ref _serverName, value); OnPropertyChanged(nameof(ConnectionSummary)); }
    }
    public string DatabaseName
    {
        get => _databaseName;
        set { SetField(ref _databaseName, value); OnPropertyChanged(nameof(ConnectionSummary)); }
    }
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
        }
    }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string Password { get => _password; set => SetField(ref _password, value); }
    public int ConnectionTimeout { get => _connectionTimeout; set => SetField(ref _connectionTimeout, value); }
    public bool TrustServerCertificate { get => _trustServerCertificate; set => SetField(ref _trustServerCertificate, value); }
    public bool Encrypt { get => _encrypt; set => SetField(ref _encrypt, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetField(ref _connectionStatus, value); }
    public bool IsConnected { get => _isConnected; set => SetField(ref _isConnected, value); }
    public bool IsTesting { get => _isTesting; set => SetField(ref _isTesting, value); }
    public bool IsExpanded { get => _isExpanded; set => SetField(ref _isExpanded, value); }

    public string ConnectionSummary =>
        string.IsNullOrWhiteSpace(ServerName) ? string.Empty : $"{ServerName} / {DatabaseName}";

    public bool IsSqlAuth         => AuthType == AuthType.SqlAuth;
    public bool IsEntraMfa        => AuthType == AuthType.EntraMfa;
    public bool ShowUsernameField => AuthType == AuthType.SqlAuth || AuthType == AuthType.EntraMfa;
    public bool ShowPasswordField => AuthType == AuthType.SqlAuth;

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

    public ObservableCollection<ConnectionProfile> SavedProfiles { get; } = new();

    public ConnectionProfile? SelectedSavedProfile
    {
        get => _selectedSavedProfile;
        set
        {
            SetField(ref _selectedSavedProfile, value);
            if (value != null) LoadFromProfile(value);
        }
    }

    public AsyncRelayCommand TestConnectionCommand { get; }
    public RelayCommand SaveProfileCommand { get; }
    public RelayCommand ToggleExpandCommand { get; }

    public ConnectionViewModel(ConnectionProfileStore store)
    {
        _store = store;
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveProfileCommand = new RelayCommand(SaveProfile);
        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        RefreshProfiles();
    }

    public void RefreshProfiles()
    {
        var current = _selectedSavedProfile?.ProfileName;
        SavedProfiles.Clear();
        foreach (var p in _store.Load())
            SavedProfiles.Add(p);
        if (current != null)
            _selectedSavedProfile = SavedProfiles.FirstOrDefault(p => p.ProfileName == current);
    }

    private void SaveProfile()
    {
        var all = _store.Load();
        var current = ToProfile();
        if (string.IsNullOrWhiteSpace(current.ProfileName))
            current.ProfileName = current.DisplayName;

        var idx = all.FindIndex(p => p.ProfileName == current.ProfileName);
        if (idx >= 0)
            all[idx] = current;
        else
            all.Add(current);

        _store.Save(all);
        RefreshProfiles();
        _selectedSavedProfile = SavedProfiles.FirstOrDefault(p => p.ProfileName == current.ProfileName);
        OnPropertyChanged(nameof(SelectedSavedProfile));
    }

    private void LoadFromProfile(ConnectionProfile p)
    {
        ProfileName = p.ProfileName;
        ServerName = p.ServerName;
        DatabaseName = p.DatabaseName;
        AuthType = p.AuthType;
        Username = p.Username ?? string.Empty;
        Password = p.Password ?? string.Empty;
        ConnectionTimeout = p.ConnectionTimeout;
        TrustServerCertificate = p.TrustServerCertificate;
        Encrypt = p.Encrypt;
        IsConnected = false;
        ConnectionStatus = string.Empty;
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
        ConnectionTimeout = ConnectionTimeout,
        TrustServerCertificate = TrustServerCertificate,
        Encrypt = Encrypt
    };

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(ServerName) &&
        !string.IsNullOrWhiteSpace(DatabaseName) &&
        (AuthType != AuthType.SqlAuth || !string.IsNullOrWhiteSpace(Username));
}
