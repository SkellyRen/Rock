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

using System.Collections.Generic;
using Rock.Model;

namespace Rock.ViewModels.Blocks.Cms.SiteList
{
    /// <summary>
    /// The Site List Options Bag
    /// </summary>
    public class SiteListOptionsBag
    {
        /// <summary>
        /// Gets or sets the site type.
        /// </summary>
        public List<SiteType> SiteType { get; set; }

        /// <summary>
        /// Gets or sets the block title.
        /// </summary>
        public string BlockTitle { get; set; }

        /// <summary>
        /// Boolean value that shows or hides the site icon 
        /// </summary>
        public string ShowSiteIcon { get; set; }
    }
}
