using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using sl_img_prcr.Services;
using System.IO;
using System.Threading.Tasks;

namespace sl_img_prcr.Functions
{

    public class ImgTransfer
    {
        private readonly ILogger<ImgTransfer> _logger;
        private readonly BlobStorageService _blobStorageService;

        public ImgTransfer(ILogger<ImgTransfer> logger, BlobStorageService blobStorageService)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
        }

        [Function("PostImage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing image upload...");

            // Check if file exists
            if (req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("No image file found in the request.");
            }

            var file = req.Form.Files[0];

            // Validate file size (e.g., max 2 MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                return new BadRequestObjectResult("File size exceeds 5 MB.");
            }

            // Validate file type (accept only JPEG and PNG)
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
            {
                return new BadRequestObjectResult("Only .jpg and .png formats are allowed.");
            }

            // Upload file to Blob Storage
            string containerName = "images";
            string blobUrl = await _blobStorageService.UploadImageAsync(file.OpenReadStream(), file.FileName, containerName);

            return new OkObjectResult(new { BlobUrl = blobUrl });
        }

        [Function("GetLastImages")]
        public async Task<IActionResult> GetImages([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            var containerClient = _blobStorageService.GetBlobContainerClient("images");
            var blobs = containerClient.GetBlobsAsync();

            var blobUrls = new List<string>();

            await foreach (var blob in blobs)
            {
                var blobClient = containerClient.GetBlobClient(blob.Name);
                blobUrls.Add(blobClient.Uri.ToString());
            }

            return new OkObjectResult(blobUrls.Take(10));  // Limit to last 10 images
        }

    }

}