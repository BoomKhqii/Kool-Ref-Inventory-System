using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        [BindProperty] public string Products { get; set; }
        [BindProperty] public int Quantities { get; set; }

        //string connectionString = "Server=db,1433;Database=Koolref;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";
        public List<Display> Index { get; set; } = new();

        public IActionResult OnGet()
        {
            return RedirectToPage("/ItemSupply");
        }

        // Kept temporarily so the previous quantity query can be restored if stock tracking is added later.
        private void LoadStockOverview()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT
                    il.name AS products,
                    ISNULL(SUM(dpi.quantity), 0) AS Quantity
                FROM Koolref.dbo.ItemList AS il
                    LEFT JOIN Koolref.dbo.DeliveryProcessedItem AS dpi
                        ON CONVERT(NVARCHAR(50), dpi.itemId)
                        = CONVERT(NVARCHAR(50), il.itemId)
                GROUP BY
                    il.itemId,
                    il.name
                ORDER BY
                    Quantity ASC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Index = new List<Display>();
                        while (reader.Read())
                        {
                            Index.Add(new Display
                            {
                                Products = reader["products"].ToString(),
                                Quantities = Convert.ToInt32(reader["Quantity"])
                            });
                        }
                    }
                }
            }
        }
    }
    public class Display
    {
        public string Products { get; set; }
        public int Quantities { get; set; }
    }
}
