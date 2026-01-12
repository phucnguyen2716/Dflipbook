using Dflipbook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        [HttpGet("/viewer/{*fileName}")]
        public async Task<IActionResult> ViewFilePdf(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest("Filename is empty");

                var ext = Path.GetExtension(fileName).ToLower();
                var signedUrl = await _supabaseService.GetPdfUrlAsync(fileName);

                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp-pdf");
                Directory.CreateDirectory(tempDir);

                string pdfPath;

                if (ext == ".pdf")
                {
                    pdfPath = Path.Combine(tempDir, Path.GetFileName(fileName));
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(signedUrl);
                    await System.IO.File.WriteAllBytesAsync(pdfPath, bytes);
                }
                else
                {
                    var tempInput = Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid() + ext
                    );

                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(signedUrl);
                    await System.IO.File.WriteAllBytesAsync(tempInput, bytes);

                    pdfPath = await ConvertToPdf(tempInput, tempDir);

                    System.IO.File.Delete(tempInput);
                }

                var pdfUrl = "/temp-pdf/" + Path.GetFileName(pdfPath);

                return Json(new { url = pdfUrl });
            }
            catch (Exception ex)
            {
                // 🔥 BẮT BUỘC để debug
                return StatusCode(500, ex.Message);
            }
        }



        public async Task<string> ConvertToPdf(string inputPath, string outputDir)
        {
            var sofficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";

            if (!System.IO.File.Exists(sofficePath))
                throw new Exception("LibreOffice (soffice.exe) not found");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = sofficePath,
                    Arguments = $"--headless --convert-to pdf \"{inputPath}\" --outdir \"{outputDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return Path.Combine(
                outputDir,
                Path.GetFileNameWithoutExtension(inputPath) + ".pdf"
            );
        }


    }
}
