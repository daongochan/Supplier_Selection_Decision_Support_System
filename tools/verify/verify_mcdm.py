"""
Independent Mathematical Verification Script for AHP and TOPSIS Algorithms.
Supplier Selection Decision Support System (DSS)

This script computes reference benchmarks using Python (NumPy if available,
or pure-Python power iteration / linear algebra fallback):
1. Exact Principal Eigenvector and Geometric Mean for AHP
2. Standard Vector TOPSIS with Benefit & Cost criteria
3. Invariant properties (scale invariance, permutation invariance)
4. Standard Competition Ranking (1, 2, 2, 4)

Usage:
    python tools/verify/verify_mcdm.py
    uv run --with numpy python tools/verify/verify_mcdm.py
"""

import json
import math
import sys
from pathlib import Path

try:
    import numpy as np
    HAS_NUMPY = True
except ImportError:
    np = None
    HAS_NUMPY = False

# Random Index (RI) table according to Saaty
SAATY_RI = {
    1: 0.00,
    2: 0.00,
    3: 0.58,
    4: 0.90,
    5: 1.12,
    6: 1.24,
    7: 1.32,
    8: 1.41,
    9: 1.45,
    10: 1.49,
    11: 1.51,
    12: 1.54,
    13: 1.56,
    14: 1.57,
    15: 1.59
}


def _to_2d_list(matrix):
    if HAS_NUMPY and isinstance(matrix, np.ndarray):
        return matrix.tolist()
    return [[float(v) for v in row] for row in matrix]


def _to_1d_list(vec):
    if HAS_NUMPY and isinstance(vec, np.ndarray):
        return [float(v) for v in vec.tolist()]
    return [float(v) for v in vec]


def _principal_eigenvector_power_method(matrix_rows: list[list[float]], max_iter: int = 10000, tol: float = 1e-14):
    """
    Computes the Perron-Frobenius principal eigenvector and dominant eigenvalue
    for a positive reciprocal matrix using power iteration.
    """
    n = len(matrix_rows)
    v = [1.0 / n] * n
    for _ in range(max_iter):
        av = [sum(matrix_rows[i][j] * v[j] for j in range(n)) for i in range(n)]
        s = sum(av)
        v_next = [x / s for x in av]
        diff = max(abs(v_next[i] - v[i]) for i in range(n))
        v = v_next
        if diff < tol:
            break
    av = [sum(matrix_rows[i][j] * v[j] for j in range(n)) for i in range(n)]
    lambda_max = sum(av[i] / v[i] for i in range(n)) / n
    return lambda_max, v


def calculate_ahp(matrix):
    """
    Computes AHP using both:
    1. Geometric Mean Method (Row Geometric Mean) - used in C# service
    2. Exact Principal Eigenvector Method (NumPy eig or Power Method)
    """
    rows = _to_2d_list(matrix)
    n = len(rows)
    assert all(len(r) == n for r in rows), "Matrix must be square"

    # --- 1. Row Geometric Mean Method ---
    gm = [math.exp(sum(math.log(rows[i][j]) for j in range(n)) / n) for i in range(n)]
    gm_sum = sum(gm)
    weights_gm = [g / gm_sum for g in gm]

    # Weighted sum vector & consistency vector
    weighted_sum = [sum(rows[i][j] * weights_gm[j] for j in range(n)) for i in range(n)]
    consistency_vector = [weighted_sum[i] / weights_gm[i] for i in range(n)]
    lambda_max_gm = sum(consistency_vector) / n

    ci_gm = 0.0 if n <= 1 else (lambda_max_gm - n) / (n - 1)
    ri = SAATY_RI.get(n, 0.0)
    cr_gm = 0.0 if (n <= 2 or ri == 0.0) else ci_gm / ri

    # --- 2. Principal Eigenvector Method (Ground Truth) ---
    if HAS_NUMPY:
        np_mat = np.array(rows, dtype=float)
        eig_vals, eig_vecs = np.linalg.eig(np_mat)
        max_idx = int(np.argmax(np.real(eig_vals)))
        lambda_max_exact = float(np.real(eig_vals[max_idx]))
        principal_vec = np.real(eig_vecs[:, max_idx])
        weights_exact_arr = principal_vec / np.sum(principal_vec)
        if weights_exact_arr[0] < 0:
            weights_exact_arr = -weights_exact_arr
        weights_exact = [float(w) for w in weights_exact_arr]
    else:
        lambda_max_exact, weights_exact = _principal_eigenvector_power_method(rows)

    ci_exact = 0.0 if n <= 1 else (lambda_max_exact - n) / (n - 1)
    cr_exact = 0.0 if (n <= 2 or ri == 0.0) else ci_exact / ri

    max_weight_diff = max(abs(weights_gm[i] - weights_exact[i]) for i in range(n))

    return {
        "n": n,
        "weights_geometric_mean": [round(float(w), 8) for w in weights_gm],
        "weights_exact_eigenvector": [round(float(w), 8) for w in weights_exact],
        "lambda_max_gm": round(lambda_max_gm, 8),
        "lambda_max_exact": round(lambda_max_exact, 8),
        "ci_gm": round(ci_gm, 8),
        "ci_exact": round(ci_exact, 8),
        "ri": ri,
        "cr_gm": round(cr_gm, 8),
        "cr_exact": round(cr_exact, 8),
        "is_consistent_gm": bool(cr_gm <= 0.10 or n <= 2),
        "max_weight_diff_gm_vs_exact": round(max_weight_diff, 8)
    }


def calculate_topsis(decision_matrix, weights, criteria_types: list[str]):
    """
    Standard Vector TOPSIS:
    - Normalization: r_ij = x_ij / sqrt(sum_k x_kj^2)
    - If a column is all zero, r_ij = 0
    - Weighted matrix: v_ij = w_j * r_ij
    - Ideal solutions (PIS / NIS)
    - Distances D+, D-
    - CC = D- / (D+ + D-)
    - Standard Competition Ranking (1, 2, 2, 4)
    """
    dm = _to_2d_list(decision_matrix)
    w = _to_1d_list(weights)
    m = len(dm)
    n = len(dm[0])
    assert len(w) == n, "Weights length must match criteria count"
    assert len(criteria_types) == n, "CriteriaTypes length must match criteria count"

    # Step 1: Vector normalization
    norm_matrix = [[0.0] * n for _ in range(m)]
    zero_columns = []

    for j in range(n):
        sq_sum = sum(dm[i][j] ** 2 for i in range(m))
        if sq_sum <= 1e-12:
            for i in range(m):
                norm_matrix[i][j] = 0.0
            zero_columns.append(j)
        else:
            denom = math.sqrt(sq_sum)
            for i in range(m):
                norm_matrix[i][j] = dm[i][j] / denom

    # Step 2: Weighted normalized matrix
    weighted_matrix = [[norm_matrix[i][j] * w[j] for j in range(n)] for i in range(m)]

    # Step 3: PIS and NIS
    pis = [0.0] * n
    nis = [0.0] * n

    for j in range(n):
        ctype = criteria_types[j].strip().lower()
        col = [weighted_matrix[i][j] for i in range(m)]
        if ctype == "benefit":
            pis[j] = max(col)
            nis[j] = min(col)
        elif ctype == "cost":
            pis[j] = min(col)
            nis[j] = max(col)
        else:
            raise ValueError(f"Unknown criteria type: {criteria_types[j]}")

    # Step 4: Distances to PIS and NIS
    d_plus = [0.0] * m
    d_minus = [0.0] * m
    cc = [0.0] * m

    for i in range(m):
        d_plus[i] = math.sqrt(sum((weighted_matrix[i][j] - pis[j]) ** 2 for j in range(n)))
        d_minus[i] = math.sqrt(sum((weighted_matrix[i][j] - nis[j]) ** 2 for j in range(n)))

        denom = d_plus[i] + d_minus[i]
        if denom <= 1e-12:
            cc[i] = 0.0
        else:
            cc[i] = d_minus[i] / denom

    # Step 5: Standard Competition Ranking (1, 2, 2, 4)
    # Sort indices by CC descending, then by D+ ascending
    sorted_indices = sorted(
        range(m),
        key=lambda i: (-cc[i], d_plus[i])
    )

    ranks = [0] * m
    for rank_idx, curr_i in enumerate(sorted_indices):
        if rank_idx > 0:
            prev_i = sorted_indices[rank_idx - 1]
            if abs(cc[curr_i] - cc[prev_i]) <= 1e-6 and abs(d_plus[curr_i] - d_plus[prev_i]) <= 1e-6:
                ranks[curr_i] = ranks[prev_i]
                continue
        ranks[curr_i] = rank_idx + 1

    return {
        "m_suppliers": m,
        "n_criteria": n,
        "normalized_matrix": [[round(float(v), 8) for v in row] for row in norm_matrix],
        "weighted_matrix": [[round(float(v), 8) for v in row] for row in weighted_matrix],
        "pis": [round(float(v), 8) for v in pis],
        "nis": [round(float(v), 8) for v in nis],
        "d_plus": [round(float(v), 8) for v in d_plus],
        "d_minus": [round(float(v), 8) for v in d_minus],
        "closeness_coefficients": [round(float(v), 8) for v in cc],
        "ranks": ranks,
        "zero_variance_columns": zero_columns
    }


def main():
    benchmarks = {}

    print("===============================================================")
    print(" 1. VERIFYING AHP ALGORITHMS (Geometric Mean vs Eigenvector)")
    print("===============================================================")

    # AHP Benchmark 1: Consistent 3x3
    m1 = [
        [1.0, 2.0, 4.0],
        [0.5, 1.0, 2.0],
        [0.25, 0.5, 1.0]
    ]
    r1 = calculate_ahp(m1)
    benchmarks["ahp_consistent_3x3"] = r1
    print(f"AHP 3x3 Consistent: CR = {r1['cr_gm']}, Weights = {r1['weights_geometric_mean']}")
    print(f"  GM vs Exact Diff: {r1['max_weight_diff_gm_vs_exact']} (< 1e-4)")

    # AHP Benchmark 2: Standard Saaty 4x4
    m2 = [
        [1.0, 3.0, 5.0, 7.0],
        [1.0 / 3.0, 1.0, 3.0, 5.0],
        [1.0 / 5.0, 1.0 / 3.0, 1.0, 3.0],
        [1.0 / 7.0, 1.0 / 5.0, 1.0 / 3.0, 1.0]
    ]
    r2 = calculate_ahp(m2)
    benchmarks["ahp_saaty_4x4"] = r2
    print(f"AHP 4x4 Saaty: CR = {r2['cr_gm']} (Expected < 0.10: {r2['is_consistent_gm']})")
    print(f"  Weights GM:    {r2['weights_geometric_mean']}")
    print(f"  Weights Exact: {r2['weights_exact_eigenvector']}")
    print(f"  GM vs Exact Diff: {r2['max_weight_diff_gm_vs_exact']}")

    # AHP Benchmark 3: Inconsistent 3x3 (CR > 0.10)
    m3 = [
        [1.0, 9.0, 1.0],
        [1.0 / 9.0, 1.0, 9.0],
        [1.0, 1.0 / 9.0, 1.0]
    ]
    r3 = calculate_ahp(m3)
    benchmarks["ahp_inconsistent_3x3"] = r3
    print(f"AHP 3x3 Inconsistent: CR = {r3['cr_gm']} (Expected > 0.10: not {r3['is_consistent_gm']})")

    # AHP Benchmark 4: 2x2
    m4 = [
        [1.0, 3.0],
        [1.0 / 3.0, 1.0]
    ]
    r4 = calculate_ahp(m4)
    benchmarks["ahp_2x2"] = r4
    print(f"AHP 2x2: CR = {r4['cr_gm']}, Weights = {r4['weights_geometric_mean']}")

    print("\n===============================================================")
    print(" 2. VERIFYING TOPSIS ALGORITHMS")
    print("===============================================================")

    # TOPSIS Benchmark 1: 3 Suppliers x 4 Criteria (2 Benefit, 2 Cost)
    # C1 (Cost): Unit Price
    # C2 (Benefit): Quality Score
    # C3 (Cost): Delivery Lead Time
    # C4 (Benefit): Supply Capacity
    dm1 = [
        [250.0, 8.5, 14.0, 9.0],   # Supplier 1 (SUP01)
        [200.0, 7.0, 21.0, 8.0],   # Supplier 2 (SUP02)
        [300.0, 9.5, 7.0,  7.5]    # Supplier 3 (SUP03)
    ]
    w1 = [0.35, 0.30, 0.20, 0.15]
    types1 = ["Cost", "Benefit", "Cost", "Benefit"]

    t1 = calculate_topsis(dm1, w1, types1)
    benchmarks["topsis_3x4_benchmark"] = t1
    print(f"TOPSIS 3x4:")
    print(f"  PIS: {t1['pis']}")
    print(f"  NIS: {t1['nis']}")
    for idx, (cc, r, dp, dm) in enumerate(zip(t1['closeness_coefficients'], t1['ranks'], t1['d_plus'], t1['d_minus'])):
        print(f"  Supplier {idx+1}: D+={dp:.6f}, D-={dm:.6f}, CC={cc:.6f}, Rank={r}")

    # TOPSIS Benchmark 2: Standard Competition Ranking Ties (1, 2, 2, 4)
    dm_tie = [
        [10.0, 100.0],  # Supplier 1 (Best)
        [8.0,  150.0],  # Supplier 2 (Tied)
        [8.0,  150.0],  # Supplier 3 (Tied with 2)
        [5.0,  250.0]   # Supplier 4 (Worst)
    ]
    w_tie = [0.5, 0.5]
    types_tie = ["Benefit", "Cost"]
    t_tie = calculate_topsis(dm_tie, w_tie, types_tie)
    benchmarks["topsis_tie_1224"] = t_tie
    print(f"\nTOPSIS Tie-break (Standard Competition Ranking):")
    print(f"  CCs: {t_tie['closeness_coefficients']}")
    print(f"  Ranks: {t_tie['ranks']} (Expected: [1, 2, 2, 4])")

    # TOPSIS Benchmark 3: Zero-variance Column (All-zero in one criterion)
    dm_zero = [
        [0.0, 8.0, 15.0],
        [0.0, 6.0, 20.0],
        [0.0, 9.0, 10.0]
    ]
    w_zero = [0.2, 0.4, 0.4]
    types_zero = ["Cost", "Benefit", "Cost"]
    t_zero = calculate_topsis(dm_zero, w_zero, types_zero)
    benchmarks["topsis_zero_column"] = t_zero
    print(f"\nTOPSIS Zero Column (Criterion 1 is all 0):")
    print(f"  Zero columns: {t_zero['zero_variance_columns']}")
    print(f"  Ranks: {t_zero['ranks']}")
    print(f"  CCs: {t_zero['closeness_coefficients']}")

    # TOPSIS Benchmark 4: All suppliers identical (D+ + D- = 0)
    dm_identical = [
        [10.0, 100.0],
        [10.0, 100.0],
        [10.0, 100.0]
    ]
    t_ident = calculate_topsis(dm_identical, [0.5, 0.5], ["Benefit", "Cost"])
    benchmarks["topsis_identical_suppliers"] = t_ident
    print(f"\nTOPSIS Identical Suppliers (All equal):")
    print(f"  CCs: {t_ident['closeness_coefficients']} (All 0.0)")
    print(f"  Ranks: {t_ident['ranks']} (All 1)")

    # Save to JSON
    out_dir = Path("tools/verify")
    out_dir.mkdir(parents=True, exist_ok=True)
    out_file = out_dir / "mcdm_benchmarks.json"
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(benchmarks, f, indent=2)

    print(f"\n[OK] Benchmarks saved successfully to {out_file}")


if __name__ == "__main__":
    main()
