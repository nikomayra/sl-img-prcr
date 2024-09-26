using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace sl_img_prcr
{
    public class ImgUpload
    {
        private readonly ILogger<ImgUpload> _logger;

        public ImgUpload(ILogger<ImgUpload> logger)
        {
            _logger = logger;
        }

        [Function("ImgUpload")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("You're gay!");
        }
    }
}
