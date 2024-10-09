using System.IO;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;

namespace sl_img_prcr.Services
{
    public class ImageProcessorService
    {

        private const int gifWidth = 250;
        private const int gifHeight = 250;
        private const int frameDelay = 200;

        public bool IsValidImage(IFormFile file)
        {
            // List of allowed MIME types for images
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/tiff", "image/bmp" };

            // Check the content type (MIME type) of the uploaded file
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return false;
            }

            // Optionally, check the file extension as well
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return false;
            }

            return true;
        }

        public Stream CreateGifFromImages(Stream startImage, Stream middleImage, Stream endImage)
        {
            // Load the images and extract the first frame
            using var start = Image.Load<Rgba32>(startImage);
            using var middle = Image.Load<Rgba32>(middleImage);
            using var end = Image.Load<Rgba32>(endImage);
            using var gifImage = new Image<Rgba32>(gifWidth, gifHeight);
            gifImage.Frames.AddFrame(start.Frames.RootFrame);
            gifImage.Frames.AddFrame(middle.Frames.RootFrame);
            gifImage.Frames.AddFrame(end.Frames.RootFrame);

            gifImage.Metadata.GetGifMetadata().RepeatCount = 0; // Infinite loop
            gifImage.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelay;

            var gifStream = new MemoryStream();
            gifImage.SaveAsGif(gifStream, new GifEncoder());
            gifStream.Position = 0;
            return gifStream;
        }

    }
}