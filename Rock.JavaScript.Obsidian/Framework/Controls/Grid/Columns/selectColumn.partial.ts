import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import SelectCell from "../Cells/selectCell.partial.obs";
import SelectHeaderCell from "../Cells/selectHeaderCell.partial.obs";

export default defineComponent({
    props: {
        ...standardColumnProps,

        name: {
            type: String as PropType<string>,
            default: "__select"
        },

        formatComponent: {
            type: Object as PropType<Component>,
            default: SelectCell
        },

        headerComponent: {
            type: Object as PropType<Component>,
            default: SelectHeaderCell
        },

        headerClass: {
            type: String as PropType<string>,
            default: "grid-select-field"
        },

        itemClass: {
            type: String as PropType<string>,
            default: "grid-select-field"
        }
    }
});
