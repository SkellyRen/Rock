// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Workflow.Action.Groups
{
    /// <summary>
    /// Adds a new sign-up opportunity to a provided project.
    /// </summary>
    [ActionCategory( "Groups" )]
    [Description( "Adds a new sign-up opportunity to a provided project." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Sign-Up Project Opportunity Add" )]

    [WorkflowAttribute( "Sign-Up Project",
        Description = "The sign-up project group that the opportunity should be added to.",
        Key = AttributeKey.SignUpProject,
        IsRequired = true,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.GroupFieldType" },
        Order = 0 )]

    [WorkflowAttribute( "Location",
        Description = "The location for the opportunity.",
        Key = AttributeKey.Location,
        IsRequired = true,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.LocationFieldType" },
        Order = 1 )]

    [WorkflowAttribute( "Schedule",
        Description = "The schedule for the opportunity.",
        Key = AttributeKey.Schedule,
        IsRequired = true,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.ScheduleFieldType" },
        Order = 2 )]

    [WorkflowTextOrAttribute( "Minimum Capacity",
        "Attribute Value",
        Description = "The minimum capacity for the opportunity.",
        Key = AttributeKey.MinimumCapacity,
        IsRequired = false,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.IntegerFieldType", "Rock.Field.Types.TextFieldType" },
        Order = 3 )]

    [WorkflowTextOrAttribute( "Desired Capacity",
        "Attribute Value",
        Description = "The desired capacity for the opportunity.",
        Key = AttributeKey.DesiredCapacity,
        IsRequired = false,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.IntegerFieldType", "Rock.Field.Types.TextFieldType" },
        Order = 4 )]

    [WorkflowTextOrAttribute( "Maximum Capacity",
        "Attribute Value",
        Description = "The maximum capacity for the opportunity.",
        Key = AttributeKey.MaximumCapacity,
        IsRequired = false,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.IntegerFieldType", "Rock.Field.Types.TextFieldType" },
        Order = 5 )]

    [WorkflowTextOrAttribute( "Reminder Details",
        "Attribute Value",
        Description = "The reminder details for the opportunity.",
        Key = AttributeKey.ReminderDetails,
        IsRequired = false,
        FieldTypeClassNames = new string[]
        {
            "Rock.Field.Types.TextFieldType",
            "Rock.Field.Types.MemoFieldType",
            "Rock.Field.Types.HtmlFieldType",
            "Rock.Field.Types.StructuredContentEditorFieldType"
        },
        Order = 6 )]

    [WorkflowTextOrAttribute( "Confirmation Details",
        "Attribute Value",
        Description = "The confirmation details for the opportunity.",
        Key = AttributeKey.ConfirmationDetails,
        IsRequired = false,
        FieldTypeClassNames = new string[]
        {
            "Rock.Field.Types.TextFieldType",
            "Rock.Field.Types.MemoFieldType",
            "Rock.Field.Types.HtmlFieldType",
            "Rock.Field.Types.StructuredContentEditorFieldType"
        },
        Order = 7 )]

    [Rock.SystemGuid.EntityTypeGuid( "A917A5D4-76D2-42ED-A2C3-7B72A2F0A12A" )]
    public class AddSignUpProjectOpportunity : ActionComponent
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string SignUpProject = "SignUpProject";
            public const string Location = "Location";
            public const string Schedule = "Schedule";
            public const string MinimumCapacity = "MinimumCapacity";
            public const string DesiredCapacity = "DesiredCapacity";
            public const string MaximumCapacity = "MaximumCapacity";
            public const string ReminderDetails = "ReminderDetails";
            public const string ConfirmationDetails = "ConfirmationDetails";
            public const string ProjectOpportunity = "ProjectOpportunity";
        }

        #endregion

        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Get the sign-up project group.
            var groupGuid = GetAttributeValue( action, AttributeKey.SignUpProject, true ).AsGuidOrNull();
            if ( !groupGuid.HasValue )
            {
                errorMessages.Add( "Invalid sign-up project provided." );
                return false;
            }

            var groupId = new GroupService( rockContext ).GetId( groupGuid.Value );
            if ( !groupId.HasValue )
            {
                errorMessages.Add( "The sign-up project provided does not exist." );
                return false;
            }

            // Get the location.
            var locationGuid = GetAttributeValue( action, AttributeKey.Location, true ).AsGuidOrNull();
            if ( !locationGuid.HasValue )
            {
                errorMessages.Add( "Invalid location provided." );
                return false;
            }

            var locationId = new LocationService( rockContext ).GetId( locationGuid.Value );
            if ( !locationId.HasValue )
            {
                errorMessages.Add( "The location provided does not exist." );
                return false;
            }

            // Get the schedule.
            var scheduleGuid = GetAttributeValue( action, AttributeKey.Schedule, true ).AsGuidOrNull();
            if ( !scheduleGuid.HasValue )
            {
                errorMessages.Add( "Invalid schedule provided." );
                return false;
            }

            var schedule = new ScheduleService( rockContext ).GetNoTracking( scheduleGuid.Value );
            if ( schedule == null )
            {
                errorMessages.Add( "The schedule provided does not exist." );
                return false;
            }

            // Create a GroupLocation record if one doesn't already exist.
            var groupLocationService = new GroupLocationService( rockContext );
            var groupLocation = groupLocationService
                .Queryable()
                .Include( gl => gl.Schedules )
                .Include( gl => gl.GroupLocationScheduleConfigs )
                .FirstOrDefault( gl => gl.GroupId == groupId.Value && gl.LocationId == locationId.Value );

            if ( groupLocation == null )
            {
                groupLocation = new GroupLocation
                {
                    GroupId = groupId.Value,
                    LocationId = locationId.Value
                };

                groupLocationService.Add( groupLocation );

                // Initial save so we can use the new GroupLocation ID below.
                rockContext.SaveChanges();
            }

            // Create a GroupLocationSchedule record if one doesn't already exist.
            if ( !groupLocation.Schedules.Any( s => s.Id == schedule.Id ) )
            {
                groupLocation.Schedules.Add( schedule );
            }

            // Create a GroupLocationScheduleConfig record if one doesn't already exist.
            var groupLocationScheduleConfig = groupLocation.GroupLocationScheduleConfigs.FirstOrDefault( c => c.ScheduleId == schedule.Id );
            if ( groupLocationScheduleConfig == null )
            {
                groupLocationScheduleConfig = new GroupLocationScheduleConfig
                {
                    GroupLocationId = groupLocation.Id,
                    ScheduleId = schedule.Id
                };

                groupLocation.GroupLocationScheduleConfigs.Add( groupLocationScheduleConfig );
            }

            // Update GroupLocationScheduleConfig values.
            groupLocationScheduleConfig.MinimumCapacity = GetAttributeValue( action, AttributeKey.MinimumCapacity, true ).AsIntegerOrNull();
            groupLocationScheduleConfig.DesiredCapacity = GetAttributeValue( action, AttributeKey.DesiredCapacity, true ).AsIntegerOrNull();
            groupLocationScheduleConfig.MaximumCapacity = GetAttributeValue( action, AttributeKey.MaximumCapacity, true ).AsIntegerOrNull();
            groupLocationScheduleConfig.ReminderAdditionalDetails = GetAttributeValue( action, AttributeKey.ReminderDetails, true );
            groupLocationScheduleConfig.ConfirmationAdditionalDetails = GetAttributeValue( action, AttributeKey.ConfirmationDetails, true );

            rockContext.SaveChanges();

            action.AddLogEntry( "Sign-Up Project opportunity created." );

            return true;
        }
    }
}
