import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import SecurityCell from "../Cells/securityCell.partial.obs";
import { IGridState } from "@Obsidian/Types/Controls/grid";

export default defineComponent({
    props: {
        ...standardColumnProps,

        name: {
            type: String as PropType<string>,
            default: "__security"
        },

        formatComponent: {
            type: Object as PropType<Component>,
            default: SecurityCell
        },

        headerClass: {
            type: String as PropType<string>,
            default: "grid-columncommand"
        },

        itemClass: {
            type: String as PropType<string>,
            default: "grid-columncommand"
        },

        /**
         * The row item title to use when opening the security dialog. If a
         * plain string is provided it is the field name that contains the
         * item name. Otherwise it is a function that will be called with the
         * row and grid state and must return a string.
         */
        itemTitle: {
            type: [Function, String] as PropType<((row: Record<string, unknown>, grid: IGridState) => string) | string>,
            required: false
        }
    }
});
