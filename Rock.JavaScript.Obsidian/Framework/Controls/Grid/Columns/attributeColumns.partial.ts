import PickExistingFilter from "../Filters/pickExistingFilter.partial.obs";
import { defineComponent, PropType } from "vue";
import { AttributeFieldDefinitionBag } from "@Obsidian/ViewModels/Core/Grid/attributeFieldDefinitionBag";
import { ColumnFilter } from "@Obsidian/Types/Controls/grid";
import { pickExistingFilterMatches } from "@Obsidian/Core/Controls/grid";

export default defineComponent({
    props: {
        attributes: {
            type: Array as PropType<AttributeFieldDefinitionBag[]>,
            default: []
        },

        __attributeColumns: {
            type: Boolean as PropType<boolean>,
            default: true
        },

        filter: {
            type: Object as PropType<ColumnFilter>,
            default: {
                component: PickExistingFilter,
                matches: pickExistingFilterMatches
            } as ColumnFilter
        }
    }
});
