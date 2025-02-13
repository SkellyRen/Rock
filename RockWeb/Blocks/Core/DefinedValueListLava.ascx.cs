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
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Text;
using Rock.Security;

namespace RockWeb.Blocks.Core
{
    /// <summary>
    /// Takes a defined type and returns all defined values and merges them with a lava template
    /// </summary>
    [DisplayName( "Defined Value List Lava" )]
    [Category( "Core" )]
    [Description( "Takes a defined type and returns all defined values and merges them with a lava template." )]

    [DefinedTypeField( "Defined Type",
        Description = "The defined type to load values for merge fields.",
        Key = AttributeKey.DefinedType )]

    [CodeEditorField( "Lava Template",
        Description = "Lava template to use to display content",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = true,
        DefaultValue = @"{% for definedValue in DefinedValues %}
    {{ definedValue.Value }}
{% endfor %}",
        Order = 4,
        Key = AttributeKey.LiquidTemplate )]

    [Rock.SystemGuid.BlockTypeGuid( "C4ADDDFA-DF16-467E-9285-B1FF0FC066ED" )]
    public partial class DefinedValueListLiquid : Rock.Web.UI.RockBlock
    {
        public static class AttributeKey
        {
            public const string DefinedType = "DefinedType";
            public const string LiquidTemplate = "LiquidTemplate";
        }

        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

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
            LoadContent();
        }



        #endregion

        #region Methods

        protected void LoadContent()
        {
            List<DefinedValueCache> definedValues = new List<DefinedValueCache>();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            // TODO: When support for "Person" is not supported anymore (should use "CurrentPerson" instead), remove this line
            mergeFields.TryAdd( "Person", CurrentPerson );


            string selectedDefinedType = GetAttributeValue( AttributeKey.DefinedType );

            if ( !string.IsNullOrWhiteSpace( selectedDefinedType ) )
            {
                var dtItem = DefinedTypeCache.Get( Guid.Parse( selectedDefinedType ) );

                foreach ( var item in dtItem.DefinedValues )
                {
                    definedValues.Add( item );
                }

                mergeFields.Add( "DefinedValues", definedValues );

                string template = GetAttributeValue( AttributeKey.LiquidTemplate );
                lContent.Text = template.ResolveMergeFields( mergeFields );
            }

        }

        #endregion
    }
}