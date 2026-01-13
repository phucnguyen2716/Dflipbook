<!DOCTYPE html>
<html lang="vi">

<body>

<h1>README: Hiển thị tài liệu từ Supabase dưới dạng Flipbook/PDF</h1>

<h2>Mục lục</h2>
<ul>
    <li><a href="#muc-dich">1. Mục đích</a></li>
    <li><a href="#cai-dat-moi-truong">2. Cài đặt môi trường</a></li>
    <li><a href="#cau-hinh-supabase">3. Cấu hình Supabase</a></li>
    <li><a href="#service-supabase">4. Tạo SupabaseService</a></li>
    <li><a href="#controller">5. Controller để lấy danh sách tài liệu</a></li>
    <li><a href="#view">6. View hiển thị danh sách và xem trực tiếp</a></li>
    <li><a href="#chu-y">7. Chú ý khi triển khai</a></li>
    <li><a href="#flipbook">8. Flipbook (tùy chọn nâng cao)</a></li>
</ul>

<hr>

<h2 id="muc-dich">1. Mục đích</h2>
<p>Hướng dẫn cách:</p>
<ul>
    <li>Lấy danh sách file từ <strong>Supabase Storage</strong>.</li>
    <li>Hiển thị tài liệu <strong>PDF</strong> trực tiếp.</li>
    <li>Chuyển ảnh <strong>PNG/JPG</strong> sang PDF và hiển thị.</li>
    <li>Tích hợp hiển thị bằng <strong>iframe</strong> hoặc Flipbook.</li>
</ul>

<hr>

<h2 id="cai-dat-moi-truong">2. Cài đặt môi trường</h2>
<ol>
    <li>Tạo project ASP.NET Core hoặc thêm vào project hiện tại.</li>
    <li>Cài đặt package Supabase:
        <pre>dotnet add package Supabase</pre>
    </li>
    <li>Cài đặt <strong>jsPDF</strong> trong View để xuất PDF từ ảnh:
        <pre>&lt;script src="https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js"&gt;&lt;/script&gt;</pre>
    </li>
</ol>

<hr>

<h2 id="cau-hinh-supabase">3. Cấu hình Supabase</h2>
<p>Tạo <strong>appsettings.json</strong>:</p>
<pre>
{
  "SupabaseSettings": {
    "Url": "https://&lt;your-project&gt;.supabase.co",
    "ApiKey": "&lt;your-api-key&gt;",
    "StorageBucket": "pdf-bucket"
  }
}
</pre>

<p>Tạo class <strong>SupabaseSettings.cs</strong>:</p>
<pre>
namespace Dflipbook.Models
{
    public class SupabaseSettings
    {
        public string Url { get; set; }
        public string ApiKey { get; set; }
        public string StorageBucket { get; set; }
    }
}
</pre>

<hr>

<h2 id="service-supabase">4. Tạo SupabaseService</h2>
<p>File: <strong>SupabaseService.cs</strong></p>
<pre>
using Microsoft.Extensions.Options;
using Supabase;

namespace Dflipbook.Models
{
    public class SupabaseService
    {
        private readonly Client _client;
        private readonly string _bucketName;

        public SupabaseService(Client client, IOptions&lt;SupabaseSettings&gt; options)
        {
            _client = client;
            _bucketName = options.Value.StorageBucket;
        }

        // Lấy signed URL cho PDF
        public async Task&lt;string&gt; GetPdfUrlAsync(string fileName)
        {
            var bucket = _client.Storage.From(_bucketName);
            return await bucket.CreateSignedUrl(fileName, 120);
        }

        // Lấy danh sách file trong bucket
        public async Task&lt;List&lt;string&gt;&gt; ListFilesAsync()
        {
            var bucket = _client.Storage.From(_bucketName);
            var files = await bucket.List();
            return files.Select(f =&gt; f.Name).ToList();
        }
    }
}
</pre>

<hr>

<h2 id="controller">5. Controller để lấy danh sách tài liệu</h2>
<p>File: <strong>DocumentController.cs</strong></p>
<pre>
using Dflipbook.Models;
using Microsoft.AspNetCore.Mvc;

public class DocumentController : Controller
{
    private readonly SupabaseService _supabaseService;

    public DocumentController(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task&lt;IActionResult&gt; Index()
    {
        var files = await _supabaseService.ListFilesAsync();
        return View(files);
    }
}
</pre>

<hr>

<h2 id="view">6. View hiển thị danh sách và xem trực tiếp</h2>
<p>File: <strong>Index.cshtml</strong></p>
<pre>
@model List&lt;string&gt;

&lt;h2&gt;Danh sách tài liệu từ Supabase&lt;/h2&gt;
&lt;hr /&gt;

&lt;ul style="list-style: none; padding-left: 0;"&gt;
    @foreach (var file in Model)
    {
        &lt;li class="pdf-item mb-2"&gt;
            &lt;span class="pdf-icon"&gt;📄&lt;/span&gt;
            &lt;strong&gt;@file&lt;/strong&gt;
            &lt;button class="btn btn-primary btn-view" data-filename="@file"&gt;Xem ngay&lt;/button&gt;
        &lt;/li&gt;
    }
&lt;/ul&gt;

&lt;div id="pdf-container" style="min-height:600px; border:1px solid #ddd; margin-top:20px;"&gt;
    &lt;p style="text-align:center; padding-top:200px; color:#666;"&gt;
        Chọn một tài liệu phía trên để bắt đầu đọc
    &lt;/p&gt;
&lt;/div&gt;

&lt;script src="https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js"&gt;&lt;/script&gt;
&lt;script&gt;
document.querySelectorAll('.btn-view').forEach(btn =&gt; {
    btn.addEventListener('click', async () =&gt; {
        const filename = btn.getAttribute('data-filename');
        const ext = filename.split('.').pop().toLowerCase();
        const fileUrl = `https://&lt;your-project&gt;.supabase.co/storage/v1/object/public/pdf-bucket/${filename}`;

        if(ext === 'pdf') {
            document.getElementById('pdf-container').innerHTML = `
                &lt;iframe src="${fileUrl}" style="width:100%; height:600px;" frameborder="0"&gt;&lt;/iframe&gt;
            `;
        }
        else if(ext === 'png' || ext === 'jpg' || ext === 'jpeg') {
            try {
                const response = await fetch(fileUrl);
                if(!response.ok) throw new Error('Không tải được file ảnh');
                const blob = await response.blob();
                const img = new Image();
                img.src = URL.createObjectURL(blob);
                img.onload = () =&gt; {
                    const pdf = new window.jspdf.jsPDF();
                    const pageWidth = pdf.internal.pageSize.getWidth();
                    const pageHeight = pdf.internal.pageSize.getHeight();
                    const ratio = Math.min(pageWidth / img.width, pageHeight / img.height);
                    const imgWidth = img.width * ratio;
                    const imgHeight = img.height * ratio;
                    const x = (pageWidth - imgWidth) / 2;
                    const y = (pageHeight - imgHeight) / 2;
                    pdf.addImage(img, 'PNG', x, y, imgWidth, imgHeight);
                    const pdfBlob = pdf.output('blob');
                    const url = URL.createObjectURL(pdfBlob);
                    document.getElementById('pdf-container').innerHTML = `
                        &lt;iframe src="${url}" style="width:100%; height:600px;"&gt;&lt;/iframe&gt;
                    `;
                };
                img.onerror = () =&gt; alert('Không tải được ảnh từ Supabase.');
            } catch(e) {
                alert('Lỗi khi tải ảnh: ' + e.message);
            }
        } else {
            alert('File này chưa hỗ trợ xem trực tiếp.');
        }
    });
});
&lt;/script&gt;
</pre>

<hr>

<h2 id="chu-y">7. Chú ý khi triển khai</h2>
<ul>
    <li><strong>PDF:</strong> hiển thị trực tiếp qua iframe.</li>
    <li><strong>PNG/JPG:</strong> chuyển sang PDF bằng jsPDF và hiển thị qua iframe.</li>
    <li><strong>Định dạng khác:</strong> thông báo "chưa hỗ trợ xem trực tiếp".</li>
    <li>Có thể tích hợp Flipbook để trải nghiệm đẹp mắt.</li>
</ul>

<hr>

<h2 id="flipbook">8. Flipbook (tùy chọn nâng cao)</h2>
<ul>
    <li>Tải Turn.js: <a href="https://www.turnjs.com/" target="_blank">https://www.turnjs.com/</a></li>
    <li>Thay vì iframe, tạo div chứa từng trang PDF/ảnh để render dạng lật trang.</li>
    <li>Kết hợp Supabase fetch + jsPDF hoặc ảnh PNG/JPG làm page source.</li>
</ul>
<hr>

<h2 id="convert-controller">9. Controller nâng cao: Convert & Cache PDF (Server-side)</h2>

<p>Phần này mở rộng Controller để:</p>
<ul>
    <li>Không convert lặp lại cùng một file</li>
    <li>Chỉ convert khi PDF chưa tồn tại</li>
    <li>Lưu PDF tạm trong <code>wwwroot/temp-pdf</code></li>
    <li>Dùng cho iframe, DFlip, Flipbook</li>
</ul>

<h3>Luồng xử lý</h3>
<ul>
    <li><strong>PDF:</strong> copy về server 1 lần</li>
    <li><strong>PNG / JPG:</strong> convert sang PDF</li>
    <li><strong>DOCX / PPTX / XLSX:</strong> dùng LibreOffice convert</li>
    <li><strong>Đã tồn tại PDF:</strong> dùng lại, không convert</li>
</ul>

<pre>
/viewer/{filename}
        ↓
Kiểm tra wwwroot/temp-pdf/{filename}.pdf
        ↓
Tồn tại → trả URL
Chưa có → tải từ Supabase → convert → lưu → trả URL
</pre>

---

<h3>Ví dụ Controller: ViewerController.cs</h3>

<pre>
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
</pre>

---

<h2 id="libreoffice">10. Convert Office sang PDF bằng LibreOffice</h2>

<p>Cài LibreOffice trên server (Windows):</p>

<pre>
https://www.libreoffice.org/download/download/
</pre>

<p>Hàm convert:</p>

<pre>
private async Task ConvertOfficeToPdf(string inputPath, string outputDir)
{
    var sofficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";

    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = sofficePath,
            Arguments = $"--headless --convert-to pdf \"{inputPath}\" --outdir \"{outputDir}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();
    await process.WaitForExitAsync();
}
</pre>

<p><strong>Lưu ý:</strong></p>
<ul>
    <li>Server production cần cài LibreOffice</li>
    <li>Docker cần image có sẵn LibreOffice</li>
</ul>

---

<h2 id="dflip">11. Tích hợp DFlip / Flipbook</h2>

<p>Sau khi Controller trả về URL PDF, chỉ cần truyền cho DFlip:</p>

<pre>
function openFlip(filename) {
    fetch(`/viewer/${encodeURIComponent(filename)}`)
        .then(res => res.json())
        .then(data => {
            $('#flipbookContainer').html('');
            $('#flipbookContainer').flipBook({
                source: data.url,
                lightBox: false,
                layout: 3,
                skin: 'light',
                pageMode: 'double'
            });
        });
}
</pre>

<p>Ưu điểm:</p>
<ul>
    <li>PDF load nhanh</li>
    <li>DFlip nhận đúng source PDF</li>
    <li>Không lỗi "Unknown source type"</li>
</ul>

---

<h2 id="performance">12. Hiệu năng & Best Practices</h2>

<ul>
    <li>✔ Cache PDF theo <strong>tên file</strong></li>
    <li>✔ Không convert lặp</li>
    <li>✔ DFlip chỉ đọc file tĩnh</li>
    <li>✔ Có thể cleanup <code>temp-pdf</code> theo cron</li>
</ul>

<p>Gợi ý cleanup:</p>

<pre>
Xóa file temp-pdf cũ hơn 7 ngày
</pre>

---

<h2 id="tong-ket">13. Tổng kết</h2>

<ul>
    <li>Supabase = nơi lưu file gốc</li>
    <li>ASP.NET = xử lý + convert + cache</li>
    <li>Client = iframe / Flipbook / DFlip</li>
</ul>

<p><strong>Mô hình này phù hợp:</strong></p>
<ul>
    <li>Hệ thống tài liệu</li>
    <li>E-learning</li>
    <li>Hồ sơ – biểu mẫu</li>
    <li>Admin dashboard</li>
</ul>
<hr>

<h2 id="libreoffice-requirement">2.1. Yêu cầu bắt buộc: Cài LibreOffice (Server-side)</h2>

<p>
Để hệ thống có thể <strong>convert các file không phải PDF</strong> (DOCX, PPTX, XLSX, ODT, v.v.)
sang PDF ở phía <strong>server</strong>, bạn <strong>bắt buộc phải cài LibreOffice</strong>.
</p>

<h3>Vì sao cần LibreOffice?</h3>
<ul>
    <li>ASP.NET không convert Office sang PDF native</li>
    <li>LibreOffice hỗ trợ convert headless (không giao diện)</li>
    <li>Ổn định, miễn phí, dùng tốt cho server</li>
</ul>

<p>
Controller sẽ gọi trực tiếp:
</p>

<pre>
soffice --headless --convert-to pdf input.docx
</pre>

---

<h3>Hệ điều hành được hỗ trợ</h3>
<ul>
    <li>✔ Windows Server / Windows 10+</li>
    <li>✔ Linux (Ubuntu, Debian)</li>
    <li>✔ Docker Container</li>
</ul>

---

<h3>Cài LibreOffice trên Windows</h3>

<ol>
    <li>Tải tại:
        <br>
        <a href="https://www.libreoffice.org/download/download/" target="_blank">
            https://www.libreoffice.org/download/download/
        </a>
    </li>
    <li>Cài đặt mặc định (Next → Next → Finish)</li>
    <li>Đường dẫn mặc định sau khi cài:
        <pre>C:\Program Files\LibreOffice\program\soffice.exe</pre>
    </li>
</ol>

<h4>Kiểm tra nhanh</h4>
<pre>
"C:\Program Files\LibreOffice\program\soffice.exe" --version
</pre>

---

<h3>Cài LibreOffice trên Linux (Ubuntu)</h3>

<pre>
sudo apt update
sudo apt install libreoffice -y
</pre>

<p>Kiểm tra:</p>

<pre>
soffice --version
</pre>

<p>Đường dẫn thường là:</p>

<pre>
/usr/bin/soffice
</pre>

---

<h3>Cài LibreOffice trong Docker (Khuyến nghị)</h3>

<p>Ví dụ Dockerfile:</p>

<pre>
FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN apt-get update \
    && apt-get install -y libreoffice \
    && apt-get clean

WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "YourApp.dll"]
</pre>

---

<h3>Cấu hình đường dẫn LibreOffice trong code</h3>

<p>Windows:</p>
<pre>
var sofficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";
</pre>

<p>Linux / Docker:</p>
<pre>
var sofficePath = "soffice";
</pre>

<p><strong>Khuyến nghị:</strong> đưa vào <code>appsettings.json</code></p>

<pre>
"LibreOffice": {
  "Path": "C:\\Program Files\\LibreOffice\\program\\soffice.exe"
}
</pre>

---

<h3>Lỗi thường gặp & cách xử lý</h3>

<table border="1" cellpadding="8" cellspacing="0">
    <tr>
        <th>Lỗi</th>
        <th>Nguyên nhân</th>
        <th>Cách xử lý</th>
    </tr>
    <tr>
        <td>soffice.exe not found</td>
        <td>Chưa cài LibreOffice</td>
        <td>Cài LibreOffice</td>
    </tr>
    <tr>
        <td>Failed to start process 'soffice'</td>
        <td>Sai đường dẫn</td>
        <td>Kiểm tra path</td>
    </tr>
    <tr>
        <td>Access denied</td>
        <td>Server không có quyền</td>
        <td>Run service với quyền đủ</td>
    </tr>
</table>

---

<h3>Lưu ý quan trọng khi triển khai Production</h3>

<ul>
    <li>✔ Không convert đồng thời quá nhiều file</li>
    <li>✔ Nên cache PDF sau khi convert</li>
    <li>✔ Cleanup <code>wwwroot/temp-pdf</code> định kỳ</li>
    <li>✔ Không gọi LibreOffice trong request quá dài</li>
</ul>

<p>
<strong>Gợi ý nâng cao:</strong>
</p>
<ul>
    <li>Background job (Hangfire)</li>
    <li>Queue convert</li>
    <li>Hash file để cache</li>
</ul>

</body>
</html>
