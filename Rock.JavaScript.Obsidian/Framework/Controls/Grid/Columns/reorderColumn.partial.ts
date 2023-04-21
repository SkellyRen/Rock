import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import ReorderCell from "../Cells/reorderCell.partial.obs";

export default defineComponent({
    props: {
        ...standardColumnProps,

        name: {
            type: String as PropType<string>,
            default: "__reorder"
        },

        formatComponent: {
            type: Object as PropType<Component>,
            default: ReorderCell
        },

        headerClass: {
            type: String as PropType<string>,
            default: "grid-columnreorder"
        },

        itemClass: {
            type: String as PropType<string>,
            default: "grid-columnreorder"
        },

        onOrderChanged: {
            type: Function as PropType<(item: Record<string, unknown>, beforeItem: Record<string, unknown> | null, order: number) => void | Promise<void> | boolean | Promise<boolean>>,
            required: false
        }
    }
});
