using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Kool_Ref_Inventory_System.Pages
{
    public class ServiceModel : PageModel
    {
        [BindProperty] public List<string> InputTechnician { get; set; }
        [BindProperty] public string WorkScope { get; set; }
        [BindProperty] public string TimeIn { get; set; }
        [BindProperty] public string TimeOut { get; set; }
        [BindProperty] public string DateStarted { get; set; }
        [BindProperty] public string DateEnded { get; set; }
        [BindProperty] public string Customer { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty] public int DeliveryReceipt { get; set; }
        [BindProperty] public int InVoice { get; set; }
        [BindProperty] public string Item { get; set; }
        [BindProperty] public string Description { get; set; }
        [BindProperty] public string Supplier { get; set; }
        [BindProperty] public int Quantity { get; set; }
        [BindProperty] public decimal Price { get; set; }
        [BindProperty] public string Location { get; set; }

        public List<Items> Inventory { get; set; }
        public List<Service> ServiceReport { get; set; }
        public List<string> Technicians { get; set; } = new List<string>();
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
        
        public IActionResult OnPost()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Existing Parameters
                    cmd.Parameters.AddWithValue("@item", Item ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@description", Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@supplier", Supplier ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@quantity", Quantity);
                    cmd.Parameters.AddWithValue("@price", Price);
                    cmd.Parameters.AddWithValue("@location", Location ?? (object)DBNull.Value);

                    // --- NEW PARAMETERS START HERE ---

                    // Join the list of technicians into one string (Comma Separated)
                    string techs = (InputTechnician != null && InputTechnician.Any())
                                   ? string.Join(", ", InputTechnician)
                                   : null;
                    cmd.Parameters.AddWithValue("@technicians", techs ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@workScope", WorkScope ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@customer", Customer ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@address", Address ?? (object)DBNull.Value);

                    // Handling Dates/Times (Checking for empty strings before sending to SQL)
                    cmd.Parameters.AddWithValue("@timeIn", string.IsNullOrEmpty(TimeIn) ? (object)DBNull.Value : TimeIn);
                    cmd.Parameters.AddWithValue("@timeOut", string.IsNullOrEmpty(TimeOut) ? (object)DBNull.Value : TimeOut);
                    cmd.Parameters.AddWithValue("@dateStarted", string.IsNullOrEmpty(DateStarted) ? (object)DBNull.Value : DateStarted);
                    cmd.Parameters.AddWithValue("@dateEnded", string.IsNullOrEmpty(DateEnded) ? (object)DBNull.Value : DateEnded);

                    // --- NEW PARAMETERS END HERE ---

                    // Your existing DeliveryReceipt / InVoice logic
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
                /*
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
                */
            }
            return RedirectToPage("/Service");
        }

        public void OnGet()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                //string query = "SELECT * FROM dbo.InandOutSystem";
                string query = "SELECT * FROM dbo.ServiceReport JOIN dbo.TechnicianListOrders ON dbo.ServiceReport.JobOrder=dbo.TechnicianListOrders.JobOrder JOIN dbo.InandOutSystem ON dbo.ServiceReport.InVoice=dbo.InandOutSystem.inVoice";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ServiceReport = new List<Service>();
                        Technicians = new List<string>();
                        Inventory = new List<Items>();
                        while (reader.Read())
                        {
                            ServiceReport.Add(new Service
                            {
                                WorkScope = reader["WorkScope"].ToString(),
                                TimeIn = reader["TimeIn"].ToString(),
                                TimeOut = reader["TimeOut"].ToString(),
                                DateStarted = Convert.ToDateTime(reader["DateStarted"]).ToString("yyyy-MM-dd"),
                                DateEnded = Convert.ToDateTime(reader["DateEnded"]).ToString("yyyy-MM-dd"),
                                Customer = reader["Customer"].ToString(),
                                Address = reader["Address"].ToString(),
                                IUD = reader["DeliveryReceipt"] != DBNull.Value
                                      ? Convert.ToInt32(reader["DeliveryReceipt"])
                                      : (reader["InVoice"] != DBNull.Value ? Convert.ToInt32(reader["InVoice"]) : 0),
                            });

                            // Loop from 0 to 9 to handle Technician0 through Technician9
                            for (int i = 0; i <= 9; i++)
                            {
                                string columnName = "Technicians" + i;
                                if (reader[columnName] != DBNull.Value)
                                    Technicians.Add(reader[columnName].ToString());
                            }

                            Inventory.Add(new Items
                            {
                                Item = reader["Item"].ToString(),
                                Description = reader["Description"].ToString(),
                                Supplier = reader["Supplier"].ToString(),
                                Date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
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
    public class Service
    {
        public string WorkScope { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string DateStarted { get; set; }
        public string DateEnded { get; set; }
        public string Customer { get; set; }
        public string Address { get; set; }
        public int IUD { get; set; }
    }
}
