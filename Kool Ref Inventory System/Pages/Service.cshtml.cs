using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages;

public class ServiceModel : PageModel
{
    private readonly string _connectionString;

    public ServiceModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
    }

    [BindProperty] public ServiceReportInput Report { get; set; } = new();
    public List<ServiceRecord> Records { get; private set; } = new();
    public List<ClientOption> ClientCatalog { get; private set; } = new();
    public List<string> TechnicianSuggestions { get; private set; } = new();
    public bool OpenReportDialog { get; private set; }
    [TempData] public string? SuccessMessage { get; set; }

    public IActionResult OnGet()
    {
        if (!IsLoggedIn()) return RedirectToPage("/Login");
        LoadPageData();
        EnsureRepeatableInputs();
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!IsLoggedIn()) return RedirectToPage("/Login");

        Report.Technicians ??= new();
        Report.Technicians = Report.Technicians
            .Select(name => name?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Report.ScopesOfWork ??= new();
        Report.ScopesOfWork = Report.ScopesOfWork
            .Select(scope => scope?.Trim() ?? string.Empty)
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        LoadSuggestions();
        ValidateReport();
        if (!ModelState.IsValid) return ShowReportDialog();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int clientId = GetOrCreateClient(connection, transaction);
                InsertServiceReport(connection, transaction, clientId);
                InsertScopes(connection, transaction);
                InsertTechnicians(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            ModelState.AddModelError("Report.ServiceReceipt", "That service receipt already exists.");
            return ShowReportDialog();
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            ModelState.AddModelError("Report.DeliveryReceipt", "The delivery receipt does not exist in the Inventory Ledger.");
            return ShowReportDialog();
        }

        SuccessMessage = $"Service report {Report.ServiceReceipt} was saved successfully.";
        return RedirectToPage();
    }

    private bool IsLoggedIn() => HttpContext.Session.GetString("Username") != null;

    private void ValidateReport()
    {
        if (Report.Technicians.Count == 0)
            ModelState.AddModelError("Report.Technicians", "Add at least one technician.");
        if (Report.Technicians.Count > 10)
            ModelState.AddModelError("Report.Technicians", "A report can have at most ten technicians.");
        if (Report.Technicians.Any(name => name.Length > 200))
            ModelState.AddModelError("Report.Technicians", "Technician names cannot exceed 200 characters.");

        if (Report.ScopesOfWork.Count == 0)
            ModelState.AddModelError("Report.ScopesOfWork", "Add at least one scope of work.");
        if (Report.ScopesOfWork.Count > 20)
            ModelState.AddModelError("Report.ScopesOfWork", "A report can have at most twenty scopes of work.");
        if (Report.ScopesOfWork.Any(scope => scope.Length > 2000))
            ModelState.AddModelError("Report.ScopesOfWork", "Each scope of work cannot exceed 2,000 characters.");

        if (Report.DateStarted.HasValue && Report.DateEnded.HasValue)
        {
            if (Report.DateEnded.Value.Date < Report.DateStarted.Value.Date)
                ModelState.AddModelError("Report.DateEnded", "Date ended cannot be before date started.");
            else if (Report.DateEnded.Value.Date == Report.DateStarted.Value.Date
                && Report.TimeIn.HasValue && Report.TimeOut.HasValue
                && Report.TimeOut.Value < Report.TimeIn.Value)
                ModelState.AddModelError("Report.TimeOut", "Time out cannot be before time in on the same date.");
        }

    }

    private int GetOrCreateClient(SqlConnection connection, SqlTransaction transaction)
    {
        const string find = @"SELECT TOP (1) clientId FROM dbo.Client WITH (UPDLOCK, HOLDLOCK)
                              WHERE [name] = @name AND [address] = @address ORDER BY clientId;";
        using (var command = new SqlCommand(find, connection, transaction))
        {
            command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = Report.Customer.Trim();
            command.Parameters.Add("@address", SqlDbType.NVarChar, 500).Value = Report.Address.Trim();
            object? value = command.ExecuteScalar();
            if (value != null && value != DBNull.Value) return Convert.ToInt32(value);
        }

        int clientId;
        using (var command = new SqlCommand(
            "SELECT ISNULL(MAX(clientId), 25999) + 1 FROM dbo.Client WITH (UPDLOCK, HOLDLOCK);",
            connection, transaction))
            clientId = Convert.ToInt32(command.ExecuteScalar());

        const string insert = "INSERT INTO dbo.Client (clientId, [name], [address]) VALUES (@id, @name, @address);";
        using var insertCommand = new SqlCommand(insert, connection, transaction);
        insertCommand.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
        insertCommand.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = Report.Customer.Trim();
        insertCommand.Parameters.Add("@address", SqlDbType.NVarChar, 500).Value = Report.Address.Trim();
        insertCommand.ExecuteNonQuery();
        return clientId;
    }

    private void InsertServiceReport(SqlConnection connection, SqlTransaction transaction, int clientId)
    {
        const string sql = @"INSERT INTO dbo.ServiceReport
            (serviceReceipt, timeIn, timeOut, dateStarted, dateEnded, clientId, deliveryReceipt)
            VALUES (@receipt, @timeIn, @timeOut, @started, @ended, @clientId, @deliveryReceipt);";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@receipt", SqlDbType.Int).Value = Report.ServiceReceipt!.Value;
        command.Parameters.Add("@timeIn", SqlDbType.Time).Value = Report.TimeIn!.Value;
        command.Parameters.Add("@timeOut", SqlDbType.Time).Value = Report.TimeOut!.Value;
        command.Parameters.Add("@started", SqlDbType.Date).Value = Report.DateStarted!.Value.Date;
        command.Parameters.Add("@ended", SqlDbType.Date).Value = Report.DateEnded!.Value.Date;
        command.Parameters.Add("@clientId", SqlDbType.Int).Value = clientId;
        command.Parameters.Add("@deliveryReceipt", SqlDbType.Int).Value = Report.DeliveryReceipt!.Value;
        command.ExecuteNonQuery();
    }

    private void InsertScopes(SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = "INSERT INTO dbo.ServiceScopeOfWork (serviceReceipt, scopeOfWork) VALUES (@receipt, @scope);";
        foreach (string scope in Report.ScopesOfWork)
        {
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@receipt", SqlDbType.Int).Value = Report.ServiceReceipt!.Value;
            command.Parameters.Add("@scope", SqlDbType.NVarChar, -1).Value = scope;
            command.ExecuteNonQuery();
        }
    }

    private void InsertTechnicians(SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = "INSERT INTO dbo.ServiceTechnician (serviceReceipt, technician) VALUES (@receipt, @name);";
        foreach (string name in Report.Technicians)
        {
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@receipt", SqlDbType.Int).Value = Report.ServiceReceipt!.Value;
            command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
            command.ExecuteNonQuery();
        }
    }

    private IActionResult ShowReportDialog()
    {
        LoadRecords();
        EnsureRepeatableInputs();
        OpenReportDialog = true;
        return Page();
    }

    private void EnsureRepeatableInputs()
    {
        if (Report.Technicians == null || Report.Technicians.Count == 0)
            Report.Technicians = new() { string.Empty };
        if (Report.ScopesOfWork == null || Report.ScopesOfWork.Count == 0)
            Report.ScopesOfWork = new() { string.Empty };
    }

    private void LoadPageData()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        ClientCatalog = ReadClientCatalog(connection);
        TechnicianSuggestions = ReadSuggestions(connection, "technician", "dbo.ServiceTechnician");
        Records = ReadRecords(connection);
    }

    private void LoadSuggestions()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        ClientCatalog = ReadClientCatalog(connection);
        TechnicianSuggestions = ReadSuggestions(connection, "technician", "dbo.ServiceTechnician");
    }

    private void LoadRecords()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        Records = ReadRecords(connection);
    }

    private static List<string> ReadSuggestions(SqlConnection connection, string column, string table)
    {
        string sql = $"SELECT DISTINCT {column} FROM {table} WHERE NULLIF(LTRIM(RTRIM({column})), '') IS NOT NULL ORDER BY {column};";
        var result = new List<string>();
        using var command = new SqlCommand(sql, connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read()) result.Add(reader.GetString(0).Trim());
        return result;
    }

    private static List<ClientOption> ReadClientCatalog(SqlConnection connection)
    {
        const string sql = @"SELECT clientId, [name], [address]
                             FROM dbo.Client
                             ORDER BY [name], [address], clientId;";
        var result = new List<ClientOption>();
        using var command = new SqlCommand(sql, connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(new ClientOption {
                Id = Convert.ToInt32(reader["clientId"]),
                Name = reader["name"]?.ToString()?.Trim() ?? "",
                Address = reader["address"]?.ToString()?.Trim() ?? ""
            });
        return result;
    }

    private static List<ServiceRecord> ReadRecords(SqlConnection connection)
    {
        const string sql = @"SELECT sr.serviceReceipt, sr.timeIn, sr.timeOut, sr.dateStarted, sr.dateEnded,
            c.[name] clientName, c.[address] clientAddress, sr.deliveryReceipt
            FROM dbo.ServiceReport sr INNER JOIN dbo.Client c ON c.clientId = sr.clientId
            ORDER BY sr.serviceReceipt DESC;";
        var result = new List<ServiceRecord>();
        using (var command = new SqlCommand(sql, connection))
        using (SqlDataReader reader = command.ExecuteReader())
            while (reader.Read())
                result.Add(new ServiceRecord {
                    ServiceReceipt = Convert.ToInt32(reader["serviceReceipt"]),
                    TimeIn = (TimeSpan)reader["timeIn"], TimeOut = (TimeSpan)reader["timeOut"],
                    DateStarted = Convert.ToDateTime(reader["dateStarted"]), DateEnded = Convert.ToDateTime(reader["dateEnded"]),
                    Customer = reader["clientName"]?.ToString() ?? "", Address = reader["clientAddress"]?.ToString() ?? "",
                    DeliveryReceipt = reader["deliveryReceipt"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["deliveryReceipt"])
                });

        var byReceipt = result.ToDictionary(record => record.ServiceReceipt);
        LoadChildren(connection, "SELECT serviceReceipt, technician FROM dbo.ServiceTechnician ORDER BY serviceReceipt DESC, technician;",
            byReceipt, (record, value) => record.Technicians.Add(value));
        LoadChildren(connection, "SELECT serviceReceipt, scopeOfWork FROM dbo.ServiceScopeOfWork ORDER BY serviceReceipt DESC, scopeOfWork;",
            byReceipt, (record, value) => record.ScopesOfWork.Add(value));
        LoadDeliveryItems(connection, byReceipt);
        return result;
    }

    private static void LoadDeliveryItems(SqlConnection connection, Dictionary<int, ServiceRecord> records)
    {
        const string sql = @"SELECT sr.serviceReceipt, dpi.itemId, item.[name] itemName,
            CASE WHEN dpi.total IS NOT NULL AND dpi.quantity > 0 THEN dpi.total / dpi.quantity ELSE item.price END unitPrice,
            dpi.quantity, dpi.total
            FROM dbo.ServiceReport sr
            INNER JOIN dbo.DeliveryProcessedItem dpi ON dpi.deliveryReceipt = sr.deliveryReceipt
            OUTER APPLY (SELECT TOP (1) catalog.[name], catalog.price FROM dbo.ItemList catalog
                         WHERE catalog.itemId = dpi.itemId ORDER BY catalog.[name]) item
            ORDER BY sr.serviceReceipt DESC, dpi.itemId;";
        using var command = new SqlCommand(sql, connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!records.TryGetValue(Convert.ToInt32(reader["serviceReceipt"]), out ServiceRecord? record))
                continue;

            record.DeliveryItems.Add(new DeliveryItem {
                ItemId = reader["itemId"]?.ToString() ?? "",
                ItemName = reader["itemName"]?.ToString() ?? "",
                Quantity = Convert.ToInt32(reader["quantity"]),
                UnitPrice = reader["unitPrice"] == DBNull.Value ? null : Convert.ToDecimal(reader["unitPrice"]),
                Total = reader["total"] == DBNull.Value ? null : Convert.ToDecimal(reader["total"])
            });
        }
    }

    private static void LoadChildren(SqlConnection connection, string sql, Dictionary<int, ServiceRecord> records,
        Action<ServiceRecord, string> add)
    {
        using var command = new SqlCommand(sql, connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
            if (records.TryGetValue(Convert.ToInt32(reader[0]), out ServiceRecord? record))
                add(record, reader[1]?.ToString() ?? "");
    }

    public class ServiceReportInput
    {
        [Required(ErrorMessage = "Service receipt is required."), Range(1, int.MaxValue)] public int? ServiceReceipt { get; set; }
        public List<string> ScopesOfWork { get; set; } = new() { "" };
        [Required] public TimeSpan? TimeIn { get; set; }
        [Required] public TimeSpan? TimeOut { get; set; }
        [Required, DataType(DataType.Date)] public DateTime? DateStarted { get; set; } = DateTime.Today;
        [Required, DataType(DataType.Date)] public DateTime? DateEnded { get; set; } = DateTime.Today;
        [Required, StringLength(200)] public string Customer { get; set; } = "";
        [Required, StringLength(500)] public string Address { get; set; } = "";
        public List<string> Technicians { get; set; } = new() { "" };
        [Required(ErrorMessage = "Delivery receipt is required."), Range(1, int.MaxValue)] public int? DeliveryReceipt { get; set; }
    }

    public class ClientOption { public int Id { get; set; } public string Name { get; set; } = ""; public string Address { get; set; } = ""; }
    public class ServiceRecord
    {
        public int ServiceReceipt { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateEnded { get; set; }
        public string Customer { get; set; } = "";
        public string Address { get; set; } = "";
        public List<string> Technicians { get; } = new();
        public List<string> ScopesOfWork { get; } = new();
        public int? DeliveryReceipt { get; set; }
        public List<DeliveryItem> DeliveryItems { get; } = new();
    }
    public class DeliveryItem
    {
        public string ItemId { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Total { get; set; }
    }
}
