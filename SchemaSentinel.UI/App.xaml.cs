using System.Windows;
using Microsoft.Data.SqlClient;

namespace SchemaSentinel.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var provider = new ActiveDirectoryAuthenticationProvider();
        SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryInteractive, provider);
    }
}
