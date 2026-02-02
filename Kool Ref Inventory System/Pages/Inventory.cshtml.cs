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
        [BindProperty] public bool InOut { get; set; }
        [BindProperty] public string Location { get; set; }

        public string Message { get; set; }
        public void OnPost()
        {
            Debug.WriteLine("onpost called");

            string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO dbo.InandOutSystem (Item, Description, Supplier, Quantity, Price, InOut, Location) VALUES (@item, @description, @supplier, @quantity, @price, @inOut, @location)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item", Item);
                    cmd.Parameters.AddWithValue("@description", Description);
                    cmd.Parameters.AddWithValue("@supplier", Supplier);
                    cmd.Parameters.AddWithValue("@quantity", Quantity);
                    cmd.Parameters.AddWithValue("@price", Price);
                    cmd.Parameters.AddWithValue("@inOut", InOut);
                    cmd.Parameters.AddWithValue("@location", Location);
                    cmd.ExecuteNonQuery();
                }
            }

            Message = "Data submitted successfully!";
        }


        /*
        public void OnPost()
        {

            string connectionString = "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO dbo.InandOutSystem (Item, Description, Supplier, Quantity, Price, InOut, Location) VALUES (@item, @description, @supplier, @quantity, @price, @inOut, @location)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item", Item);
                    cmd.Parameters.AddWithValue("@description", Description);
                    cmd.Parameters.AddWithValue("@supplier", Supplier);
                    cmd.Parameters.AddWithValue("@quantity", Quantity);
                    cmd.Parameters.AddWithValue("@price", Price);
                    cmd.Parameters.AddWithValue("@inOut", InOut);
                    cmd.Parameters.AddWithValue("@location", Location);
                    cmd.ExecuteNonQuery();
                }

                /*
                public void OnGet()
                {
                    using var conn = new MySqlConnection(
                        "server=localhost;user=root;password=1234;database=testdb");
                    // Server=localhost\SQLEXPRESS01;Database=master;Trusted_Connection=True;

                    conn.Open();
                }
                */
        // Source - https://stackoverflow.com/a/1345531
        // Posted by marc_s, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-01-04, License - CC BY-SA 4.0

        //string connectionString = "server=localhost;database=Inventory;integrated Security=SSPI;";
        //string connectionString = builder.Configuration.GetConnectionString("Inventory");
        /*
        using(SqlConnection _con = new SqlConnection(connectionString))
        {
           string queryStatement = "SELECT TOP 5 * FROM dbo.Customers ORDER BY CustomerID";

           using(SqlCommand _cmd = new SqlCommand(queryStatement, _con))
           {
              DataTable customerTable = new DataTable("Top5Customers");

    SqlDataAdapter _dap = new SqlDataAdapter(_cmd);

    _con.Open();
              _dap.Fill(customerTable);
              _con.Close();

           }
        }
    */
        /*
        [HttpPost]
        public IActionResult UpdateStatus(int id, bool status)
        {
            string connString = "DESKTOP-8KJ2D7L";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                // This is the SQL command sent to SSMS
                string sql = "UPDATE Users SET IsIn = @status WHERE UserID = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@status", status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok("Status Updated in SSMS!");
        }*/
        /*
                    }
                }
            */
    }
}
