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
using System.ComponentModel;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Linq;
using System.Collections.Generic;
using Rock.Blocks.Types.Mobile.Connection;
using Rock.Common.Mobile.Blocks.Reminders.ReminderDashboard;
using Rock.Common.Mobile.Blocks.Reminders;

namespace Rock.Blocks.Types.Mobile.Reminders
{
    /// <summary>
    /// A mobile block used to display information about
    /// existing reminders for a person.
    /// </summary>
    [DisplayName( "Reminder Dashboard" )]
    [Category( "Reminders" )]
    [Description( "Allows management of the current person's reminders." )]
    [IconCssClass( "fa fa-user-check" )]

    #region Block Attributes

    [LinkedPage(
        "Reminder List Page",
        Description = "Page to link to when user taps on a reminder type or reminder filter card. PersonGuid is passed in the query string, as well as corresponding filter parameters.",
        IsRequired = false,
        Key = AttributeKey.ReminderListPage,
        Order = 0 )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_REMINDERS_REMINDER_DASHBOARD )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_REMINDERS_REMINDER_DASHBOARD )]
    public class ReminderDashboard : RockMobileBlockType
    {
        #region Keys

        /// <summary>
        /// The block setting attribute keys for the <see cref="ConnectionTypeList"/> block.
        /// </summary>
        private static class AttributeKey
        {
            public const string ReminderListPage = "ReminderListPage";
        }

        #endregion

        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override int RequiredMobileAbiVersion => 5;

        /// <inheritdoc />
        public override string MobileBlockType => "Rock.Mobile.Blocks.Reminders.ReminderDashboard";

        /// <inheritdoc />
        public override object GetMobileConfigurationValues()
        {
            return new Rock.Common.Mobile.Blocks.Reminders.ReminderDashboard.Configuration
            {
                ListPageGuid = GetAttributeValue( AttributeKey.ReminderListPage ).AsGuidOrNull()
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a list of <see cref="ReminderTypeInfoBag" /> objects that
        /// we use to display information in the mobile shell.
        /// </summary>
        /// <returns></returns>
        private List<ReminderTypeInfoBag> GetReminderTypes( RockContext rockContext )
        {
            if ( RequestContext?.CurrentPerson?.PrimaryAliasId == null )
            {
                return null;
            }

            var personAliasId = RequestContext.CurrentPerson.PrimaryAliasId.Value;

            var reminderTypeService = new ReminderTypeService( rockContext );

            var reminderTypeInfoBags = reminderTypeService.GetTypesAndRemindersAssignedToPerson( personAliasId )
                .Select( x => new ReminderTypeInfoBag
                {
                    TotalReminderCount = x.Count(),
                    Guid = x.Key.Guid,
                    HighlightColor = x.Key.HighlightColor,
                    Name = x.Key.Name,
                    EntityTypeName = x.Key.EntityType.FriendlyName
                } )
                .ToList();

            return reminderTypeInfoBags;
        }

        /// <summary>
        /// Gets information about filtering options for reminder types.
        /// This is a pretty specific use case where we want to display the filter option
        /// and then # of reminders that the filtered type would provide
        /// </summary>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        private List<FilteredReminderOptionBag> GetFilteredReminderOptionBags( RockContext rockContext )
        {
            var filteredReminderOptionBag = new List<FilteredReminderOptionBag>
            {
                //
                // The 'Due' filtered reminder option.
                //
                new FilteredReminderOptionBag
                {
                    Name = "Due",
                    CssClass = "reminders-due",
                    IconClass = "fa fa-bell",
                    TotalReminderCount = GetTotalRemindersForFilteredType( "due", rockContext ),
                    Order = 1
                },

                //
                // The 'Future' filtered reminder option.
                //
                new FilteredReminderOptionBag
                {
                    Name = "Future",
                    CssClass = "reminders-future",
                    IconClass = "fa fa-calendar",
                    TotalReminderCount = GetTotalRemindersForFilteredType( "future", rockContext ),
                    Order = 2
                },

                //
                // The 'All' filtered reminder option.
                //
                new FilteredReminderOptionBag
                {
                    Name = "All",
                    CssClass = "reminders-all",
                    IconClass = "fa fa-inbox",
                    TotalReminderCount = GetTotalRemindersForFilteredType( "", rockContext ),
                    Order = 3
                },

                //
                // The 'Completed' filtered reminder option.
                //
                new FilteredReminderOptionBag
                {
                    Name = "Completed",
                    CssClass = "reminders-completed",
                    IconClass = "fa fa-check",
                    TotalReminderCount = GetTotalRemindersForFilteredType( "completed", rockContext ),
                    Order = 4
                }
            };

            return filteredReminderOptionBag.OrderBy( x => x.Order ).ToList();
        }

        /// <summary>
        /// Gets the total number of reminders depending on the filter passed in.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="rockContext"></param>
        /// <returns>The # of reminders.</returns>
        private int GetTotalRemindersForFilteredType( string filter, RockContext rockContext )
        {
            var reminders = new ReminderService( rockContext )
                .GetReminders( RequestContext.CurrentPerson.Id, null, null, null )
                .Where( r => r.ReminderType.IsActive );

            // Get the reminders that are past due.
            if ( filter == "due" )
            {
                reminders = reminders.Where( r => r.ReminderDate < RockDateTime.Now );
            }
            // Get the reminders that are upcoming.
            else if ( filter == "future" )
            {
                reminders = reminders.Where( r => r.ReminderDate > RockDateTime.Now );
            }
            // Get the reminders that are completed.
            else if ( filter == "completed" )
            {
                reminders = reminders.Where( r => r.IsComplete );
            }

            return reminders.Count();
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the data to present on the dashboard.
        /// </summary>
        /// <returns></returns>
        [BlockAction]
        public BlockActionResult GetReminderDashboardData()
        {
            // We need a person to view this block.
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionUnauthorized();
            }

            using( var rockContext = new RockContext() )
            {
                // Get the list of filtered reminder types and the count associated with them.
                var filteredReminderOptions = GetFilteredReminderOptionBags( rockContext );

                // Get the list of associated reminder types and the count of reminders associated with them.
                var reminderTypes = GetReminderTypes( rockContext );

                return ActionOk( new ReminderDashboardInfoBag
                {
                    FilteredReminderOptionBags = filteredReminderOptions,
                    ReminderTypeInfoBags = reminderTypes,
                } );
            }
        }

        #endregion

    }
}
