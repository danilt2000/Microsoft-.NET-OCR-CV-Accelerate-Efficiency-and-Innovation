using iText.Kernel.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using OpenAI.Chat;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Geom;
using PDFtoImage;
using SkiaSharp;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using Rectangle = System.Drawing.Rectangle;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Helpers
{
    public class PdfHelper
    {
        public static byte[] ExtractPages(byte[] pdfBytes, int start, int end)
        {
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfDoc = new PdfDocument(new PdfReader(pdfStream));
            using var outputStream = new MemoryStream();
            var outputPdfDoc = new PdfDocument(new PdfWriter(outputStream));

            for (var i = start; i <= end && i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                outputPdfDoc.AddPage(page.CopyTo(outputPdfDoc));
            }

            pdfDoc.Close();
            outputPdfDoc.Close();

            return outputStream.ToArray();
        }

        public static List<byte[]> ExtractPages(byte[] pdfBytes)
        {
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfDoc = new PdfDocument(new PdfReader(pdfStream));

            var toReturn = new List<byte[]>();
            var totalPages = pdfDoc.GetNumberOfPages();
            for (var i = 1; i <= totalPages; i++)
            {
                using var outputStream = new MemoryStream();
                var page = pdfDoc.GetPage(i);
                var outputPdfDoc = new PdfDocument(new PdfWriter(outputStream));
                outputPdfDoc.AddPage(page.CopyTo(outputPdfDoc));
                outputPdfDoc.Close();
                toReturn.Add(outputStream.ToArray());
            }

            pdfDoc.Close();
            return toReturn;
        }

        public static int GetNumberOfPages(byte[] pdfBytes)
        {
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfDoc = new PdfDocument(new PdfReader(pdfStream));

            var totalPages = pdfDoc.GetNumberOfPages();
            pdfDoc.Close();

            return totalPages;
        }

        public static Bitmap ConvertPdfToBitmapOptimized(byte[] input, int dpi)
        {
            using var ms = new MemoryStream(input);
            
            // Convert PDF pages to SkiaSharp images using PDFtoImage
            var skImages = PDFtoImage.Conversion.ToImages(ms, options: new(Dpi: dpi)).ToList();
            
            if (skImages.Count == 0)
                throw new InvalidOperationException("Could not convert PDF to images");

            int totalHeight = 0;
            int maxWidth = 0;
            var pageDimensions = new List<(int Width, int Height)>();

            // Calculate total dimensions
            foreach (var skImage in skImages)
            {
                pageDimensions.Add((skImage.Width, skImage.Height));
                maxWidth = Math.Max(maxWidth, skImage.Width);
                totalHeight += skImage.Height;
            }

            // Create the final bitmap
            var bitmapToDrawOn = new Bitmap(maxWidth, totalHeight, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(bitmapToDrawOn);
            graphics.Clear(Color.White);

            int heightPosition = 0;

            // Combine all pages into one bitmap
            for (int i = 0; i < skImages.Count; i++)
            {
                var skImage = skImages[i];
                var (pageWidth, pageHeight) = pageDimensions[i];

                // Convert SkiaSharp image to System.Drawing.Bitmap
                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var skStream = new MemoryStream(skData.ToArray());
                using var pageBitmap = new Bitmap(skStream);

                // Draw the page bitmap to the combined bitmap
                graphics.DrawImage(pageBitmap, 0, heightPosition, pageWidth, pageHeight);

                heightPosition += pageHeight;
            }

            // Dispose SkiaSharp images
            foreach (var skImage in skImages)
            {
                skImage.Dispose();
            }

            return bitmapToDrawOn;
        }

        public static byte[] ExtractPages(byte[] pdfBytes, List<(int start, int end)> pageRanges)
        {
            using var pdfStream = new MemoryStream(pdfBytes);
            var pdfDoc = new PdfDocument(new PdfReader(pdfStream));
            using var outputStream = new MemoryStream();
            var outputPdfDoc = new PdfDocument(new PdfWriter(outputStream));

            foreach (var range in pageRanges)
            {
                for (var i = range.start; i <= range.end && i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    var page = pdfDoc.GetPage(i);
                    outputPdfDoc.AddPage(page.CopyTo(outputPdfDoc));
                }
            }

            pdfDoc.Close();
            outputPdfDoc.Close();

            return outputStream.ToArray();
        }

        public static byte[] MergePdfs(List<byte[]> pdfFiles)
        {
            if (pdfFiles.Count == 1)
                return pdfFiles.First();

            using var outputStream = new MemoryStream();
            using var mergedPdf = new PdfDocument(new PdfWriter(outputStream));

            foreach (var pdfBytes in pdfFiles)
            {
                using var pdfStream = new MemoryStream(pdfBytes);
                using var pdfDoc = new PdfDocument(new PdfReader(pdfStream));
                int numberOfPages = pdfDoc.GetNumberOfPages();

                for (int i = 1; i <= numberOfPages; i++)
                {
                    var page = pdfDoc.GetPage(i);
                    mergedPdf.AddPage(page.CopyTo(mergedPdf));
                }
            }

            mergedPdf.Close();
            return outputStream.ToArray();
        }

        public static byte[] CreatePdfFromImages(List<byte[]> imageBytesList)
        {
            using var outputStream = new System.IO.MemoryStream();
            var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 0, 0, 0, 0);
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, outputStream);
            document.Open();

            for (int i = 0; i < imageBytesList.Count; i++)
            {
                using var imageStream = new System.IO.MemoryStream(imageBytesList[i]);
                var image = iTextSharp.text.Image.GetInstance(imageStream);

                image.ScaleToFit(document.PageSize.Width, document.PageSize.Height);
                image.SetAbsolutePosition(0, 0);

                document.Add(image);

                if (i < imageBytesList.Count - 1)
                {
                    document.NewPage();
                }
            }

            document.Close();
            return outputStream.ToArray();
        }

        public static byte[] ReSavePdf(byte[] originalPdf)
        {
            using var inputStream = new MemoryStream(originalPdf);
            using var outputStream = new MemoryStream();
            using (var pdfReader = new PdfReader(inputStream))
            using (var pdfWriter = new PdfWriter(outputStream))
            using (var pdfDocSource = new PdfDocument(pdfReader))
            using (var pdfDocTarget = new PdfDocument(pdfWriter))
            {
                var pdfMerger = new PdfMerger(pdfDocTarget);
                pdfMerger.Merge(pdfDocSource, 1, pdfDocSource.GetNumberOfPages());
                pdfDocTarget.Close();
            }
            return outputStream.ToArray();
        }

        public static (BinaryData binaryData, ChatImageDetailLevel detailLevel) ConvertPdfToImage(byte[] pdfBytes)
        {
            var bitmap = PdfHelper.ConvertPdfToBitmapOptimized(pdfBytes, 300);

            BinaryData imageBytes;
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
                imageBytes = new BinaryData(ms.ToArray());
            }

            if (imageBytes is null)
            {
                throw new Exception("Conversion of PDF failed");
            }

            var detailLevel = bitmap is { Size: { Width: <= 512, Height: <= 512 } }
                ? ChatImageDetailLevel.Low
                : ChatImageDetailLevel.High;
            return (imageBytes, detailLevel);
        }
    }
}
