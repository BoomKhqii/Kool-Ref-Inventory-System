using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages;

public class LoginModel : PageModel
{
    private readonly string _connectionString;

    public LoginModel(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
    }

    [BindProperty, Required] public string Username { get; set; } = "";
    [BindProperty, Required] public string Password { get; set; } = "";

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();

        const string sql = @"SELECT TOP (1) userId, [name], [password], [type]
            FROM dbo.[User]
            WHERE [name] = @username
            ORDER BY userId;";

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@username", SqlDbType.NChar, 10).Value = Username.Trim();
        using SqlDataReader reader = command.ExecuteReader();

        if (!reader.Read()) return InvalidLogin();

        string storedHash = reader["password"]?.ToString() ?? "";
        try
        {
            if (!BCrypt.Net.BCrypt.Verify(Password, storedHash)) return InvalidLogin();
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return InvalidLogin();
        }

        int userId = Convert.ToInt32(reader["userId"]);
        string accountName = reader["name"]?.ToString()?.Trim() ?? Username.Trim();
        string userType = reader["type"]?.ToString()?.Trim().ToUpperInvariant() ?? "";

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", userId);
        HttpContext.Session.SetString("Username", accountName);
        HttpContext.Session.SetString("UserType", userType);
        return RedirectToPage("/ItemSupply");
    }

    private IActionResult InvalidLogin()
    {
        ModelState.AddModelError("", "Invalid username or password");
        Password = "";
        return Page();
    }
}
