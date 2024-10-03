using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace sl_img_prcr.Services
{
    public class BlobStorageService
    {
        private readonly ILogger<BlobStorageService> _logger;

        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(ILogger<BlobStorageService> logger, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string containerName)
        {

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            
            // Define unique filename and create new blobClient Object
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            try
            {
                await blobClient.UploadAsync(imageStream, false);
            }catch(RequestFailedException ex){
                _logger.LogInformation(ex, "Blob upload failed with an error.");
            }

            return blobClient.Uri.ToString();
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {      
            return _blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}