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

        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";
        public List<Display> Index { get; set; } = new();

        public void OnGet()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    Item AS products,
                    SUM(
                        CASE 
                            WHEN deliveryReceipt IS NOT NULL AND inVoice IS NULL THEN Quantity       -- add stock
                            WHEN inVoice IS NOT NULL AND deliveryReceipt IS NULL THEN -Quantity      -- remove stock
                            ELSE 0
                        END
                    ) AS Quantity
                FROM Koolref.dbo.InandOutSystem
                GROUP BY Item
                ORDER BY Quantity ASC;";

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
                                Quantities = Convert.ToInt32(reader["quantity"])
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
