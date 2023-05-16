using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System;
using System.ComponentModel;

namespace Rock.Jobs
{
    /// <summary>
    /// Run once job for v15 to update WorkflowId columns after switch from computed Column.
    /// </summary>
    [DisplayName( "Rock Update Helper v15.0 - Update WorkflowId columns." )]
    [Description( "This job update all WorkflowId columns on the Workflow table using the format specified in 'ufnWorkflow_GetWorkflowId'." )]

    [IntegerField(
    "Command Timeout",
    Key = AttributeKey.CommandTimeout,
    Description = "Maximum amount of time (in seconds) to wait for each SQL command to complete. On a large database with lots of transactions, this could take several minutes or more.",
    IsRequired = false,
    DefaultIntegerValue = 14400 )]
    public class PostV15UpdateWorkflowIds : RockJob
    {
        private static class AttributeKey
        {
            public const string CommandTimeout = "CommandTimeout";
        }

        /// <inheritdoc />
        public override void Execute()
        {
            // get the configured timeout, or default to 240 minutes if it is blank
            var commandTimeout = GetAttributeValue( AttributeKey.CommandTimeout ).AsIntegerOrNull() ?? 14400;
            var jobMigration = new JobMigration( commandTimeout );

            jobMigration.Sql( @"DECLARE @batchId INT
DECLARE @batchSize INT
DECLARE @results INT

SET @results = 1
SET @batchSize = 10000
SET @batchId = 0

-- when 0 rows returned, exit the loop
WHILE (@results > 0)
	BEGIN

		UPDATE Workflow SET WorkflowId = COALESCE( WFT.[WorkflowIdPrefix] + RIGHT( '00000' + CAST( WF.[WorkflowIdNumber] AS varchar(5) ), 5 ), '' )
		FROM WorkflowType WFT
		LEFT JOIN Workflow WF ON WFT.Id = WF.WorkflowTypeId
		WHERE (WFT.Id > @batchId
		AND WFT.Id <= @batchId + @batchSize)

		SET @results = @@ROWCOUNT
	
		-- next batch
		SET @batchId = @batchId + @batchSize

	END" );

            DeleteJob();
        }

        /// <summary>
        /// Deletes the job.
        /// </summary>
        private void DeleteJob()
        {
            using ( var rockContext = new RockContext() )
            {
                var jobService = new ServiceJobService( rockContext );
                var job = jobService.Get( GetJobId() );

                if ( job != null )
                {
                    jobService.Delete( job );
                    rockContext.SaveChanges();
                }
            }
        }
    }
}
