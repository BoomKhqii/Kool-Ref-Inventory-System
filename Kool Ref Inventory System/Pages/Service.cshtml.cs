using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kool_Ref_Inventory_System.Pages
{
    public class ServiceModel : PageModel
    {
        [BindProperty] public string WorkScope { get; set; }
        [BindProperty] public string TimeIn { get; set; }
        [BindProperty] public string TimeOut { get; set; }
        [BindProperty] public string DateStarted { get; set; }
        [BindProperty] public string DateEnded { get; set; }
        [BindProperty] public int DeliveryReceipt { get; set; }
        [BindProperty] public int InVoice { get; set; }
        [BindProperty] public string Customer { get; set; }
        [BindProperty] public string Address { get; set; }

        public void OnGet()
        {
        }
    }
}
