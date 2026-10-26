-- ============================================================================
-- Stored Procedure: usp_SearchGrievances
-- Purpose: Parameterized searching and filtering of municipal grievances.
-- Parameters:
--   @SearchTerm     : Free-text search matching Description, Ward name, or Category name
--   @Status         : Filter by integer status (0=Submitted, 1=Assigned, etc.)
--   @WardId         : Filter by municipal ward
--   @DepartmentId   : Filter by responsible department
--   @OnlyBreached   : Filter to grievances that have breached SLA
-- ============================================================================

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
