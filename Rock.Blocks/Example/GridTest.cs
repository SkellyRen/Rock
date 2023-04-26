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
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Obsidian.UI;
using Rock.ViewModels.Utility;

namespace Rock.Blocks.Example
{
    /// <summary>
    /// Allows testing the new Grid component.
    /// </summary>

    [DisplayName( "Grid Test" )]
    [Category( "Example" )]
    [Description( "Allows testing the new Grid component." )]
    [IconCssClass( "fa fa-list" )]

    [Rock.SystemGuid.EntityTypeGuid( "1934a378-57d6-44d0-b7cd-4443f347a1ee" )]
    [CustomizedGrid]
    public class GridTest : RockObsidianEntityListBlockType<PrayerRequest>
    {
        #region Properties

        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var builder = GetGridBuilder();

            return new
            {
                GridDefinition = builder.BuildDefinition()
            };
        }

        /// <inheritdoc/>
        protected override IQueryable<PrayerRequest> GetListQueryable( RockContext rockContext )
        {
            var count = RequestContext.GetPageParameter( "count" )?.AsIntegerOrNull() ?? 10_000;

            return base.GetListQueryable( rockContext ).Take( count );
        }

        /// <summary>
        /// Gets the grid builder that will be used to construct the definition
        /// and the final row data.
        /// </summary>
        /// <returns>A <see cref="GridBuilder{T}"/> instance that will handle building the grid data.</returns>
        protected override GridBuilder<PrayerRequest> GetGridBuilder()
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
                .AddAttributeFields( GetGridAttributes() );
        }

        #endregion

        #region Block Actions

        #endregion
    }
}
