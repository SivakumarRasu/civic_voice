-- ============================================================================
-- View: vw_DepartmentSlaReport
-- Purpose: Department-wise grievance aggregation showing total, open, resolved,
--          and SLA-breached grievances.
-- Design Note: Aggregates from Departments -> Categories -> Grievances.
--              Status enum mapping: 0=Submitted, 1=Assigned, 2=InProgress, 3=Resolved, 4=Rejected.
--              SLA Breach: Unresolved, non-rejected (Status in 0, 1, 2) and DueAt < GETUTCDATE().
-- ============================================================================

CREATE OR ALTER VIEW dbo.vw_DepartmentSlaReport
AS
SELECT 
    d.DepartmentId,
    d.Name AS DepartmentName,
    d.Email AS DepartmentEmail,
    COUNT(g.GrievanceId) AS TotalGrievances,
    SUM(CASE WHEN g.Status IN (0, 1, 2) THEN 1 ELSE 0 END) AS OpenGrievances,
    SUM(CASE WHEN g.Status = 3 THEN 1 ELSE 0 END) AS ResolvedGrievances,
    SUM(CASE WHEN g.Status = 4 THEN 1 ELSE 0 END) AS RejectedGrievances,
    SUM(CASE WHEN g.Status IN (0, 1, 2) AND g.DueAt < GETUTCDATE() THEN 1 ELSE 0 END) AS SlaBreachedGrievances,
    CASE 
        WHEN COUNT(g.GrievanceId) = 0 THEN 0.0
        ELSE CAST(ROUND(SUM(CASE WHEN g.Status = 3 THEN 1.0 ELSE 0.0 END) * 100.0 / COUNT(g.GrievanceId), 2) AS DECIMAL(5,2))
    END AS ResolutionRatePercentage
FROM dbo.Departments d
LEFT JOIN dbo.Categories c ON d.DepartmentId = c.DepartmentId
LEFT JOIN dbo.Grievances g ON c.CategoryId = g.CategoryId
GROUP BY d.DepartmentId, d.Name, d.Email;
GO
