using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace TextToMultiPageTiff
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Text to Multi-Page TIFF Converter");
            Console.WriteLine("=================================");

            try
            {
                // Display menu options
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Convert text input to multi-page TIFF");
                Console.WriteLine("2. Convert text file to multi-page TIFF");
                Console.WriteLine("3. Convert multiple text files to single TIFF");
                Console.Write("Enter your choice (1-3): ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ConvertTextInput();
                        break;
                    case "2":
                        ConvertTextFile();
                        break;
                    case "3":
                        ConvertMultipleFiles();
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Using default text input mode.");
                        ConvertTextInput();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void ConvertTextInput()
        {
            Console.WriteLine("\nEnter text (press Ctrl+Z on new line to finish):");
            StringBuilder textBuilder = new StringBuilder();
            string line;
            
            while ((line = Console.ReadLine()) != null)
            {
                textBuilder.AppendLine(line);
            }

            string text = textBuilder.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                text = GenerateSampleText();
                Console.WriteLine("Using sample text...");
            }

            Console.Write("Enter output filename (without extension): ");
            string filename = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(filename))
                filename = "multipage_output";

            MultiPageTiffConverter converter = new MultiPageTiffConverter();
            converter.ConvertTextToMultiPageTiff(text, $"{filename}.tiff");
            
            Console.WriteLine($"Multi-page TIFF created: {filename}.tiff");
        }

        static void ConvertTextFile()
        {
            Console.Write("Enter path to text file: ");
            string filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found!");
                return;
            }

            string text = File.ReadAllText(filePath);
            string outputPath = Path.ChangeExtension(filePath, ".tiff");

            MultiPageTiffConverter converter = new MultiPageTiffConverter();
            converter.ConvertTextToMultiPageTiff(text, outputPath);
            
            Console.WriteLine($"Multi-page TIFF created: {outputPath}");
        }

        static void ConvertMultipleFiles()
        {
            Console.Write("Enter directory path containing text files: ");
            string dirPath = Console.ReadLine();

            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine("Directory not found!");
                return;
            }

            string[] textFiles = Directory.GetFiles(dirPath, "*.txt");
            if (textFiles.Length == 0)
            {
                Console.WriteLine("No .txt files found in directory!");
                return;
            }

            Console.Write("Enter output filename (without extension): ");
            string filename = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(filename))
                filename = "combined_output";

            MultiPageTiffConverter converter = new MultiPageTiffConverter();
            converter.ConvertMultipleFilesToTiff(textFiles, $"{filename}.tiff");
            
            Console.WriteLine($"Multi-page TIFF created from {textFiles.Length} files: {filename}.tiff");
        }

        static string GenerateSampleText()
        {
            return @"Sample Document - Page 1
This is a demonstration of multi-page TIFF conversion.
The converter automatically splits long text into multiple pages
based on the available space on each page.

Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.

--- PAGE BREAK ---

Sample Document - Page 2
This text will appear on the second page of the TIFF document.
You can use '--- PAGE BREAK ---' to force a new page.

Features:
- Automatic page splitting
- Custom page breaks
- Multiple input sources
- Configurable formatting
- High-quality text rendering

--- PAGE BREAK ---

Sample Document - Page 3
This is the final page of the sample document.
The converter handles as many pages as needed to fit all your text.

Thank you for using the Multi-Page TIFF Converter!";
        }
    }

    public class MultiPageTiffConverter
    {
        public int PageWidth { get; set; } = 850;
        public int PageHeight { get; set; } = 1100; // Letter size ratio
        public int Margin { get; set; } = 50;
        public string FontName { get; set; } = "Arial";
        public float FontSize { get; set; } = 11;
        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;
        public int LinesPerPage { get; set; } = 45;

        public void ConvertTextToMultiPageTiff(string text, string outputPath)
        {
            List<string> pages = SplitTextIntoPages(text);
            CreateMultiPageTiff(pages, outputPath);
            
            Console.WriteLine($"Created {pages.Count} pages in TIFF file.");
        }

        public void ConvertMultipleFilesToTiff(string[] filePaths, string outputPath)
        {
            List<string> allPages = new List<string>();

            foreach (string filePath in filePaths)
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    
                    // Add file header
                    string headerText = $"File: {fileName}\n{new string('=', 40)}\n\n{fileContent}";
                    
                    List<string> filePages = SplitTextIntoPages(headerText);
                    allPages.AddRange(filePages);
                    
                    // Add separator page between files
                    if (filePath != filePaths.Last())
                    {
                        allPages.Add($"\n\n{new string('-', 20)} END OF FILE {new string('-', 20)}\n\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                }
            }

            CreateMultiPageTiff(allPages, outputPath);
            Console.WriteLine($"Created {allPages.Count} pages from {filePaths.Length} files.");
        }

        private List<string> SplitTextIntoPages(string text)
        {
            List<string> pages = new List<string>();
            
            // Split by explicit page breaks first
            string[] explicitPages = text.Split(new string[] { "--- PAGE BREAK ---" }, StringSplitOptions.None);
            
            foreach (string pageText in explicitPages)
            {
                string[] lines = pageText.Split(new char[] { '\n', '\r' }, StringSplitOptions.None);
                StringBuilder currentPage = new StringBuilder();
                int lineCount = 0;

                foreach (string line in lines)
                {
                    // Calculate how many display lines this text line will take
                    int wrappedLines = CalculateWrappedLines(line);
                    
                    if (lineCount + wrappedLines > LinesPerPage && currentPage.Length > 0)
                    {
                        // Start new page
                        pages.Add(currentPage.ToString());
                        currentPage.Clear();
                        lineCount = 0;
                    }

                    currentPage.AppendLine(line);
                    lineCount += Math.Max(1, wrappedLines);
                }

                if (currentPage.Length > 0)
                {
                    pages.Add(currentPage.ToString());
                }
            }

            return pages.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        }

        private int CalculateWrappedLines(string line)
        {
            if (string.IsNullOrEmpty(line))
                return 1;

            // Rough calculation: assume average character width
            int charactersPerLine = (int)((PageWidth - 2 * Margin) / (FontSize * 0.6));
            return Math.Max(1, (int)Math.Ceiling((double)line.Length / charactersPerLine));
        }

        private void CreateMultiPageTiff(List<string> pages, string outputPath)
        {
            if (pages.Count == 0)
            {
                throw new ArgumentException("No pages to convert");
            }

            ImageCodecInfo tiffCodec = GetTiffEncoder();
            if (tiffCodec == null)
            {
                throw new InvalidOperationException("TIFF encoder not found");
            }

            // Create the first page
            using (Bitmap firstPage = CreatePageBitmap(pages[0], 1, pages.Count))
            {
                EncoderParameters encoderParams = new EncoderParameters(2);
                encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
                encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

                firstPage.Save(outputPath, tiffCodec, encoderParams);
            }

            // Add remaining pages
            if (pages.Count > 1)
            {
                using (Image tiffImage = Image.FromFile(outputPath))
                {
                    for (int i = 1; i < pages.Count; i++)
                    {
                        using (Bitmap nextPage = CreatePageBitmap(pages[i], i + 1, pages.Count))
                        {
                            EncoderParameters pageParams = new EncoderParameters(2);
                            pageParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionPage);
                            pageParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

                            tiffImage.SaveAdd(nextPage, pageParams);
                        }
                    }

                    // Flush and close the multi-frame TIFF
                    EncoderParameters flushParams = new EncoderParameters(1);
                    flushParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
                    tiffImage.SaveAdd(flushParams);
                }
            }
        }

        private Bitmap CreatePageBitmap(string text, int pageNumber, int totalPages)
        {
            Bitmap bitmap = new Bitmap(PageWidth, PageHeight);
            
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Set high quality rendering
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(BackgroundColor);

                using (Font font = new Font(FontName, FontSize, FontStyle.Regular))
                using (Font headerFont = new Font(FontName, FontSize - 2, FontStyle.Italic))
                using (SolidBrush textBrush = new SolidBrush(TextColor))
                using (SolidBrush headerBrush = new SolidBrush(Color.Gray))
                {
                    // Draw header with page number
                    string header = $"Page {pageNumber} of {totalPages}";
                    graphics.DrawString(header, headerFont, headerBrush, PageWidth - Margin - 100, Margin - 30);

                    // Draw main content
                    Rectangle textArea = new Rectangle(Margin, Margin, PageWidth - (2 * Margin), PageHeight - (2 * Margin) - 30);
                    
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.FormatFlags = StringFormatFlags.LineLimit;

                    graphics.DrawString(text.Trim(), font, textBrush, textArea, stringFormat);

                    // Draw footer
                    string footer = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    graphics.DrawString(footer, headerFont, headerBrush, Margin, PageHeight - Margin + 10);
                }
            }

            return bitmap;
        }

        private ImageCodecInfo GetTiffEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Tiff.Guid);
        }
    }

    // Configuration class for advanced settings
    public class TiffConversionSettings
    {
        public int PageWidth { get; set; } = 850;
        public int PageHeight { get; set; } = 1100;
        public int Margin { get; set; } = 50;
        public string FontName { get; set; } = "Arial";
        public float FontSize { get; set; } = 11;
        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;
        public int LinesPerPage { get; set; } = 45;
        public bool IncludePageNumbers { get; set; } = true;
        public bool IncludeTimestamp { get; set; } = true;
        public string PageBreakMarker { get; set; } = "--- PAGE BREAK ---";
    }
}