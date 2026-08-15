using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Timers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kool_Ref_Inventory_System.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }

        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";
        //string connectionString = "Server=db,1433;Database=Koolref;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";

        /*
        public IActionResult OnPost()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string detailsQuery = "INSERT INTO Koolref.dbo.[User] (userId, name, password) VALUES (26002, @username, @password)";

                using (SqlCommand cmd = new SqlCommand(detailsQuery, conn))
                {

                    cmd.Parameters.AddWithValue("@username", Username);
                    cmd.Parameters.AddWithValue("@password", HashBCrypt(Password));
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToPage("/Login");
        }
        */
       
        public IActionResult OnPost()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT password FROM Koolref.dbo.[User] WHERE name = @username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", Username);

                    object result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        ModelState.AddModelError("", "Invalid username or password");
                        return Page();
                    }

                    string storedHash = result.ToString();

                    cmd.Parameters.AddWithValue("@password", HashBCrypt(Password));
                    cmd.ExecuteNonQuery();

                    bool passwordCorrect = BCrypt.Net.BCrypt.Verify(Password, storedHash);

                    if (!passwordCorrect)
                    {
                        ModelState.AddModelError("", "Invalid username or password");
                        return Page();
                    }

                    HttpContext.Session.SetString("Username", Username);
                }
            }

            return RedirectToPage("/Inventory");
        }

        public string HashBCrypt(string data) { return BCrypt.Net.BCrypt.HashPassword(data); }
    }
}
