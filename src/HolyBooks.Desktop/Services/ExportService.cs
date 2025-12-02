using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlWordprocessing = DocumentFormat.OpenXml.Wordprocessing;
using HolyBooks.Core.Models;

namespace HolyBooks.Desktop.Services;

/// <summary>
/// שירות ייצוא ל-PDF ו-Word
/// </summary>
public class ExportService
{
    public ExportService()
    {
        // Set QuestPDF license (Community license is free for most uses)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region PDF Export

    /// <summary>
    /// ייצוא דף מקורות ל-PDF
    /// </summary>
    public void ExportSourceSheetToPdf(SourceSheet sheet, List<SheetItem> items, string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.ContentFromRightToLeft(); // RTL for Hebrew!

                page.Header()
                    .Text(sheet.Title)
                    .FontSize(24)
                    .Bold()
                    .FontFamily("David")
                    .AlignCenter();

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(12);

                        if (!string.IsNullOrEmpty(sheet.Description))
                        {
                            column.Item()
                                .Text(sheet.Description)
                                .FontSize(12)
                                .FontFamily("David")
                                .Italic();
                        }

                        foreach (var item in items.OrderBy(i => i.SortOrder))
                        {
                            column.Item().Element(c => RenderSheetItem(c, item));
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("עמוד ");
                        text.CurrentPageNumber();
                        text.Span(" מתוך ");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf(outputPath);
    }

    private void RenderSheetItem(IContainer container, SheetItem item)
    {
        switch (item.ItemType)
        {
            case "heading":
                container
                    .PaddingTop(8)
                    .Text(item.CustomText ?? "")
                    .FontSize(18)
                    .Bold()
                    .FontFamily("David");
                break;

            case "text":
                container
                    .Text(item.CustomText ?? "")
                    .FontSize(14)
                    .FontFamily("David")
                    .LineHeight(1.6f);
                break;

            case "source":
                container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(12)
                    .Column(col =>
                    {
                        col.Item()
                            .Text(item.SourceRef ?? "")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2)
                            .FontFamily("David");

                        col.Item()
                            .PaddingTop(8)
                            .Text(item.SourceText ?? "")
                            .FontSize(14)
                            .FontFamily("David")
                            .LineHeight(1.8f);

                        if (!string.IsNullOrEmpty(item.Note))
                        {
                            col.Item()
                                .PaddingTop(8)
                                .Background(Colors.Yellow.Lighten4)
                                .Padding(8)
                                .Text($"הערה: {item.Note}")
                                .FontSize(12)
                                .FontFamily("David")
                                .Italic();
                        }
                    });
                break;

            case "divider":
                container
                    .PaddingVertical(8)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten1);
                break;
        }
    }

    /// <summary>
    /// ייצוא פרק ל-PDF
    /// </summary>
    public void ExportChapterToPdf(Chapter chapter, Content content, string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.ContentFromRightToLeft();

                page.Header()
                    .Column(col =>
                    {
                        col.Item()
                            .Text(chapter.Book?.Title ?? "")
                            .FontSize(14)
                            .FontFamily("David")
                            .AlignCenter();

                        col.Item()
                            .Text(chapter.Title)
                            .FontSize(22)
                            .Bold()
                            .FontFamily("David")
                            .AlignCenter();
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Text(content.TextContent)
                    .FontSize(16)
                    .FontFamily("David")
                    .LineHeight(2.0f);

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.CurrentPageNumber();
                    });
            });
        }).GeneratePdf(outputPath);
    }

    #endregion

    #region Word Export

    /// <summary>
    /// ייצוא דף מקורות ל-Word
    /// </summary>
    public void ExportSourceSheetToWord(SourceSheet sheet, List<SheetItem> items, string outputPath)
    {
        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new OpenXmlWordprocessing.Document();
        var body = mainPart.Document.AppendChild(new OpenXmlWordprocessing.Body());

        // RTL Settings for Hebrew
        var sectionProps = new OpenXmlWordprocessing.SectionProperties(
            new OpenXmlWordprocessing.BiDi(),
            new OpenXmlWordprocessing.PageMargin
            {
                Top = 1440,    // 1 inch in twips
                Right = 1440,
                Bottom = 1440,
                Left = 1440
            }
        );

        // Title
        body.AppendChild(CreateHeadingParagraph(sheet.Title, 1, true));

        // Description
        if (!string.IsNullOrEmpty(sheet.Description))
        {
            body.AppendChild(CreateParagraph(sheet.Description, italic: true));
        }

        body.AppendChild(new OpenXmlWordprocessing.Paragraph()); // Empty line

        // Items
        foreach (var item in items.OrderBy(i => i.SortOrder))
        {
            switch (item.ItemType)
            {
                case "heading":
                    body.AppendChild(CreateHeadingParagraph(item.CustomText ?? "", 2, false));
                    break;

                case "text":
                    body.AppendChild(CreateParagraph(item.CustomText ?? ""));
                    break;

                case "source":
                    // Source reference
                    body.AppendChild(CreateParagraph(item.SourceRef ?? "", bold: true, color: "1976D2"));

                    // Source text in a bordered paragraph
                    var sourcePara = CreateParagraph(item.SourceText ?? "");
                    var pProps = sourcePara.GetFirstChild<OpenXmlWordprocessing.ParagraphProperties>() ?? new OpenXmlWordprocessing.ParagraphProperties();
                    pProps.AppendChild(new OpenXmlWordprocessing.ParagraphBorders(
                        new OpenXmlWordprocessing.RightBorder { Val = OpenXmlWordprocessing.BorderValues.Single, Size = 12, Color = "CCCCCC" }
                    ));
                    pProps.AppendChild(new OpenXmlWordprocessing.Indentation { Right = "720" }); // 0.5 inch indent
                    body.AppendChild(sourcePara);

                    // Note
                    if (!string.IsNullOrEmpty(item.Note))
                    {
                        body.AppendChild(CreateParagraph($"הערה: {item.Note}", italic: true, shading: "FFFDE7"));
                    }

                    body.AppendChild(new OpenXmlWordprocessing.Paragraph()); // Empty line
                    break;

                case "divider":
                    var dividerPara = new OpenXmlWordprocessing.Paragraph(
                        new OpenXmlWordprocessing.ParagraphProperties(
                            new OpenXmlWordprocessing.ParagraphBorders(
                                new OpenXmlWordprocessing.BottomBorder { Val = OpenXmlWordprocessing.BorderValues.Single, Size = 6, Color = "CCCCCC" }
                            )
                        ),
                        new OpenXmlWordprocessing.Run(new OpenXmlWordprocessing.Text(" "))
                    );
                    body.AppendChild(dividerPara);
                    break;
            }
        }

        body.AppendChild(sectionProps);
    }

    private OpenXmlWordprocessing.Paragraph CreateHeadingParagraph(string text, int level, bool center)
    {
        var fontSize = level switch
        {
            1 => "48", // 24pt
            2 => "36", // 18pt
            _ => "28"  // 14pt
        };

        var run = new OpenXmlWordprocessing.Run(
            new OpenXmlWordprocessing.RunProperties(
                new OpenXmlWordprocessing.Bold(),
                new OpenXmlWordprocessing.FontSize { Val = fontSize },
                new OpenXmlWordprocessing.RunFonts { Ascii = "David", HighAnsi = "David", ComplexScript = "David" },
                new OpenXmlWordprocessing.RightToLeftText()
            ),
            new OpenXmlWordprocessing.Text(text)
        );

        var paraProps = new OpenXmlWordprocessing.ParagraphProperties(
            new OpenXmlWordprocessing.BiDi(),
            new OpenXmlWordprocessing.SpacingBetweenLines { After = "200" }
        );

        if (center)
        {
            paraProps.AppendChild(new OpenXmlWordprocessing.Justification { Val = OpenXmlWordprocessing.JustificationValues.Center });
        }

        return new OpenXmlWordprocessing.Paragraph(paraProps, run);
    }

    private OpenXmlWordprocessing.Paragraph CreateParagraph(string text, bool bold = false, bool italic = false,
                                       string? color = null, string? shading = null)
    {
        var runProps = new OpenXmlWordprocessing.RunProperties(
            new OpenXmlWordprocessing.FontSize { Val = "28" }, // 14pt
            new OpenXmlWordprocessing.RunFonts { Ascii = "David", HighAnsi = "David", ComplexScript = "David" },
            new OpenXmlWordprocessing.RightToLeftText()
        );

        if (bold) runProps.AppendChild(new OpenXmlWordprocessing.Bold());
        if (italic) runProps.AppendChild(new OpenXmlWordprocessing.Italic());
        if (color != null) runProps.AppendChild(new OpenXmlWordprocessing.Color { Val = color });

        var run = new OpenXmlWordprocessing.Run(runProps, new OpenXmlWordprocessing.Text(text));

        var paraProps = new OpenXmlWordprocessing.ParagraphProperties(
            new OpenXmlWordprocessing.BiDi(),
            new OpenXmlWordprocessing.SpacingBetweenLines { Line = "360", LineRule = OpenXmlWordprocessing.LineSpacingRuleValues.Auto }
        );

        if (shading != null)
        {
            paraProps.AppendChild(new OpenXmlWordprocessing.Shading
            {
                Val = OpenXmlWordprocessing.ShadingPatternValues.Clear,
                Fill = shading
            });
        }

        return new OpenXmlWordprocessing.Paragraph(paraProps, run);
    }

    /// <summary>
    /// ייצוא פרק ל-Word
    /// </summary>
    public void ExportChapterToWord(Chapter chapter, Content content, string outputPath)
    {
        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new OpenXmlWordprocessing.Document();
        var body = mainPart.Document.AppendChild(new OpenXmlWordprocessing.Body());

        // RTL Settings
        var sectionProps = new OpenXmlWordprocessing.SectionProperties(
            new OpenXmlWordprocessing.BiDi(),
            new OpenXmlWordprocessing.PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 }
        );

        // Book title
        if (chapter.Book != null)
        {
            body.AppendChild(CreateParagraph(chapter.Book.Title, bold: true));
        }

        // Chapter title
        body.AppendChild(CreateHeadingParagraph(chapter.Title, 1, true));
        body.AppendChild(new OpenXmlWordprocessing.Paragraph());

        // Content - split by paragraphs
        var paragraphs = content.TextContent.Split(new[] { "\n\n", "\r\n\r\n" },
                                                     StringSplitOptions.RemoveEmptyEntries);

        foreach (var para in paragraphs)
        {
            body.AppendChild(CreateParagraph(para.Trim()));
        }

        body.AppendChild(sectionProps);
    }

    #endregion
}
