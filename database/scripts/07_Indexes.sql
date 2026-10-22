/* =====================================================================
   7. INDEX BỔ SUNG
   ===================================================================== */

CREATE INDEX IX_Users_Role_Status               ON Users(Role, Status);
CREATE INDEX IX_EvaluationBatches_Group         ON EvaluationBatches(GroupID);
CREATE INDEX IX_EvaluationBatches_Status        ON EvaluationBatches(Status);
CREATE INDEX IX_EvaluationScores_Batch          ON EvaluationScores(BatchID);
CREATE INDEX IX_EvaluationScores_Supplier       ON EvaluationScores(SupplierID);
CREATE INDEX IX_EvaluationScores_Batch_Supplier ON EvaluationScores(BatchID, SupplierID);
CREATE INDEX IX_AHPPairwise_Batch               ON AHPPairwiseComparisons(BatchID);
CREATE INDEX IX_AHPPairwise_Batch_Row           ON AHPPairwiseComparisons(BatchID, CriteriaRowID);
CREATE INDEX IX_AHPPairwise_Batch_Col           ON AHPPairwiseComparisons(BatchID, CriteriaColID);
CREATE INDEX IX_AHPCriteriaWeights_Batch        ON AHPCriteriaWeights(BatchID);
CREATE INDEX IX_TopsisIdeal_Batch               ON TopsisIdealSolutions(BatchID);
CREATE INDEX IX_TopsisIdeal_Criteria            ON TopsisIdealSolutions(CriteriaID);
CREATE INDEX IX_TopsisResults_Batch_Rank        ON TopsisResults(BatchID, RankNo);
CREATE INDEX IX_TopsisResults_CC                ON TopsisResults(BatchID, ClosenessCoefficient DESC);
CREATE INDEX IX_Suppliers_Status                ON Suppliers(Status);
CREATE INDEX IX_AuditLogs_User_Time             ON AuditLogs(UserID, ActionTime);
GO
