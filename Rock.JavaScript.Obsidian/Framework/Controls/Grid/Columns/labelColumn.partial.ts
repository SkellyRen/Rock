import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import LabelCell from "../Cells/labelCell.partial.obs";
import { ColumnDefinition, ExportValueFunction, QuickFilterValueFunction } from "@Obsidian/Types/Controls/grid";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";

function textValue(row: Record<string, unknown>, column: ColumnDefinition): string | undefined {
    if (!column.field) {
        return undefined;
    }

    const value = row[column.field];

    if (typeof value === "object") {
        if (value === null || value["text"] === null || value["text"] === undefined) {
            return "";
        }

        return `${(value as ListItemBag).text}`;
    }

    return `${value}`;
}

export default defineComponent({
    props: {
        ...standardColumnProps,

        formatComponent: {
            type: Object as PropType<Component>,
            default: LabelCell
        },

        quickFilterValue: {
            type: Object as PropType<QuickFilterValueFunction | string>,
            default: textValue
        },

        exportValue: {
            type: Function as PropType<ExportValueFunction>,
            default: textValue
        },

        /**
         * The lookup table to use when applying a custom label type tag to
         * the badge. The key is the text value of the field. The value is
         * a standard label suffix such as `primary` or `danger`.
         */
        classSource: {
            type: Object as PropType<Record<string, string>>,
            required: false
        },

        /**
         * The lookup table to use when applying a custom background color to
         * the badge. The key is the text value of the field. The value is
         * a standard CSS color designation.
         */
        colorSource: {
            type: Object as PropType<Record<string, string>>,
            required: false
        }
    }
});
