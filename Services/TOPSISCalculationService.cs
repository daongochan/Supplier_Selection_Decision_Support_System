using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class TOPSISCalculationService
{
    // =========================================================
    // CONSTANTS
    // =========================================================

    /// <summary>
    /// Sai số cho phép khi kiểm tra tổng trọng số AHP.
    /// Trọng số AHP đã được chuẩn hóa nên tổng phải xấp xỉ 1.
    /// </summary>
    private const double WeightSumTolerance = 1e-4;

    /// <summary>
    /// Ngưỡng số học dùng để tránh chia cho số quá nhỏ.
    /// </summary>
    private const double NumericalEpsilon = 1e-12;

    // =========================================================
    // MAIN CALCULATION
    // =========================================================

    /// <summary>
    /// Thực hiện đầy đủ quy trình TOPSIS:
    ///
    /// 1. Chuẩn hóa ma trận quyết định
    /// 2. Tạo ma trận chuẩn hóa có trọng số
    /// 3. Xác định PIS / NIS
    /// 4. Tính khoảng cách tới PIS / NIS
    /// 5. Tính Closeness Coefficient
    /// 6. Xếp hạng Supplier
    /// </summary>
    public TOPSISCalculationResult Calculate(
        double[,] decisionMatrix,
        double[] weights,
        string[] criteriaTypes)
    {
        ArgumentNullException.ThrowIfNull(decisionMatrix);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(criteriaTypes);

        var supplierCount = decisionMatrix.GetLength(0);
        var criteriaCount = decisionMatrix.GetLength(1);

        // =====================================================
        // BASIC VALIDATION
        // =====================================================

        if (supplierCount < 2)
        {
            throw new InvalidOperationException(
                "Đánh giá TOPSIS yêu cầu tối thiểu 2 nhà cung cấp để so sánh xếp hạng.");
        }

        if (criteriaCount == 0)
        {
            throw new InvalidOperationException(
                "Ma trận TOPSIS không được rỗng tiêu chí.");
        }

        if (weights.Length != criteriaCount)
        {
            throw new InvalidOperationException(
                "Số lượng trọng số AHP không khớp số Criteria.");
        }

        if (criteriaTypes.Length != criteriaCount)
        {
            throw new InvalidOperationException(
                "Số lượng CriteriaType không khớp số Criteria.");
        }

        ValidateWeights(weights);
        ValidateCriteriaTypes(criteriaTypes);
        ValidateDecisionMatrix(decisionMatrix);

        // =====================================================
        // 1. VECTOR NORMALIZATION
        //
        // r_ij = x_ij / sqrt(sum(x_ij^2))
        //
        // Sử dụng scaling theo maxAbs để giảm nguy cơ overflow
        // khi x_ij có giá trị rất lớn.
        // =====================================================

        var normalized =
            new double[supplierCount, criteriaCount];
        var zeroVarianceCriteria = new List<int>();

        for (var j = 0; j < criteriaCount; j++)
        {
            double maxAbsoluteValue = 0.0;

            for (var i = 0; i < supplierCount; i++)
            {
                var value = decisionMatrix[i, j];

                var absoluteValue = Math.Abs(value);

                if (absoluteValue > maxAbsoluteValue)
                {
                    maxAbsoluteValue = absoluteValue;
                }
            }

            if (maxAbsoluteValue <= NumericalEpsilon)
            {
                // Cột toàn 0: không ném lỗi mà chuẩn hóa về 0,
                // ghi nhận tiêu chí không có khả năng phân biệt giữa các nhà cung cấp
                zeroVarianceCriteria.Add(j);
                for (var i = 0; i < supplierCount; i++)
                {
                    normalized[i, j] = 0.0;
                }
                continue;
            }

            double scaledSquaredSum = 0.0;

            for (var i = 0; i < supplierCount; i++)
            {
                var scaledValue =
                    decisionMatrix[i, j] /
                    maxAbsoluteValue;

                scaledSquaredSum +=
                    scaledValue * scaledValue;
            }

            if (!double.IsFinite(scaledSquaredSum) ||
                scaledSquaredSum <= NumericalEpsilon)
            {
                throw new InvalidOperationException(
                    $"Không thể chuẩn hóa Criteria thứ {j + 1}.");
            }

            var denominator =
                Math.Sqrt(scaledSquaredSum);

            for (var i = 0; i < supplierCount; i++)
            {
                var scaledValue =
                    decisionMatrix[i, j] /
                    maxAbsoluteValue;

                normalized[i, j] =
                    scaledValue /
                    denominator;

                EnsureFinite(
                    normalized[i, j],
                    $"Normalized Matrix [{i + 1},{j + 1}]");
            }
        }

        // =====================================================
        // 2. WEIGHTED NORMALIZED MATRIX
        //
        // v_ij = w_j * r_ij
        // =====================================================

        var weighted =
            new double[supplierCount, criteriaCount];

        for (var i = 0; i < supplierCount; i++)
        {
            for (var j = 0; j < criteriaCount; j++)
            {
                weighted[i, j] =
                    normalized[i, j] *
                    weights[j];

                EnsureFinite(
                    weighted[i, j],
                    $"Weighted Matrix [{i + 1},{j + 1}]");
            }
        }

        // =====================================================
        // 3. PIS / NIS
        // =====================================================

        var pis =
            new double[criteriaCount];

        var nis =
            new double[criteriaCount];

        for (var j = 0; j < criteriaCount; j++)
        {
            var criteriaType =
                criteriaTypes[j].Trim();

            var columnValues =
                Enumerable
                    .Range(0, supplierCount)
                    .Select(i => weighted[i, j])
                    .ToArray();

            if (string.Equals(
                criteriaType,
                CriteriaType.Benefit,
                StringComparison.OrdinalIgnoreCase))
            {
                // Benefit:
                // PIS = maximum
                // NIS = minimum

                pis[j] =
                    columnValues.Max();

                nis[j] =
                    columnValues.Min();
            }
            else if (string.Equals(
                criteriaType,
                CriteriaType.Cost,
                StringComparison.OrdinalIgnoreCase))
            {
                // Cost:
                // PIS = minimum
                // NIS = maximum

                pis[j] =
                    columnValues.Min();

                nis[j] =
                    columnValues.Max();
            }
            else
            {
                // Trường hợp này đã được kiểm tra trước đó,
                // nhưng giữ guard để bảo vệ calculation engine.
                throw new InvalidOperationException(
                    $"CriteriaType '{criteriaType}' không hợp lệ. " +
                    $"Chỉ hỗ trợ '{CriteriaType.Benefit}' hoặc '{CriteriaType.Cost}'.");
            }

            EnsureFinite(
                pis[j],
                $"PIS[{j + 1}]");

            EnsureFinite(
                nis[j],
                $"NIS[{j + 1}]");
        }

        // =====================================================
        // 4. DISTANCE TO PIS / NIS
        //
        // D+ = sqrt(sum((vij - PISj)^2))
        // D- = sqrt(sum((vij - NISj)^2))
        //
        // CC = D- / (D+ + D-)
        // =====================================================

        var distances =
            new TOPSISSupplierDistance[supplierCount];

        for (var i = 0; i < supplierCount; i++)
        {
            double squaredDistanceToPIS = 0.0;
            double squaredDistanceToNIS = 0.0;

            for (var j = 0; j < criteriaCount; j++)
            {
                var deltaPIS =
                    weighted[i, j] -
                    pis[j];

                var deltaNIS =
                    weighted[i, j] -
                    nis[j];

                squaredDistanceToPIS +=
                    deltaPIS * deltaPIS;

                squaredDistanceToNIS +=
                    deltaNIS * deltaNIS;
            }

            if (!double.IsFinite(squaredDistanceToPIS) ||
                !double.IsFinite(squaredDistanceToNIS))
            {
                throw new InvalidOperationException(
                    $"Khoảng cách TOPSIS không hợp lệ đối với Supplier thứ {i + 1}.");
            }

            var distanceToPIS =
                Math.Sqrt(squaredDistanceToPIS);

            var distanceToNIS =
                Math.Sqrt(squaredDistanceToNIS);

            EnsureFinite(
                distanceToPIS,
                $"DistanceToPIS[{i + 1}]");

            EnsureFinite(
                distanceToNIS,
                $"DistanceToNIS[{i + 1}]");

            var denominator =
                distanceToPIS +
                distanceToNIS;

            double closenessCoefficient;

            if (denominator <= NumericalEpsilon)
            {
                /*
                 * Trường hợp Supplier nằm đồng thời tại PIS và NIS.
                 * Khi tất cả Supplier có cùng điểm trên mọi Criteria,
                 * khoảng cách đến PIS và NIS đều bằng 0.
                 *
                 * Giữ CC = 0 để tránh chia cho 0.
                 */
                closenessCoefficient = 0.0;
            }
            else
            {
                closenessCoefficient =
                    distanceToNIS /
                    denominator;
            }

            EnsureFinite(
                closenessCoefficient,
                $"ClosenessCoefficient[{i + 1}]");

            distances[i] =
                new TOPSISSupplierDistance
                {
                    DistanceToPIS = distanceToPIS,
                    DistanceToNIS = distanceToNIS,
                    ClosenessCoefficient = closenessCoefficient
                };
        }

        // =====================================================
        // 5. RANKING (Standard Competition Ranking: 1, 2, 2, 4)
        // =====================================================

        var orderedIndexes =
            Enumerable
                .Range(0, supplierCount)
                .OrderByDescending(
                    i =>
                        distances[i]
                            .ClosenessCoefficient)
                .ThenBy(
                    i =>
                        distances[i]
                            .DistanceToPIS)
                .ToArray();

        const double tieTolerance = 1e-6;
        var rankByIndex = new int[supplierCount];
        var hasTies = false;

        for (var rank = 0; rank < orderedIndexes.Length; rank++)
        {
            var currentIndex = orderedIndexes[rank];
            if (rank > 0)
            {
                var prevIndex = orderedIndexes[rank - 1];
                var prevDistance = distances[prevIndex];
                var currentDistance = distances[currentIndex];

                var ccEqual = Math.Abs(prevDistance.ClosenessCoefficient - currentDistance.ClosenessCoefficient) <= tieTolerance;
                var pisEqual = Math.Abs(prevDistance.DistanceToPIS - currentDistance.DistanceToPIS) <= tieTolerance;

                if (ccEqual && pisEqual)
                {
                    rankByIndex[currentIndex] = rankByIndex[prevIndex];
                    hasTies = true;
                    continue;
                }
            }

            rankByIndex[currentIndex] = rank + 1;
        }

        var isIndistinguishable = distances.All(d => d.DistanceToPIS <= NumericalEpsilon && d.DistanceToNIS <= NumericalEpsilon);

        // =====================================================
        // 6. RESULT
        // =====================================================

        return new TOPSISCalculationResult
        {
            NormalizedMatrix = normalized,
            WeightedMatrix = weighted,
            PISValues = pis,
            NISValues = nis,
            Distances = distances,
            RankByIndex = rankByIndex,
            ZeroVarianceCriteriaIndices = zeroVarianceCriteria,
            HasTies = hasTies,
            IsIndistinguishable = isIndistinguishable
        };
    }

    // =========================================================
    // VALIDATE WEIGHTS
    // =========================================================

    private static void ValidateWeights(
        double[] weights)
    {
        if (weights.Length == 0)
        {
            throw new InvalidOperationException(
                "Danh sách trọng số AHP không được rỗng.");
        }

        double weightSum = 0.0;

        for (var i = 0; i < weights.Length; i++)
        {
            var weight = weights[i];

            EnsureFinite(
                weight,
                $"Weight[{i + 1}]");

            if (weight <= 0)
            {
                throw new InvalidOperationException(
                    $"Trọng số AHP tại vị trí {i + 1} phải lớn hơn 0.");
            }

            weightSum += weight;
        }

        if (!double.IsFinite(weightSum) ||
            weightSum <= NumericalEpsilon)
        {
            throw new InvalidOperationException(
                "Tổng trọng số AHP phải lớn hơn 0.");
        }

        /*
         * Không tự động normalize lại trọng số.
         *
         * TOPSIS phải sử dụng đúng trọng số được sinh bởi AHP.
         */

        if (Math.Abs(weightSum - 1.0) >
            WeightSumTolerance)
        {
            throw new InvalidOperationException(
                $"Tổng trọng số AHP phải xấp xỉ 1. " +
                $"Hiện tại = {weightSum:0.000000}.");
        }
    }

    // =========================================================
    // VALIDATE CRITERIA TYPES
    // =========================================================

    private static void ValidateCriteriaTypes(
        string[] criteriaTypes)
    {
        for (var i = 0; i < criteriaTypes.Length; i++)
        {
            var criteriaType =
                criteriaTypes[i]?.Trim();

            if (string.IsNullOrWhiteSpace(criteriaType))
            {
                throw new InvalidOperationException(
                    $"CriteriaType tại vị trí {i + 1} không được để trống.");
            }

            var isBenefit =
                string.Equals(
                    criteriaType,
                    CriteriaType.Benefit,
                    StringComparison.OrdinalIgnoreCase);

            var isCost =
                string.Equals(
                    criteriaType,
                    CriteriaType.Cost,
                    StringComparison.OrdinalIgnoreCase);

            if (!isBenefit && !isCost)
            {
                throw new InvalidOperationException(
                    $"CriteriaType '{criteriaType}' không hợp lệ. " +
                    $"Chỉ hỗ trợ '{CriteriaType.Benefit}' hoặc '{CriteriaType.Cost}'.");
            }
        }
    }

    // =========================================================
    // VALIDATE DECISION MATRIX
    // =========================================================

    private static void ValidateDecisionMatrix(
        double[,] decisionMatrix)
    {
        var supplierCount =
            decisionMatrix.GetLength(0);

        var criteriaCount =
            decisionMatrix.GetLength(1);

        for (var i = 0; i < supplierCount; i++)
        {
            for (var j = 0; j < criteriaCount; j++)
            {
                var value =
                    decisionMatrix[i, j];

                EnsureFinite(
                    value,
                    $"Decision Matrix [{i + 1},{j + 1}]");
            }
        }
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
                $"Giá trị TOPSIS không hữu hạn tại {location}.");
        }
    }
}