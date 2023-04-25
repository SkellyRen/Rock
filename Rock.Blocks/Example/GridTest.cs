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
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Obsidian.UI;
using Rock.ViewModels.Core.Grid;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.Example
{
    /// <summary>
    /// Allows testing the new Grid component.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockObsidianBlockType" />

    [DisplayName( "Grid Test" )]
    [Category( "Example" )]
    [Description( "Allows testing the new Grid component." )]
    [IconCssClass( "fa fa-list" )]

    [Rock.SystemGuid.EntityTypeGuid( "1934a378-57d6-44d0-b7cd-4443f347a1ee" )]
    [CustomizedGrid]
    public class GridTest : RockObsidianBlockType
    {
        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        public override object GetObsidianBlockInitialization()
        {
            var builder = GetGridBuilder( GetGridAttributes() );

            return new
            {
                GridDefinition = builder.BuildDefinition()
            };
        }

        private List<AttributeCache> GetGridAttributes()
        {
            var entityTypeId = EntityTypeCache.GetId<PrayerRequest>().Value;

            return AttributeCache.GetByEntityTypeQualifier( entityTypeId, string.Empty, string.Empty, false )
                .Where( a => a.IsGridColumn )
                .OrderBy( a => a.Order )
                .ThenBy( a => a.Name )
                .ToList();
        }

        private GridBuilder<PrayerRequest> GetGridBuilder( List<AttributeCache> gridAttributes )
        {
            return new GridBuilder<PrayerRequest>()
                .WithBlock( this )
                .AddField( "guid", pr => pr.Guid.ToString() )
                .AddField( "personId", pr => pr.RequestedByPersonAlias?.PersonId )
                .AddField( "name", pr => new { pr.FirstName, pr.LastName } )
                .AddTextField( "email", pr => pr.Email )
                .AddDateTimeField( "enteredDateTime", pr => pr.EnteredDateTime )
                .AddDateTimeField( "expirationDateTime", pr => pr.ExpirationDate )
                .AddField( "isUrgent", pr => pr.IsUrgent )
                .AddField( "isPublic", pr => pr.IsPublic )
                .AddField( "id", pr => pr.Id )
                .AddField( "mode", pr => new ListItemBag
                {
                    Value = pr.IsUrgent == true ? "#900000" : "#009000",
                    Text = pr.IsUrgent != true ? "Closed" : "Open"
                } )
                .AddAttributeFields( gridAttributes );
        }

        private static Dictionary<int, int> GetPrimaryAliasIds( RockContext rockContext, List<int> personIds )
        {
            var personAliasIdLookup = new Dictionary<int, int>();
            var personAliasService = new PersonAliasService( rockContext );

            // Get the data in chunks just in case we have a large list of
            // PersonIds (to avoid a SQL Expression limit error).
            while ( personIds.Any() )
            {
                var personIdsChunk = personIds.Take( 1_000 );
                personIds = personIds.Skip( 1_000 ).ToList();

                var chunkedPrimaryAliasIds = personAliasService.Queryable()
                    .Where( pa => pa.PersonId == pa.AliasPersonId && personIdsChunk.Contains( pa.PersonId ) )
                    .Select( pa => new
                    {
                        pa.Id,
                        pa.PersonId
                    } )
                    .ToList();

                foreach ( var aliasId in chunkedPrimaryAliasIds )
                {
                    personAliasIdLookup.AddOrIgnore( aliasId.PersonId, aliasId.Id );
                }
            }

            return personAliasIdLookup;
        }

        [BlockAction]
        public BlockActionResult GetGridData()
        {
            using ( var rockContext = new RockContext() )
            {
                var count = RequestContext.GetPageParameter( "count" )?.AsIntegerOrNull() ?? 10_000;

                var gridAttributes = GetGridAttributes();
                var gridAttributeIds = gridAttributes.Select( a => a.Id ).ToList();

                var prayerRequests = new PrayerRequestService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Take( count )
                    .ToList();

                Helper.LoadFilteredAttributes( prayerRequests, rockContext, a => gridAttributeIds.Contains( a.Id ) );

                var data = GetGridBuilder( gridAttributes ).Build( prayerRequests );

                return ActionOk( data );
            }
        }

        [BlockAction]
        public BlockActionResult CreateGridEntitySet( GridEntitySetBag entitySet )
        {
            if ( entitySet == null )
            {
                return ActionBadRequest( "No entity set data was provided." );
            }

            // Determine the entity type of the items this entity set will represent.
            var entityType = entitySet.EntityTypeKey.IsNotNullOrWhiteSpace()
                ? EntityTypeCache.Get( entitySet.EntityTypeKey.AsGuid() )
                : null;

            // Create the basic entity set, expire in 5 minutes.
            var rockEntitySet = new EntitySet()
            {
                EntityTypeId = entityType?.Id,
                ExpireDateTime = RockDateTime.Now.AddMinutes( 5 )
            };

            var entitySetItems = new List<EntitySetItem>();

            using ( var rockContext = new RockContext() )
            {
                if ( entityType != null )
                {
                    // We have an entity type. Lookup all the identifiers from the
                    // supplied entity keys.
                    var entityKeys = entitySet.Items.Select( i => i.EntityKey ).ToList();
                    var entityIdLookup = Rock.Reflection.GetEntityIdsForEntityType( entityType, entityKeys, true, rockContext );

                    // Create an entity set item for each item that was provided.
                    // If we couldn't find an identifier, then skip it.
                    foreach ( var item in entitySet.Items )
                    {
                        if ( !entityIdLookup.TryGetValue( item.EntityKey, out var entityId ) )
                        {
                            continue;
                        }

                        entitySetItems.Add( new EntitySetItem
                        {
                            EntityId = entityId,
                            AdditionalMergeValues = item.AdditionalMergeValues
                        } );
                    }
                }
                else
                {
                    // Non entity type, so just stuff the merge values into the item.
                    entitySetItems.AddRange( entitySet.Items.Select( i => new EntitySetItem
                    {
                        EntityId = 0,
                        AdditionalMergeValues = i.AdditionalMergeValues
                    } ) );
                }

                // Return an error if we couldn't create any items.
                if ( !entitySetItems.Any() )
                {
                    return ActionBadRequest( "No entities were found to create the set." );
                }

                // Create the entity set first so we can get the identifier.
                var entitySetService = new EntitySetService( rockContext );
                entitySetService.Add( rockEntitySet );
                rockContext.SaveChanges();

                // Use the entity set identifier to populate all the items.
                entitySetItems.ForEach( a =>
                {
                    a.EntitySetId = rockEntitySet.Id;
                } );

                // Insert everything at once, bypassing EF.
                rockContext.BulkInsert( entitySetItems );

                // Todo: Change to IdKey.
                return ActionOk( rockEntitySet.Id.ToString() );
            }
        }

        [BlockAction]
        public BlockActionResult CreateGridCommunication( GridCommunicationBag communication )
        {
            if ( communication == null )
            {
                return ActionBadRequest( "No communication data was provided." );
            }

            using ( var rockContext = new RockContext() )
            {
                var currentPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;

                // Lookup all the identifiers from the supplied entity keys.
                var personKeys = communication.Recipients.Select( i => i.EntityKey ).ToList();
                var personIdLookup = Rock.Reflection.GetEntityIdsForEntityType( EntityTypeCache.Get<Person>(), personKeys, true, rockContext );

                if ( personIdLookup.Count == 0 )
                {
                    return ActionBadRequest( "Grid has no recipients." );
                }

                // Create the blank communication to be filled in later.
                var communicationRockContext = new RockContext();
                var communicationService = new CommunicationService( communicationRockContext );
                var rockCommunication = new Rock.Model.Communication
                {
                    IsBulkCommunication = true,
                    Status = Model.CommunicationStatus.Transient,
                    AdditionalMergeFields = communication.MergeFields,
                    SenderPersonAliasId = currentPersonAliasId,
                    UrlReferrer = communication.FromUrl
                };

                // Save communication to get the Id.
                communicationService.Add( rockCommunication );
                communicationRockContext.SaveChanges();

                // Get the primary aliases
                var personAliasIdLookup = GetPrimaryAliasIds( rockContext, personIdLookup.Values.ToList() );

                var currentDateTime = RockDateTime.Now;
                var communicationRecipientList = new List<CommunicationRecipient>( communication.Recipients.Count );

                foreach ( var item in communication.Recipients )
                {
                    if ( !personIdLookup.TryGetValue( item.EntityKey, out var personId ) )
                    {
                        continue;
                    }

                    if (!personAliasIdLookup.TryGetValue( personId, out var personAliasId ) )
                    {
                        continue;
                    }

                    // NOTE: Set CreatedDateTime, ModifiedDateTime, etc. manually
                    // since we are using BulkInsert.
                    var recipient = new CommunicationRecipient
                    {
                        CommunicationId = rockCommunication.Id,
                        PersonAliasId = personAliasId,
                        AdditionalMergeValues = item.AdditionalMergeValues,
                        CreatedByPersonAliasId = currentPersonAliasId,
                        ModifiedByPersonAliasId = currentPersonAliasId,
                        CreatedDateTime = currentDateTime,
                        ModifiedDateTime = currentDateTime
                    };

                    communicationRecipientList.Add( recipient );
                }

                // BulkInsert to quickly insert the CommunicationRecipient records. Note: This is much faster, but will bypass EF and Rock processing.
                var communicationRecipientRockContext = new RockContext();
                communicationRecipientRockContext.BulkInsert( communicationRecipientList );

                // Todo: Change to IdKey.
                return ActionOk( rockCommunication.Id.ToString() );
            }
        }
    }
}
