using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Services;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentIntelligenceController : ControllerBase
{
    private readonly DocumentIntelligenceService _documentIntelligenceService;
    private readonly ChatGptService _chatGptService;

    public DocumentIntelligenceController(DocumentIntelligenceService documentIntelligenceService, ChatGptService chatGptService)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _chatGptService = chatGptService;
    }

    // POST /DocumentIntelligence/analyze
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Analyze([FromForm] AnalyzeUploadRequest req)
    {
        await using var s = req.File.OpenReadStream();

        var (result, opId) = await _documentIntelligenceService.AnalyzeFileAsync(
            s,
            ct: HttpContext.RequestAborted);

        var shaped = result.Documents.Select(doc => new
        {
            doc.DocumentType,
            Fields = doc.Fields.ToDictionary(
                kv => kv.Key,
                kv => new
                {
                    kv.Value.Content,
                    kv.Value.Confidence,
                    kv.Value.FieldType
                })
        });

        return Ok(new { opId, Documents = shaped });
    }

    // POST /DocumentIntelligence/analyze/precise
    [HttpPost("analyze/precise")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> AnalyzePrecise([FromForm] AnalyzeUploadRequest req)
    {
        byte[] pdfBytes;
        using (var ms = new MemoryStream((int)Math.Min(req.File.Length, int.MaxValue)))
        {
            await req.File.CopyToAsync(ms);
            pdfBytes = ms.ToArray();
        }

        //TODO: RENAME TO ENG

        var result = await _chatGptService.AskGptPdfHighQuality<BankAccount>("Extract data according to the scheme", "Bank Account Number Section with the field name", string.Empty, "gpt-4.1", pdfBytes);

        return Ok(result);
    }
}
