using iText.Kernel.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using OpenAI.Chat;
using Xfinium.Pdf;
using Xfinium.Pdf.Rendering;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

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
            var doc = new PdfFixedDocument(ms);

            int totalHeight = 0;
            int maxWidth = 0;

            var pageDimensions = new List<(int Width, int Height)>();

            foreach (var page in doc.Pages)
            {
                double pageWidthInInches = page.Width / 72.0;
                double pageHeightInInches = page.Height / 72.0;

                int pageWidthInPixels = (int)(pageWidthInInches * dpi);
                int pageHeightInPixels = (int)(pageHeightInInches * dpi);

                pageDimensions.Add((pageWidthInPixels, pageHeightInPixels));

                maxWidth = Math.Max(maxWidth, pageWidthInPixels);
                totalHeight += pageHeightInPixels;
            }

            var bitmapToDrawOn = new Bitmap(maxWidth, totalHeight);
            using var g = Graphics.FromImage(bitmapToDrawOn);

            int heightPosition = 0;

            for (int i = 0; i < doc.Pages.Count; i++)
            {
                var page = doc.Pages[i];
                var (pageWidth, pageHeight) = pageDimensions[i];

                using var result = new MemoryStream();
                var renderer = new PdfPageRenderer(page);

                renderer.ConvertPageToImage(dpi, result, PdfPageImageFormat.Png);
                result.Position = 0;

                using var sourceBitmap = new Bitmap(result);
                g.DrawImage(sourceBitmap, 0, heightPosition, pageWidth, pageHeight);

                heightPosition += pageHeight;
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
