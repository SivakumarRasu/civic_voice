-- ============================================================================
-- Trigger: trg_GrievanceStatusHistory
-- Purpose: Automatically records grievance lifecycle status changes into StatusHistories.
-- Design Rules:
--   1. Executes AFTER UPDATE on Grievances.
--   2. Ignores updates where Status did not change (UPDATE(Status) + i.Status <> d.Status).
--   3. Handles multi-row updates in a single set-based statement.
--   4. Prevents recursive triggering (TRIGGER_NESTLEVEL() > 1).
--   5. Records UTC timestamp (SYSUTCDATETIME()).
--   6. Captures actor from SESSION_CONTEXT(N'CurrentUserId') or database user.
-- ============================================================================

CREATE OR ALTER TRIGGER dbo.trg_GrievanceStatusHistory
ON dbo.Grievances
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Prevent recursion
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- Quick exit if Status was not part of the UPDATE statement
    IF NOT UPDATE(Status) RETURN;

    -- Set-based insert for multi-row updates where Status actually changed
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
