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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Core
{
    /// <summary>
    /// Takes a entity type and displays a person's following items for that entity using a Lava template.
    /// </summary>
    [DisplayName( "Following By Entity" )]
    [Category( "Core" )]
    [Description( "Takes a entity type and displays a person's following items for that entity using a Lava template." )]

    [EntityTypeField( "Entity Type",
        Description = "The type of entity to show following for.",
        Order = 0,
        Key = AttributeKey.EntityType )]

    [TextField( "Link URL",
        Description = "The address to use for the link. The text '[Id]' will be replaced with the Id of the entity '[Guid]' will be replaced with the Guid.",
        IsRequired = false,
        DefaultValue = "/samplepage/[Id]",
        Order = 1,
        Key = AttributeKey.LinkUrl )]

    [CodeEditorField( "Lava Template",
        Description = "Lava template to use to display content",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = true,
        DefaultValue = @"<div class=""panel panel-block""> 
<div class=""panel-heading"">
    <h4 class=""panel-title"">Followed {{ EntityType | Pluralize }}</h4>
</div>
<div class=""panel-body"">
    <ul>
    {% for item in FollowingItems %}
        {% assign deleteKey = item.Id | Append:',' | Append:BlockId %}

        {% if EntityType == 'Person' %}
            {% assign itemName = item.FullName %}
        {% else %}
            {% assign itemName = item.Name %}
        {% endif %}
        {% if LinkUrl != '' %}
            <li><a href=""{{ LinkUrl | Replace:'[Id]',item.Id }}"">{{ itemName }}</a> 
            <a class=""pull-right"" href = ""#"" onclick = ""{{ deleteKey | Postback:'DeleteFollowing' }}"">
            <i class=""fa fa-times""></i>
			</a></li>
        {% else %}
            <li>{{ itemName }}
            <a class=""pull-right"" href = ""#"" onclick = ""{{ deleteKey | Postback:'DeleteFollowing' }}"">
            <i class=""fa fa-times""></i>
			</a></li>
        {% endif %}
    {% endfor %}

    {% if HasMore %}
        <li><i class='fa fa-fw''></i> <small>(showing top {{ Quantity }})</small></li>
    {% endif %}
    </ul>
</div>
</div>",
        Order = 2,
        Key = AttributeKey.LavaTemplate )]

    [IntegerField( "Max Results",
        Description = "The maximum number of results to display.",
        IsRequired = true,
        DefaultValue = "100",
        Order = 4,
        Key = AttributeKey.MaxResults )]

    [Rock.SystemGuid.BlockTypeGuid( "36B56055-7AA2-4169-82DD-CCFBD2C7B4CC" )]
    public partial class FollowingByEntityLava : Rock.Web.UI.RockBlock
    {
        public static class AttributeKey
        {
            public const string EntityType = "EntityType";
            public const string LinkUrl = "LinkUrl";
            public const string MaxResults = "MaxResults";
            public const string LavaTemplate = "LavaTemplate";
        }

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            RouteAction();
            if ( !Page.IsPostBack )
            {
                LoadContent();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            RouteAction();
            LoadContent();
        }

        #endregion

        #region Methods

        protected void LoadContent()
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            var entityType = EntityTypeCache.Get( GetAttributeValue( AttributeKey.EntityType ).AsGuid() );

            if ( entityType != null )
            {

                RockContext rockContext = new RockContext();

                int personId = this.CurrentPersonId.Value;

                var followingService = new FollowingService( rockContext );
                IQueryable<IEntity> qryFollowedItems = followingService.GetFollowedItems( entityType.Id, personId );

                int quantity = GetAttributeValue( AttributeKey.MaxResults ).AsInteger();
                var items = qryFollowedItems.Take( quantity + 1 ).Distinct().ToList();

                bool hasMore = ( quantity < items.Count );

                mergeFields.Add( "FollowingItems", items.Take( quantity ) );
                mergeFields.Add( "HasMore", hasMore );
                mergeFields.Add( "EntityType", entityType.FriendlyName );
                mergeFields.Add( "LinkUrl", GetAttributeValue( AttributeKey.LinkUrl ) );
                mergeFields.Add( "Quantity", quantity );
                mergeFields.Add( "BlockId", this.BlockId );

                string template = GetAttributeValue( AttributeKey.LavaTemplate );
                lContent.Text = template.ResolveMergeFields( mergeFields );

            }
            else
            {
                lContent.Text = string.Format( "<div class='alert alert-warning'>Please configure an entity in the block settings." );

            }
        }

        /// <summary>
        /// Route the request to the correct panel
        /// </summary>
        private void RouteAction()
        {
            var sm = ScriptManager.GetCurrent( Page );

            if ( Request.Form["__EVENTARGUMENT"] != null )
            {
                string[] eventArgs = Request.Form["__EVENTARGUMENT"].Split( '^' );

                if ( eventArgs.Length == 2 )
                {
                    string action = eventArgs[0];
                    var parameters = eventArgs[1].Split( ',' );

                    if ( parameters.Length != 2 )
                    {
                        return;
                    }

                    var entityId = parameters[0].AsIntegerOrNull();
                    var blockId = parameters[1].AsIntegerOrNull();

                    if ( entityId == null || blockId != this.BlockId )
                    {
                        return;
                    }

                    switch ( action )
                    {
                        case "DeleteFollowing":
                            DeleteFollowing( entityId.Value );
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the entity.
        /// </summary>
        /// <param name="entityId">The group member identifier.</param>
        private void DeleteFollowing( int entityId )
        {
            var entityType = EntityTypeCache.Get( GetAttributeValue( AttributeKey.EntityType ).AsGuid() );
            var personId = this.CurrentPersonId.Value;

            using ( var rockContext = new RockContext() )
            {
                List<int> entityIds = new List<int> { entityId };

                // If this is a person we need to use the Person Alias instead.
                if ( entityType.Guid == Rock.SystemGuid.EntityType.PERSON.AsGuid() )
                {
                    entityIds = new PersonAliasService( rockContext )
                        .Queryable()
                        .Where( pa => pa.PersonId == entityId )
                        .Select( pa => pa.Id )
                        .ToList();

                    entityType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON_ALIAS.AsGuid() );
                }

                var followingService = new FollowingService( rockContext );

                var followings = followingService.Queryable()
                            .Where( a => a.EntityTypeId == entityType.Id )
                            .Where( a => entityIds.Contains( a.EntityId ) )
                            .Where( a => string.IsNullOrEmpty( a.PurposeKey ) )
                            .Where( a => a.PersonAlias.PersonId == personId );

                foreach ( var following in followings )
                {
                    followingService.Delete( following );
                }

                rockContext.SaveChanges();
            }

            LoadContent();
        }

        #endregion
    }
}