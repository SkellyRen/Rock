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

import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { ColumnDefinition, ExportValueFunction, QuickFilterValueFunction } from "@Obsidian/Types/Controls/grid";
import { RockDateTime } from "@Obsidian/Utility/rockDateTime";
import { Component, defineComponent, PropType } from "vue";
import DateColumnCell from "../Cells/dateCell.partial.obs";

function getExportValue(row: Record<string, unknown>, column: ColumnDefinition): RockDateTime | undefined {
    if (!column.field) {
        return undefined;
    }

    const value = row[column.field];

    if (typeof value !== "string") {
        return undefined;
    }

    return RockDateTime.parseISO(value) ?? undefined;
}


export default defineComponent({
    props: {
        ...standardColumnProps,

        formatComponent: {
            type: Object as PropType<Component>,
            default: DateColumnCell
        },

        quickFilterValue: {
            type: Object as PropType<QuickFilterValueFunction | string>,
            default: (r: Record<string, unknown>, c: ColumnDefinition) => {
                if (!c.field) {
                    return undefined;
                }

                const value = r[c.field];

                if (typeof value !== "string") {
                    return undefined;
                }

                return RockDateTime.parseISO(value)?.toASPString("d");
            }
        },

        exportValue: {
            type: Function as PropType<ExportValueFunction>,
            default: getExportValue
        }
    }
});
