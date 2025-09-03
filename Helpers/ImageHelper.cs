using System.Drawing;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Helpers
{
    public class ImageHelper
    {
        public static string GetImageType(string base64String)
        {
            var imageData = Convert.FromBase64String(base64String);

            if (imageData.Length < 4) return "unknown";

            if (imageData[0] == 0xFF && imageData[1] == 0xD8)
                return "image/jpeg";

            if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                return "image/png";

            if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                return "image/gif";

            if (imageData is [0x3C, 0x3F, 0x78, 0x6D, ..])
                return "image/svg+xml";

            return "unknown";
        }

        public static byte[] OverlayGridWithLabels(
        byte[] inputImageBytes,
        int rows = 10,
        int cols = 10)
        {
            using var inputStream = new MemoryStream(inputImageBytes);
            using var bmp = new Bitmap(inputStream);

            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int cellWidth = bmp.Width / cols;
            int cellHeight = bmp.Height / rows;

            using var thinRedPen = new Pen(Color.Red, 2f);

            for (int j = 0; j <= cols; j++)
            {
                int x = j * cellWidth;
                g.DrawLine(thinRedPen, x, 0, x, bmp.Height);
            }

            for (int i = 0; i <= rows; i++)
            {
                int y = i * cellHeight;
                g.DrawLine(thinRedPen, 0, y, bmp.Width, y);
            }

            using var sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            float FindMaxFontSize(string text, int maxWidth, int maxHeight)
            {
                float fontSize = Math.Min(maxWidth, maxHeight);
                using var testFont = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                SizeF size = g.MeasureString(text, testFont);
                while ((size.Width > maxWidth * 0.4f || size.Height > maxHeight * 0.4f) && fontSize > 1)
                {
                    fontSize -= 1;
                    using var tmpFont = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    size = g.MeasureString(text, tmpFont);
                }
                return fontSize;
            }

            for (int j = 0; j < cols; j++)
            {
                string colLabel = ((char)('A' + j)).ToString();
                float fontSize = FindMaxFontSize(colLabel, cellWidth, cellHeight);
                using var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                float x = j * cellWidth + cellWidth / 2f;

                if (j > 0 && j < cols - 1)
                    g.DrawString(colLabel, font, Brushes.Red, x, cellHeight / 4f, sf);

                if (j > 0 && j < cols - 1)
                    g.DrawString(colLabel, font, Brushes.Red, x, bmp.Height - cellHeight / 4f - font.Size, sf);
            }

            for (int i = 0; i < rows; i++)
            {
                string rowLabel = (i + 1).ToString();
                float fontSize = FindMaxFontSize(rowLabel, cellWidth, cellHeight);
                using var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                float y = i * cellHeight + cellHeight / 2f;

                if (i > 0 && i < rows - 1)
                    g.DrawString(rowLabel, font, Brushes.Red, cellWidth / 4f, y - font.Size / 2f, sf);

                if (i > 0 && i < rows - 1)
                    g.DrawString(rowLabel, font, Brushes.Red, bmp.Width - cellWidth / 4f, y - font.Size / 2f, sf);
            }

            using var outputStream = new MemoryStream();
            bmp.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return outputStream.ToArray();
        }

        public static byte[] CropGridCellsWithSideNeighbors(
         byte[] inputImageBytes,
         int rows,
         int cols,
         List<string> cellLabels)
        {
            using var inputStream = new MemoryStream(inputImageBytes);
            using var bmp = new Bitmap(inputStream);

            if (Array.IndexOf(bmp.PropertyIdList, 0x0112) > -1)
            {
                var orientation = (int)bmp.GetPropertyItem(0x0112)!.Value![0];
                switch (orientation)
                {
                    case 3:
                        bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 6:
                        bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 8:
                        bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
                bmp.RemovePropertyItem(0x0112);
            }

            int cellWidth = bmp.Width / cols;
            int cellHeight = bmp.Height / rows;

            var allRows = new HashSet<int>();
            var allCols = new HashSet<int>();

            foreach (var label in cellLabels)
            {
                char colChar = label[0];
                int colIndex = colChar - 'A';
                int rowIndex = int.Parse(label.Substring(1)) - 1;

                allRows.Add(rowIndex);
                allCols.Add(colIndex);

                if (colIndex - 1 >= 0) allCols.Add(colIndex - 1);
                if (colIndex + 1 < cols) allCols.Add(colIndex + 1);
            }

            int minRow = allRows.Min();
            int maxRow = allRows.Max();
            int minCol = allCols.Min();
            int maxCol = allCols.Max();

            int x = minCol * cellWidth;
            int y = minRow * cellHeight;
            int width = (maxCol - minCol + 1) * cellWidth;
            int height = (maxRow - minRow + 1) * cellHeight;

            Rectangle cropRect = new Rectangle(x, y, width, height);

            using var cropped = bmp.Clone(cropRect, bmp.PixelFormat);
            using var outputStream = new MemoryStream();
            cropped.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return outputStream.ToArray();
        }

        public static byte[] CropGridCellsWithCircularNeighborsWideSides(
         byte[] inputImageBytes,
         int rows,
         int cols,
         List<string> cellLabels)
        {
            using var inputStream = new MemoryStream(inputImageBytes);
            using var bmp = new Bitmap(inputStream);

            if (Array.IndexOf(bmp.PropertyIdList, 0x0112) > -1)
            {
                var orientation = (int)bmp.GetPropertyItem(0x0112)!.Value![0];
                switch (orientation)
                {
                    case 3:
                        bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 6:
                        bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 8:
                        bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
                bmp.RemovePropertyItem(0x0112);
            }

            int cellWidth = bmp.Width / cols;
            int cellHeight = bmp.Height / rows;

            var allRows = new HashSet<int>();
            var allCols = new HashSet<int>();

            foreach (var label in cellLabels)
            {
                char colChar = label[0];
                int colIndex = colChar - 'A';
                int rowIndex = int.Parse(label.Substring(1)) - 1;

                allRows.Add(rowIndex);
                allCols.Add(colIndex);
            }

            int minRow = allRows.Min();
            int maxRow = allRows.Max();
            int minCol = allCols.Min();
            int maxCol = allCols.Max();

            minRow = Math.Max(0, minRow - 1);
            maxRow = Math.Min(rows - 1, maxRow + 1);
            minCol = Math.Max(0, minCol - 2);
            maxCol = Math.Min(cols - 1, maxCol + 2);

            int x = minCol * cellWidth;
            int y = minRow * cellHeight;
            int width = (maxCol - minCol + 1) * cellWidth;
            int height = (maxRow - minRow + 1) * cellHeight;

            Rectangle cropRect = new Rectangle(x, y, width, height);

            using var cropped = bmp.Clone(cropRect, bmp.PixelFormat);
            using var outputStream = new MemoryStream();
            cropped.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return outputStream.ToArray();
        }
    }
}
