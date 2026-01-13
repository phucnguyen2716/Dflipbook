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

                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp-pdf");
                Directory.CreateDirectory(tempDir);

                foreach (var file in Directory.GetFiles(tempDir, "*.pdf"))
                {
                    System.IO.File.Delete(file);
                }


                // 🔑 PDF name = file gốc nhưng đổi sang .pdf
                var pdfFileName = Path.GetFileNameWithoutExtension(fileName) + ".pdf";
                var pdfPath = Path.Combine(tempDir, pdfFileName);

                // ✅ NẾU PDF ĐÃ TỒN TẠI → KHÔNG CONVERT
                if (!System.IO.File.Exists(pdfPath))
                {
                    var signedUrl = await _supabaseService.GetPdfUrlAsync(fileName);
                    using var http = new HttpClient();

                    if (ext == ".pdf")
                    {
                        var bytes = await http.GetByteArrayAsync(signedUrl);
                        await System.IO.File.WriteAllBytesAsync(pdfPath, bytes);
                    }
                    else
                    {
                        var tempInput = Path.Combine(
                            Path.GetTempPath(),
                            Path.GetFileName(fileName) // ❌ không dùng Guid
                        );

                        var bytes = await http.GetByteArrayAsync(signedUrl);
                        await System.IO.File.WriteAllBytesAsync(tempInput, bytes);

                        await ConvertToPdf(tempInput, tempDir);

                        System.IO.File.Delete(tempInput);

                    }
                }

                return Json(new
                {
                    url = "/temp-pdf/" + pdfFileName
                });
            }
            catch (Exception ex)
            {
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
