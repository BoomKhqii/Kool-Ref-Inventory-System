using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kool_Ref_Inventory_System.Pages
{
    public class InventoryModel : PageModel
    {
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";
        //string connectionString = "Server=db,1433;Database=Koolref;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
        public List<Items> Inventory { get; set; }


        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToPage("/Login");
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT
                    dr.deliveryReceipt,
                    dr.[date],
                    dr.[location],

                    dpi.itemId,
                    il.[name],
                    il.price AS pricePerItem,
                    dpi.quantity,

                    il.price * dpi.quantity AS total

                FROM dbo.DeliveryReport AS dr

                LEFT JOIN dbo.DeliveryProcessedItem AS dpi
                    ON CONVERT(NVARCHAR(50), dr.deliveryReceipt)
                     = CONVERT(NVARCHAR(50), dpi.deliveryReceipt)

                LEFT JOIN dbo.ItemList AS il
                    ON CONVERT(NVARCHAR(50), dpi.itemId)
                     = CONVERT(NVARCHAR(50), il.itemId)

                ORDER BY
                    dr.deliveryReceipt DESC,
                    dpi.itemId;";
                /*
                @"
                    SELECT
                        dr.deliveryReceipt,
                        dr.[date],
                        dr.[location],

                        dpi.itemId,
                        il.[name],
                        il.price AS pricePerItem,
                        dpi.quantity,

                        il.price * dpi.quantity AS total

                    FROM dbo.DeliveryReport AS dr

                    LEFT JOIN dbo.DeliveryProcessedItem AS dpi
                        ON dr.deliveryReceipt = dpi.deliveryReceipt

                    LEFT JOIN dbo.ItemList AS il
                        ON dpi.itemId = il.itemId

                    ORDER BY
                        dr.deliveryReceipt DESC,
                        dpi.itemId;";
                */

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Inventory = new List<Items>();
                        while (reader.Read())
                        {
                            Inventory.Add(new Items
                            {
                                itemName = reader["name"].ToString(),
                                itemTotal = Convert.ToDecimal(reader["total"]),
                                itemId = reader["itemId"]?.ToString() ?? "",
                                delivery_date = reader["date"] == DBNull.Value ? null : Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                itemQuantity = Convert.ToInt32(reader["quantity"]),
                                itemPricePerX = Convert.ToDecimal(reader["pricePerItem"]),
                                delivery_location = reader["location"].ToString(),
                                deliveryReceipt = Convert.ToInt32(reader["deliveryReceipt"])
                            });
                        }
                    }
                }
            }

            return Page();
        }
    }

    public class Items
    {
        public int deliveryReceipt { get; set; }
        public string delivery_date { get; set; }
        public string delivery_location { get; set; }
        public string itemId { get; set; }
        public string itemName { get; set; }
        public decimal itemPricePerX { get; set; }
        public int itemQuantity { get; set; }
        public decimal itemTotal { get; set; }
    }
}
