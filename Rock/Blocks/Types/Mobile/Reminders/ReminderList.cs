using Nest;

using Rock.Attribute;
using Rock.Common.Mobile.Blocks.Reminders.ReminderList;
using Rock.Data;
using Rock.Mobile;
using Rock.Model;
using Rock.Web.Cache;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

namespace Rock.Blocks.Types.Mobile.Reminders
{
    /// <summary>
    /// A block used to show existing reminders for a current person.
    /// </summary>
    [DisplayName( "Reminder List" )]
    [Category( "Reminders" )]
    [Description( "Allows management of the current person's reminders." )]
    [IconCssClass( "fa fa-user-check" )]

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_REMINDERS_REMINDER_LIST )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_REMINDERS_REMINDER_LIST )]
    public class ReminderList : RockMobileBlockType
    {
        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override int RequiredMobileAbiVersion => 5;

        /// <inheritdoc />
        public override string MobileBlockType => "Rock.Mobile.Blocks.Reminders.ReminderList";

        #endregion

        private List<ReminderInfoBag> GetReminderBags( Guid personGuid, Guid? entityTypeGuid, Guid? entityGuid, Guid? reminderTypeGuid, int startIndex, int count, ReminderListFilterBag filter = null )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderService = new ReminderService( rockContext );
                var personService = new PersonService( rockContext );

                var reminders = reminderService.GetReminders( personGuid, entityTypeGuid, entityGuid, reminderTypeGuid );

                reminders = FilterReminders( filter, reminders );

                var reminderBags = reminders.Select( r => new ReminderInfoBag
                {
                    Guid = r.Guid,
                    Note = r.Note,
                    ReminderTypeName = r.ReminderType.Name,
                    ReminderTypeGuid = r.ReminderType.Guid,
                    ReminderDate = r.ReminderDate,
                    EntityId = r.EntityId,
                    EntityTypeId = r.ReminderType.EntityTypeId,
                    EntityTypeName = r.ReminderType.EntityType.Name,
                } )
                    .OrderByDescending( r => r.EntityId )
                    .Skip( startIndex )
                    .Take( count )
                    .ToList();

                reminderBags.ForEach( ( bag ) => bag.PopulateAdditionalInformation( personService ) );

                return reminderBags;
            }
        }

        private IQueryable<Reminder> FilterReminders( ReminderListFilterBag filter, IQueryable<Reminder> reminders )
        {
            if ( filter == null )
            {
                return reminders;
            }

            if ( filter.CompletionFilter != ReminderListFilterBag.CompletionFilterValue.None )
            {
                if ( filter.CompletionFilter == ReminderListFilterBag.CompletionFilterValue.Active )
                {
                    reminders = reminders.Where( r => !r.IsComplete );
                }

                else if ( filter.CompletionFilter == ReminderListFilterBag.CompletionFilterValue.Complete )
                {
                    reminders = reminders.Where( r => r.IsComplete );
                }
            }

            if ( filter.DueFilter != ReminderListFilterBag.DueFilterValue.None )
            {
                if ( filter.DueFilter == ReminderListFilterBag.DueFilterValue.DueThisMonth )
                {
                    var startOfMonth = RockDateTime.Now.StartOfMonth();
                    var nextMonthDate = RockDateTime.Now.AddMonths( 1 );
                    var nextMonthStartDate = new DateTime( nextMonthDate.Year, nextMonthDate.Month, 1 );
                    reminders = reminders.Where( r => r.ReminderDate >= startOfMonth && r.ReminderDate < nextMonthStartDate );
                }
                else if ( filter.DueFilter == ReminderListFilterBag.DueFilterValue.DueThisWeek )
                {
                    var nextWeekStartDate = RockDateTime.Now.EndOfWeek( RockDateTime.FirstDayOfWeek ).AddDays( 1 );
                    var startOfWeek = nextWeekStartDate.AddDays( -7 );
                    reminders = reminders.Where( r => r.ReminderDate >= startOfWeek && r.ReminderDate < nextWeekStartDate );
                }
                else if ( filter.DueFilter == ReminderListFilterBag.DueFilterValue.Due )
                {
                    var currentDate = RockDateTime.Now;
                    reminders = reminders.Where( r => r.ReminderDate <= currentDate );
                }
            }

            if ( filter.ReminderType.HasValue )
            {
                var reminderTypeId = new ReminderTypeService( new RockContext() ).GetId( filter.ReminderType.Value );
                reminders = reminders.Where( r => r.ReminderTypeId == reminderTypeId );
            }

            return reminders;
        }

        #region Block Actions

        [BlockAction]
        public BlockActionResult GetReminders( Guid personGuid, Guid? entityTypeGuid, Guid? entityGuid, Guid? reminderTypeGuid, int startIndex, int count, ReminderListFilterBag filter )
        {
            var reminders = GetReminderBags( personGuid, entityTypeGuid, entityGuid, reminderTypeGuid, startIndex, count, filter );

            return ActionOk( reminders );
        }

        #endregion

        public class ReminderListBag
        {
            public bool HasMore { get; set; }
            public List<ReminderInfoBag> Reminders { get; set; }
        }

        /// <summary>
        /// Contains information about a reminder.
        /// </summary>
        public class ReminderInfoBag
        {
            public Guid Guid { get; set; }
            public string EntityTypeName { get; set; }
            public string ReminderTypeName { get; set; }
            public Guid ReminderTypeGuid { get; set; }
            public DateTimeOffset ReminderDate { get; set; }
            public string Note { get; set; }
            public int EntityTypeId { get; set; }
            public int EntityId { get; set; }
            public string PhotoUrl { get; private set; }
            public string Name { get; set; }

            public void PopulateAdditionalInformation( PersonService personService )
            {
                var entityType = EntityTypeCache.Get( EntityTypeId );

                string path;
                if ( entityType != null && entityType.Guid == Rock.SystemGuid.EntityType.PERSON.AsGuid() )
                {
                    path = personService.Get( EntityId ).PhotoUrl;
                }
                else
                {
                    path = $"/GetAvatar.ashx?text={EntityTypeName.SubstringSafe( 0, 1 )}";
                }

                PhotoUrl = MobileHelper.BuildPublicApplicationRootUrl( path );

                if ( entityType != null && EntityId != 0 )
                {
                    var entityDescription = Reflection.GetIEntityForEntityType( entityType.GetEntityType(), EntityId ).ToStringSafe();
                    Name = entityDescription;
                }
            }
        }
    }
}
