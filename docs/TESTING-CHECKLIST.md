# SupplierSelectionDSS - Quality Assurance & User Acceptance Testing Checklist

This checklist provides a comprehensive verification guide for QA engineers, developers, and stakeholders to test the end-to-end functionality of `SupplierSelectionDSS`.

---

## 1. Role-Based Access Control (RBAC) Matrix

| Feature / Action | Admin | Purchasing Staff | Expert | Manager |
| :--- | :---: | :---: | :---: | :---: |
| View Dashboard | Yes | Yes | Yes | Yes |
| Manage Suppliers & Product Groups | Yes | Yes | No | No |
| Manage Evaluation Criteria | Yes | Yes | No | No |
| Create / Start Evaluation Batch | Yes | Yes | No | No |
| Cancel Batch (Draft or Evaluating) | Yes | Yes | No | No |
| Input / Edit Evaluation Scores | Yes | Yes | Yes | No |
| Perform AHP Pairwise Comparisons | Yes | No | Yes | No |
| Run TOPSIS Ranking Calculation | Yes | No | Yes | No |
| Review Final Ranking & Metrics | Yes | No | Yes | Yes |
| Approve / Re-evaluate Decision | Yes | No | No | Yes |
| Export Excel & PDF Reports | Yes | Yes | No | Yes |
| View Audit Logs | Yes | No | No | No |
| Manage User Accounts | Yes | No | No | No |

---

## 2. End-to-End Workflow Checklist

### Phase 1: Setup & Initialization (Admin / Purchasing)
- [ ] **TC-01: Create Batch**:
  - Navigate to *Batches* > *Create New*.
  - Fill in Batch Code (e.g. `BATCH-2026-Q1`), Name, select a Product Group, select $\ge 2$ Suppliers and $\ge 2$ Criteria.
  - Save and verify status is **Draft**.
- [ ] **TC-02: Validation on Start**:
  - Verify that a Batch with $< 2$ suppliers or $< 2$ criteria cannot be started.
  - Click **Start Evaluation**.
  - Verify Batch status transitions from **Draft** to **Evaluating**.
  - Verify audit log entry is recorded with action `CreateBatch` and `StartBatch`.

### Phase 2: Evaluation Scoring (Purchasing / Expert)
- [ ] **TC-03: Score Input**:
  - Open the *Evaluation* matrix for the batch.
  - Input numeric scores (scale 1–10) for each supplier-criterion pair.
  - Save scores. Verify success notification toast appears.
  - Verify missing cells are highlighted if the matrix is incomplete.

### Phase 3: Analytic Hierarchy Process (AHP) (Expert / Admin)
- [ ] **TC-04: AHP Pairwise Comparison**:
  - Navigate to the *AHP* section for the batch.
  - Enter pairwise comparison values (1 to 9 scale).
  - Verify reciprocal values ($a_{ji} = 1 / a_{ij}$) are automatically calculated.
- [ ] **TC-05: Consistency Ratio (CR) Check**:
  - If $CR > 0.10$, verify system flags the matrix as **Inconsistent** and displays the exact CR (e.g., $CR = 0.1456$).
  - Verify that **TOPSIS cannot be started** while AHP is inconsistent.
  - Adjust comparisons so $CR \le 0.10$.
  - Verify status changes to **Consistent** with a green badge.

### Phase 4: TOPSIS Calculation (Expert / Admin)
- [ ] **TC-06: Calculate TOPSIS**:
  - Navigate to *TOPSIS* and trigger calculation.
  - Verify Euclidean normalized matrix is generated.
  - Verify Positive Ideal Solution ($PIS$) and Negative Ideal Solution ($NIS$) are computed respecting Benefit vs Cost criteria.
  - Verify separation distances ($S^+$, $S^-$) and relative closeness scores ($C_i \in [0, 1]$) are computed.
  - Verify suppliers are ranked in descending order of closeness score ($C_i$).

### Phase 5: Cascade Invalidation (Critical Rule)
- [ ] **TC-07: Score Edit Invalidation**:
  - Return to *Evaluation* and change a supplier score.
  - Save scores.
  - Navigate to *TOPSIS* or *Decision Result*.
  - Verify existing TOPSIS results are **invalidated** and cleared, requiring recalculation to ensure mathematical consistency.
- [ ] **TC-08: AHP Weight Change Invalidation**:
  - Recalculate TOPSIS.
  - Go to *AHP*, change comparison values, and save.
  - Verify TOPSIS results are **invalidated** again.

### Phase 6: Managerial Decision & Approval (Manager / Admin)
- [ ] **TC-09: Decision Review**:
  - Log in as Manager. Open *Decision Result*.
  - Review Top #1 recommended supplier and full ranking table.
- [ ] **TC-10: Manager Discretion**:
  - Verify Manager can choose a supplier other than Rank #1 if operational constraints require it, provided mandatory decision notes are supplied.
- [ ] **TC-11: Approve**:
  - Select approved supplier, enter approval notes, click **Approve Decision**.
  - Verify Batch status transitions to **Completed**.
  - Verify Batch is now **Locked** from further score/AHP edits.
- [ ] **TC-12: Re-evaluate**:
  - On a separate evaluating batch, test **Request Re-evaluation** with justification.
  - Verify status remains `Evaluating` and audit log records the re-evaluation reason.

### Phase 7: Batch Cancellation
- [ ] **TC-13: Cancel from Draft**:
  - Open a Draft batch. Click **Cancel Batch**.
  - Confirm dialog prompt.
  - Verify status is now **Cancelled**.
- [ ] **TC-14: Cancel from Evaluating**:
  - Open an Evaluating batch. Click **Cancel Batch**.
  - Confirm dialog prompt.
  - Verify status is now **Cancelled**.
- [ ] **TC-15: Completed Batch Cannot be Cancelled**:
  - Open a Completed batch. Verify **Cancel Batch** button is not available.

### Phase 8: Reports & Exports
- [ ] **TC-16: Excel Export**:
  - Go to *Reports* for a completed batch.
  - Click **Export Excel (.xlsx)**.
  - Verify file downloads cleanly and contains metadata, weights, scores, TOPSIS rankings, and approval signature block.
- [ ] **TC-17: PDF Export**:
  - Click **Export PDF (.pdf)**.
  - Verify file opens cleanly with proper pagination, header, footer, and styling.

### Phase 9: UI/UX & Client Controls
- [ ] **TC-18: Breadcrumb Navigation**:
  - Verify breadcrumb displays accurate parent links and current page name.
- [ ] **TC-19: Sidebar Icons**:
  - Verify all menu items have corresponding Bootstrap Icons and active highlights.
- [ ] **TC-20: Toast Notifications**:
  - Verify alerts appear in floating top-right stack and auto-dismiss after 5 seconds.
- [ ] **TC-21: Double Submit Prevention**:
  - Submit an evaluation or calculation form.
  - Verify submit button displays loading spinner and disables duplicate clicks.
