using Microsoft.Extensions.Options;
using Supabase;

namespace Dflipbook.Models
{
    public class SupabaseService
    {
        private readonly Client _client;
        private readonly string _bucketName;

        public SupabaseService(Client client, IOptions<SupabaseSettings> options)
        {
            _client = client;
            _bucketName = options.Value.StorageBucket;
        }

        // Lấy signed URL cho PDF
        public async Task<string> GetPdfUrlAsync(string fileName)
        {
            var bucket = _client.Storage.From(_bucketName);
            return await bucket.CreateSignedUrl(fileName, 120); // 5 phút
        }

        // Lấy danh sách file trong bucket
        public async Task<List<string>> ListFilesAsync()
        {
            var bucket = _client.Storage.From(_bucketName);
            var files = await bucket.List();
            return files.Select(f => f.Name).ToList();
        }
    }
}
