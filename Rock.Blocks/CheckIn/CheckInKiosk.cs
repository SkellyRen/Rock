﻿// <copyright>
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Rock.Attribute;
using Rock.CheckIn;
using Rock.CheckIn.v2;
using Rock.CheckIn.v2.Labels;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Utility;
using Rock.Utility.ExtensionMethods;
using Rock.ViewModels.Blocks.CheckIn.CheckInKiosk;
using Rock.ViewModels.CheckIn;
using Rock.ViewModels.CheckIn.Labels;
using Rock.Web.Cache;

namespace Rock.Blocks.CheckIn
{
    /// <summary>
    /// The standard Rock block for performing check-in at a kiosk.
    /// </summary>

    [DisplayName( "Check-in Kiosk" )]
    [Category( "Check-in" )]
    [Description( "The standard Rock block for performing check-in at a kiosk." )]
    [IconCssClass( "fa fa-clipboard-check" )]
    [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    [LinkedPage( "Setup Page",
        Description = "The page to use when kiosk setup is required.",
        Key = AttributeKey.SetupPage,
        IsRequired = true,
        Order = 0 )]

    [BooleanField( "Show Counts By Location",
        Description = "When displaying attendance counts on the admin login screen this will group the counts by location first instead of area first.",
        DefaultBooleanValue = false,
        Key = AttributeKey.ShowCountsByLocation,
        Order = 1 )]

    [ContentChannelField( "Promotions Content Channel",
        Description = "The content channel to use for displaying promotions on the kiosk welcome screen.",
        Key = AttributeKey.PromotionsContentChannel,
        IsRequired = false,
        Order = 2 )]

    [CustomDropdownListField( "REST Key",
        Description = "If your kiosk pages are configured for anonymous access then you must create a REST key with access to the check-in API endpoints and select it here.",
        Key = AttributeKey.RestKey,
        ListSource = RestKeyAttributeQuery,
        IsRequired = false,
        Order = 3)]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "b208cafe-2194-4308-aa52-a920c516805a" )]
    [Rock.SystemGuid.BlockTypeGuid( "a27fd0aa-67ee-44c3-9e5f-3289c6a210f3" )]
    public class CheckInKiosk : RockBlockType
    {
        #region Keys

        private static class AttributeKey
        {
            public const string SetupPage = "SetupPage";
            public const string ShowCountsByLocation = "ShowCountsByLocation";
            public const string PromotionsContentChannel = "PromotionsContentChannel";
            public const string RestKey = "RestKey";
        }

        private static class PageParameterKey
        {
        }

        #endregion

        #region Constants

        private const string RestKeyAttributeQuery = @"SELECT [U].[Guid] AS [Value], [P].[LastName] AS [Text]
FROM [UserLogin] AS [U]
INNER JOIN [Person] AS [P] ON [P].[Id] = [U].[PersonId]
INNER JOIN [DefinedValue] AS [RT] ON [RT].[Id] = [P].[RecordTypeValueId]
WHERE [RT].[Guid] = '" + SystemGuid.DefinedValue.PERSON_RECORD_TYPE_RESTUSER + "'";

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var apiKey = string.Empty;

            RequestContext.Response.AddCssLink( RequestContext.ResolveRockUrl( "~/Styles/Blocks/Checkin/CheckInKiosk.css" ), true );

            if ( RequestContext.CurrentPerson == null && GetAttributeValue( AttributeKey.RestKey ).IsNotNullOrWhiteSpace() )
            {
                var activeRecordStatusValueId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid(), RockContext ).Id;
                var userGuid = GetAttributeValue( AttributeKey.RestKey ).AsGuid();

                apiKey = new UserLoginService( RockContext ).Queryable()
                    .Where( u => u.Guid == userGuid && u.Person.RecordStatusValueId == activeRecordStatusValueId )
                    .Select( u => u.ApiKey )
                    .FirstOrDefault() ?? string.Empty;
            }

            return new
            {
                ApiKey = apiKey,
                CurrentTheme = PageCache.Layout?.Site?.Theme?.ToLower(),
                SetupPageRoute = this.GetLinkedPageUrl( AttributeKey.SetupPage ),
                ShowCountsByLocation = GetAttributeValue( AttributeKey.ShowCountsByLocation ).AsBoolean()
            };
        }

        /// <summary>
        /// Gets the automatic configuration for the specified kiosk.
        /// </summary>
        /// <param name="director">The check-in director for this operation.</param>
        /// <param name="kiosk">The kiosk whose configuration should be retrieved.</param>
        /// <returns>An instance of <see cref="KioskConfigurationBag"/> if the kiosk was valid; otherwise <c>null</c>.</returns>
        private KioskConfigurationBag GetKioskConfiguration( CheckInDirector director, SavedKioskConfigurationBag savedConfiguration )
        {
            var kiosk = DeviceCache.GetByIdKey( savedConfiguration.KioskId, RockContext );
            var templateGroupType = GroupTypeCache.GetByIdKey( savedConfiguration.TemplateId, RockContext );

            if ( kiosk == null || templateGroupType == null )
            {
                return null;
            }

            var areas = director.GetKioskAreas( kiosk );
            var template = director.GetConfigurationTemplateBag( templateGroupType );

            if ( template == null )
            {
                return null;
            }

            return new KioskConfigurationBag
            {
                Kiosk = CheckInKioskSetup.GetKioskBag( kiosk ),
                Areas = areas.Where( a => savedConfiguration.AreaIds.Contains( a.IdKey ) )
                    .Select( a => new CheckInItemBag
                    {
                        Id = a.IdKey,
                        Name = a.Name
                    } ).ToList(),
                Template = template
            };
        }

        /// <summary>
        /// Gets the printer device that the kiosk is configured to use.
        /// </summary>
        /// <param name="kioskId">The encrypted identifier of the kiosk.</param>
        /// <returns>An instance of <see cref="DeviceCache"/> that represents the printer device or <c>null</c></returns>
        private DeviceCache GetKioskPrinter( string kioskId )
        {
            var kiosk = DeviceCache.GetByIdKey( kioskId, RockContext );

            if ( kiosk == null )
            {
                return null;
            }

            if ( !kiosk.PrinterDeviceId.HasValue )
            {
                return null;
            }

            return DeviceCache.Get( kiosk.PrinterDeviceId.Value, RockContext );
        }

        /// <summary>
        /// Gets the promotion items that should be displayed on kiosks at the
        /// specified location.
        /// </summary>
        /// <param name="campusId">The identifier of the <see cref="Campus"/> to filter items for.</param>
        /// <returns>A collection of <see cref="PromotionBag"/> objects that represent the promotions to display.</returns>
        private List<PromotionBag> GetPromotionItems( int? campusId )
        {
            var promotionContentChannelGuid = GetAttributeValue( AttributeKey.PromotionsContentChannel ).AsGuidOrNull();

            if ( !promotionContentChannelGuid.HasValue )
            {
                return new List<PromotionBag>();
            }

            var contentChannel = new ContentChannelService( RockContext )
                .Queryable()
                .AsNoTracking()
                .Include( cc => cc.Items )
                .Where( cc => cc.Guid == promotionContentChannelGuid.Value )
                .FirstOrDefault();

            if ( contentChannel == null )
            {
                return new List<PromotionBag>();
            }

            contentChannel.Items.LoadAttributes( RockContext );

            // Get the campus to filter for as well as the current date.
            var campus = campusId.HasValue
                ? CampusCache.Get( campusId.Value )
                : null;
            var campusGuid = campus?.Guid ?? Guid.Empty;
            var now = campus?.CurrentDateTime ?? RockDateTime.Now;

            // Filter items by date.
            var promotionItems = contentChannel.Items
                .Where( item => item.StartDateTime <= now
                    && ( !item.ExpireDateTime.HasValue || item.ExpireDateTime >= now ) );

            // Filter items by approval.
            if ( contentChannel.RequiresApproval )
            {
                promotionItems = promotionItems.Where( item => item.Status == ContentChannelItemStatus.Approved );
            }

            // Filter items by kiosk campus.
            promotionItems = promotionItems
                .Where( item => item.GetAttributeValue( "Campuses" ).IsNullOrWhiteSpace()
                    || item.GetAttributeValue( "Campuses" ).SplitDelimitedValues().AsGuidList().Contains( campusGuid ) );

            // Order the items.
            promotionItems = contentChannel.ItemsManuallyOrdered
                ? contentChannel.Items.OrderBy( item => item.Order )
                : contentChannel.Items.OrderBy( item => item.StartDateTime );

            return promotionItems
                .Select( item => new PromotionBag
                {
                    Url = $"{RequestContext.RootUrlPath}/GetImage.ashx?guid={item.GetAttributeValue( "Image" )}",
                    Duration = item.GetAttributeValue( "DisplayDuration" ).AsIntegerOrNull() ?? 15
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the bags that represent the current attendance records that can
        /// be reprinted.
        /// </summary>
        /// <param name="campusId">The campus to limit the attendance records to and determine the current timestamp.</param>
        /// <returns>A collection of <see cref="ReprintAttendanceBag"/> objects that represent the attendance records.</returns>
        private List<ReprintAttendanceBag> GetCurrentAttendanceForReprint( int? campusId )
        {
            var now = RockDateTime.Now;

            if ( campusId.HasValue )
            {
                var campus = CampusCache.Get( campusId.Value );

                if ( campus != null )
                {
                    now = campus.CurrentDateTime;
                }
            }

            var attendanceQry = CheckInDirector.GetCurrentAttendanceQuery( now, RockContext );

            if ( campusId.HasValue )
            {
                attendanceQry = attendanceQry.Where( a => a.CampusId.HasValue && a.CampusId.Value == campusId.Value );
            }

            var items = attendanceQry
                .Select( a => new
                {
                    a.Id,
                    a.StartDateTime,
                    a.PersonAlias.Person.NickName,
                    a.PersonAlias.Person.LastName,
                    a.AttendanceCode.Code,
                    GroupId = a.Occurrence.GroupId.Value,
                    LocationId = a.Occurrence.LocationId.Value,
                    ScheduleId = a.Occurrence.ScheduleId.Value
                } )
                .ToList();

            var groupIds = items.Select( a => a.GroupId ).Distinct().ToList();
            var locationIds = items.Select( a => a.LocationId ).Distinct().ToList();
            var scheduleIds = items.Select( a => a.ScheduleId ).Distinct().ToList();

            // Get all the groups, locations and schedules from cache in bulk.
            // This improves performance because it is very likely that otherwise
            // we would be requesting the same cached object repeatedly.
            var groups = GroupCache.GetMany( groupIds, RockContext )
                .Select( g => new
                {
                    g.Id,
                    Bag = new CheckInItemBag
                    {
                        Id = g.IdKey,
                        Name = g.Name
                    }
                } )
                .ToList();
            var locations = NamedLocationCache.GetMany( locationIds, RockContext )
                .Select( l => new
                {
                    l.Id,
                    Bag = new CheckInItemBag
                    {
                        Id = l.IdKey,
                        Name = l.Name
                    }
                } )
                .ToList();
            var schedules = NamedScheduleCache.GetMany( scheduleIds, RockContext )
                .Select( s => new
                {
                    s.Id,
                    Bag = new CheckInItemBag
                    {
                        Id = s.IdKey,
                        Name = s.Name
                    }
                } )
                .ToList();

            return items
                .Select( a =>
                {
                    var group = groups.FirstOrDefault( g => g.Id == a.GroupId );
                    var location = locations.FirstOrDefault( l => l.Id == a.LocationId );
                    var schedule = schedules.FirstOrDefault( s => s.Id == a.ScheduleId );

                    if ( group == null || location == null || schedule == null )
                    {
                        return null;
                    }

                    return new ReprintAttendanceBag
                    {
                        Id = IdHasher.Instance.GetHash( a.Id ),
                        StartDateTime = a.StartDateTime.ToRockDateTimeOffset(),
                        NickName = a.NickName,
                        LastName = a.LastName,
                        SecurityCode = a.Code,
                        Group = group.Bag,
                        Location = location.Bag,
                        Schedule = schedule.Bag
                    };
                } )
                .Where( a => a != null )
                .ToList();
        }

        /// <summary>
        /// Gets the curent attendance to be used with calculation room counts.
        /// </summary>
        /// <param name="locationIds">The locations to query to get attendance.</param>
        /// <param name="campusId">The campus to limit the attendance records to and determine the current timestamp.</param>
        /// <returns>A set of objects that represent the attendance records that can be used to determine counts in various ways.</returns>
        private List<ActiveAttendanceBag> GetCurrentAttendanceForLocations( List<int> locationIds, int? campusId )
        {
            var campus = campusId.HasValue ? CampusCache.Get( campusId.Value ) : null;
            var now = campus?.CurrentDateTime ?? RockDateTime.Now;

            // Load only the required properties for the current attendance.
            return CheckInDirector.GetCurrentAttendance( now, locationIds, RockContext )
                .Select( a => new ActiveAttendanceBag
                {
                    Id = a.AttendanceId,
                    AreaId = a.GroupTypeId,
                    GroupId = a.GroupId,
                    LocationId = a.LocationId,
                    Status = a.Status
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the groups and related location identifiers for the set of areas.
        /// </summary>
        /// <param name="areaIds">The area identifiers whose groups and locations are retrieved.</param>
        /// <returns>A collection of <see cref="GroupOpportunityBag"/> objects that represent the groups and locations.</returns>
        private List<GroupOpportunityBag> GetGroupsAndLocationsForAreas( List<int> areaIds )
        {
            var groupLocationQry = new GroupLocationService( RockContext ).Queryable()
                .Select( gl => new
                {
                    gl.LocationId,
                    gl.GroupId
                } );

            // Load all groups for these areas and the associated location identifiers.
            var groupsAndLocations = new GroupService( RockContext )
                .Queryable()
                .Where( g => areaIds.Contains( g.GroupTypeId ) )
                .GroupJoin( groupLocationQry, g => g.Id, gl => gl.GroupId, ( g, gl ) => new
                {
                    Group = g,
                    GroupLocation = gl
                } )
                .SelectMany( a => a.GroupLocation.DefaultIfEmpty(), ( g, gl ) => new
                {
                    g.Group.Id,
                    g.Group.Name,
                    g.Group.GroupTypeId,
                    LocationId = ( int? ) gl.LocationId
                } )
                .ToList();

            // Now we have a list of multiple rows for groups. Aggregate that
            // data into a single group object that contains all the location
            // identifiers as a list of IdKey values.
            return groupsAndLocations
                .GroupBy( a => new { a.Id, a.Name, a.GroupTypeId } )
                .Select( g => new GroupOpportunityBag
                {
                    Id = IdHasher.Instance.GetHash( g.Key.Id ),
                    Name = g.Key.Name,
                    AreaId = IdHasher.Instance.GetHash( g.Key.GroupTypeId ),
                    LocationIds = g.Where( l => l.LocationId.HasValue )
                        .Select( l => IdHasher.Instance.GetHash( l.LocationId.Value ) )
                        .ToList()
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the path to the group, including the group itself. This is
        /// represented as a string like <c>&quot;Grand Parent &gt; Parent &gt;
        /// Child&quot;</c>.
        /// </summary>
        /// <param name="groupId">The group identifier whose path is to be returned.</param>
        /// <returns>A string representing the group path.</returns>
        private string GetGroupPath( int groupId )
        {
            var names = new List<string>();
            var group = GroupCache.Get( groupId, RockContext );

            while ( group != null && !group.IsArchived )
            {
                names.Add( group.Name );
                group = group.ParentGroup;
            }

            names.Reverse();

            return string.Join( " > ", names );
        }

        /// <summary>
        /// Gets the path to the group, not including the location itself. This is
        /// represented as a string like <c>&quot;Grand Parent &gt; Parent&quot;</c>.
        /// </summary>
        /// <param name="locationId">The location identifier whose path is to be returned.</param>
        /// <returns>A string representing the group path.</returns>
        private string GetLocationAncestorPath( int locationId )
        {
            var names = new List<string>();
            var location = NamedLocationCache.Get( locationId, RockContext );

            while ( location != null )
            {
                names.Add( location.Name );
                location = location.ParentLocation;
            }

            names.Reverse();

            return string.Join( " > ", names );
        }

        /// <summary>
        /// Gets the schedule bags that are available for use when configuring
        /// scheduling for the group locations.
        /// </summary>
        /// <returns>A collection of bags that represent the schedules.</returns>
        private List<CheckInItemBag> GetScheduleBags()
        {
            int scheduleCategoryId = CategoryCache.Get( Rock.SystemGuid.Category.SCHEDULE_SERVICE_TIMES.AsGuid() ).Id;
            var scheduleService = new ScheduleService( RockContext );

            // Limit Schedules to ones that have a CheckInStartOffsetMinutes
            // and are active and in the right category.
            return scheduleService.Queryable()
                .Where( a => a.CheckInStartOffsetMinutes != null
                    && a.IsActive
                    && a.CategoryId == scheduleCategoryId )
                .OrderBy( s => s.Name )
                .Select( s => new
                {
                    s.Id,
                    s.Name
                } )
                .ToList()
                .Select( s => new CheckInItemBag
                {
                    Id = IdHasher.Instance.GetHash( s.Id ),
                    Name = s.Name
                } )
                .ToList();
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the kiosk configuration to use given the saved configuration
        /// options.
        /// </summary>
        /// <param name="savedConfiguration">The options the kiosk was configured with.</param>
        /// <returns>The full configuration data for the kiosk.</returns>
        [BlockAction]
        public BlockActionResult GetKioskConfiguration( SavedKioskConfigurationBag savedConfiguration )
        {
            var director = new CheckInDirector( RockContext );
            var configuration = GetKioskConfiguration( director, savedConfiguration );

            if ( configuration == null )
            {
                return ActionBadRequest( "Configuration is not valid." );
            }

            return ActionOk( configuration );
        }

        /// <summary>
        /// Gets the promotion list defined for the template and kiosk.
        /// </summary>
        /// <param name="templateId">The check-in template identifier.</param>
        /// <param name="kioskId">The kiosk identifier.</param>
        /// <returns>A list of <see cref="PromotionBag"/> objects.</returns>
        [BlockAction]
        public BlockActionResult GetPromotionList( string templateId, string kioskId )
        {
            var promotionContentChannelGuid = GetAttributeValue( AttributeKey.PromotionsContentChannel ).AsGuidOrNull();

            if ( !promotionContentChannelGuid.HasValue )
            {
                return ActionOk( new List<PromotionBag>() );
            }

            var kiosk = DeviceCache.GetByIdKey( kioskId, RockContext );

            if ( kiosk == null )
            {
                return ActionBadRequest( "Invalid kiosk." );
            }

            return ActionOk( GetPromotionItems( kiosk.GetCampusId() ) );
        }

        /// <summary>
        /// Gets the current attendance counts for this kiosk. This will return
        /// all attendance records that match the kiosk's configured locations
        /// and the specified area list.
        /// </summary>
        /// <param name="kioskId">The encrypted kiosk identifier.</param>
        /// <param name="areaIds">The list of encrypted area identifiers.</param>
        /// <returns>An object that contains all the information to display the counts.</returns>
        [BlockAction]
        public BlockActionResult GetCurrentAttendance( string kioskId, List<string> areaIds )
        {
            var kiosk = DeviceCache.GetByIdKey( kioskId, RockContext );

            if ( kiosk == null )
            {
                return ActionBadRequest( "Kiosk not found." );
            }

            var kioskLocations = kiosk.GetAllLocations().ToList();
            var kioskLocationIds = kioskLocations.Select( l => l.Id ).ToList();
            var attendance = GetCurrentAttendanceForLocations( kioskLocationIds, kiosk.GetCampusId() );

            // Translate all the area IdKey values to Id numbers.
            var areaIdNumbers = areaIds.Select( id => IdHasher.Instance.GetId( id ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            var groups = GetGroupsAndLocationsForAreas( areaIdNumbers );

            // Translate all the locations into bags the client will be able
            // to use.
            var locations = kioskLocations
                .Select( l => new LocationStatusItemBag
                {
                    Id = l.IdKey,
                    Name = l.Name,
                    IsOpen = l.IsActive
                } )
                .ToList();

            return ActionOk( new GetCurrentAttendanceResponseBag
            {
                Attendance = attendance,
                Groups = groups,
                Locations = locations
            } );
        }

        /// <summary>
        /// Verifies that the PIN code is valid and can be used. This is used
        /// by the UI to perform the initial login step to the admins creen.
        /// </summary>
        /// <param name="pinCode">The PIN code to validate.</param>
        /// <returns>A 200-OK status if the PIN code was valid.</returns>
        [BlockAction]
        public BlockActionResult ValidatePinCode( string pinCode )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            return ActionOk();
        }

        /// <summary>
        /// Sets the open/closed status of a location.
        /// </summary>
        /// <param name="pinCode">The PIN code to authenticate the person as.</param>
        /// <param name="locationId">The encrypted identifier of the location.</param>
        /// <param name="isOpen"><c>true</c> if the location should be opened; otherwise <c>false</c>.</param>
        /// <returns>A 200-OK status if the location's status was updated.</returns>
        [BlockAction]
        public BlockActionResult SetLocationStatus( string pinCode, string locationId, bool isOpen )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var location = new LocationService( RockContext ).Get( locationId, false );

            if ( location == null )
            {
                return ActionBadRequest( "Location not found." );
            }

            location.IsActive = isOpen;
            RockContext.SaveChanges();

            // Clear the old v1 cache to match functionality.
            Rock.CheckIn.KioskDevice.Clear();

            return ActionOk();
        }

        /// <summary>
        /// Gets the attendance list for use when displaying the list of attendance
        /// records that can be reprinted.
        /// </summary>
        /// <param name="pinCode">The PIN code to authorize the request.</param>
        /// <param name="kioskId">The encrypted identifier of the kiosk.</param>
        /// <param name="searchValue">An optional search value to limit the results.</param>
        /// <returns>A list of attendance records.</returns>
        [BlockAction]
        public BlockActionResult GetReprintAttendanceList( string pinCode, string kioskId, string searchValue )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var kiosk = DeviceCache.GetByIdKey( kioskId, RockContext );

            if ( kiosk == null )
            {
                return ActionBadRequest( "Kiosk was not found." );
            }

            return ActionOk( GetCurrentAttendanceForReprint( kiosk.GetCampusId() ) );
        }

        /// <summary>
        /// Request to print labels for a single attendance record that already
        /// exists in the database. This is used to re-print lost labels.
        /// </summary>
        /// <param name="pinCode">The PIN code to authorize the request.</param>
        /// <param name="kioskId">The encrypted identifier of the kiosk.</param>
        /// <param name="attendanceId">The encrypted identifier of the attendance to print labels for.</param>
        /// <returns>A 200-OK response that indicates if any errors occurred during printing.</returns>
        [BlockAction]
        public async Task<BlockActionResult> PrintLabels( string pinCode, string kioskId, string attendanceId )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var printer = GetKioskPrinter( kioskId );

            if ( printer == null )
            {
                return ActionBadRequest( "This kiosk does not have a printer defined." );
            }

            var attendanceIdNumber = IdHasher.Instance.GetId( attendanceId );

            if ( !attendanceIdNumber.HasValue )
            {
                return ActionBadRequest( "Invalid attendance." );
            }

            // If the attendance record has legacy labels then use those for
            // re-printing instead of the new label format.
            if ( RockContext.Set<AttendanceData>().Any( a => a.Id == attendanceIdNumber.Value ) )
            {
                var attendance = new AttendanceService( RockContext ).Get( attendanceIdNumber.Value );
                var attendanceIds = new List<int> { attendance.Id };
                var possibleLabels = ZebraPrint.GetLabelTypesForPerson( attendance.PersonAlias.PersonId, attendanceIds );
                var fileGuids = possibleLabels.Select( pl => pl.FileGuid ).ToList();

                var (legacyMessages, legacyClientLabels) = ZebraPrint.ReprintZebraLabels( fileGuids, attendance.PersonAlias.PersonId, attendanceIds, null );

                var clientLabels = legacyClientLabels
                    .Select( label => new LegacyClientLabelBag
                    {
                        LabelFile = RequestContext.RootUrlPath + label.LabelFile,
                        LabelKey = label.LabelKey,
                        MergeFields = label.MergeFields,
                        PrinterAddress = label.PrinterAddress
                    } )
                    .ToList();

                return ActionOk( new
                {
                    ErrorMessages = legacyMessages,
                    LegacyLabels = clientLabels
                } );
            }

            // Use the new label format for re-printing.
            var labels = director.LabelProvider.RenderLabels( new List<int> { attendanceIdNumber.Value }, null, false );

            var errorMessages = labels.Where( l => l.Error.IsNotNullOrWhiteSpace() )
                .Select( l => l.Error )
                .ToList();

            if ( !labels.Any() )
            {
                errorMessages.Add( "No labels to print." );
            }

            labels = labels.Where( l => l.Error.IsNullOrWhiteSpace() ).ToList();
            foreach ( var label in labels )
            {
                label.PrintTo = printer;
                label.PrintFrom = PrintFrom.Server;
            }

            // Print the labels with a 5 second timeout.
            var cts = new CancellationTokenSource( 5_000 );
            var printProvider = new LabelPrintProvider();

            try
            {
                var printerErrors = await printProvider.PrintLabelsAsync( labels, cts.Token );

                errorMessages.AddRange( printerErrors );
            }
            catch ( TaskCanceledException ) when ( cts.IsCancellationRequested )
            {
                errorMessages.Add( "Timeout waiting for labels to print." );
            }

            return ActionOk( new
            {
                ErrorMessages = errorMessages
            } );
        }

        /// <summary>
        /// Gets all the currently scheduled group locations. This is used by
        /// the Schedule Locations screen to build the grid of locations and
        /// schedules that are currently configured.
        /// </summary>
        /// <param name="pinCode">The PIN code to authorize the request.</param>
        /// <param name="kioskId">The encrypted identifier of the kiosk.</param>
        /// <param name="areaIds">The encrypted identifiers of the areas the kiosk is configured for.</param>
        /// <returns>The list of schedules and locations that can be scheduled.</returns>
        [BlockAction]
        public BlockActionResult GetScheduledLocations( string pinCode, string kioskId, List<string> areaIds )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var kiosk = DeviceCache.GetByIdKey( kioskId, RockContext );

            if ( kiosk == null )
            {
                return ActionBadRequest( "Invalid kiosk." );
            }

            // Translate all the IdKey values to Id numbers.
            var areaIdNumbers = areaIds.Select( id => IdHasher.Instance.GetId( id ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            var groupLocationService = new GroupLocationService( RockContext );
            var groupTypeService = new GroupTypeService( RockContext );
            var locationIds = kiosk.GetAllLocationIds().ToList();

            var templateGroupPaths = new Dictionary<int, List<CheckinAreaPath>>();
            var currentAndDescendantGroupTypeIds = new List<int>();
            foreach ( var groupType in GroupTypeCache.GetMany( areaIdNumbers ) )
            {
                foreach ( var parentGroupType in groupType.ParentGroupTypes )
                {
                    if ( !templateGroupPaths.ContainsKey( parentGroupType.Id ) )
                    {
                        templateGroupPaths.Add( parentGroupType.Id, groupTypeService.GetCheckinAreaDescendantsPath( parentGroupType.Id ).ToList() );
                    }
                }

                currentAndDescendantGroupTypeIds.Add( groupType.Id );
                currentAndDescendantGroupTypeIds.AddRange( groupTypeService.GetCheckinAreaDescendants( groupType.Id ).Select( a => a.Id ).ToList() );
            }

            var areaPaths = new List<CheckinAreaPath>( templateGroupPaths.Values.SelectMany( v => v ) );

            var groupLocations = groupLocationService.Queryable()
                .Where( gl => currentAndDescendantGroupTypeIds.Contains( gl.Group.GroupTypeId ) )
                .Where( gl => locationIds.Contains( gl.LocationId ) )
                .OrderBy( gl => gl.Group.Name )
                .ThenBy( gl => gl.Location.Name )
                .Select( gl => new
                {
                    GroupLocationId = gl.Id,
                    LocationId = gl.Location.Id,
                    LocationName = gl.Location.Name,
                    gl.Location.ParentLocationId,
                    gl.GroupId,
                    GroupName = gl.Group.Name,
                    ScheduleIdList = gl.Schedules.Select( s => s.Id ),
                    gl.Group.GroupTypeId
                } )
                .ToList();

            var locationService = new LocationService( RockContext );
            var locationPaths = new Dictionary<int, string>();

            var scheduledLocations = groupLocations
                .Select( groupLocation =>
                {
                    var scheduledLocation = new ScheduledLocationBag
                    {
                        GroupLocationId = IdHasher.Instance.GetHash( groupLocation.GroupLocationId ),
                        GroupPath = GetGroupPath( groupLocation.GroupId ),
                        AreaPath = areaPaths.Where( gt => gt.GroupTypeId == groupLocation.GroupTypeId )
                            .Select( gt => gt.Path )
                            .FirstOrDefault(),
                        LocationName = groupLocation.LocationName,
                        ScheduleIds = groupLocation.ScheduleIdList
                            .Select( id => IdHasher.Instance.GetHash( id ) )
                            .ToList()
                    };

                    if ( groupLocation.ParentLocationId.HasValue )
                    {
                        if ( !locationPaths.TryGetValue( groupLocation.ParentLocationId.Value, out var locationPath ) )
                        {
                            locationPath = GetLocationAncestorPath( groupLocation.ParentLocationId.Value );

                            locationPaths.Add( groupLocation.ParentLocationId.Value, locationPath );
                        }

                        scheduledLocation.LocationPath = locationPath;
                    }

                    return scheduledLocation;
                } )
                .ToList();

            return ActionOk( new GetScheduledLocationsResponseBag
            {
                Schedules = GetScheduleBags(),
                ScheduledLocations = scheduledLocations
            } );
        }

        /// <summary>
        /// Saves all the scheduled locations to match the data provided. This
        /// will add and remove any GroupLocationSchedule that are required to
        /// match the provided values.
        /// </summary>
        /// <param name="pinCode">The PIN code to authorize the request.</param>
        /// <param name="scheduledLocations">The group locations and selected schedules.</param>
        /// <returns>A 200-OK response that indicates everything was saved correctly.</returns>
        [BlockAction]
        public BlockActionResult SaveScheduledLocations( string pinCode, List<ScheduledLocationBag> scheduledLocations )
        {
            var director = new CheckInDirector( RockContext );

            if ( !director.TryAuthenticatePin( pinCode, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            // Normally we would want to validate all the data to be sure it
            // only specified valid locations and groups. But in the case of
            // this block we have decided that we are trusting the client by
            // way of the pinCode.

            // Load all the group locations in a single query, along with the
            // schedule information.
            var groupLocationIds = scheduledLocations
                .Select( sl => IdHasher.Instance.GetId( sl.GroupLocationId ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();
            var groupLocations = new GroupLocationService( RockContext )
                .Queryable()
                .Include( gl => gl.Schedules )
                .Where( gl => groupLocationIds.Contains( gl.Id ) )
                .ToList();

            // Get the schedule IdKey values that are valid. This is used so we
            // don't delete a schedule that wasn't available for selection.
            var validScheduleIds = GetScheduleBags().Select( s => s.Id ).ToList();
            var scheduleService = new ScheduleService( RockContext );

            foreach ( var scheduledLocation in scheduledLocations )
            {
                var groupLocation = groupLocations.FirstOrDefault( gl => gl.IdKey == scheduledLocation.GroupLocationId );

                if ( groupLocation == null )
                {
                    return ActionBadRequest( "Group or Location was not valid." );
                }

                // Add any schedules that are new.
                foreach ( var scheduleIdKey in scheduledLocation.ScheduleIds )
                {
                    var scheduleId = IdHasher.Instance.GetId( scheduleIdKey );

                    if ( !scheduleId.HasValue )
                    {
                        continue;
                    }

                    if ( !groupLocation.Schedules.Any( s => s.Id == scheduleId ) )
                    {
                        groupLocation.Schedules.Add( scheduleService.Get( scheduleId.Value ) );
                    }
                }

                // Remove any schedules that are old.
                foreach ( var schedule in groupLocation.Schedules.ToList() )
                {
                    if ( !scheduledLocation.ScheduleIds.Contains( schedule.IdKey ) && validScheduleIds.Contains( schedule.IdKey ) )
                    {
                        groupLocation.Schedules.Remove( schedule );
                    }
                }
            }

            if ( RockContext.SaveChanges() > 0 )
            {
                // Temporary until legacy check-in is removed.
                KioskDevice.Clear();
            }

            return ActionOk();
        }

        #endregion
    }
}
