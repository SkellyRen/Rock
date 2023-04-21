import { standardColumnProps } from "@Obsidian/Core/Controls/grid";
import { Component, defineComponent, PropType } from "vue";
import DeleteCell from "../Cells/deleteCell.partial.obs";

export default defineComponent({
    props: {
        ...standardColumnProps,

        name: {
            type: String as PropType<string>,
            default: "__delete"
        },

        formatComponent: {
            type: Object as PropType<Component>,
            default: DeleteCell
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
         * Disables the normal confirmation message displayed before calling
         * the click handler.
         */
        disableConfirmation: {
            type: Boolean as PropType<boolean>,
            default: false
        },

        /**
         * Called when the delete button has been clicked and the confirmation
         * has been approved.
         */
        onClick: {
            type: Function as PropType<((key: string) => void) | ((key: string) => Promise<void>)>,
            required: false
        }
    }
});
