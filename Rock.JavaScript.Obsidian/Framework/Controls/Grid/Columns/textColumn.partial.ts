import TextFilter from "../Filters/textFilter.partial.obs";
import { standardColumnProps, textFilterMatches } from "@Obsidian/Core/Controls/grid";
import { PropType, VNode, defineComponent } from "vue";
import TextCell from "../Cells/textCell.partial";
import { ColumnFilter } from "@Obsidian/Types/Controls/grid";

export default defineComponent({
    props: {
        ...standardColumnProps,

        format: {
            type: Object as PropType<VNode>,
            default: TextCell
        },

        filter: {
            type: Object as PropType<ColumnFilter>,
            default: {
                component: TextFilter,
                matches: textFilterMatches
            } as ColumnFilter
        }
    }
});
