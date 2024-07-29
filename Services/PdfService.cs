using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;


public class PdfService
{
    public string ExtractTextFromPdf(Stream pdfStream)
    {
        var sb = new StringBuilder();

        using (var document = UglyToad.PdfPig.PdfDocument.Open(pdfStream))
        {
            for (var i = 1; i <= document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                var words = page.GetWords().ToList();
                
                if (!words.Any())
                {
                    sb.AppendLine(); // Empty page
                    continue;
                }

                float lastBottom = (float)words[0].BoundingBox.Bottom;
                float lineHeight = words.Max(w => (float)w.BoundingBox.Height);
                float pageLeft = words.Min(w => (float)w.BoundingBox.Left);
                StringBuilder lineSb = new StringBuilder();

                bool isFirstLine = true;

                foreach (var word in words)
                {
                    float wordBottom = (float)word.BoundingBox.Bottom;
                    float wordLeft = (float)word.BoundingBox.Left;

                    if (lastBottom - wordBottom > lineHeight / 2)
                    {
                        // New line detected
                        if (isFirstLine)
                        {
                            sb.AppendLine(InsertSpacesIntoLongWords(lineSb.ToString().TrimEnd()));
                            isFirstLine = false;
                        }
                        else
                        {
                            sb.AppendLine(lineSb.ToString().TrimEnd());
                        }
                        lineSb.Clear();
                        
                        // Add empty lines if the gap is large enough
                        int emptyLines = (int)((lastBottom - wordBottom) / lineHeight) - 1;
                        for (int k = 0; k < emptyLines; k++)
                        {
                            sb.AppendLine();
                        }

                        // Add indentation
                        if (wordLeft - pageLeft > 10) // Adjust this value as needed
                        {
                            lineSb.Append("    "); // Add 4 spaces for indentation
                        }
                    }
                    else if (lineSb.Length > 0)
                    {
                        // Add space between words on the same line
                        lineSb.Append(' ');
                    }

                    lineSb.Append(word.Text);
                    lastBottom = wordBottom;
                }

                // Add the last line
                sb.AppendLine(lineSb.ToString().TrimEnd());
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string InsertSpacesIntoLongWords(string line)
    {
        // Example logic to insert spaces into long words
        return Regex.Replace(line, @"(\S{10,})", "$1 "); // Adds a space after words longer than 10 characters
    }
}