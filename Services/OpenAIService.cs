using Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Helpers;
using Newtonsoft.Json;
using NJsonSchema;
using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;

public class GridExtractionLabels
{
    [Description("Cell Labels. Allowed values: A1,B1,C1,D1,E1,F1,G1,H1,I1,A2,B2,...,I9")]
    public List<string>? CellLabels { get; set; }
}

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Services
{
    public sealed class ChatGptService
    {
        private readonly OpenAIClient _client;

        public ChatGptService(IConfiguration cfg)
        {
            _client = new OpenAIClient(cfg.GetValue<string>("ChatGpt:ConnectionString"));
        }

        public async Task<T> AskGptPdfHighQuality<T>(string message, string specialMessage, string systemPrompt, string modelName,
            byte[] pdfBytes) where T : class
        {
            var image = PdfHelper.ConvertPdfToImage(pdfBytes);

            var imageWithGridLabels = ImageHelper.OverlayGridWithLabels(
                image.binaryData.ToArray(),
                10,
                10
            );

            var sectors = await AskGptWithSystemPrompt<GridExtractionLabels>("",
                $"Your task is to extract sectors where is located {specialMessage}. Be as consistent and precise as possible", modelName, BinaryData.FromBytes(imageWithGridLabels), "image/jpeg", ChatImageDetailLevel.High);

            if (sectors.CellLabels == null)
                return (null)!;

            var trimmedImageWithMoreSectors = ImageHelper.CropGridCellsWithCircularNeighborsWideSides(
                image.binaryData.ToArray(),
                10,
                10,
                sectors.CellLabels!
            );

            var result = await AskGptWithSystemPrompt<T>(message, systemPrompt, modelName, BinaryData.FromBytes(trimmedImageWithMoreSectors), "image/png", ChatImageDetailLevel.High);

            return result;
        }

        public async Task<T> AskGptWithSystemPrompt<T>(string message, string systemPrompt, string modelName, BinaryData imageBytes, string imageBytesMediaType, ChatImageDetailLevel? imageDetailLevel = null, bool fixRotation = true)
        {
            var userMessageContent = new ChatMessageContent(new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(message),
                ChatMessageContentPart.CreateImagePart(imageBytes, imageBytesMediaType, imageDetailLevel)
            });

            var result = await AskGpt<T>([
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessageContent)
                ],
                modelName,
                0);

            return result!;
        }

        public async Task<T?> AskGptWithSystemPrompt<T>(string message, string systemPrompt)
        {
            return await AskGpt<T>([
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(message)
            ]);
        }

        public async Task<T?> AskGpt<T>(List<ChatMessage> prompts, string modelName = "gpt-4o", float temperature = 0f)
        {
            return await AskGptInternal<T>(prompts, modelName, temperature);
        }


        private async Task<T?> AskGptInternal<T>(List<ChatMessage> prompts, string modelName, float temperature)
        {
            var options = new ChatCompletionOptions
            {
                Temperature = temperature
            };

            if (typeof(T) != typeof(string))
            {
                var schema = JsonSchema.FromType<T>();
                var schemaText = schema.ToJson();

                options.ResponseFormat =
                    ChatResponseFormat.CreateJsonSchemaFormat(
                        typeof(T).Name,
                        BinaryData.FromString(schemaText));
            }

            var chatClient = _client.GetChatClient(modelName);
            var response = await chatClient.CompleteChatAsync(prompts, options);
            var result = response.Value.Content.First().Text;

            return typeof(T) == typeof(string)
                ? (T)(object)result
                : JsonConvert.DeserializeObject<T>(result);
        }
    }
}
