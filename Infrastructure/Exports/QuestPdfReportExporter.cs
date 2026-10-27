using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplierSelectionDSS.Services;
using System.Globalization;

namespace SupplierSelectionDSS.Infrastructure.Exports;

public static class QuestPdfReportExporter
{
    public static void ExportToFile(ReportData data, string filePath)
    {
        var document = BuildPdfDocument(data);
        document.GeneratePdf(filePath);
    }

    private static QuestPDF.Fluent.Document BuildPdfDocument(ReportData data)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header()
                    .Text("SUPPLIER SELECTION DSS")
                    .SemiBold()
                    .FontSize(16);

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        column.Item()
                            .Text("Final Evaluation Report")
                            .Bold()
                            .FontSize(18);

                        column.Item()
                            .Text($"Batch: {data.Batch.BatchCode} · {data.Batch.BatchName}");

                        column.Item()
                            .Text($"Product Group: {data.Batch.Group?.GroupCode} · {data.Batch.Group?.GroupName}");

                        column.Item()
                            .Text($"Status: {data.Batch.Status}");

                        column.Item()
                            .Text($"Suppliers: {data.Batch.Suppliers.Count} · Criteria: {data.Batch.Criteria.Count}");

                        column.Item()
                            .PaddingTop(8)
                            .Text("AHP")
                            .Bold()
                            .FontSize(13);

                        if (data.AhpResult != null)
                        {
                            column.Item()
                                .Text($"Lambda Max: {data.AhpResult.LambdaMax:0.000000}   " +
                                      $"CI: {data.AhpResult.Civalue:0.000000}   " +
                                      $"RI: {data.AhpResult.Rivalue:0.000000}   " +
                                      $"CR: {data.AhpResult.Crvalue:0.000000}");

                            column.Item()
                                .Text($"Consistency: {(data.AhpResult.IsConsistent ? "Consistent" : "Inconsistent")}");
                        }

                        column.Item()
                            .PaddingTop(8)
                            .Text("Criteria Weights")
                            .Bold()
                            .FontSize(13);

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Text("Criteria")
                                        .Bold();

                                    header.Cell()
                                        .AlignRight()
                                        .Text("Weight")
                                        .Bold();
                                });

                                foreach (var weight in data.AhpWeights)
                                {
                                    table.Cell()
                                        .Text($"{weight.CriteriaCode} · {weight.CriteriaName}");

                                    table.Cell()
                                        .AlignRight()
                                        .Text($"{weight.WeightValue:0.000000}");
                                }
                            });

                        column.Item()
                            .PaddingTop(8)
                            .Text("TOPSIS Ranking")
                            .Bold()
                            .FontSize(13);

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(35);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("#").Bold();
                                    header.Cell().Text("Supplier").Bold();
                                    header.Cell().Text("D+").Bold();
                                    header.Cell().Text("D-").Bold();
                                    header.Cell().Text("CC").Bold();
                                });

                                foreach (var result in data.TopsisResults)
                                {
                                    table.Cell().Text(result.RankNo.ToString());
                                    table.Cell().Text($"{result.Supplier.SupplierCode} · {result.Supplier.SupplierName}");
                                    table.Cell().Text(result.DistanceToPis.ToString("0.000000", CultureInfo.InvariantCulture));
                                    table.Cell().Text(result.DistanceToNis.ToString("0.000000", CultureInfo.InvariantCulture));
                                    table.Cell().Text(result.ClosenessCoefficient.ToString("0.000000", CultureInfo.InvariantCulture));
                                }
                            });

                        if (data.SensitivityAnalysis != null)
                        {
                            column.Item()
                                .PaddingTop(8)
                                .Text("Sensitivity Analysis (±10%, ±20%)")
                                .Bold()
                                .FontSize(13);

                            column.Item()
                                .Text($"Robustness: {data.SensitivityAnalysis.RobustnessPercentage:0.0}% · " +
                                      $"{(data.SensitivityAnalysis.IsOverallRobust ? "Robust Decision" : "Sensitive to Changes")}");

                            column.Item()
                                .Text(data.SensitivityAnalysis.SummaryMessage);
                        }

                        column.Item()
                            .PaddingTop(8)
                            .Text("Final Decision")
                            .Bold()
                            .FontSize(13);


                        if (data.Approval != null)
                        {
                            column.Item()
                                .Text($"Supplier: {data.Approval.SelectedSupplier?.SupplierCode ?? "—"} · " +
                                      $"{data.Approval.SelectedSupplier?.SupplierName ?? "—"}");

                            column.Item()
                                .Text($"Status: {data.Approval.Status}");

                            column.Item()
                                .Text($"Approved by: {data.Approval.ApprovedByNavigation?.FullName ?? "—"}");

                            column.Item()
                                .Text($"Approval date: {data.Approval.ApprovalDate:dd/MM/yyyy HH:mm}");

                            if (!string.IsNullOrWhiteSpace(data.Approval.Notes))
                            {
                                column.Item()
                                    .Text($"Notes: {data.Approval.Notes}");
                            }
                        }
                        else
                        {
                            column.Item()
                                .Text("No final approval recorded.");
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Supplier Selection DSS");
            });
        });
    }
}
