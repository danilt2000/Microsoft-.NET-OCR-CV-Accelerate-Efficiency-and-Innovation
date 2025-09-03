using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Models;
using Microsoft.Extensions.Options;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Services;

public sealed class DocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly DocumentIntelligenceOptions _opt;

    public DocumentIntelligenceService(IOptions<DocumentIntelligenceOptions> opt)
    {
        _opt = opt.Value;
        _client = new DocumentIntelligenceClient(new Uri(_opt.Endpoint), new AzureKeyCredential(_opt.Key));
    }

    public async Task<(AnalyzeResult result, string operationId)> AnalyzeFileAsync(
        Stream file,
        string? modelId = null,
        CancellationToken ct = default)
    {
        var opts = new AnalyzeDocumentOptions(modelId ?? _opt.ModelId, await BinaryData.FromStreamAsync(file, ct))
        {
            Locale = "en",
        };

        var op = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, opts, ct);
        return (op.Value, op.Id);
    }
}