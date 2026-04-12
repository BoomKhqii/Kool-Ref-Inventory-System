using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;


namespace Kool_Ref_Inventory_System.Pages
{
    public class ServiceModel : PageModel
    {
        [BindProperty] public List<string> Technician { get; set; }
        [BindProperty] public string WorkScope { get; set; }
        [BindProperty] public string TimeIn { get; set; }
        [BindProperty] public string TimeOut { get; set; }
        [BindProperty] public String DateStarted { get; set; }
        [BindProperty] public String DateEnded { get; set; }
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
        [BindProperty] public string Date { get; set; }
        public class CombinedViewModel
        {
            public Service ServiceReport { get; set; }
            public List<string> Technicians { get; set; }
            public Items Inventory { get; set; }
        }
        public List<CombinedViewModel> Records { get; set; }
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";

        public IActionResult OnPost()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string serviceQuery = "INSERT INTO Koolref.dbo.ServiceReport (WorkScope, TimeIn, TimeOut, DateStarted, DateEnded, Customer, Adddress, DeliveryReceipt, InVoice) VALUES (@workscope, @timeIn, @timeOut, @dateStarted, @dateEnded, @customer, @address, @deliveryReceipt, @inVoice)";
                string inventoryQuery = "INSERT INTO Koolref.dbo.InandOutSystem (Item, Description, Supplier, Date, Quantity, Price, Location, DeliveryReceipt, InVoice) VALUES (@item, @description, @supplier, @date, @quantity, @price, @location, @deliveryReceipt, @inVoice)";
                string technicianListQuery = "INSERT INTO Koolref.dbo.TechnicianListOrders (Technicians0, Technicians1, Technicians2, Technicians3, Technicians4, Technicians5, Technicians6, Technicians7, Technicians8, Technicians9) VALUES (@technicians0, @technicians1, @technicians2, @technicians3, @technicians4, @technicians5, @technicians6, @technicians7, @technicians8, @technicians9)";

                // Technician Query
                using (SqlCommand cmd = new SqlCommand(technicianListQuery, conn))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i < Technician.Count)
                            cmd.Parameters.AddWithValue($"@technicians{i}", (object)Technician[i] ?? DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue($"@technicians{i}", DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                }
                /*
                using (SqlCommand cmd = new SqlCommand(technicianListQuery, conn))
                {
                    // Loop 10 times to fill @technicians0 through @technicians9
                    for (int i = 0; i < 10; i++)
                    {
                        // Check if the list has an item at this index
                        object value = (InputTechnician != null && InputTechnician.Count > i)
                            ? (object)InputTechnician[i]
                            : DBNull.Value;

                        cmd.Parameters.AddWithValue($"@technicians{i}", value);
                    }

                    cmd.ExecuteNonQuery();
                }
                */

                // Service Report Query
                using (SqlCommand cmd = new SqlCommand(serviceQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@workScope", WorkScope);
                    cmd.Parameters.AddWithValue("@timeIn", string.IsNullOrEmpty(TimeIn) ? (object)DBNull.Value : TimeIn);
                    cmd.Parameters.AddWithValue("@timeOut", string.IsNullOrEmpty(TimeOut) ? (object)DBNull.Value : TimeOut);
                    cmd.Parameters.AddWithValue("@dateStarted", string.IsNullOrEmpty(DateStarted) ? (object)DBNull.Value : DateStarted);
                    cmd.Parameters.AddWithValue("@dateEnded", string.IsNullOrEmpty(DateEnded) ? (object)DBNull.Value : DateEnded);
                    cmd.Parameters.AddWithValue("@customer", Customer);
                    cmd.Parameters.AddWithValue("@address", Address);
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
           
                // Inventory Query
                using (SqlCommand cmd = new SqlCommand(inventoryQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@item", Item);
                    cmd.Parameters.AddWithValue("@description", Description);
                    cmd.Parameters.AddWithValue("@supplier", Supplier);
                    cmd.Parameters.AddWithValue("@date", string.IsNullOrEmpty(Date) ? (object)DBNull.Value : Date);
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
            return RedirectToPage("/Service");
        }

        public void OnGet()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                //string query = "SELECT * FROM dbo.InandOutSystem";
                //string query = "SELECT * FROM dbo.ServiceReport JOIN dbo.TechnicianListOrders ON dbo.ServiceReport.JobOrder=dbo.TechnicianListOrders.JobOrder JOIN dbo.InandOutSystem ON dbo.ServiceReport.InVoice=dbo.InandOutSystem.inVoice";
                string query = @"
                    SELECT * FROM dbo.ServiceReport 
                    JOIN dbo.TechnicianListOrders 
                        ON dbo.ServiceReport.JobOrder = dbo.TechnicianListOrders.JobOrder 
                    JOIN dbo.InandOutSystem 
                        ON (dbo.ServiceReport.InVoice = dbo.InandOutSystem.inVoice 
                            OR dbo.ServiceReport.DeliveryReceipt = dbo.InandOutSystem.deliveryReceipt)
                    ORDER BY Koolref.dbo.ServiceReport.JobOrder DESC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Records = new List<CombinedViewModel>();

                        while (reader.Read())
                        {
                            var technicians = new List<string>();

                            for (int i = 0; i <= 9; i++)
                            {
                                string columnName = "Technicians" + i;
                                if (reader[columnName] != DBNull.Value)
                                {
                                    string value = reader[columnName].ToString();
                                    if (!string.IsNullOrWhiteSpace(value))
                                        technicians.Add(value);
                                }
                            }

                            Records.Add(new CombinedViewModel
                            {
                                ServiceReport = new Service
                                {
                                    WorkScope = reader["WorkScope"]?.ToString() ?? "",
                                    TimeIn = reader["TimeIn"]?.ToString() ?? "",
                                    TimeOut = reader["TimeOut"]?.ToString() ?? "",
                                    DateStarted = reader["DateStarted"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["DateStarted"]).ToString("yyyy-MM-dd")
                                        : "",
                                    DateEnded = reader["DateEnded"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["DateEnded"]).ToString("yyyy-MM-dd")
                                        : "",
                                    Customer = reader["Customer"]?.ToString() ?? "",
                                    Address = reader["Adddress"]?.ToString() ?? "",
                                    IUD = (reader["DeliveryReceipt"] as int?) ?? (reader["InVoice"] as int?) ?? 0
                                },

                                Technicians = technicians,

                                Inventory = new Items
                                {
                                    Item = reader["Item"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Supplier = reader["Supplier"].ToString(),
                                    DateInv = reader["Date"] == DBNull.Value ? null :
                                        Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    Location = reader["Location"].ToString(),
                                    IUD = (reader["DeliveryReceipt"] as int?) ?? (reader["InVoice"] as int?) ?? 0
                                }
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
