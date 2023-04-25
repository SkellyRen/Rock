import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import NumberCell from "../Cells/numberCell.partial.obs";
import { ColumnDefinition, ExportValueFunction } from "@Obsidian/Types/Controls/grid";

/**
 * Gets the value to use when exporting a cell of this column.
 *
 * @param row The row that will be exported.
 * @param column The column that will be exported.
 *
 * @returns A RockDateTime value or undefined if the cell has no value.
 */
function getExportValue(row: Record<string, unknown>, column: ColumnDefinition): number | undefined {
    if (!column.field) {
        return undefined;
    }

    const value = row[column.field];

    if (typeof value !== "number") {
        return undefined;
    }

    return value;
}

export default defineComponent({
    props: {
        ...standardColumnProps,

        formatComponent: {
            type: Object as PropType<Component>,
            default: NumberCell
        },

        exportValue: {
            type: Function as PropType<ExportValueFunction>,
            default: getExportValue
        }
    }
});
