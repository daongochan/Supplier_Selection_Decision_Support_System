using System;

namespace SupplierSelectionDSS.Services;

public class AHPCalculationService
{
    // =========================================================
    // CONSTANTS
    // =========================================================

    /// <summary>
    /// Ngưỡng consistency ratio dùng để xác định ma trận nhất quán.
    /// Theo thông lệ AHP, CR <= 0.10 được xem là chấp nhận được.
    /// </summary>
    private const double ConsistencyRatioThreshold = 0.10;

    /// <summary>
    /// Sai số cho phép đối với phần tử đường chéo.
    /// Ví dụ: 0.999999 được coi là gần bằng 1.
    /// </summary>
    private const double MatrixDiagonalTolerance = 1e-6;

    /// <summary>
    /// Sai số cho phép khi kiểm tra tính reciprocal:
    /// aji ≈ 1 / aij.
    /// </summary>
    private const double ReciprocityTolerance = 1e-3;

    /// <summary>
    /// Sai số nhỏ dùng để tránh chia cho số gần bằng 0.
    /// </summary>
    private const double NumericalEpsilon = 1e-12;

    // =========================================================
    // RANDOM INDEX TABLE
    // =========================================================

    /*
     * Random Index (RI) theo bảng Saaty:
     *
     * n = 1  -> 0.00
     * n = 2  -> 0.00
     * n = 3  -> 0.58
     * n = 4  -> 0.90
     * n = 5  -> 1.12
     * n = 6  -> 1.24
     * n = 7  -> 1.32
     * n = 8  -> 1.41
     * n = 9  -> 1.45
     * n = 10 -> 1.49
     */

    private static readonly IReadOnlyDictionary<int, double> RandomIndex =
        new Dictionary<int, double>
        {
            [1] = 0.00,
            [2] = 0.00,
            [3] = 0.58,
            [4] = 0.90,
            [5] = 1.12,
            [6] = 1.24,
            [7] = 1.32,
            [8] = 1.41,
            [9] = 1.45,
            [10] = 1.49
        };

    // =========================================================
    // MAIN CALCULATION
    // =========================================================

    /// <summary>
    /// Tính trọng số AHP bằng phương pháp Geometric Mean
    /// và đánh giá tính nhất quán của ma trận.
    /// </summary>
    public AHPCalculationResult Calculate(double[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var rowCount = matrix.GetLength(0);
        var columnCount = matrix.GetLength(1);

        if (rowCount == 0 || columnCount == 0)
        {
            throw new InvalidOperationException(
                "AHP matrix không được rỗng.");
        }

        if (rowCount != columnCount)
        {
            throw new InvalidOperationException(
                "AHP matrix phải là ma trận vuông.");
        }

        var n = rowCount;

        ValidateMatrix(matrix);

        // =====================================================
        // 1. GEOMETRIC MEAN
        // =====================================================

        /*
         * Thay vì:
         *
         * product *= matrix[i, j]
         *
         * ta dùng:
         *
         * exp(average(log(matrix[i, j])))
         *
         * để giảm nguy cơ overflow/underflow.
         */

        var geometricMeans = new double[n];

        for (var i = 0; i < n; i++)
        {
            double logSum = 0.0;

            for (var j = 0; j < n; j++)
            {
                logSum += Math.Log(matrix[i, j]);
            }

            geometricMeans[i] =
                Math.Exp(logSum / n);

            if (!double.IsFinite(geometricMeans[i]) ||
                geometricMeans[i] <= NumericalEpsilon)
            {
                throw new InvalidOperationException(
                    $"Không thể tính Geometric Mean cho hàng {i + 1} của ma trận AHP.");
            }
        }

        // =====================================================
        // 2. NORMALIZE GEOMETRIC MEANS
        // =====================================================

        var totalGeometricMean = geometricMeans.Sum();

        if (!double.IsFinite(totalGeometricMean) ||
            totalGeometricMean <= NumericalEpsilon)
        {
            throw new InvalidOperationException(
                "Không thể chuẩn hóa trọng số AHP.");
        }

        var weights = new double[n];

        for (var i = 0; i < n; i++)
        {
            weights[i] =
                geometricMeans[i] /
                totalGeometricMean;

            if (!double.IsFinite(weights[i]) ||
                weights[i] <= NumericalEpsilon)
            {
                throw new InvalidOperationException(
                    $"Trọng số AHP không hợp lệ tại tiêu chí thứ {i + 1}.");
            }
        }

        // =====================================================
        // 3. WEIGHTED SUM VECTOR
        // =====================================================

        var weightedSum = new double[n];

        for (var i = 0; i < n; i++)
        {
            double sum = 0.0;

            for (var j = 0; j < n; j++)
            {
                sum += matrix[i, j] * weights[j];
            }

            if (!double.IsFinite(sum))
            {
                throw new InvalidOperationException(
                    $"Weighted Sum Vector không hợp lệ tại hàng {i + 1}.");
            }

            weightedSum[i] = sum;
        }

        // =====================================================
        // 4. CONSISTENCY VECTOR
        // =====================================================

        var consistencyVector = new double[n];

        for (var i = 0; i < n; i++)
        {
            if (Math.Abs(weights[i]) <= NumericalEpsilon)
            {
                throw new InvalidOperationException(
                    $"Không thể tính Consistency Vector tại hàng {i + 1}.");
            }

            consistencyVector[i] =
                weightedSum[i] /
                weights[i];

            if (!double.IsFinite(consistencyVector[i]))
            {
                throw new InvalidOperationException(
                    $"Consistency Vector không hợp lệ tại hàng {i + 1}.");
            }
        }

        // =====================================================
        // 5. LAMBDA MAX
        // =====================================================

        var lambdaMax =
            consistencyVector.Average();

        if (!double.IsFinite(lambdaMax))
        {
            throw new InvalidOperationException(
                "Không thể tính Lambda Max của ma trận AHP.");
        }

        // =====================================================
        // 6. CONSISTENCY INDEX (CI)
        // =====================================================

        var ci =
            n <= 1
                ? 0.0
                : (lambdaMax - n) / (n - 1);

        if (!double.IsFinite(ci))
        {
            throw new InvalidOperationException(
                "Không thể tính Consistency Index (CI).");
        }

        // =====================================================
        // 7. RANDOM INDEX (RI)
        // =====================================================

        var ri = GetRandomIndex(n);

        // =====================================================
        // 8. CONSISTENCY RATIO (CR)
        // =====================================================

        var cr =
            n <= 2 || ri <= 0.0
                ? 0.0
                : ci / ri;

        if (!double.IsFinite(cr))
        {
            throw new InvalidOperationException(
                "Không thể tính Consistency Ratio (CR).");
        }

        // =====================================================
        // 9. CONSISTENCY CHECK
        // =====================================================

        var isConsistent =
            n <= 2 ||
            cr <= ConsistencyRatioThreshold;

        // =====================================================
        // 10. RESULT
        // =====================================================

        return new AHPCalculationResult
        {
            Weights = weights,
            LambdaMax = lambdaMax,
            CIValue = ci,
            RIValue = ri,
            CRValue = cr,
            IsConsistent = isConsistent,
            N = n
        };
    }

    // =========================================================
    // MATRIX VALIDATION
    // =========================================================

    private static void ValidateMatrix(double[,] matrix)
    {
        var n = matrix.GetLength(0);

        for (var i = 0; i < n; i++)
        {
            // -------------------------------------------------
            // Validate diagonal
            // -------------------------------------------------

            var diagonalValue = matrix[i, i];

            EnsureFinite(
                diagonalValue,
                $"[{i + 1},{i + 1}]");

            if (Math.Abs(diagonalValue - 1.0) >
                MatrixDiagonalTolerance)
            {
                throw new InvalidOperationException(
                    $"AHP matrix: phần tử đường chéo [{i + 1},{i + 1}] phải bằng 1.");
            }

            // -------------------------------------------------
            // Validate value and reciprocity
            // -------------------------------------------------

            for (var j = 0; j < n; j++)
            {
                var value = matrix[i, j];

                EnsureFinite(
                    value,
                    $"[{i + 1},{j + 1}]");

                if (value <= 0)
                {
                    throw new InvalidOperationException(
                        $"AHP matrix chứa giá trị không hợp lệ tại [{i + 1},{j + 1}]. Giá trị phải lớn hơn 0.");
                }
            }
        }

        // -----------------------------------------------------
        // Reciprocity chỉ cần kiểm tra một nửa ma trận.
        //
        // aij * aji ≈ 1
        // -----------------------------------------------------

        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                var value = matrix[i, j];
                var reciprocal = matrix[j, i];

                var expected = 1.0 / value;

                if (Math.Abs(reciprocal - expected) >
                    ReciprocityTolerance)
                {
                    throw new InvalidOperationException(
                        $"Ma trận AHP không thỏa mãn tính nghịch đảo tại [{i + 1},{j + 1}] và [{j + 1},{i + 1}].");
                }
            }
        }
    }

    // =========================================================
    // RANDOM INDEX
    // =========================================================

    private static double GetRandomIndex(int n)
    {
        if (RandomIndex.TryGetValue(n, out var ri))
        {
            return ri;
        }

        throw new InvalidOperationException(
            $"AHP hiện hỗ trợ tối đa 10 Criteria cho bảng RI chuẩn (n = {n}).");
    }

    // =========================================================
    // NUMERICAL VALIDATION
    // =========================================================

    private static void EnsureFinite(
        double value,
        string location)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                $"AHP matrix chứa giá trị không hữu hạn tại {location}.");
        }
    }
}

// =============================================================
// AHP CALCULATION RESULT
// =============================================================

public class AHPCalculationResult
{
    public double[] Weights { get; init; } = Array.Empty<double>();

    public double LambdaMax { get; init; }

    public double CIValue { get; init; }

    public double RIValue { get; init; }

    public double CRValue { get; init; }

    public bool IsConsistent { get; init; }

    public int N { get; init; }
}