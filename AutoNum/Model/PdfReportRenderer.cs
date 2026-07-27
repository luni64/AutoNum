using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO;

namespace AutoNumber.Model;

/// <summary>
/// Renders the visible, printable PDF report (title, description, image, names table) via
/// QuestPDF. This is the PDF sibling of the WPF live preview and the GDI+ JPG renderer —
/// column-width rules are shared through <see cref="NamesTableLayout"/> so the three stay
/// consistent. The editable AutoNum payload attachment is NOT added here; see
/// <see cref="PdfPayloadStore"/> and FileManager.WritePdf for the embedding step.
/// </summary>
internal static class PdfReportRenderer
{
    public static byte[] Render(
        SidecarExportData exportData,
        byte[]? photoBytes,
        int namesColumnCount,
        bool showRowDividers,
        Func<int, string> formatRowDividerText)
    {
        var heading = string.IsNullOrWhiteSpace(exportData.Title) ? "Ohne Titel" : exportData.Title;
        var hasId = !string.IsNullOrWhiteSpace(exportData.Id);
        var hasDescription = !string.IsNullOrWhiteSpace(exportData.Description);
        var columnCount = Math.Clamp(namesColumnCount, 1, 4);
        var tableReferenceWidth = 360d / columnCount;
        var pdfNumberColumnWidth = Math.Clamp(NamesTableLayout.ResolveNumberColumnWidth(tableReferenceWidth) * 0.5f, 24f, 48f);

        var orderedPersons = exportData.Persons
            .OrderBy(person => person.Row <= 0 ? int.MaxValue : person.Row)
            .ThenBy(person => person.Number)
            .ToList();
        // Ordered by lowest label number rather than row index, so the table reads 1..n for
        // top-down and bottom-up numbering alike (see Analyzer.PlacePersonNames).
        var assignedRowGroups = orderedPersons
            .Where(person => person.Row > 0)
            .GroupBy(person => person.Row)
            .OrderBy(group => group.Min(person => person.Number))
            .ToList();
        var unassignedPersons = orderedPersons
            .Where(person => person.Row <= 0)
            .ToList();

        var documentTimestamp = DateTimeOffset.TryParse(exportData.GeneratedAt, out var generatedAt)
            ? generatedAt
            : DateTimeOffset.Now;
        var createdDate = documentTimestamp.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));

        using var pdfStream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().AlignRight().Text(createdDate);

                    column.Item().Text(heading).FontSize(20).SemiBold();

                    if (hasId)
                    {
                        column.Item().PaddingTop(12).Text($"Bild-ID: {exportData.Id}").FontSize(11);
                    }

                    if (hasDescription)
                    {
                        column.Item().PaddingTop(8).Text("Beschreibung").FontSize(13).SemiBold();
                        column.Item().Text(exportData.Description);
                    }

                    if (photoBytes is not null)
                    {
                        const float maxImageWidth = 428f;
                        const float maxImageHeight = 320f;

                        column.Item()
                            .PaddingTop(8)
                            .AlignCenter()
                            .MaxWidth(maxImageWidth)
                            .MaxHeight(maxImageHeight)
                            .Image(photoBytes)
                            .FitArea();
                    }

                    column.Item().PaddingTop(8).Text("Namensliste").FontSize(14).SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            for (var c = 0; c < columnCount; c++)
                            {
                                cols.ConstantColumn(pdfNumberColumnWidth);
                                cols.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            for (var c = 0; c < columnCount; c++)
                            {
                                header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Nr.").SemiBold();
                                header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Name").SemiBold();
                            }
                        });

                        void RenderPersonRows(IReadOnlyList<SidecarPerson> persons)
                        {
                            for (var index = 0; index < persons.Count; index += columnCount)
                            {
                                for (var c = 0; c < columnCount; c++)
                                {
                                    var personIndex = index + c;
                                    if (personIndex < persons.Count)
                                    {
                                        var person = persons[personIndex];
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(person.Number.ToString());
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(person.Name);
                                    }
                                    else
                                    {
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(string.Empty);
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(string.Empty);
                                    }
                                }
                            }
                        }

                        foreach (var rowGroup in assignedRowGroups)
                        {
                            if (showRowDividers)
                            {
                                table.Cell()
                                    .ColumnSpan((uint)(columnCount * 2))
                                    .Border(NamesTableLayout.PdfBorderWidth)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(NamesTableLayout.CellPadding)
                                    .Text(formatRowDividerText(rowGroup.Key))
                                    .SemiBold();
                            }

                            RenderPersonRows(rowGroup.OrderBy(person => person.Number).ToList());
                        }

                        if (unassignedPersons.Count > 0)
                        {
                            RenderPersonRows(unassignedPersons);
                        }
                    });
                });
            });
        })
        .WithMetadata(new DocumentMetadata
        {
            Title = heading,
            Author = "AutoNum",
            Subject = "AutoNum export",
            Keywords = "AutoNum, Face labels, Numbered image, PDF",
            Creator = "AutoNum",
            Producer = "QuestPDF",
            Language = "de-DE",
            CreationDate = documentTimestamp,
            ModifiedDate = documentTimestamp
        })
        .GeneratePdf(pdfStream);

        return pdfStream.ToArray();
    }
}
