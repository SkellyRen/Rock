import TextFilter from "../Filters/textFilter.partial.obs";
import { standardColumnProps, textFilterMatches } from "@Obsidian/Core/Controls/grid";
import { Component, PropType, defineComponent } from "vue";
import TextCell from "../Cells/textCell.partial";
import { ColumnDefinition, ColumnFilter, ExportValueFunction } from "@Obsidian/Types/Controls/grid";

function getTextValue(row: Record<string, unknown>, column: ColumnDefinition): string | undefined {
    if (!column.field) {
        return undefined;
    }

    const value = row[column.field];

    if (typeof value !== "string") {
        return undefined;
    }

    return value;
}

export default defineComponent({
    props: {
        ...standardColumnProps,

        formatComponent: {
            type: Object as PropType<Component>,
            default: TextCell
        },

        filter: {
            type: Object as PropType<ColumnFilter>,
            default: {
                component: TextFilter,
                matches: textFilterMatches
            } as ColumnFilter
        },

        exportValue: {
            type: Function as PropType<ExportValueFunction>,
            default: getTextValue
        }
    }
});
