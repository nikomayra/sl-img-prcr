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

        private const string UNPROCESSED_CONTAINER = "images";
        private const string PROCESSED_CONTAINER = "gifs";


        public BlobStorageService(ILogger<BlobStorageService> logger, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string position)
        {

            if (string.IsNullOrWhiteSpace(position))
            {
                throw new ArgumentException("Position cannot be null or empty", nameof(position));
            }
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(UNPROCESSED_CONTAINER);
            await containerClient.CreateIfNotExistsAsync();
            
            // Define unique filename and create new blobClient Object
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            try
            {
                // Upload the image
                await blobClient.UploadAsync(imageStream, false);
                
                // Add position metadata
                var metadata = new Dictionary<string, string>
                {
                    { "position", position }
                };
                await blobClient.SetMetadataAsync(metadata);
            }
            catch(RequestFailedException ex)
            {
                _logger.LogInformation(ex, "Blob image upload failed with an error.");
            }

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadGifAsync(Stream gifStream, string fileName)
        {
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(PROCESSED_CONTAINER);
            await containerClient.CreateIfNotExistsAsync();
            
            // Define unique filename and create new blobClient Object
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            try
            {
                // Upload the gif
                await blobClient.UploadAsync(gifStream, false);
            }
            catch(RequestFailedException ex)
            {
                _logger.LogInformation(ex, "Blob gif upload failed with an error.");
            }

            return blobClient.Uri.ToString();
            
        }

        public async Task<BlobClient?> GetBlobByPositionAsync(string position)
        {
            var containerClient = GetBlobContainerClient(UNPROCESSED_CONTAINER);
            var matchingBlobs = new List<BlobClient>();
            
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();

                if (properties.Value.Metadata.TryGetValue("position", out string? value) && value == position)
                {
                    matchingBlobs.Add(blobClient);
                }
            }

            if (matchingBlobs.Count > 0)
            {
                var random = new Random();
                int randomIndex = random.Next(matchingBlobs.Count);
                return matchingBlobs[randomIndex];
            }

            return null;
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(UNPROCESSED_CONTAINER);
            var blobClient = containerClient.GetBlobClient(blobName);

            try
            {
                await blobClient.DeleteIfExistsAsync(); // Deletes the blob if it exists
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Failed to delete blob {blobName}.");
            }
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {      
            return _blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}