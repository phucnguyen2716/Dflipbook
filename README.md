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

</body>
</html>
