using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Kool_Ref_Inventory_System.Pages
{
    public class ItemSupplyModel : PageModel
    {
        private readonly string _connectionString;

        public ItemSupplyModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
        }

        [BindProperty(SupportsGet = true)]
        [StringLength(200)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public AddItemInput AddItem { get; set; } = new();

        [BindProperty]
        public EditItemInput EditItem { get; set; } = new();

        public List<SupplyItem> Items { get; private set; } = new();
        public bool OpenAddDialog { get; private set; }
        public bool OpenEditDialog { get; private set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            LoadItems();
            return Page();
        }

        public IActionResult OnPostAdd()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            ModelState.ClearValidationState(nameof(EditItem));
            foreach (string key in ModelState.Keys
                .Where(key => key.StartsWith($"{nameof(EditItem)}.", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(key);
            }

            if (!TryValidateModel(AddItem, nameof(AddItem)))
            {
                LoadItems();
                OpenAddDialog = true;
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                    INSERT INTO dbo.ItemList (itemId, [name], price)
                    VALUES (@code, @name, @price);";

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add("@code", SqlDbType.NVarChar, 50).Value = AddItem.Code.Trim();
                command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = AddItem.Name.Trim();
                AddPriceParameter(command, "@price", AddItem.Price ?? 0m);
                command.ExecuteNonQuery();
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            {
                ModelState.AddModelError("AddItem.Code", "That item code already exists.");
                LoadItems();
                OpenAddDialog = true;
                return Page();
            }

            SuccessMessage = $"Item {AddItem.Code.Trim()} was added successfully.";
            return RedirectToPage(new { SearchTerm });
        }

        public IActionResult OnPostEdit()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            ModelState.ClearValidationState(nameof(AddItem));
            foreach (string key in ModelState.Keys
                .Where(key => key.StartsWith($"{nameof(AddItem)}.", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(key);
            }

            if (!TryValidateModel(EditItem, nameof(EditItem)))
            {
                LoadItems();
                OpenEditDialog = true;
                return Page();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                const string query = @"
                    UPDATE dbo.ItemList
                    SET [name] = @name,
                        price = @price
                    WHERE itemId = @code;";

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add("@code", SqlDbType.NVarChar, 50).Value = EditItem.Code.Trim();
                command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = EditItem.Name.Trim();
                AddPriceParameter(command, "@price", EditItem.Price!.Value);

                if (command.ExecuteNonQuery() == 0)
                {
                    ModelState.AddModelError(string.Empty, "The item could not be found. It may have been removed.");
                    LoadItems();
                    OpenEditDialog = true;
                    return Page();
                }
            }

            SuccessMessage = $"Item {EditItem.Code.Trim()} was updated successfully.";
            return RedirectToPage(new { SearchTerm });
        }

        private bool IsLoggedIn() => HttpContext.Session.GetString("Username") != null;

        private void LoadItems()
        {
            Items = new List<SupplyItem>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string query = @"
                SELECT itemId, [name], price
                FROM dbo.ItemList
                WHERE @searchPattern IS NULL
                   OR [name] LIKE @searchPattern ESCAPE '\'
                ORDER BY [name], itemId;";

            using var command = new SqlCommand(query, connection);
            string? searchPattern = string.IsNullOrWhiteSpace(SearchTerm)
                ? null
                : $"%{EscapeLikePattern(SearchTerm.Trim())}%";
            command.Parameters.Add("@searchPattern", SqlDbType.NVarChar, 400).Value =
                searchPattern is null ? DBNull.Value : searchPattern;

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Items.Add(new SupplyItem
                {
                    Code = reader["itemId"]?.ToString() ?? string.Empty,
                    Name = reader["name"]?.ToString() ?? string.Empty,
                    Price = reader["price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["price"])
                });
            }
        }

        private static string EscapeLikePattern(string value) => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

        private static void AddPriceParameter(SqlCommand command, string name, decimal value)
        {
            SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Money);
            parameter.Value = value;
        }

        public class AddItemInput
        {
            [Required(ErrorMessage = "Item code is required.")]
            [StringLength(50, ErrorMessage = "Item code cannot be longer than 50 characters.")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "Item name is required.")]
            [StringLength(200, ErrorMessage = "Item name cannot be longer than 200 characters.")]
            public string Name { get; set; } = string.Empty;

            [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Price cannot be negative.")]
            public decimal? Price { get; set; }
        }

        public class EditItemInput
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "Item name is required.")]
            [StringLength(200, ErrorMessage = "Item name cannot be longer than 200 characters.")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Price is required.")]
            [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Price cannot be negative.")]
            public decimal? Price { get; set; }
        }

        public class SupplyItem
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }
    }
}
