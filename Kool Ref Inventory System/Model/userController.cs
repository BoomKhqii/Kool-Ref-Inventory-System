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
