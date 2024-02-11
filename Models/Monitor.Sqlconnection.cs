using Microsoft.Data.SqlClient;

namespace Brandmauer;

public partial class Monitor
{
    public string Sqlconnection_Host { get; set; } = string.Empty;
    public string Sqlconnection_Database { get; set; } = "master";
    public string Sqlconnection_Username { get; set; } = string.Empty;
    public string Sqlconnection_Password { get; set; } = string.Empty;

    bool Check_Sqlconnection()
    {
        if (Sqlconnection_Host == string.Empty)
            return true;

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Sqlconnection_Host,
            InitialCatalog = Sqlconnection_Database,
            ConnectTimeout = 10,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };
        if (Sqlconnection_Username != string.Empty)
        {
            builder.UserID = Sqlconnection_Username;
            builder.Password = Sqlconnection_Password;
        }

        builder.ConnectTimeout = 30;

        try
        {
            var connectionString = builder.ToString();
            var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            sqlConnection.Close();
            try { sqlConnection.Dispose(); } catch { }
            return true;
        }
        catch (Exception ex)
        {
            Audit.Error<Monitor>(ex);
            return false;
        }
    }
}
