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
using System;
using System.Collections.Generic;
using System.ComponentModel;

using Rock;
using Rock.Attribute;
using Rock.Security;
using Rock.Tasks;

namespace RockWeb.Blocks.Security
{
    /// <summary>
    /// </summary>
    [DisplayName( "Log Out" )]
    [Category( "Security" )]
    [Description( "This block logs the current person out." )]

    [LinkedPage( "Redirect Page", "The page to redirect the user to.", false, order: 0 )]
    [CodeEditorField( "Message", "The message to display if no redirect page was provided.", Rock.Web.UI.Controls.CodeEditorMode.Lava, defaultValue: @"<div class=""alert alert-success"">You have been logged out.</div>", order: 1 )]

    [Rock.SystemGuid.BlockTypeGuid( "CCB87054-8AA3-4F44-AA48-19BD028C4190" )]
    public partial class Logout : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            var currentPerson = CurrentPerson;

            if ( this.UserCanEdit )
            {
                lOutput.Text = @"<div class=""alert alert-warning"">Since you have edit access to this page we have taken the liberty to not log you out.</div>";
                lbAdminLogout.Visible = true;
            }
            else
            {
                LogoutPerson();
            }

            base.OnLoad( e );
        }

        #endregion

        /// <summary>
        /// Logs out the person.
        /// </summary>
        private void LogoutPerson()
        {
            var currentPerson = CurrentPerson;

            if ( currentPerson != null )
            {
                if ( CurrentUser != null )
                {
                    var updateUserLastActivityMsg = new UpdateUserLastActivity.Message
                    {
                        UserId = CurrentUser.Id,
                        LastActivityDate = RockDateTime.Now,
                        IsOnline = false
                    };
                    updateUserLastActivityMsg.Send();
                }

                Authorization.SignOut();

                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "RedirectPage" ) ) )
                {
                    NavigateToLinkedPage( "RedirectPage" );
                }
                else
                {
                    // display message
                    var message = GetAttributeValue( "Message" );

                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "CurrentPerson", currentPerson );

                    lOutput.Text = message.ResolveMergeFields( mergeFields );
                }
            }

            lbAdminLogout.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the lbAdminLogout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAdminLogout_Click( object sender, EventArgs e )
        {
            LogoutPerson();
        }
    }
}