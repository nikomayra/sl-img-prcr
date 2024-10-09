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

        private const int imageWidth = 250;
        private const int imageHeight = 250;


        public ProgramRoutes(ILogger<ProgramRoutes> logger, BlobStorageService blobStorageService, ImageProcessorService imageProcessorService)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _imageProcessorService = imageProcessorService;
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

            // Validate file size (e.g., max 1 MB)
            if (file.Length > 1 * 1024 * 1024)
            {
                return new BadRequestObjectResult("File size exceeds 1 mb.");
            }

            // Validate image file (checks MIME type and file extensions)
            if (!_imageProcessorService.IsValidImage(file))
            {
                return new BadRequestObjectResult("Not allowed image file type.");
            }

             //Validate image dimensions
            using (var imageStream = file.OpenReadStream())
            {
                using var image = Image.Load<Rgba32>(imageStream);
                if (image.Width != imageWidth || image.Height != imageHeight)
                {
                    return new BadRequestObjectResult($"Image must be {imageWidth}x{imageHeight} pixels.");
                }
            }

            // Upload file to Blob Storage
            string blobUrl = await _blobStorageService.UploadImageAsync(file.OpenReadStream(), file.FileName, position!);
            
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
                await _blobStorageService.UploadGifAsync(gifStream, "generatedGif.gif");
                
                // Optionally delete the original images after processing
                await _blobStorageService.DeleteBlobAsync(startImage.Name);
                await _blobStorageService.DeleteBlobAsync(middleImage.Name);
                await _blobStorageService.DeleteBlobAsync(endImage.Name);
            }
        }

    }

}