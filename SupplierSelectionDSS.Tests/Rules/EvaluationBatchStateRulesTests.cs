using System;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Domain.Rules;
using Xunit;

namespace SupplierSelectionDSS.Tests.Rules;

public class EvaluationBatchStateRulesTests
{
    [Theory]
    [InlineData(BatchStatus.Draft, 2, 2, true)]
    [InlineData(BatchStatus.Draft, 5, 4, true)]
    [InlineData(BatchStatus.Draft, 1, 2, false)]
    [InlineData(BatchStatus.Draft, 2, 1, false)]
    [InlineData(BatchStatus.Evaluating, 3, 3, false)]
    [InlineData(BatchStatus.Completed, 3, 3, false)]
    [InlineData(BatchStatus.Cancelled, 3, 3, false)]
    public void CanStart_ValidatesStateAndCounts(string status, int suppliers, int criteria, bool expected)
    {
        var result = EvaluationBatchStateRules.CanStart(status, suppliers, criteria);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EnsureCanStart_Valid_DoesNotThrow()
    {
        var ex = Record.Exception(() => EvaluationBatchStateRules.EnsureCanStart(BatchStatus.Draft, 3, 3, "BATCH-001"));
        Assert.Null(ex);
    }

    [Fact]
    public void EnsureCanStart_WrongStatus_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanStart(BatchStatus.Evaluating, 3, 3, "BATCH-001"));
        Assert.Contains("trạng thái hiện tại là Evaluating", ex.Message);
    }

    [Fact]
    public void EnsureCanStart_InsufficientSuppliers_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanStart(BatchStatus.Draft, 1, 3, "BATCH-001"));
        Assert.Contains("ít nhất 2 nhà cung cấp", ex.Message);
    }

    [Fact]
    public void EnsureCanStart_InsufficientCriteria_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanStart(BatchStatus.Draft, 2, 1, "BATCH-001"));
        Assert.Contains("ít nhất 2 tiêu chí", ex.Message);
    }

    [Theory]
    [InlineData(BatchStatus.Evaluating, true)]
    [InlineData(BatchStatus.Draft, false)]
    [InlineData(BatchStatus.Completed, false)]
    [InlineData(BatchStatus.Cancelled, false)]
    public void CanEvaluate_ValidatesOnlyEvaluatingStatus(string status, bool expected)
    {
        Assert.Equal(expected, EvaluationBatchStateRules.CanEvaluate(status));
    }

    [Fact]
    public void EnsureCanEvaluate_NotEvaluating_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => EvaluationBatchStateRules.EnsureCanEvaluate(BatchStatus.Draft));
    }

    [Fact]
    public void CanPerformTOPSIS_ValidPrerequisites_ReturnsTrue()
    {
        var canPerform = EvaluationBatchStateRules.CanPerformTOPSIS(
            BatchStatus.Evaluating,
            hasAhpResult: true,
            isAhpConsistent: true,
            hasCompleteScores: true);

        Assert.True(canPerform);
    }

    [Fact]
    public void CanPerformTOPSIS_InconsistentAHP_ReturnsFalse()
    {
        var canPerform = EvaluationBatchStateRules.CanPerformTOPSIS(
            BatchStatus.Evaluating,
            hasAhpResult: true,
            isAhpConsistent: false,
            hasCompleteScores: true);

        Assert.False(canPerform);
    }

    [Fact]
    public void CanPerformTOPSIS_IncompleteScores_ReturnsFalse()
    {
        var canPerform = EvaluationBatchStateRules.CanPerformTOPSIS(
            BatchStatus.Evaluating,
            hasAhpResult: true,
            isAhpConsistent: true,
            hasCompleteScores: false);

        Assert.False(canPerform);
    }

    [Fact]
    public void EnsureCanPerformTOPSIS_InconsistentAHP_IncludesCrInMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanPerformTOPSIS(
                BatchStatus.Evaluating,
                hasAhpResult: true,
                isAhpConsistent: false,
                crValue: 0.1456));

        Assert.Contains("CR = 0.1456", ex.Message);
    }

    [Theory]
    [InlineData(BatchStatus.Draft, true)]
    [InlineData(BatchStatus.Evaluating, true)]
    [InlineData(BatchStatus.Completed, false)]
    [InlineData(BatchStatus.Cancelled, false)]
    public void CanCancel_AllowsOnlyDraftAndEvaluating(string status, bool expected)
    {
        Assert.Equal(expected, EvaluationBatchStateRules.CanCancel(status));
    }

    [Fact]
    public void EnsureCanCancel_WhenCompleted_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanCancel(BatchStatus.Completed));

        Assert.Contains("không thể hủy bỏ", ex.Message);
    }

    [Fact]
    public void EnsureCanCancel_WhenCancelled_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EvaluationBatchStateRules.EnsureCanCancel(BatchStatus.Cancelled));

        Assert.Contains("đã ở trạng thái hủy", ex.Message);
    }

    [Fact]
    public void EnsureCanCancel_WhenDraftOrEvaluating_Succeeds()
    {
        var ex1 = Record.Exception(() => EvaluationBatchStateRules.EnsureCanCancel(BatchStatus.Draft));
        var ex2 = Record.Exception(() => EvaluationBatchStateRules.EnsureCanCancel(BatchStatus.Evaluating));

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Theory]
    [InlineData(BatchStatus.Completed, true)]
    [InlineData(BatchStatus.Cancelled, true)]
    [InlineData(BatchStatus.Draft, false)]
    [InlineData(BatchStatus.Evaluating, false)]
    public void IsLocked_ReturnsTrueForTerminalStates(string status, bool expected)
    {
        Assert.Equal(expected, EvaluationBatchStateRules.IsLocked(status));
    }
}
