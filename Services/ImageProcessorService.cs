using System.IO;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text;

namespace sl_img_prcr.Services
{
    public class ImageProcessorService
    {
        private const int frameDelay = 300;
        private const int imageWidth = 250;
        private const int imageHeight = 250;

        public (bool isValid, string error) IsValidImage(IFormFile file)
        {
            
            // Validate file size (e.g., max 1 MB)
            if (file.Length > 1 * 1024 * 1024)
            {
                return (false, "File size > 1 mb");
            }

            // Check the content type (MIME type) of the uploaded file
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/tiff", "image/bmp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return (false, "File type not supported");
            }

            // Optionally, check the file extension as well
            var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".tiff", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "File type not supported");
            }

            //Validate image dimensions & aspect ratio
            using (var imageStream = file.OpenReadStream())
            {
                using var image = Image.Load<Rgba32>(imageStream);
                var aspectRatio = image.Width / image.Height;
                if (aspectRatio < 0.9 && aspectRatio > 1.1) // Allow 10% off-square images through.
                {
                    return (false, "Image aspect ratio must be 1:1");
                }
                else if(image.Width < 128){
                    return (false, "Image must be >= 128x128px");
                }
            }

            return (true, "");
        }

        public Stream CreateGifFromImages(Stream startImage, Stream middleImage, Stream endImage)
        {
            // Load the start image as the base for the GIF.
            using var gifImage = Image.Load<Rgba32>(startImage);
            
            // Set up GIF metadata for repeating.
            var gifMetaData = gifImage.Metadata.GetGifMetadata();
            gifMetaData.RepeatCount = 0; // Infinite loop

            // Set delay for the start frame.
            GifFrameMetadata startFrameMetadata = gifImage.Frames.RootFrame.Metadata.GetGifMetadata();
            startFrameMetadata.FrameDelay = frameDelay;

            // Load middle frame image
            using var middle = Image.Load<Rgba32>(middleImage);

            // Set delay for the middle frame.
            var middleFrameMetadata = middle.Frames.RootFrame.Metadata.GetGifMetadata();
            middleFrameMetadata.FrameDelay = frameDelay;

            // Add the middle image frame
            gifImage.Frames.AddFrame(middle.Frames.RootFrame);

            // Load end frame image
            using var end = Image.Load<Rgba32>(endImage);

            // Set delay for the end frame.
            var endFrameMetadata = end.Frames.RootFrame.Metadata.GetGifMetadata();
            endFrameMetadata.FrameDelay = frameDelay;

            // Add the end image frame
            gifImage.Frames.AddFrame(end.Frames.RootFrame);

            // Save the GIF to a stream.
            var gifStream = new MemoryStream();
            gifImage.SaveAsGif(gifStream, new GifEncoder());
            gifStream.Position = 0; // Reset stream position for reading.
            return gifStream;
        }

        public Stream ResizeImage250 (Stream image){

            using Image img = Image.Load(image);
            int width = imageWidth;
            int height = imageHeight;
            img.Mutate(x => x.Resize(width, height));

            var imgStream = new MemoryStream();
            img.Save(imgStream, new JpegEncoder());
            imgStream.Position = 0;
            return imgStream;
        }

    }
}