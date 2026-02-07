using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kool_Ref_Inventory_System.Pages
{
    public class InventoryModel : PageModel
    {

        [BindProperty] public string Item { get; set; }
        [BindProperty] public string Description { get; set; }
        [BindProperty] public string Supplier { get; set; }
        [BindProperty] public int Quantity { get; set; }
        [BindProperty] public decimal Price { get; set; }
        [BindProperty] public string Location { get; set; }
        [BindProperty] public int DeliveryReceipt { get; set; }
        [BindProperty] public int InVoice { get; set; }
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        public List<Items> Inventory { get; set; }

        public void OnPost()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO dbo.InandOutSystem (Item, Description, Supplier, Quantity, Price, Location, DeliveryReceipt, InVoice) VALUES (@item, @description, @supplier, @quantity, @price, @location, @deliveryReceipt, @inVoice)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item", Item);
                    cmd.Parameters.AddWithValue("@description", Description);
                    cmd.Parameters.AddWithValue("@supplier", Supplier);
                    cmd.Parameters.AddWithValue("@quantity", Quantity);
                    cmd.Parameters.AddWithValue("@price", Price);
                    cmd.Parameters.AddWithValue("@location", Location);
                    if (DeliveryReceipt == 0)
                    {

                        cmd.Parameters.AddWithValue("@deliveryReceipt", DBNull.Value);
                        cmd.Parameters.AddWithValue("@inVoice", InVoice);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@inVoice", DBNull.Value);
                        cmd.Parameters.AddWithValue("@deliveryReceipt", DeliveryReceipt);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

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
                                Quantity = Convert.ToInt32(reader["Quantity"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Location = reader["Location"].ToString(),
                                DeliveryReceipt = reader["DeliveryReceipt"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DeliveryReceipt"]),
                                InVoice = reader["InVoice"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InVoice"])
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
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public int DeliveryReceipt { get; set; }
        public int InVoice { get; set; }
    }
}
