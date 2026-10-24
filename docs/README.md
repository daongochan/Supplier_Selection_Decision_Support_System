# Documentation Index — Supplier Selection DSS

Welcome to the documentation suite for the **Supplier Selection Decision Support System (Supplier Selection DSS)**.

---

## 1. Active Canonical Documentation

These documents reflect the current operational state of the codebase:

| Document | Scope & Contents |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | High-level system architecture, technology stack (.NET 10, EF Core, SQL Server), project layout, and data model relationships. |
| [ALGORITHMS.md](ALGORITHMS.md) | Mathematical formulas and computational logic for **AHP** (pairwise comparison, geometric mean, $\lambda_{\max}$, consistency ratio $CR$) and **TOPSIS** (vector normalization, weighted matrix, PIS/NIS Euclidean distances, closeness coefficient $C_i$, sensitivity analysis). |
| [WORKFLOW-AND-PERMISSIONS.md](WORKFLOW-AND-PERMISSIONS.md) | Role-Based Access Control matrix (Administrator, PurchasingStaff, Expert, Manager), evaluation batch lifecycle states (`Draft` $\rightarrow$ `Evaluating` $\rightarrow$ `Completed` / `Cancelled`), and concurrency invariants. |
| [DESIGN-SYSTEM.md](DESIGN-SYSTEM.md) | Design tokens, typography (Source Serif 4 + System Sans), color palettes, core components, visual asset fallbacks (`duck.jpg`, WebP/PNG/JPG), and deterministic zero-waterfall CSS pipeline. |
| [PERFORMANCE.md](PERFORMANCE.md) | Benchmark methodology, index optimizations, pagination, caching strategy, and benchmark database (`SupplierSelectionDSS_Perf`) setup. |

---

## 2. Quality Assurance & Testing Suite (`docs/qa/`)

| Document | Scope & Contents |
|---|---|
| [qa/TEST_PLAN.md](qa/TEST_PLAN.md) | Comprehensive test strategy, test categories (Unit, Integration, SQL Server, Parity, Security), and CI/CD gates. |
| [qa/TEST_MATRIX.md](qa/TEST_MATRIX.md) | End-to-end test matrix covering 14 functional and non-functional areas (Auth, RBAC, Dashboard, Master Data, Scoring, AHP, TOPSIS, Sensitivity, Decision, Reporting, and Error Handling). |
| [qa/SCREEN_INVENTORY.md](qa/SCREEN_INVENTORY.md) | Complete inventory of user interfaces, Razor views, route bindings, and authorized roles. |
| [qa/BLANK_SCREEN_REPORT.md](qa/BLANK_SCREEN_REPORT.md) | Audit analysis and permanent remediation of HTTP 400-599 empty-body status code handling. |
| [qa/evidence/](qa/evidence/) | Historical performance and crawler run artifacts. |

---

## 3. Architecture Proposals (`docs/proposals/`)

| Proposal | Status | Description |
|---|---|---|
| [proposals/GROUP-AHP-PROPOSAL.md](proposals/GROUP-AHP-PROPOSAL.md) | **PROPOSAL (Not Implemented)** | Research specification for Multi-Expert Group AHP using Geometric Mean (AIJ) to aggregate judgments across multiple committee members. The system currently uses a single shared AHP matrix per evaluation batch. |

---

## 4. Archive (`docs/archive/`)

Contains historical point-in-time documents, completed implementation plans, superseded UI drafts, and earlier audit reports preserved for auditability. See [docs/archive/](archive/).
