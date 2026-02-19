using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kool_Ref_Inventory_System.Pages
{
    public class InventoryModel : PageModel
    {
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        public List<Items> Inventory { get; set; }


        public void OnGet()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM dbo.InandOutSystem";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Inventory = new List<Items>();
                        while (reader.Read())
                        {
                            Inventory.Add(new Items
                            {
                                Item = reader["Item"].ToString(),
                                Description = reader["Description"].ToString(),
                                Supplier = reader["Supplier"].ToString(),
                                Date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                Quantity = Convert.ToInt32(reader["Quantity"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Location = reader["Location"].ToString(),
                                IUD = reader["DeliveryReceipt"] != DBNull.Value
                                  ? Convert.ToInt32(reader["DeliveryReceipt"])
                                  : (reader["InVoice"] != DBNull.Value ? Convert.ToInt32(reader["InVoice"]) : 0)
                            });
                        }
                    }
                }
            }
        }
    }

    public class Items
    {
        public string Item { get; set; }
        public string Description { get; set; }
        public string Supplier { get; set; }
        public string Date { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public int IUD { get; set; }
        public int DeliveryReceipt { get; set; }
        public int InVoice { get; set; }
    }
}
