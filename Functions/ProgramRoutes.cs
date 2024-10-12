using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using sl_img_prcr.Services;
using System.IO;
using System.Threading.Tasks;

namespace sl_img_prcr.Functions
{

    public class ProgramRoutes
    {
        private readonly ILogger<ProgramRoutes> _logger;
        private readonly BlobStorageService _blobStorageService;
        private readonly ImageProcessorService _imageProcessorService;
        private readonly RandomTitleService _randomTitleService;

        public ProgramRoutes(ILogger<ProgramRoutes> logger, BlobStorageService blobStorageService, ImageProcessorService imageProcessorService, RandomTitleService randomTitleService)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _imageProcessorService = imageProcessorService;
            _randomTitleService = randomTitleService;
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
            var position = req.Form["position"];

            // Ensure position value included and valid
            if (StringValues.IsNullOrEmpty(position) || (position != "start" && position != "middle" && position != "end"))
            {
                return new BadRequestObjectResult("Image Position type unspecified or incorrect.");
            }

            // Validate image file: size, aspect ratio, file type
            var (isValid, error) = _imageProcessorService.IsValidImage(file);
            if (!isValid)
            {
                return new BadRequestObjectResult(error);
            }

            //Resize image file
            Stream fileStream = _imageProcessorService.ResizeImage250(file.OpenReadStream());

            // Upload file to Blob Storage
            string blobUrl = await _blobStorageService.UploadImageAsync(fileStream, file.FileName, position!);
            
            // Attempt to create a Gif if possible
            await CreateGifIfPossible();

            return new OkObjectResult(new { BlobUrl = blobUrl });
        }

        private async Task CreateGifIfPossible()
        {
            var startImage = await _blobStorageService.GetBlobByPositionAsync("start");
            var middleImage = await _blobStorageService.GetBlobByPositionAsync("middle");
            var endImage = await _blobStorageService.GetBlobByPositionAsync("end");

            if (startImage != null && middleImage != null && endImage != null)
            {
                using var startStream = await startImage.OpenReadAsync();
                using var middleStream = await middleImage.OpenReadAsync();
                using var endStream = await endImage.OpenReadAsync();
                
                var gifStream = _imageProcessorService.CreateGifFromImages(startStream, middleStream, endStream);
                
                // Upload the GIF to processed images container
                string randomTitle = _randomTitleService.GetRandomTitle();
                await _blobStorageService.UploadGifAsync(gifStream, randomTitle + ".gif");
                
                // Optionally delete the original images after processing
                await _blobStorageService.DeleteBlobAsync(startImage.Name);
                await _blobStorageService.DeleteBlobAsync(middleImage.Name);
                await _blobStorageService.DeleteBlobAsync(endImage.Name);
            }
        }

        [Function("GetLastGifs")]
        public async Task<IActionResult> GetImages([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            var containerClient = _blobStorageService.GetBlobContainerClient("gifs");
            var blobs = containerClient.GetBlobsAsync();
            var blobUrls = new List<string>();
            await foreach (var blob in blobs)
            {
                var blobClient = containerClient.GetBlobClient(blob.Name);
                blobUrls.Add(blobClient.Uri.ToString());
            }
            return new OkObjectResult(blobUrls.Take(50));
        }
    }

}