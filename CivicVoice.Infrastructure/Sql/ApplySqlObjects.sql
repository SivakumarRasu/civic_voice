-- ============================================================================
-- CivicVoice Database Custom Objects Script
-- Applies View, Stored Procedure, and Trigger in correct order.
-- ============================================================================

PRINT 'Applying vw_DepartmentSlaReport...';
GO
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

PRINT 'Applying usp_SearchGrievances...';
GO
CREATE OR ALTER PROCEDURE dbo.usp_SearchGrievances
    @SearchTerm NVARCHAR(200) = NULL,
    @Status INT = NULL,
    @WardId INT = NULL,
    @DepartmentId INT = NULL,
    @OnlyBreached BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        g.GrievanceId,
        g.CitizenId,
        g.Description,
        g.PhotoPath,
        g.IsAnonymous,
        g.CreatedAt,
        g.DueAt,
        g.Status,
        w.WardId,
        w.Name AS WardName,
        w.Area AS WardArea,
        c.CategoryId,
        c.Name AS CategoryName,
        d.DepartmentId,
        d.Name AS DepartmentName,
        CASE 
            WHEN g.Status IN (0, 1, 2) AND g.DueAt < GETUTCDATE() THEN 1 
            ELSE 0 
        END AS IsBreached
    FROM dbo.Grievances g
    INNER JOIN dbo.Wards w ON g.WardId = w.WardId
    INNER JOIN dbo.Categories c ON g.CategoryId = c.CategoryId
    INNER JOIN dbo.Departments d ON c.DepartmentId = d.DepartmentId
    WHERE 
        (@Status IS NULL OR g.Status = @Status)
        AND (@WardId IS NULL OR g.WardId = @WardId)
        AND (@DepartmentId IS NULL OR d.DepartmentId = @DepartmentId)
        AND (@OnlyBreached = 0 OR (g.Status IN (0, 1, 2) AND g.DueAt < GETUTCDATE()))
        AND (
            @SearchTerm IS NULL 
            OR TRIM(@SearchTerm) = ''
            OR g.Description LIKE '%' + @SearchTerm + '%'
            OR w.Name LIKE '%' + @SearchTerm + '%'
            OR c.Name LIKE '%' + @SearchTerm + '%'
            OR d.Name LIKE '%' + @SearchTerm + '%'
        )
    ORDER BY g.CreatedAt DESC;
END;
GO

PRINT 'Applying trg_GrievanceStatusHistory...';
GO
CREATE OR ALTER TRIGGER dbo.trg_GrievanceStatusHistory
ON dbo.Grievances
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1 RETURN;
    IF NOT UPDATE(Status) RETURN;

    INSERT INTO dbo.StatusHistories (GrievanceId, OldStatus, NewStatus, ChangedAt, ChangedBy)
    SELECT 
        i.GrievanceId,
        d.Status AS OldStatus,
        i.Status AS NewStatus,
        SYSUTCDATETIME() AS ChangedAt,
        COALESCE(CAST(SESSION_CONTEXT(N'CurrentUserId') AS NVARCHAR(450)), SUSER_SNAME(), N'System') AS ChangedBy
    FROM inserted i
    INNER JOIN deleted d ON i.GrievanceId = d.GrievanceId
    WHERE i.Status <> d.Status;
END;
GO

PRINT 'Custom SQL objects successfully applied.';
GO
