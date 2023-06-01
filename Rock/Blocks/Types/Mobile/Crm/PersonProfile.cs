using System.ComponentModel;

using Rock.Attribute;
using Rock.Common.Mobile.Blocks.Crm.PersonProfile;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Linq;
using Rock.Mobile;

namespace Rock.Blocks.Types.Mobile.Crm
{
    /// <summary>
    /// The Person Profile Block.
    /// Implements the <see cref="Rock.Blocks.RockMobileBlockType" />
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockMobileBlockType" />

    [DisplayName( "Person Profile" )]
    [Category( "Crm" )]
    [Description( "The person profile block." )]
    [IconCssClass( "fa fa-id-card" )]


    [CodeEditorField( "Header Template",
        Description = "Lava template used to render the header above the reminder edit fields.",
        IsRequired = false,
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Xml,
        Key = AttributeKey.HeaderTemplate,
        DefaultValue = _defaultHeaderXaml,
        Order = 0 )]

    [CodeEditorField( "Custom Actions Template",
        Description = "Lava template used to render custom actions (such as navigation) below the action buttons.",
        IsRequired = false,
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Xml,
        Key = AttributeKey.CustomActionsTemplate,
        Order = 1 )]

    [CodeEditorField( "Badge Bar Template",
        Description = "Lava template used to render the header above the reminder edit fields.",
        IsRequired = false,
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Xml,
        Key = AttributeKey.BadgeBarTemplate,
        Order = 2 )]

    [ContextAware( typeof( Person ) )]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_CRM_PERSON_PROFILE )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_CRM_PERSON_PROFILE )]
    public class PersonProfile : RockMobileBlockType
    {
        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override int RequiredMobileAbiVersion => 5;

        /// <inheritdoc />
        public override string MobileBlockType => "Rock.Mobile.Blocks.Crm.PersonProfile";

        #endregion

        #region Keys

        /// <summary>
        /// The attribute keys for this block.
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The header template attribute key.
            /// </summary>
            public const string HeaderTemplate = "HeaderTemplate";

            /// <summary>
            /// The header template attribute key.
            /// </summary>
            public const string CustomActionsTemplate = "CustomActionsTemplate";

            /// <summary>
            /// The header template attribute key.
            /// </summary>
            public const string BadgeBarTemplate = "BadgeBarTemplate";
        }

        #endregion

        #region Constants

        private const string _defaultHeaderXaml = "";

        #endregion

        #region Methods

        /// <summary>
        /// Gets the header template.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetHeaderTemplate( Person person )
        {
            var template = GetAttributeValue( AttributeKey.HeaderTemplate );
            var mergeFields = RequestContext.GetCommonMergeFields();

            mergeFields.Add( "Person", person );

            template = template.ResolveMergeFields( mergeFields );

            return template;
        }

        /// <summary>
        /// Gets the header template.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetBadgeBarTemplate( Person person )
        {
            var template = GetAttributeValue( AttributeKey.BadgeBarTemplate );
            var mergeFields = RequestContext.GetCommonMergeFields();

            mergeFields.Add( "Person", person );

            template = template.ResolveMergeFields( mergeFields );

            return template;
        }

        /// <summary>
        /// Gets the header template.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetCustomActionsTemplate( Person person )
        {
            var template = GetAttributeValue( AttributeKey.CustomActionsTemplate );
            var mergeFields = RequestContext.GetCommonMergeFields();

            mergeFields.Add( "Person", person );

            template = template.ResolveMergeFields( mergeFields );

            return template;
        }

        private ContactInformationBag GetPersonContactInformation( Person person )
        {
            var mobileNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            var homeNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
            var workNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );

            return new Rock.Common.Mobile.Blocks.Crm.PersonProfile.ContactInformationBag
            {
                MobileNumber = GetPhoneNumberBag ( mobileNumber ),
                HomeNumber = GetPhoneNumberBag( homeNumber ),
                WorkNumber = GetPhoneNumberBag( workNumber ),
                Email = person.Email,
                CommunicationPreference = person.CommunicationPreference.ToMobile(),
            };
        }

        private PhoneNumberBag GetPhoneNumberBag( PhoneNumber phoneNumber )
        {
            if( phoneNumber  == null )
            {
                return null;
            }

            return new PhoneNumberBag
            {
                PhoneNumber = phoneNumber.NumberFormatted,
                Description = phoneNumber.Description,
                Guid = phoneNumber.Guid,
            };
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the person profile data.
        /// </summary>
        /// <returns>BlockActionResult.</returns>
        [BlockAction]
        public BlockActionResult GetPersonProfileData()
        {
            var person = RequestContext.GetContextEntity<Person>() ?? RequestContext.CurrentPerson;

            if( person == null )
            {
                return ActionNotFound();
            }

            return ActionOk( new Rock.Common.Mobile.Blocks.Crm.PersonProfile.ResponseBag
            {
                HeaderTemplate = GetHeaderTemplate( person ),
                BadgeBarTemplate = GetBadgeBarTemplate( person ),
                CustomActionsTemplate = GetCustomActionsTemplate( person ),
                ContactInformation = GetPersonContactInformation( person )
            } );
        }

        #endregion
    }
}
