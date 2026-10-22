# SupplierSelectionDSS - Architecture & Technical Design Specification

## 1. Executive Summary & Domain Scope

`SupplierSelectionDSS` is an enterprise Decision Support System (DSS) designed to solve complex multi-criteria supplier selection problems by synthesizing:
1. **Analytic Hierarchy Process (AHP)**: Determination of relative criteria weights through structured pairwise comparisons and Saaty consistency evaluation.
2. **Technique for Order of Preference by Similarity to Ideal Solution (TOPSIS)**: Multi-attribute ranking of suppliers based on Euclidean distance to Positive and Negative Ideal Solutions ($PIS$ and $NIS$).
3. **Managerial Decision Workflow**: Human-in-the-loop decision-making where managers review rankings, inspect sensitivity and consistency metrics, and record formal approval or re-evaluation justifications.

---

## 2. System Architecture & Layering

The application operates as a single-project ASP.NET Core MVC application (.NET 10) backed by Entity Framework Core 10 and Microsoft SQL Server. The database schema is frozen; all optimizations respect existing table schemas and invariants.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Web / Presentation Layer                      │
│   Controllers (EvaluationBatch, Evaluation, AHP, TOPSIS, DecisionResult)│
│   Razor Views + Modular CSS (tokens, base, components, pages)           │
│   Client Scripts (site.js: double-submit, toasts, modal confirm)        │
│   Authorization Policies (PolicyNames) + ClaimsPrincipalExtensions     │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
┌────────────────────────────────────▼────────────────────────────────────┐
│                             Domain Layer                                │
│   Enums (BatchStatus, UserRole, DecisionApprovalStatus)                 │
│   State Rules (EvaluationBatchStateRules)                               │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
┌────────────────────────────────────▼────────────────────────────────────┐
│                           Application Services                          │
│   Calculation Engines: AHPCalculationService, TOPSISCalculationService  │
│   Orchestration: EvaluationBatchService, EvaluationService, AHPService, │
│                  TOPSISService, DecisionResultService, AuditLogService   │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
┌────────────────────────────────────▼────────────────────────────────────┐
│                    Infrastructure & Data Access Layer                   │
│   Exporters: ClosedXmlExcelReportExporter, QuestPdfReportExporter       │
│   ReportService (Orchestrator for exports and views)                    │
│   SupplierSelectionDssDbContext (EF Core 10)                            │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Mathematical & Algorithmic Engines

### 3.1 AHP Calculation Engine (`AHPCalculationService`)
- **Pairwise Comparison Matrix**: $A = [a_{ij}]$ where $a_{ji} = 1 / a_{ij}$ and $a_{ii} = 1$.
- **Weight Vector ($w$)**: Calculated using the geometric mean method (normalized principal eigenvector approximation).
- **Principal Eigenvalue ($\lambda_{max}$)**: $\lambda_{max} = \frac{1}{n} \sum_{i=1}^n \frac{(A w)_i}{w_i}$.
- **Consistency Index ($CI$)**: $CI = \frac{\lambda_{max} - n}{n - 1}$.
- **Random Index ($RI$)**: Extended Saaty Random Index table up to $n = 15$:
  $$\{1: 0.00, 2: 0.00, 3: 0.58, 4: 0.90, 5: 1.12, 6: 1.24, 7: 1.32, 8: 1.41, 9: 1.45, 10: 1.49, 11: 1.51, 12: 1.54, 13: 1.56, 14: 1.57, 15: 1.59\}$$
- **Consistency Ratio ($CR$)**: $CR = \frac{CI}{RI}$. The comparison is declared consistent if $CR \le 0.10$.

### 3.2 TOPSIS Calculation Engine (`TOPSISCalculationService`)
- **Evaluation Matrix**: $X = [x_{ij}]$ where $i$ indexes suppliers and $j$ indexes criteria.
- **Euclidean Normalization**:
  $$r_{ij} = \frac{x_{ij}}{\sqrt{\sum_{k=1}^m x_{kj}^2}}$$
- **Weighted Normalized Matrix**:
  $$v_{ij} = w_j \cdot r_{ij}$$
- **Ideal Solutions**:
  - **Positive Ideal Solution ($PIS$ / $A^+$)**:
    $v_j^+ = \max_i v_{ij}$ (if Benefit) or $\min_i v_{ij}$ (if Cost).
  - **Negative Ideal Solution ($NIS$ / $A^-$)**:
    $v_j^- = \min_i v_{ij}$ (if Benefit) or $\max_i v_{ij}$ (if Cost).
- **Separation Distances**:
  $$S_i^+ = \sqrt{\sum_{j=1}^n (v_{ij} - v_j^+)^2}, \quad S_i^- = \sqrt{\sum_{j=1}^n (v_{ij} - v_j^-)^2}$$
- **Relative Closeness ($C_i$)**:
  $$C_i = \frac{S_i^-}{S_i^+ + S_i^-}, \quad 0 \le C_i \le 1$$
  Suppliers are sorted in descending order of $C_i$ (higher closeness = superior supplier).

---

## 4. Workflow Lifecycle & State Machine

```
              ┌────────────────────────┐
              │         Draft          │
              └───────────┬────────────┘
                          │ StartAsync() (Requires >= 2 suppliers, >= 2 criteria)
                          ▼
              ┌────────────────────────┐
   ┌─────────►│       Evaluating       │◄────────┐
   │          └─────┬────────────┬─────┘         │
   │                │            │               │ ReevaluateAsync()
   │  Save Scores / │            │ ApproveAsync()│
   │  AHP Weights   │            │               │
   │  (Invalidates  │            ▼               │
   │   TOPSIS)      │  ┌──────────────────┐      │
   │                │  │    Completed     │──────┘
   │                │  └──────────────────┘
   │                │ CancelAsync()
   │                ▼
   │          ┌────────────────────────┐
   └──────────│       Cancelled        │
              └────────────────────────┘
```

### 4.1 State Invariant Rules (`EvaluationBatchStateRules`)
1. **CanStart**: Only batches in `Draft` state with $\ge 2$ suppliers and $\ge 2$ criteria can transition to `Evaluating`.
2. **CanEvaluate**: Only batches in `Evaluating` state accept score entries.
3. **CanPerformTOPSIS**: Requires `Evaluating` state, complete score matrix, and AHP calculated with $CR \le 0.10$.
4. **CanCancel**: Only `Draft` and `Evaluating` batches can be cancelled. `Completed` and `Cancelled` batches are terminal and locked.
5. **Cascade Invalidation**:
   - Whenever evaluation scores are edited/saved (`EvaluationService.SaveAsync`), all existing `TopsisResults` and `TopsisIdealSolutions` for the batch are deleted.
   - Whenever AHP pairwise comparisons or weights are updated (`AHPService.SaveComparisonsAsync`, `AHPService.CalculateAsync`), existing `TopsisResults` and `TopsisIdealSolutions` are deleted.
   - This prevents stale or mathematically inconsistent rankings from persisting into decision-making.

---

## 5. Security & Authorization Architecture

### 5.1 Claims Principal Extensions (`Web/Extensions/ClaimsPrincipalExtensions.cs`)
Eliminates redundant claim string lookups across all controllers:
- `User.GetUserId()`: Parses integer User ID safely.
- `User.GetDisplayName()`: Retrieves GivenName or Name fallback.
- `User.GetRole()`: Retrieves ClaimTypes.Role.

### 5.2 Declarative Policies (`Web/Authorization/PolicyNames.cs`)
Registered centrally in `Program.cs`:
- `PolicyNames.BatchManage`: Administrator, PurchasingStaff.
- `PolicyNames.EvaluationManage`: Administrator, PurchasingStaff, Expert.
- `PolicyNames.AhpManage`: Administrator, Expert.
- `PolicyNames.TopsisManage`: Administrator, Expert.
- `PolicyNames.DecisionApprove`: Administrator, Manager.
- `PolicyNames.ReportView`: Administrator, Manager, PurchasingStaff.
- `PolicyNames.AuditLogView`: Administrator.
- `PolicyNames.UserManage`: Administrator.

---

## 6. Reporting & Export Infrastructure

The previous monolithic `ReportService` (~1,800 lines) was refactored by extracting export format concerns into dedicated strategy classes:
- **`Infrastructure/Exports/ClosedXmlExcelReportExporter.cs`**: Generates high-fidelity Excel workbooks with styled summary metadata, criteria weight tables, evaluation matrices, TOPSIS rankings, and formal decision signatures.
- **`Infrastructure/Exports/QuestPdfReportExporter.cs`**: Produces publication-grade PDF documents with vector charts, page numbers, running headers, and audit stamps.
- **`Services/ReportService.cs`**: Acts strictly as the orchestrator, fetching and compiling `ReportData` and delegating binary output generation to the corresponding exporter.

---

## 7. UI/UX Design System & Client Controls

- **Design System Tokens (`tokens.css`)**: Typography (`Source Serif 4`), semantic palette (`--terracotta`, `--sage`, `--lavender`, `--sky`, `--amber`, `--danger`).
- **Sidebar Navigation (`_Sidebar.cshtml`)**: Semantic Bootstrap Icons applied to all navigational links with active route highlighting.
- **Breadcrumb System (`_Breadcrumb.cshtml`)**: Automatic contextual hierarchy displayed on non-dashboard pages.
- **Enterprise Footer (`_Footer.cshtml`)**: Consistent branding, versioning, and copyright notice.
- **Toast Notifications (`_Toast.cshtml`)**: Floating alert stack supporting Success, Danger, Warning, and Info with auto-dismiss after 5 seconds.
- **Confirmation Modal (`_ConfirmationModal.cshtml`)**: Modal dialog for critical DSS operations.
- **Double-Submit Prevention (`site.js`)**: Automatic disablement and spinner activation on form submissions to protect computation integrity.
