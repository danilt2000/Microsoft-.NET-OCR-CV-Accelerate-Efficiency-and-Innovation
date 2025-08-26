using Microsoft.AspNetCore.Mvc;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentIntelligenceController : ControllerBase
    {
        public DocumentIntelligenceController()
        {
        }

        [HttpGet(Name = "GetDocumentIntelligence")]
        public IActionResult Get()
        {
            return Ok(new
            {
                Test = "test"
            });
        }
    }
}
