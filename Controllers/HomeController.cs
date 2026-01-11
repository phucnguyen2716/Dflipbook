using Microsoft.AspNetCore.Mvc;
using Dflipbook.Models;

namespace Dflipbook.Controllers
{
    public class HomeController : Controller
    {
        private readonly SupabaseService _supabaseService;

        public HomeController(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<IActionResult> Index()
        {
            var fileNames = await _supabaseService.ListFilesAsync();
            return View(fileNames); 
        }

        [HttpGet]
        public async Task<IActionResult> GetPdf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest();

            var pdfUrl = await _supabaseService.GetPdfUrlAsync(fileName);
            return Json(new { url = pdfUrl });
        }
    }
}
