using Rock.Attribute;
using System.ComponentModel;

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

    }
}
