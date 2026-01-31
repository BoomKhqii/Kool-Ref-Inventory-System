using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using System.Data;
/*
string connectionString = "server=localhost;database=Inventory;integrated Security=SSPI;";
//string connectionString = builder.Configuration.GetConnectionString("Inventory");


using (SqlConnection _con = new SqlConnection(connectionString))
{
    string queryStatement = "SELECT TOP 5 * FROM dbo.Customers ORDER BY CustomerID";

    using (SqlCommand _cmd = new SqlCommand(queryStatement, _con))
    {
        DataTable customerTable = new DataTable("Top5Customers");

        SqlDataAdapter _dap = new SqlDataAdapter(_cmd);

        _con.Open();
        _dap.Fill(customerTable);
        _con.Close();

    }
}
*/

namespace Kool_Ref_Inventory_System.Model
{
    public class userController
    {
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
    }    
}
/*
    public class UserStatus
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsIn { get; set; } // Remember: BIT in SQL = bool in C#
    }
*/
