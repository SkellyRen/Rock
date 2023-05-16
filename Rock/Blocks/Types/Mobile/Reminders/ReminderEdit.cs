using Rock.Attribute;
using System.ComponentModel;

namespace Rock.Blocks.Types.Mobile.Reminders
{
    /// <summary>
    /// A block used to add/edit reminders.
    /// </summary>
    [DisplayName( "Reminder Edit" )]
    [Category( "Reminders" )]
    [Description( "Allows adding/editing of reminders." )]
    [IconCssClass( "fa fa-user-check" )]

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_REMINDERS_REMINDER_EDIT )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_REMINDERS_REMINDER_EDIT )]
    public class ReminderEdit : RockMobileBlockType
    {
        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override int RequiredMobileAbiVersion => 5;

        /// <inheritdoc />
        public override string MobileBlockType => "Rock.Mobile.Blocks.Reminders.ReminderEdit";

        #endregion
    }
}
