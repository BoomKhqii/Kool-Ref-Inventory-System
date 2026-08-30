using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages
{
    public class InventoryModel : PageModel
    {
        private readonly string _connectionString;

        public InventoryModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
        }

        [BindProperty]
        public DeliveryReportInput Delivery { get; set; } = new();
        public List<Items> Inventory { get; private set; } = new();
        public List<ProductOption> ProductCatalog { get; private set; } = new();
        public bool OpenDeliveryDialog { get; private set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            LoadPageData();
            EnsureAtLeastOneDeliveryItem();
            return Page();
        }

        public IActionResult OnPostCreateDelivery()
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            Delivery.Items ??= new List<DeliveryLineInput>();
            ProductCatalog = LoadProductCatalog();
            ValidateDeliveryItems();

            if (!ModelState.IsValid)
            {
                LoadInventory();
                EnsureAtLeastOneDeliveryItem();
                OpenDeliveryDialog = true;
                return Page();
            }

            Dictionary<string, ProductOption> productsByCode = ProductCatalog.ToDictionary(
                product => product.Code,
                StringComparer.OrdinalIgnoreCase);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    InsertDeliveryReport(connection, transaction);

                    foreach (DeliveryLineInput line in Delivery.Items)
                    {
                        ProductOption product = productsByCode[line.ItemId.Trim()];
                        bool priceWasEdited = line.UnitPrice != line.OriginalUnitPrice;
                        decimal? effectivePrice = priceWasEdited ? line.UnitPrice : product.Price;

                        if (priceWasEdited)
                        {
                            UpdateItemPrice(connection, transaction, product.Code, line.UnitPrice);
                            product.Price = line.UnitPrice;
                        }

                        InsertDeliveryItem(
                            connection,
                            transaction,
                            product.Code,
                            effectivePrice,
                            line.Quantity!.Value);
                    }

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
                ModelState.AddModelError("Delivery.DeliveryReceipt", "That delivery receipt already exists.");
                LoadInventory();
                EnsureAtLeastOneDeliveryItem();
                OpenDeliveryDialog = true;
                return Page();
            }

            SuccessMessage = $"Delivery receipt {Delivery.DeliveryReceipt} was created with {Delivery.Items.Count} "
                + (Delivery.Items.Count == 1 ? "item." : "items.");
            return RedirectToPage();
        }

        private bool IsLoggedIn() => HttpContext.Session.GetString("Username") != null;
        private void EnsureAtLeastOneDeliveryItem()
        {
            if (Delivery.Items == null || Delivery.Items.Count == 0)
            {
                Delivery.Items = new List<DeliveryLineInput> { new() };
            }
        }

        private void ValidateDeliveryItems()
        {
            if (Delivery.Items.Count == 0)
            {
                ModelState.AddModelError("Delivery.Items", "Add at least one item to the delivery.");
                return;
            }

            Dictionary<string, ProductOption> productsByCode = ProductCatalog.ToDictionary(
                product => product.Code,
                StringComparer.OrdinalIgnoreCase);
            var seenItemCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < Delivery.Items.Count; index++)
            {
                DeliveryLineInput line = Delivery.Items[index];
                string code = line.ItemId?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(code) || !productsByCode.TryGetValue(code, out ProductOption? product))
                {
                    ModelState.AddModelError(
                        $"Delivery.Items[{index}].ProductName",
                        "Select a product from the suggestions.");
                    continue;
                }

                line.ItemId = product.Code;
                line.ProductName = product.Name;

                if (!seenItemCodes.Add(product.Code))
                {
                    ModelState.AddModelError(
                        $"Delivery.Items[{index}].ProductName",
                        "The same product cannot be added twice to one delivery.");
                }

                decimal? effectivePrice = line.UnitPrice != line.OriginalUnitPrice
                    ? line.UnitPrice
                    : product.Price;

                if (line.Quantity.HasValue
                    && effectivePrice.HasValue
                    && effectivePrice.Value * line.Quantity.Value > 922337203685477.5807m)
                {
                    ModelState.AddModelError(
                        $"Delivery.Items[{index}].Quantity",
                        "The calculated total is too large.");
                }
            }
        }

        private void InsertDeliveryReport(SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                INSERT INTO dbo.DeliveryReport (deliveryReceipt, [date], [location])
                VALUES (@deliveryReceipt, @date, @location);";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.Add("@deliveryReceipt", SqlDbType.Int).Value = Delivery.DeliveryReceipt!.Value;
            command.Parameters.Add("@date", SqlDbType.Date).Value = Delivery.Date!.Value.Date;
            command.Parameters.Add("@location", SqlDbType.NVarChar, -1).Value = Delivery.Location.Trim();
            command.ExecuteNonQuery();
        }

        private void InsertDeliveryItem(
            SqlConnection connection,
            SqlTransaction transaction,
            string itemCode,
            decimal? unitPrice,
            int quantity)
        {
            const string query = @"
                INSERT INTO dbo.DeliveryProcessedItem (deliveryReceipt, itemId, quantity, total)
                VALUES (@deliveryReceipt, @itemId, @quantity, @total);";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.Add("@deliveryReceipt", SqlDbType.Int).Value = Delivery.DeliveryReceipt!.Value;
            command.Parameters.Add("@itemId", SqlDbType.NVarChar, 50).Value = itemCode;
            command.Parameters.Add("@quantity", SqlDbType.Int).Value = quantity;

            SqlParameter totalParameter = command.Parameters.Add("@total", SqlDbType.Money);
            totalParameter.Value = unitPrice.HasValue
                ? unitPrice.Value * quantity
                : DBNull.Value;

            command.ExecuteNonQuery();
        }

        private static void UpdateItemPrice(
            SqlConnection connection,
            SqlTransaction transaction,
            string itemCode,
            decimal? price)
        {
            const string query = @"
                UPDATE dbo.ItemList
                SET price = @price
                WHERE itemId = @itemId;";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.Add("@itemId", SqlDbType.NVarChar, 50).Value = itemCode;
            SqlParameter priceParameter = command.Parameters.Add("@price", SqlDbType.Money);
            priceParameter.Value = price.HasValue ? price.Value : DBNull.Value;
            command.ExecuteNonQuery();
        }

        private void LoadPageData()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            Inventory = ReadInventory(connection);
            ProductCatalog = ReadProductCatalog(connection);
        }

        private void LoadInventory()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            Inventory = ReadInventory(connection);
        }

        private List<ProductOption> LoadProductCatalog()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            return ReadProductCatalog(connection);
        }

        private static List<Items> ReadInventory(SqlConnection connection)
        {
            const string query = @"
                SELECT
                    dr.deliveryReceipt,
                    dr.[date],
                    dr.[location],
                    dpi.itemId,
                    il.[name],
                    CASE
                        WHEN dpi.total IS NOT NULL AND dpi.quantity > 0
                            THEN dpi.total / dpi.quantity
                        ELSE il.price
                    END AS pricePerItem,
                    dpi.quantity,
                    dpi.total
                FROM dbo.DeliveryReport AS dr
                INNER JOIN dbo.DeliveryProcessedItem AS dpi
                    ON dr.deliveryReceipt = dpi.deliveryReceipt
                INNER JOIN dbo.ItemList AS il
                    ON dpi.itemId = il.itemId
                ORDER BY dr.deliveryReceipt DESC, dpi.itemId;";

            var inventory = new List<Items>();
            using var command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                inventory.Add(new Items
                {
                    deliveryReceipt = Convert.ToInt32(reader["deliveryReceipt"]),
                    delivery_date = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd"),
                    delivery_location = reader["location"]?.ToString() ?? string.Empty,
                    itemId = reader["itemId"]?.ToString() ?? string.Empty,
                    itemName = reader["name"]?.ToString() ?? string.Empty,
                    itemPricePerX = reader["pricePerItem"] == DBNull.Value
                        ? null
                        : Convert.ToDecimal(reader["pricePerItem"]),
                    itemQuantity = Convert.ToInt32(reader["quantity"]),
                    itemTotal = reader["total"] == DBNull.Value
                        ? null
                        : Convert.ToDecimal(reader["total"])
                });
            }

            return inventory;
        }

        private static List<ProductOption> ReadProductCatalog(SqlConnection connection)
        {
            const string query = @"
                SELECT itemId, [name], price
                FROM dbo.ItemList AS item
                WHERE EXISTS
                (
                    SELECT 1
                    FROM dbo.ItemList AS uniqueItem
                    WHERE uniqueItem.itemId = item.itemId
                    GROUP BY uniqueItem.itemId
                    HAVING COUNT(*) = 1
                )
                ORDER BY [name], itemId;";

            var products = new List<ProductOption>();
            using var command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                products.Add(new ProductOption
                {
                    Code = reader["itemId"]?.ToString() ?? string.Empty,
                    Name = reader["name"]?.ToString() ?? string.Empty,
                    Price = reader["price"] == DBNull.Value
                        ? null
                        : Convert.ToDecimal(reader["price"])
                });
            }

            return products;
        }

        public class DeliveryReportInput
        {
            [Required(ErrorMessage = "Delivery receipt is required.")]
            [Range(1, int.MaxValue, ErrorMessage = "Delivery receipt must be greater than zero.")]
            public int? DeliveryReceipt { get; set; }

            [Required(ErrorMessage = "Delivery date is required.")]
            [DataType(DataType.Date)]
            public DateTime? Date { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Location is required.")]
            [StringLength(500, ErrorMessage = "Location cannot be longer than 500 characters.")]
            public string Location { get; set; } = string.Empty;

            [MinLength(1, ErrorMessage = "Add at least one item to the delivery.")]
            public List<DeliveryLineInput> Items { get; set; } = new() { new() };
        }

        public class DeliveryLineInput
        {
            [Required]
            [StringLength(50)]
            public string ItemId { get; set; } = string.Empty;

            [Required(ErrorMessage = "Select a product from the suggestions.")]
            public string ProductName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Quantity is required.")]
            [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
            public int? Quantity { get; set; }

            [Range(typeof(decimal), "0.01", "922337203685477.5807", ErrorMessage = "Price must be greater than zero, or left blank when unknown.")]
            public decimal? UnitPrice { get; set; }

            public decimal? OriginalUnitPrice { get; set; }
        }

        public class ProductOption
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal? Price { get; set; }
        }
    }

    public class Items
    {
        public int deliveryReceipt { get; set; }
        public string delivery_date { get; set; } = string.Empty;
        public string delivery_location { get; set; } = string.Empty;
        public string itemId { get; set; } = string.Empty;
        public string itemName { get; set; } = string.Empty;
        public decimal? itemPricePerX { get; set; }
        public int itemQuantity { get; set; }
        public decimal? itemTotal { get; set; }
    }
}
