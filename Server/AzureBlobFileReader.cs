using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace MockApi.Server
{
    public class AzureBlobFileReader : IFileReader
    {        
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        public AzureBlobFileReader(BlobServiceClient blobServiceClient, IOptions<FileReaderOptions> fileReaderOptions)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = fileReaderOptions.Value.Root;
        }

        public async Task<string> ReadContentsAsync(string file)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(file);
                var content = await blobClient.DownloadContentAsync();
                return Encoding.UTF8.GetString(content.Value.Content);
            }
            catch(System.Exception ex)
            {
                throw new System.InvalidOperationException($"Could not read file {file} from {_containerName}", ex);
            }
        }
    }
}