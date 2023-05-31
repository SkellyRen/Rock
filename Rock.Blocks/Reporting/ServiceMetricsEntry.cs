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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Blocks.Reporting.ServiceMetricsEntry;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Blocks.Reporting
{
    /// <summary>
    /// Block for easily adding/editing metric values for any metric that has partitions of campus and service time.
    /// </summary>
    [DisplayName( "Service Metrics Entry" )]
    [Category( "Reporting" )]
    [Description( "Block for easily adding/editing metric values for any metric that has partitions of campus and service time." )]

    #region Block Attributes

    [CategoryField(
        "Schedule Category",
        Key = AttributeKey.ScheduleCategory,
        Description = "The schedule category to use for list of service times.",
        AllowMultiple = true,
        EntityTypeName = "Rock.Model.Schedule",
        IsRequired = true,
        Order = 0 )]

    [IntegerField(
        "Weeks Back",
        Key = AttributeKey.WeeksBack,
        Description = "The number of weeks back to display in the 'Week of' selection.",
        IsRequired = false,
        DefaultIntegerValue = 8,
        Order = 1 )]

    [IntegerField(
        "Weeks Ahead",
        Key = AttributeKey.WeeksAhead,
        Description = "The number of weeks ahead to display in the 'Week of' selection.",
        IsRequired = false,
        DefaultIntegerValue = 0,
        Order = 2 )]

    [MetricCategoriesField(
        "Metric Categories",
        Key = AttributeKey.MetricCategories,
        Description = "Select the metric categories to display (note: only metrics in those categories with a campus and schedule partition will displayed).",
        IsRequired = true,
        Order = 3 )]

    [CampusesField( "Campuses", "Select the campuses you want to limit this block to.", false, "", "", 4, AttributeKey.Campuses )]

    [BooleanField(
        "Insert 0 for Blank Items",
        Key = AttributeKey.DefaultToZero,
        Description = "If enabled, a zero will be added to any metrics that are left empty when entering data.",
        DefaultValue = "false",
        Order = 5 )]

    [CustomDropdownListField(
        "Metric Date Determined By",
        Key = AttributeKey.MetricDateDeterminedBy,
        Description = "This setting determines what date to use when entering the metric. 'Sunday Date' would use the selected Sunday date. 'Day from Schedule' will use the first day configured from the selected schedule.",
        DefaultValue = "0",
        ListSource = "0^Sunday Date,1^Day from Schedule",
        Order = 6 )]

    [BooleanField(
        "Limit Campus Selection to Campus Team Membership",
        Key = AttributeKey.LimitCampusByCampusTeam,
        Description = "When enabled, this would limit the campuses shown to only those where the individual was on the Campus Team.",
        DefaultBooleanValue = false,
        Order = 7 )]

    [DefinedValueField(
        "Campus Types",
        Key = AttributeKey.CampusTypes,
        Description = "Note: setting this can override the selected Campuses block setting.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_TYPE,
        AllowMultiple = true,
        IsRequired = false,
        Order = 8 )]

    [DefinedValueField(
        "Campus Status",
        Key = AttributeKey.CampusStatus,
        Description = "Note: setting this can override the selected Campuses block setting.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_STATUS,
        AllowMultiple = true,
        IsRequired = false,
        Order = 9 )]

    [BooleanField(
        "Filter Schedules by Campus",
        Key = AttributeKey.FilterByCampus,
        Description = "When enabled, only schedules that are included in the Campus Schedules will be included.",
        DefaultBooleanValue = false,
        Order = 10 )]

    #endregion Block Attributes

    [Rock.SystemGuid.EntityTypeGuid( "46199E9D-59CC-4CBC-BC05-83F6FF193147" )]
    [Rock.SystemGuid.BlockTypeGuid( "E6144C7A-2E95-431B-AB75-C588D151ACA4" )]
    public class ServiceMetricsEntry : RockObsidianBlockType
    {
        #region Keys

        private static class AttributeKey
        {
            public const string ScheduleCategory = "ScheduleCategory";
            public const string WeeksBack = "WeeksBack";
            public const string WeeksAhead = "WeeksAhead";
            public const string MetricCategories = "MetricCategories";
            public const string Campuses = "Campuses";
            public const string DefaultToZero = "DefaultToZero";
            public const string MetricDateDeterminedBy = "MetricDateDeterminedBy";
            public const string LimitCampusByCampusTeam = "LimitCampusByCampusTeam";
            public const string CampusTypes = "CampusTypes";
            public const string CampusStatus = "CampusStatus";
            public const string FilterByCampus = "FilterByCampus";
        }

        private static class PageParameterKey
        {
            public const string CampusId = "CampusId";
        }

        private static class UserPreferenceKey
        {
            public const string CampusId = "CampusGuid";
            public const string ScheduleId = "ScheduleGuid";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the metric categories block setting.
        /// </summary>
        private List<MetricCategoriesFieldAttribute.MetricCategoryPair> MetricCategories
        {
            get
            {
                return MetricCategoriesFieldAttribute.GetValueAsGuidPairs( this.GetAttributeValue( AttributeKey.MetricCategories ) );
            }
        }

        #endregion

        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        public override object GetObsidianBlockInitialization()
        {
            var box = GetInitializationBox();

            if ( !GetAttributeValue( AttributeKey.ScheduleCategory ).SplitDelimitedValues().AsGuidList().Any() )
            {
                box.ErrorMessage = "Please set the schedule category to use for list of service times.";
            }
            else if ( !this.MetricCategories.Any() )
            {
                box.ErrorMessage = "Please select the metric categories to display.";
            }

            return box;
        }

        private ServiceMetricsEntryInitializationBox GetInitializationBox()
        {
            return new ServiceMetricsEntryInitializationBox();
        }

        #region Block Actions

        // TODO JMH Do this in Obsidian code.
        ///// <summary>
        ///// Handles the BlockUpdated event of the control.
        ///// </summary>
        ///// <param name="sender">The source of the event.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //protected void Block_BlockUpdated( object sender, EventArgs e )
        //{
        //    pnlConfigurationError.Visible = false;
        //    nbNoCampuses.Visible = false;

        //    if ( CheckSelection() )
        //    {
        //        LoadDropDowns();
        //        BindMetrics();
        //    }
        //}

        // TODO JMH Do this in the Obsidian code.
        ///// <summary>
        ///// Handles the ItemCommand event of the rptrSelection control.
        ///// </summary>
        ///// <param name="source">The source of the event.</param>
        ///// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        //protected void rptrSelection_ItemCommand( object source, RepeaterCommandEventArgs e )
        //{
        //    switch ( e.CommandName )
        //    {
        //        case "Campus":
        //            _selectedCampusId = e.CommandArgument.ToString().AsIntegerOrNull();
        //            break;
        //        case "Weekend":
        //            _selectedWeekend = e.CommandArgument.ToString().AsDateTime();
        //            break;
        //        case "Service":
        //            _selectedServiceId = e.CommandArgument.ToString().AsIntegerOrNull();
        //            break;
        //    }

        //    if ( CheckSelection() )
        //    {
        //        LoadDropDowns();
        //        BindMetrics();
        //    }
        //}

        // TODO JMH Make sure number box has numeric rules for metric values.
        ///// <summary>
        ///// Handles the ItemDataBound event of the rptrMetric control.
        ///// </summary>
        ///// <param name="sender">The source of the event.</param>
        ///// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        //protected void rptrMetric_ItemDataBound( object sender, RepeaterItemEventArgs e )
        //{
        //    if ( e.Item.ItemType == ListItemType.Item )
        //    {
        //        var nbMetricValue = e.Item.FindControl( "nbMetricValue" ) as NumberBox;
        //        if ( nbMetricValue != null )
        //        {
        //            nbMetricValue.ValidationGroup = BlockValidationGroup;
        //        }
        //    }
        //}

        // TODO JMH Do this in the Obsidian code.
        ///// <summary>
        ///// Handles the SelectionChanged event of the filter controls.
        ///// </summary>
        ///// <param name="sender">The source of the event.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //protected void bddl_SelectionChanged( object sender, EventArgs e )
        //{
        //    if ( sender == bddlWeekend )
        //    {
        //        _selectedWeekend = bddlWeekend.SelectedValue.AsDateTime();
        //        LoadServicesDropDown();
        //    }
        //    else if ( sender == bddlCampus )
        //    {
        //        _selectedCampusId = bddlCampus.SelectedValueAsId();
        //        LoadServicesDropDown();
        //    }
        //    BindMetrics();
        //}

        /// <summary>
        /// Gets the campuses.
        /// </summary>
        /// <returns></returns>
        [BlockAction( "GetCampuses" )]
        public BlockActionResult GetCampuses()
        {
            var campuses = new List<CampusCache>();
            var allowedCampuses = GetAttributeValue( AttributeKey.Campuses ).SplitDelimitedValues().AsGuidList();
            var limitCampusByCampusTeam = GetAttributeValue( AttributeKey.LimitCampusByCampusTeam ).AsBoolean();
            var selectedCampusTypes = GetAttributeValue( AttributeKey.CampusTypes ).SplitDelimitedValues().AsGuidList();
            var selectedCampusStatuses = GetAttributeValue( AttributeKey.CampusStatus ).SplitDelimitedValues().AsGuidList();

            var campusQuery = CampusCache.All().Where( c => c.IsActive.HasValue && c.IsActive.Value );
            var currentPersonId = this.GetCurrentPerson().Id;

            // If specific campuses are selected, filter by those campuses.
            if ( allowedCampuses.Any() )
            {
                campusQuery = campusQuery.Where( c => allowedCampuses.Contains( c.Guid ) );
            }

            if ( limitCampusByCampusTeam )
            {
                var campusTeamGroupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_CAMPUS_TEAM.AsGuid() );
                var teamGroupIds = new GroupService( new RockContext() ).Queryable().AsNoTracking()
                    .Where( g => g.GroupTypeId == campusTeamGroupTypeId )
                    .Where( g => g.Members.Where( gm => gm.PersonId == currentPersonId ).Any() )
                    .Select( g => g.Id ).ToList();

                campusQuery = campusQuery.Where( c => c.TeamGroupId.HasValue && teamGroupIds.Contains( c.TeamGroupId.Value ) );
            }

            // If campus types are selected, filter by those.
            if ( selectedCampusTypes.Any() )
            {
                var campusTypes = DefinedValueCache.All().Where( d => selectedCampusTypes.Contains( d.Guid ) ).Select( d => d.Id ).ToList();
                campusQuery = campusQuery.Where( c => c.CampusTypeValueId.HasValue && campusTypes.Contains( c.CampusTypeValueId.Value ) );
            }

            // If campus statuses are selected, filter by those.
            if ( selectedCampusStatuses.Any() )
            {
                var campusStatuses = DefinedValueCache.All().Where( d => selectedCampusStatuses.Contains( d.Guid ) ).Select( d => d.Id ).ToList();
                campusQuery = campusQuery.Where( c => c.CampusStatusValueId.HasValue && campusStatuses.Contains( c.CampusStatusValueId.Value ) );
            }

            // Sort by name.
            campusQuery = campusQuery.OrderBy( c => c.Name );

            foreach ( var campus in campusQuery )
            {
                campuses.Add( campus );
            }

            return ActionOk( campuses.ToListItemBagList() );
        }

        /// <summary>
        /// Gets the weekend dates.
        /// </summary>
        /// <returns></returns>
        [BlockAction( "GetWeekendDates" )]
        public BlockActionResult GetWeekendDates( ServiceMetricsEntryGetWeekendDatesRequestBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest();
            }    

            // Default the resulting weekend dates to 1 week back and no weeks ahead.
            var weeksBack = 1;
            var weeksAhead = 0;

            if ( bag.WeeksBack.HasValue )
            {
                weeksBack = bag.WeeksBack.Value;
            }

            if ( bag.WeeksAhead.HasValue )
            {
                weeksAhead = bag.WeeksAhead.Value;
            }

            var dates = new List<DateTime>();

            // Load Weeks
            var sundayDate = RockDateTime.Today.SundayDate();
            var daysBack = weeksBack * 7;
            var daysAhead = weeksAhead * 7;
            var startDate = sundayDate.AddDays( 0 - daysBack );
            var date = sundayDate.AddDays( daysAhead );
            while ( date >= startDate )
            {
                dates.Add( date );
                date = date.AddDays( -7 );
            }

            return ActionOk( dates.Select( weekend => new ListItemBag
            {
                Value = weekend.ToString( "o" ),
                Text = $"Sunday {weekend.ToShortDateString()}"
            } ).ToList() );
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        [BlockAction( "GetServiceTimes" )]
        public BlockActionResult GetServiceTimes( ServiceMetricsEntryGetServiceTimesRequestBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest();
            }

            var services = new List<Schedule>();
            var scheduleCategoryGuids = GetAttributeValue( AttributeKey.ScheduleCategory ).SplitDelimitedValues().AsGuidList();
            foreach ( var scheduleCategoryGuid in scheduleCategoryGuids )
            {
                var scheduleCategory = CategoryCache.Get( scheduleCategoryGuid );
                if ( scheduleCategory != null )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        foreach ( var schedule in new ScheduleService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( s =>
                                s.IsActive &&
                                s.CategoryId.HasValue &&
                                s.CategoryId.Value == scheduleCategory.Id )
                            .OrderBy( s => s.Name ) )
                        {
                            services.Add( schedule );
                        }
                    }
                }
            }

            var filterByCampus = GetAttributeValue( AttributeKey.FilterByCampus ).AsBoolean();
            if ( filterByCampus )
            {
                var campus = CampusCache.Get( bag.CampusGuid.Value );
                services = services.Where( s => campus.CampusScheduleIds.Contains( s.Id ) ).ToList();
            }

            return ActionOk( services.ToListItemBagList() );
        }

        //public bool GetSelectionOptions( ServiceMetricsEntryGetSelectionOptionsBag bag )
        //{
        //    // If campus and schedule have been selected before, assume current weekend
        //    if ( _selectedCampusId.HasValue && _selectedServiceId.HasValue && !_selectedWeekend.HasValue )
        //    {
        //        _selectedWeekend = RockDateTime.Today.SundayDate();
        //    }

        //    var options = new List<ServiceMetricSelectItem>();

        //    if ( !_selectedCampusId.HasValue )
        //    {
        //        var campuses = GetCampuses();
        //        if ( campuses.Count == 0 )
        //        {
        //            pnlConfigurationError.Visible = true;
        //            nbNoCampuses.Visible = true;
        //            pnlSelection.Visible = false;
        //            pnlMetrics.Visible = false;
        //            return false;
        //        }
        //        else if ( campuses.Count == 1 )
        //        {
        //            _selectedCampusId = campuses.First().Id;
        //        }
        //        else
        //        {
        //            lSelection.Text = "Select Location:";
        //            foreach ( var campus in GetCampuses() )
        //            {
        //                options.Add( new ServiceMetricSelectItem( "Campus", campus.Id.ToString(), campus.Name ) );
        //            }
        //        }
        //    }

        //    if ( !options.Any() && !_selectedWeekend.HasValue )
        //    {
        //        lSelection.Text = "Select Week of:";
        //        foreach ( var weekend in GetWeekendDates( 1, 0 ) )
        //        {
        //            options.Add( new ServiceMetricSelectItem( "Weekend",  ) );
        //        }
        //    }

        //    if ( !options.Any() && !_selectedServiceId.HasValue )
        //    {
        //        lSelection.Text = "Select Service Time:";
        //        foreach ( var service in GetServices() )
        //        {
        //            options.Add( new ServiceMetricSelectItem( "Service", service.Id.ToString(), service.Name ) );
        //        }
        //    }

        //    if ( options.Any() )
        //    {
        //        rptrSelection.DataSource = options;
        //        rptrSelection.DataBind();

        //        pnlSelection.Visible = true;
        //        pnlMetrics.Visible = false;

        //        return false;
        //    }
        //    else
        //    {
        //        pnlSelection.Visible = false;
        //        pnlMetrics.Visible = true;

        //        return true;
        //    }
        //}

        /// <summary>
        /// Gets the service metrics.
        /// </summary>
        [BlockAction( "GetServiceMetrics" )]
        public BlockActionResult GetServiceMetrics( ServiceMetricsEntryGetServiceMetricsRequestBag bag )
        {
            var serviceMetricValues = new List<ServiceMetricBag>();

            var campusEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Campus ) ).Id;
            var scheduleEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Schedule ) ).Id;

            var campusGuid = bag.CampusGuid;
            var scheduleGuid = bag.ScheduleGuid;
            var weekendDate = bag.WeekendDate;

            var notes = new List<string>();

            if ( campusGuid.HasValue && scheduleGuid.HasValue && weekendDate.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var campusId = CampusCache.GetId( campusGuid.Value );
                    var scheduleId = new ScheduleService( rockContext ).GetId( scheduleGuid.Value );

                    // Update the preferences for the selected campus and schedule.
                    var preferences = this.GetBlockPersonPreferences();
                    preferences.SetValue( UserPreferenceKey.CampusId, campusId.HasValue ? campusId.Value.ToString() : "" );
                    preferences.SetValue( UserPreferenceKey.ScheduleId, scheduleId.HasValue ? scheduleId.Value.ToString() : "" );

                    var metricCategories = this.MetricCategories;
                    var metricGuids = metricCategories.Select( a => a.MetricGuid ).ToList();
                    weekendDate = GetWeekendDate( scheduleGuid, weekendDate, rockContext );

                    var metricValueService = new MetricValueService( rockContext );
                    var metrics = new MetricService( rockContext )
                        .GetByGuids( metricGuids )
                        .Where( m =>
                            m.MetricPartitions.Count == 2 &&
                            m.MetricPartitions.Any( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ) &&
                            m.MetricPartitions.Any( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId ) )
                        .OrderBy( m => m.Title )
                        .Select( m => new
                        {
                            m.Id,
                            m.Title,
                            CampusPartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ).Select( p => p.Id ).FirstOrDefault(),
                            SchedulePartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId ).Select( p => p.Id ).FirstOrDefault(),
                        } );

                    foreach ( var metric in metrics )
                    {
                        var serviceMetric = new ServiceMetricBag
                        {
                            Id = metric.Id,
                            Name = metric.Title
                        };

                        // Get the metric value.
                        if ( campusId.HasValue && weekendDate.HasValue && scheduleId.HasValue )
                        {
                            var metricValue = metricValueService
                                .Queryable()
                                .AsNoTracking()
                                .Where( v =>
                                    v.MetricId == metric.Id &&
                                    v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == weekendDate.Value &&
                                    v.MetricValuePartitions.Count == 2 &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.CampusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusId.Value ) &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.SchedulePartitionId && p.EntityId.HasValue && p.EntityId.Value == scheduleId.Value ) )
                                .FirstOrDefault();

                            if ( metricValue != null )
                            {
                                serviceMetric.Value = metricValue.YValue;

                                if ( !string.IsNullOrWhiteSpace( metricValue.Note ) &&
                                    !notes.Contains( metricValue.Note ) )
                                {
                                    notes.Add( metricValue.Note );
                                }

                            }
                        }

                        serviceMetricValues.Add( serviceMetric );
                    }
                }
            }

            return ActionOk( new ServiceMetricsEntryGetServiceMetricsResponseBag
            {
                ServiceMetrics = serviceMetricValues,
                Notes = notes.AsDelimited( Environment.NewLine + Environment.NewLine )
            } );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [BlockAction( "Save" )]
        public BlockActionResult Save( ServiceMetricsEntrySaveRequestBag bag )
        {
            var campusEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Campus ) ).Id;
            var scheduleEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Schedule ) ).Id;

            var campusGuid = bag.CampusGuid;
            var scheduleGuid = bag.ScheduleGuid;
            var weekendDate = bag.WeekendDate;

            if ( campusGuid.HasValue && scheduleGuid.HasValue && weekendDate.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var metricService = new MetricService( rockContext );
                    var metricValueService = new MetricValueService( rockContext );

                    weekendDate = GetWeekendDate( scheduleGuid, weekendDate, rockContext );

                    foreach ( var item in bag.Items )
                    {
                        var metricId = item.Id;
                        var metricYValue = item.Value;

                        // If no value was provided and the block is not configured to default to "0" then just skip this metric.
                        if ( !metricYValue.HasValue && !GetAttributeValue( AttributeKey.DefaultToZero ).AsBoolean() )
                        {
                            continue;
                        }

                        var metric = new MetricService( rockContext ).Get( metricId );

                        if ( metric != null )
                        {
                            var campusPartitionId = metric.MetricPartitions
                                .Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId )
                                .Select( p => p.Id )
                                .FirstOrDefault();

                            var schedulePartitionId = metric.MetricPartitions
                                .Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId )
                                .Select( p => p.Id )
                                .FirstOrDefault();

                            var metricValue = metricValueService
                                .Queryable()
                                .Where( v =>
                                    v.MetricId == metric.Id &&
                                    v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == weekendDate.Value &&
                                    v.MetricValuePartitions.Count == 2 &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == campusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusGuid.Value ) &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == schedulePartitionId && p.EntityId.HasValue && p.EntityId.Value == scheduleGuid.Value ) )
                                .FirstOrDefault();

                            if ( metricValue == null )
                            {
                                metricValue = new MetricValue
                                {
                                    MetricValueType = MetricValueType.Measure,
                                    MetricId = metric.Id,
                                    MetricValueDateTime = weekendDate.Value
                                };
                                metricValueService.Add( metricValue );

                                var campusValuePartition = new MetricValuePartition
                                {
                                    MetricPartitionId = campusPartitionId,
                                    EntityId = campusGuid.Value
                                };
                                metricValue.MetricValuePartitions.Add( campusValuePartition );

                                var scheduleValuePartition = new MetricValuePartition
                                {
                                    MetricPartitionId = schedulePartitionId,
                                    EntityId = scheduleGuid.Value
                                };
                                metricValue.MetricValuePartitions.Add( scheduleValuePartition );
                            }

                            metricValue.YValue = metricYValue ?? 0;

                            metricValue.Note = bag.Note;
                        }
                    }

                    rockContext.SaveChanges();
                }
            }

            return ActionOk();
        }

        #endregion

        #region Methods

        private static DateTime? GetFirstScheduledDate( DateTime? weekend, Schedule schedule )
        {
            var date = schedule.GetNextStartDateTime( weekend.Value );
            if ( date != null && date.Value.Date > weekend.Value )
            {
                date = schedule.GetNextStartDateTime( weekend.Value.AddDays( -7 ) );
            }

            return date;
        }

        private DateTime? GetWeekendDate( Guid? scheduleGuid, DateTime? weekend, RockContext rockContext )
        {
            if ( GetAttributeValue( AttributeKey.MetricDateDeterminedBy ).AsInteger() == 1 )
            {
                var scheduleService = new ScheduleService( rockContext );
                var schedule = scheduleService.Get( scheduleGuid.Value );
                weekend = GetFirstScheduledDate( weekend, schedule );
            }

            return weekend;
        }

        #endregion
    }
}
