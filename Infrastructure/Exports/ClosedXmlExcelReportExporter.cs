using ClosedXML.Excel;
using SupplierSelectionDSS.Services;
using System.Globalization;

namespace SupplierSelectionDSS.Infrastructure.Exports;

public static class ClosedXmlExcelReportExporter
{
    public static void ExportToFile(ReportData data, string filePath)
    {
        using var workbook = new XLWorkbook();
        BuildExcelWorkbook(workbook, data);
        workbook.SaveAs(filePath);
    }

    private static void BuildExcelWorkbook(
        XLWorkbook workbook,
        ReportData data)
    {
        BuildExcelSummarySheet(workbook, data);
        BuildExcelEvaluationSheet(workbook, data);
        BuildExcelAHPResultsSheet(workbook, data);
        BuildExcelTOPSISSheet(workbook, data);
        BuildExcelNormalizedSheet(workbook, data);
        BuildExcelWeightedSheet(workbook, data);
        BuildExcelSensitivitySheet(workbook, data);
    }


    private static void BuildExcelSummarySheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws = workbook.Worksheets.Add("Summary");

        ws.Cell(1, 1).Value = "SUPPLIER SELECTION DSS";
        ws.Cell(2, 1).Value = "Final Evaluation Report";

        ws.Cell(4, 1).Value = "Batch Code";
        ws.Cell(4, 2).Value = data.Batch.BatchCode;

        ws.Cell(5, 1).Value = "Batch Name";
        ws.Cell(5, 2).Value = data.Batch.BatchName;

        ws.Cell(6, 1).Value = "Product Group";
        ws.Cell(6, 2).Value = data.Batch.Group != null
            ? $"{data.Batch.Group.GroupCode} - {data.Batch.Group.GroupName}"
            : "—";

        ws.Cell(7, 1).Value = "Status";
        ws.Cell(7, 2).Value = data.Batch.Status;

        ws.Cell(8, 1).Value = "Suppliers";
        ws.Cell(8, 2).Value = data.Batch.Suppliers.Count;

        ws.Cell(9, 1).Value = "Criteria";
        ws.Cell(9, 2).Value = data.Batch.Criteria.Count;

        ws.Cell(11, 1).Value = "Final Supplier";
        ws.Cell(11, 2).Value = data.Approval?.SelectedSupplier != null
            ? data.Approval.SelectedSupplier.SupplierCode
            : "—";

        ws.Cell(12, 1).Value = "Final Supplier Name";
        ws.Cell(12, 2).Value = data.Approval?.SelectedSupplier != null
            ? data.Approval.SelectedSupplier.SupplierName
            : "—";

        ws.Cell(13, 1).Value = "Approval Status";
        ws.Cell(13, 2).Value = data.Approval?.Status ?? "—";

        ws.Cell(14, 1).Value = "Approved By";
        ws.Cell(14, 2).Value = data.Approval?.ApprovedByNavigation?.FullName ?? "—";

        ws.Cell(15, 1).Value = "Approval Date";
        ws.Cell(15, 2).Value = data.Approval != null
            ? data.Approval.ApprovalDate.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : "—";

        ws.Column(1).Width = 25;
        ws.Column(2).Width = 48;
        ws.Range("A1:B2").Style.Font.Bold = true;
        ws.Range("A4:A15").Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelEvaluationSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws = workbook.Worksheets.Add("Evaluation");
        ws.Cell(1, 1).Value = "Supplier";

        var criteria = data.Batch.Criteria
            .OrderBy(x => x.CriteriaCode)
            .ToList();

        for (var j = 0; j < criteria.Count; j++)
        {
            ws.Cell(1, j + 2).Value = criteria[j].CriteriaCode;
        }

        var suppliers = data.Batch.Suppliers
            .OrderBy(x => x.SupplierCode)
            .ToList();

        var scoreMap = data.Scores
            .ToDictionary(
                x => (x.SupplierId, x.CriteriaId),
                x => x.ScoreValue);

        for (var i = 0; i < suppliers.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = $"{suppliers[i].SupplierCode} - {suppliers[i].SupplierName}";

            for (var j = 0; j < criteria.Count; j++)
            {
                if (scoreMap.TryGetValue((suppliers[i].SupplierId, criteria[j].CriteriaId), out var score))
                {
                    ws.Cell(row, j + 2).Value = (double)score;
                }
            }
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelAHPResultsSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws = workbook.Worksheets.Add("AHP");

        ws.Cell(1, 1).Value = "AHP CONSISTENCY";
        ws.Cell(3, 1).Value = "Lambda Max";
        ws.Cell(3, 2).Value = data.AhpResult?.LambdaMax;

        ws.Cell(4, 1).Value = "CI";
        ws.Cell(4, 2).Value = data.AhpResult?.Civalue;

        ws.Cell(5, 1).Value = "RI";
        ws.Cell(5, 2).Value = data.AhpResult?.Rivalue;

        ws.Cell(6, 1).Value = "CR";
        ws.Cell(6, 2).Value = data.AhpResult?.Crvalue;

        ws.Cell(7, 1).Value = "Consistent";
        ws.Cell(7, 2).Value = data.AhpResult?.IsConsistent == true ? "Yes" : "No";

        ws.Cell(9, 1).Value = "Criteria";
        ws.Cell(9, 2).Value = "Weight";

        var row = 10;
        foreach (var weight in data.AhpWeights)
        {
            ws.Cell(row, 1).Value = $"{weight.CriteriaCode} - {weight.CriteriaName}";
            ws.Cell(row, 2).Value = (double)weight.WeightValue;
            row++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(9).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelTOPSISSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws = workbook.Worksheets.Add("TOPSIS");

        ws.Cell(1, 1).Value = "Criteria";
        ws.Cell(1, 2).Value = "Type";
        ws.Cell(1, 3).Value = "PIS";
        ws.Cell(1, 4).Value = "NIS";

        var row = 2;
        foreach (var ideal in data.Ideals)
        {
            ws.Cell(row, 1).Value = ideal.Criteria.CriteriaCode;
            ws.Cell(row, 2).Value = ideal.Criteria.CriteriaType;
            ws.Cell(row, 3).Value = (double)ideal.Pisvalue;
            ws.Cell(row, 4).Value = (double)ideal.Nisvalue;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Rank";
        ws.Cell(row, 2).Value = "Supplier";
        ws.Cell(row, 3).Value = "Distance PIS";
        ws.Cell(row, 4).Value = "Distance NIS";
        ws.Cell(row, 5).Value = "Closeness";

        row++;
        foreach (var result in data.TopsisResults)
        {
            ws.Cell(row, 1).Value = result.RankNo;
            ws.Cell(row, 2).Value = $"{result.Supplier.SupplierCode} - {result.Supplier.SupplierName}";
            ws.Cell(row, 3).Value = (double)result.DistanceToPis;
            ws.Cell(row, 4).Value = (double)result.DistanceToNis;
            ws.Cell(row, 5).Value = (double)result.ClosenessCoefficient;
            row++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelNormalizedSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        if (!data.NormalizedMatrix.Any()) return;

        var ws = workbook.Worksheets.Add("Normalized Matrix");
        ws.Cell(1, 1).Value = "Supplier";

        var criteria = data.Batch.Criteria.OrderBy(x => x.CriteriaCode).ToList();
        for (var j = 0; j < criteria.Count; j++)
        {
            ws.Cell(1, j + 2).Value = $"{criteria[j].CriteriaCode} ({criteria[j].CriteriaType})";
        }

        var row = 2;
        foreach (var supplier in data.NormalizedMatrix)
        {
            ws.Cell(row, 1).Value = $"{supplier.SupplierCode} - {supplier.SupplierName}";
            for (var j = 0; j < criteria.Count; j++)
            {
                if (supplier.Values.TryGetValue(criteria[j].CriteriaId, out var val))
                {
                    ws.Cell(row, j + 2).Value = (double)val;
                }
            }
            row++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelWeightedSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        if (!data.WeightedMatrix.Any()) return;

        var ws = workbook.Worksheets.Add("Weighted Matrix");
        ws.Cell(1, 1).Value = "Supplier";

        var criteria = data.Batch.Criteria.OrderBy(x => x.CriteriaCode).ToList();
        for (var j = 0; j < criteria.Count; j++)
        {
            ws.Cell(1, j + 2).Value = criteria[j].CriteriaCode;
        }

        var row = 2;
        foreach (var supplier in data.WeightedMatrix)
        {
            ws.Cell(row, 1).Value = $"{supplier.SupplierCode} - {supplier.SupplierName}";
            for (var j = 0; j < criteria.Count; j++)
            {
                if (supplier.Values.TryGetValue(criteria[j].CriteriaId, out var val))
                {
                    ws.Cell(row, j + 2).Value = (double)val;
                }
            }
            row++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildExcelSensitivitySheet(
        XLWorkbook workbook,
        ReportData data)
    {
        if (data.SensitivityAnalysis == null) return;

        var ws = workbook.Worksheets.Add("Sensitivity Analysis");
        ws.Cell(1, 1).Value = "SENSITIVITY ANALYSIS";
        ws.Cell(3, 1).Value = "Base Top Supplier";
        ws.Cell(3, 2).Value = $"{data.SensitivityAnalysis.BaseTopSupplierCode} - {data.SensitivityAnalysis.BaseTopSupplierName}";

        ws.Cell(4, 1).Value = "Base Closeness";
        ws.Cell(4, 2).Value = (double)data.SensitivityAnalysis.BaseTopSupplierCloseness;

        ws.Cell(5, 1).Value = "Robustness (%)";
        ws.Cell(5, 2).Value = data.SensitivityAnalysis.RobustnessPercentage;

        ws.Cell(6, 1).Value = "Overall Robust";
        ws.Cell(6, 2).Value = data.SensitivityAnalysis.IsOverallRobust ? "Yes" : "No";

        ws.Cell(7, 1).Value = "Summary";
        ws.Cell(7, 2).Value = data.SensitivityAnalysis.SummaryMessage;

        var row = 9;
        ws.Cell(row, 1).Value = "Criteria";
        ws.Cell(row, 2).Value = "Base Weight";
        ws.Cell(row, 3).Value = "Delta (%)";
        ws.Cell(row, 4).Value = "New Weight";
        ws.Cell(row, 5).Value = "Top Supplier";
        ws.Cell(row, 6).Value = "Closeness";
        ws.Cell(row, 7).Value = "Stable?";

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(row).Style.Font.Bold = true;

        row++;
        foreach (var scenario in data.SensitivityAnalysis.Scenarios)
        {
            foreach (var run in scenario.Runs)
            {
                ws.Cell(row, 1).Value = $"{scenario.CriteriaCode} - {scenario.CriteriaName}";
                ws.Cell(row, 2).Value = (double)scenario.BaseWeight;
                ws.Cell(row, 3).Value = $"{run.DeltaPercent:+0;-0;0}%";
                ws.Cell(row, 4).Value = (double)run.NewWeight;
                ws.Cell(row, 5).Value = $"{run.TopSupplierCode} - {run.TopSupplierName}";
                ws.Cell(row, 6).Value = (double)run.TopSupplierCloseness;
                ws.Cell(row, 7).Value = run.IsStable ? "Stable" : "Shifted";
                row++;
            }
        }

        ws.Columns().AdjustToContents();
    }
}

