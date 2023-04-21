import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import EditCell from "../Cells/editCell.partial.obs";

export default defineComponent({
    props: {
        ...standardColumnProps,

        name: {
            type: String as PropType<string>,
            default: "__edit"
        },

        formatComponent: {
            type: Object as PropType<Component>,
            default: EditCell
        },

        headerClass: {
            type: String as PropType<string>,
            default: "grid-columncommand"
        },

        itemClass: {
            type: String as PropType<string>,
            default: "grid-columncommand"
        },

        onClick: {
            type: Function as PropType<((key: string) => void) | ((key: string) => Promise<void>)>,
            required: false
        }
    }
});
