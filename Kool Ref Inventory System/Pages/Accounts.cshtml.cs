using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages;

public class AccountsModel : PageModel
{
    private readonly string _connectionString;

    public AccountsModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
    }

    [BindProperty] public AccountInput Account { get; set; } = new();
    public List<WorkerAccount> Workers { get; private set; } = new();
    public bool OpenAccountDialog { get; private set; }
    [TempData] public string? SuccessMessage { get; set; }

    public IActionResult OnGet()
    {
        IActionResult? accessResult = RequireBoss();
        if (accessResult != null) return accessResult;

        LoadWorkers();
        return Page();
    }

    public IActionResult OnPostCreate() // Creates accounts then encrypts password with BCrypt and stores it in the database
    {
        IActionResult? accessResult = RequireBoss(); // Blocks non Boss type
        if (accessResult != null) return accessResult;

        Account.Name = Account.Name?.Trim() ?? "";
        Account.Type = Account.Type?.Trim().ToUpperInvariant() ?? "";
        ModelState.Clear();
        TryValidateModel(Account, nameof(Account));
        if (!ModelState.IsValid) return ShowAccountDialog();

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(Account.Password);
        int userId;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                if (AccountNameExists(connection, transaction, Account.Name)) // Doesn't allow duplicate account names, even across years
                {
                    ModelState.AddModelError("Account.Name", "That account name is already in use.");
                    transaction.Rollback();
                    return ShowAccountDialog();
                }

                userId = GetNextUserId(connection, transaction); // Gets the next available userId based on the current year
                InsertAccount(connection, transaction, userId, passwordHash);// Inserts the new account into the database
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) // Handles unique constraint violations
        {
            ModelState.AddModelError("Account.Name", "The account could not be created because its ID or name already exists.");
            return ShowAccountDialog();
        }

        SuccessMessage = $"Account {userId} was created successfully.";
        return RedirectToPage();
    }

    private IActionResult? RequireBoss()    // Blocks non Boss type
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        string? username = HttpContext.Session.GetString("Username");

        if (!userId.HasValue && string.IsNullOrWhiteSpace(username))
            return RedirectToPage("/Login");

        const string sql = @"SELECT TOP (1) userId, [type]
            FROM dbo.[User]
            WHERE (@userId IS NOT NULL AND userId = @userId)
               OR (@userId IS NULL AND [name] = @username)
            ORDER BY userId;";
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@userId", SqlDbType.Int).Value = userId.HasValue ? userId.Value : DBNull.Value;
        command.Parameters.Add("@username", SqlDbType.NChar, 10).Value = username?.Trim() ?? "";
        using SqlDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }

        int resolvedUserId = Convert.ToInt32(reader["userId"]);
        string userType = reader["type"]?.ToString()?.Trim().ToUpperInvariant() ?? "";
        HttpContext.Session.SetInt32("UserId", resolvedUserId);
        HttpContext.Session.SetString("UserType", userType);
        return userType == "B" ? null : RedirectToPage("/AccessDenied");
    }

    // Checks if an account name already exists in the database, using a transaction to ensure consistency
    private static bool AccountNameExists(SqlConnection connection, SqlTransaction transaction, string name)
    {
        const string sql = "SELECT TOP (1) 1 FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE [name] = @name;";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@name", SqlDbType.NChar, 10).Value = name;
        return command.ExecuteScalar() != null;
    }

    // Gets the next available userId for the current year, using a transaction to ensure consistency
    private static int GetNextUserId(SqlConnection connection, SqlTransaction transaction)
    {
        int yearBase = DateTime.Today.Year % 100 * 1000;
        const string sql = @"SELECT ISNULL(MAX(userId), @yearBase) + 1
            FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
            WHERE userId > @yearBase AND userId <= @yearEnd;";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@yearBase", SqlDbType.Int).Value = yearBase;
        command.Parameters.Add("@yearEnd", SqlDbType.Int).Value = yearBase + 999;
        int nextId = Convert.ToInt32(command.ExecuteScalar());
        if (nextId > yearBase + 999)
            throw new InvalidOperationException("No account IDs remain for the current year.");
        return nextId;
    }

    // Inserts a new account into the database, using a transaction to ensure consistency
    private void InsertAccount(SqlConnection connection, SqlTransaction transaction, int userId, string passwordHash)
    {
        const string sql = "INSERT INTO dbo.[User] (userId, [name], [password], [type]) VALUES (@id, @name, @password, @type);";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@id", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@name", SqlDbType.NChar, 10).Value = Account.Name;
        command.Parameters.Add("@password", SqlDbType.NVarChar, -1).Value = passwordHash;
        command.Parameters.Add("@type", SqlDbType.Char, 1).Value = Account.Type;
        command.ExecuteNonQuery();
    }

    // Loads the list of workers from the database and opens the account creation dialog
    private IActionResult ShowAccountDialog()
    {
        LoadWorkers();
        OpenAccountDialog = true;
        return Page();
    }

    // Loads the list of workers from the database excluding bosses, and orders them by userId
    private void LoadWorkers()
    {
        const string sql = "SELECT userId, RTRIM([name]) [name] FROM dbo.[User] WHERE [type] = 'W' ORDER BY userId;";
        Workers = new();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
            Workers.Add(new WorkerAccount {
                UserId = Convert.ToInt32(reader["userId"]),
                Name = reader["name"]?.ToString() ?? ""
            });
    }

    public class AccountInput
    {
        // Validates the account name to be required, with a length between 2 and 10 characters
        [Required(ErrorMessage = "Account name is required."), StringLength(10, MinimumLength = 2)]
        public string Name { get; set; } = "";

        [Required, StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        public string Password { get; set; } = "";

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required, RegularExpression("^[BW]$", ErrorMessage = "Select Boss or Worker.")]
        public string Type { get; set; } = "W";
    }

    public class WorkerAccount
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
    }
}
