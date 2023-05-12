using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.Types.Mobile.Reminders
{
    /// <summary>
    /// A mobile block used to display information about
    /// existing reminders for a person.
    /// </summary>
    [DisplayName( "Reminder Dashboard" )]
    [Category( "Reminders > Reminder Dashboard" )]
    [Description( "Allows management of the current person's reminders." )]
    [IconCssClass( "fa fa-user-check" )]

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_REMINDERS_REMINDER_DASHBOARD )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_REMINDERS_REMINDER_DASHBOARD )]
    public class ReminderDashboard : RockMobileBlockType
    {
        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override int RequiredMobileAbiVersion => 5;

        /// <inheritdoc />
        public override string MobileBlockType => "Rock.Mobile.Blocks.Reminders.ReminderDashboard";

        #endregion

        #region Methods

        #endregion

        #region Block Actions

        #endregion 
    }
}
