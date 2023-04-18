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

using Rock.Attribute;
using Rock.Blocks;
using Rock.Model;
using Rock.ViewModels.Core.Grid;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace Rock.Obsidian.UI
{
    /// <summary>
    /// Extension methods for <see cref="GridBuilder{T}"/>. This provides much of
    /// specific use functionality of the builder.
    /// </summary>
    internal static class GridBuilderExtensions
    {
        /// <summary>
        /// Adds a new person field to the grid definition.
        /// </summary>
        /// <typeparam name="T">The type of the source collection that will be used to populate the grid.</typeparam>
        /// <param name="builder">The <see cref="GridBuilder{T}"/> to add the field to.</param>
        /// <param name="name">The name of the field to be added.</param>
        /// <param name="valueExpression">The expression that provides the <see cref="Person"/> to use for the cell value.</param>
        /// <returns>A reference to the original <see cref="GridBuilder{T}"/> object that can be used to chain calls.</returns>
        public static GridBuilder<T> AddPersonField<T>( this GridBuilder<T> builder, string name, Func<T, Person> valueExpression )
        {
            return builder.AddField( name, row =>
            {
                var person = valueExpression( row );

                if ( person == null )
                {
                    return null;
                }

                return new
                {
                    person.FirstName,
                    person.NickName,
                    person.LastName,
                    person.PhotoUrl
                };
            } );
        }

        /// <summary>
        /// Adds a new date and time field to the grid definition.
        /// </summary>
        /// <typeparam name="T">The type of the source collection that will be used to populate the grid.</typeparam>
        /// <param name="builder">The <see cref="GridBuilder{T}"/> to add the field to.</param>
        /// <param name="name">The name of the field to be added.</param>
        /// <param name="valueExpression">The expression that provides the <see cref=" DateTime"/> to use for the cell value.</param>
        /// <returns>A reference to the original <see cref="GridBuilder{T}"/> object that can be used to chain calls.</returns>
        public static GridBuilder<T> AddDateTimeField<T>( this GridBuilder<T> builder, string name, Func<T, DateTime?> valueExpression )
        {
            return builder.AddField( name, row => valueExpression( row )?.ToRockDateTimeOffset() );
        }

        /// <summary>
        /// Adds a new plain text field to the grid definition.
        /// </summary>
        /// <typeparam name="T">The type of the source collection that will be used to populate the grid.</typeparam>
        /// <param name="builder">The <see cref="GridBuilder{T}"/> to add the field to.</param>
        /// <param name="name">The name of the field to be added.</param>
        /// <param name="valueExpression">The expression that provides the <see cref="string"/> to use for the cell value.</param>
        /// <returns>A reference to the original <see cref="GridBuilder{T}"/> object that can be used to chain calls.</returns>
        public static GridBuilder<T> AddTextField<T>( this GridBuilder<T> builder, string name, Func<T, string> valueExpression )
        {
            return builder.AddField( name, row => valueExpression( row ) );
        }

        /// <summary>
        /// Adds a set of attribute field to the grid definition.
        /// </summary>
        /// <typeparam name="T">The type of the source collection that will be used to populate the grid.</typeparam>
        /// <param name="builder">The <see cref="GridBuilder{T}"/> to add the field to.</param>
        /// <param name="attributes">The attributes that should be added to the grid definition.</param>
        /// <returns>A reference to the original <see cref="GridBuilder{T}"/> object that can be used to chain calls.</returns>
        public static GridBuilder<T> AddAttributeFields<T>( this GridBuilder<T> builder, IEnumerable<AttributeCache> attributes )
        {
            if ( !typeof( IHasAttributes ).IsAssignableFrom( typeof( T ) ) )
            {
                throw new Exception( $"The type '{typeof( T ).FullName}' does not support attributes." );
            }

            foreach ( var attribute in attributes )
            {
                var key = attribute.Key;
                var fieldKey = $"attr_{key}";

                builder.AddField( fieldKey, item =>
                {
                    var attributeRow = item as IHasAttributes;

                    return attributeRow.GetAttributeCondensedHtmlValue( key );
                } );

                builder.AddDefinitionAction( definition =>
                {
                    var textFieldTypeGuid = SystemGuid.FieldType.TEXT.AsGuid();

                    definition.AttributeFields.Add( new AttributeFieldDefinitionBag
                    {
                        Name = fieldKey,
                        Title = attribute.Name,
                        FieldTypeGuid = attribute.FieldType?.Guid ?? textFieldTypeGuid
                    } );
                } );
            }

            return builder;
        }

        /// <summary>
        /// Adds all the standard features when displaying a grid as part of a block.
        /// </summary>
        /// <typeparam name="T">The type of the source collection that will be used to populate the grid.</typeparam>
        /// <param name="builder">The <see cref="GridBuilder{T}"/> to add the field to.</param>
        /// <param name="block">The block that is displaying this grid.</param>
        /// <returns>A reference to the original <see cref="GridBuilder{T}"/> object that can be used to chain calls.</returns>
        public static GridBuilder<T> WithBlock<T>( this GridBuilder<T> builder, IRockBlockType block )
        {
            // Add all the action URLs for the current site.
            builder.AddDefinitionAction( definition =>
            {
                var communicationUrl = "/Communication/{CommunicationId}";
                SiteCache site;

                if ( block.BlockCache.Page != null )
                {
                    site = SiteCache.Get( block.BlockCache.Page.SiteId );
                }
                else if ( block.BlockCache.Layout != null )
                {
                    site = block.BlockCache.Layout.Site;
                }
                else
                {
                    site = block.BlockCache.Site;
                }

                if ( site != null )
                {
                    var pageRef = site.CommunicationPageReference;

                    if ( pageRef.PageId > 0 )
                    {
                        var communicationPage = PageCache.Get( pageRef.PageId );

                        if ( communicationPage.IsAuthorized( Security.Authorization.VIEW, block.RequestContext.CurrentPerson ) )
                        {
                            pageRef.Parameters.AddOrReplace( "CommunicationId", "{CommunicationId}" );
                            communicationUrl = pageRef.BuildUrl();
                        }
                        else
                        {
                            communicationUrl = null;
                        }
                    }
                }

                if ( communicationUrl.IsNotNullOrWhiteSpace() )
                {
                    definition.ActionUrls.AddOrIgnore( GridActionUrlKey.Communicate, communicationUrl );
                }

                // TODO: Find a way to get the page for the route and determine
                // if the current person can view the page.
                definition.ActionUrls.AddOrIgnore( GridActionUrlKey.MergePerson, "/PersonMerge/{EntitySetId}" );
                definition.ActionUrls.AddOrIgnore( GridActionUrlKey.MergeBusiness, "/BusinessMerge/{EntitySetId}" );
                definition.ActionUrls.AddOrIgnore( GridActionUrlKey.BulkUpdate, "/BulkUpdate/{EntitySetId}" );
                definition.ActionUrls.AddOrIgnore( GridActionUrlKey.LaunchWorkflow, "/LaunchWorkflows/{EntitySetId}" );
                definition.ActionUrls.AddOrIgnore( GridActionUrlKey.MergeTemplate, "/MergeTemplate/{EntitySetId}" );
            } );

            return builder;
        }

        //private static bool IsAuthorizedForRoute( string route, Person currentPerson )
        //{
        //    try
        //    {
        //        // cast the page as a Rock Page
        //        var rockPage = Page as RockPage;
        //        if ( rockPage != null )
        //        {
        //            // If the route contains a parameter
        //            if ( route.Contains( "{0}" ) )
        //            {
        //                // replace it with a fake param
        //                route = string.Format( route, 1 );
        //            }

        //            // Get a uri
        //            Uri uri = new Uri( rockPage.ResolveRockUrlIncludeRoot( route ) );
        //            if ( uri != null )
        //            {
        //                // Find a page ref based on the uri
        //                var pageRef = new Rock.Web.PageReference( uri, Page.Request.ApplicationPath );
        //                if ( pageRef.IsValid )
        //                {
        //                    // if a valid pageref was found, check the security of the page
        //                    var page = PageCache.Get( pageRef.PageId );
        //                    if ( page != null )
        //                    {
        //                        return page.IsAuthorized( Rock.Security.Authorization.VIEW, rockPage.CurrentPerson );
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch ( Exception ex )
        //    {
        //        Rock.Model.ExceptionLogService.LogException( ex, Context );
        //        // Log and move on...
        //    }

        //    return false;
        //}
    }
}
