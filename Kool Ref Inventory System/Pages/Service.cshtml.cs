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
            public List<string> ScopesOfWork { get; set; }
            public Items Inventory { get; set; }
        }
        public List<CombinedViewModel> Records { get; set; }
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=Koolref;Trusted_Connection=True;TrustServerCertificate=True;";
        //string connectionString = "Server=db,1433;Database=Koolref;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";

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

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToPage("/Login");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                //string query = "SELECT * FROM dbo.InandOutSystem";
                //string query = "SELECT * FROM dbo.ServiceReport JOIN dbo.TechnicianListOrders ON dbo.ServiceReport.JobOrder=dbo.TechnicianListOrders.JobOrder JOIN dbo.InandOutSystem ON dbo.ServiceReport.InVoice=dbo.InandOutSystem.inVoice";
                string query = @"
                    DECLARE @cols nvarchar(MAX);
                    DECLARE @sql  nvarchar(MAX);

                    WITH NumberedScopes AS
                    (
                        SELECT
                            serviceReceipt,
                            scopeOfWork,
                            ROW_NUMBER() OVER (
                                PARTITION BY serviceReceipt
                                ORDER BY scopeOfWork
                            ) AS rn
                        FROM [Koolref].[dbo].[ServiceScopeOfWork]
                    )
                    SELECT @cols = STRING_AGG(
                        QUOTENAME('Scope of Work #' + CAST(rn AS varchar(10))),
                        ','
                    )
                    FROM
                    (
                        SELECT DISTINCT rn
                        FROM NumberedScopes
                    ) x;


                    SET @sql = '
                    WITH NumberedScopes AS
                    (
                        SELECT
                            serviceReceipt,
                            scopeOfWork,
                            ''Scope of Work #'' + CAST(
                                ROW_NUMBER() OVER (
                                    PARTITION BY serviceReceipt
                                    ORDER BY scopeOfWork
                                ) AS varchar(10)
                            ) AS scopeColumn
                        FROM [Koolref].[dbo].[ServiceScopeOfWork]
                    ),

                    Techs AS
                    (
                        SELECT
                            serviceReceipt,
                            STRING_AGG(technician, '', '') AS technicians
                        FROM dbo.ServiceTechnician
                        GROUP BY serviceReceipt
                    )

                    SELECT
                        sr.serviceReceipt,
                        sr.timeIn,
                        sr.timeOut,
                        sr.dateStarted,
                        sr.dateEnded,

                        c.name AS clientName,
                        c.address AS clientAddress,\

                        t.technicians,
                        ' + @cols + '

                    FROM dbo.ServiceReport sr

                    LEFT JOIN dbo.Client c
                        ON sr.clientId = c.clientId

                    LEFT JOIN Techs t
                        ON sr.serviceReceipt = t.serviceReceipt

                    LEFT JOIN
                    (
                        SELECT *
                        FROM NumberedScopes
                        PIVOT
                        (
                            MAX(scopeOfWork)
                            FOR scopeColumn IN (' + @cols + ')
                        ) p
                    ) s
                        ON sr.serviceReceipt = s.serviceReceipt

                    ORDER BY sr.serviceReceipt DESC;
                    ';

                    EXEC sp_executesql @sql;";
                    
                    /*
                    @"
                    DECLARE @cols nvarchar(MAX);
                    DECLARE @sql  nvarchar(MAX);

                    WITH NumberedScopes AS
                    (
                        SELECT
                            serviceReceipt,
                            scopeOfWork,
                            ROW_NUMBER() OVER (
                                PARTITION BY serviceReceipt
                                ORDER BY scopeOfWork
                            ) AS rn
                        FROM [Koolref].[dbo].[ServiceScopeOfWork]
                    )
                    SELECT @cols = STRING_AGG(
                        QUOTENAME('Scope of Work #' + CAST(rn AS varchar(10))),
                        ','
                    )
                    FROM
                    (
                        SELECT DISTINCT rn
                        FROM NumberedScopes
                    ) x;


                    SET @sql = '
                    WITH NumberedScopes AS
                    (
                        SELECT
                            serviceReceipt,
                            scopeOfWork,
                            ''Scope of Work #'' + CAST(
                                ROW_NUMBER() OVER (
                                    PARTITION BY serviceReceipt
                                    ORDER BY scopeOfWork
                                ) AS varchar(10)
                            ) AS scopeColumn
                        FROM [Koolref].[dbo].[ServiceScopeOfWork]
                    ),

                    Techs AS
                    (
                        SELECT
                            serviceReceipt,
                            STRING_AGG(technician, '', '') AS technicians
                        FROM dbo.ServiceTechnician
                        GROUP BY serviceReceipt
                    )

                    SELECT
                        sr.serviceReceipt,
                        sr.timeIn,
                        sr.timeOut,
                        sr.dateStarted,
                        sr.dateEnded,
                        sr.clientId,
                        t.technicians,
                        ' + @cols + '

                    FROM dbo.ServiceReport sr

                    LEFT JOIN Techs t
                        ON sr.serviceReceipt = t.serviceReceipt

                    LEFT JOIN
                    (
                        SELECT *
                        FROM NumberedScopes
                        PIVOT
                        (
                            MAX(scopeOfWork)
                            FOR scopeColumn IN (' + @cols + ')
                        ) p
                    ) s
                        ON sr.serviceReceipt = s.serviceReceipt

                    ORDER BY sr.serviceReceipt DESC;
                    ';

                    EXEC sp_executesql @sql;";
                */

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Records = new List<CombinedViewModel>();

                        while (reader.Read())
                        {
                            var sow = new List<string>();

                            for (int columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                            {
                                string columnName = reader.GetName(columnIndex);

                                if (!columnName.StartsWith("Scope of Work #", StringComparison.OrdinalIgnoreCase)
                                    || reader.IsDBNull(columnIndex))
                                {
                                    continue;
                                }

                                string value = reader.GetValue(columnIndex).ToString() ?? "";
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    sow.Add(value);
                                }
                            }

                            Records.Add(new CombinedViewModel
                            {
                                ServiceReport = new Service
                                {
                                    timeIn = reader["timeIn"]?.ToString() ?? "",
                                    timeOut = reader["timeOut"]?.ToString() ?? "",
                                    dateStarted = reader["dateStarted"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["dateStarted"]).ToString("yyyy-MM-dd")
                                        : "",
                                    dateEnded = reader["dateEnded"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["dateEnded"]).ToString("yyyy-MM-dd")
                                        : "",
                                    client_name = reader["clientName"]?.ToString() ?? "",
                                    client_location = reader["clientAddress"]?.ToString() ?? "",
                                    //client_id = Convert.ToInt32(reader["clientId"]),
                                    //address = reader["location"]?.ToString() ?? "",
                                    serviceReceipt = Convert.ToInt32(reader["serviceReceipt"]),
                                    technician = reader["technicians"]?.ToString() ?? ""
                                },

                                ScopesOfWork = sow,
                                /*
                                Inventory = new Items
                                {
                                    itemName = reader["Item"].ToString(),
                                    delivery_date = reader["Date"] == DBNull.Value ? null :
                                        Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                    itemQuantity = Convert.ToInt32(reader["Quantity"]),
                                    itemPricePerX = Convert.ToDecimal(reader["Price"]),
                                    delivery_location = reader["Location"].ToString(),
                                    deliveryReceipt = Convert.ToInt32(reader["deliveryReceipt"])
                                }
                                */
                            });
                        }
                    }
                }
            }
            
            return Page();
        }
    }
    public class Service
    {
        public int serviceReceipt { get; set; }
        public string timeIn { get; set; }
        public string timeOut { get; set; }
        public string dateStarted { get; set; }
        public string dateEnded { get; set; }
        public string client_location { get; set; }
        public string client_name { get; set; }
        public string address { get; set; }
        public string technician { get; set; }
    }
}
