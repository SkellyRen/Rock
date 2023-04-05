// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
import { defineComponent, ref, watch } from "vue";
import { getFieldEditorProps, getFieldConfigurationProps } from "./utils";
import DefinedValuePicker from "@Obsidian/Controls/definedValuePicker.obs";
import GroupTypePicker from "@Obsidian/Controls/groupTypePicker.obs";
import { DefinedType } from "@Obsidian/SystemGuids/definedType";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";

const enum ConfigurationValueKey {
    GroupTypePurposeValueGuid = "groupTypePurposeValueGuid"
}

export const EditComponent = defineComponent({

    name: "GroupTypeField.Edit",

    components: {
        GroupTypePicker
    },

    props: getFieldEditorProps(),

    data() {

        return {
            internalValue: ""
        };
    },

    computed: {
    },

    watch: {
        internalValue() {
            this.$emit("update:modelValue", this.internalValue?.["value"]);
        }
    },

    template: `
<GroupTypePicker v-model="internalValue" />
`
});

export const ConfigurationComponent = defineComponent({
    name: "GroupTypeField.Configuration",

    components: { DefinedValuePicker },

    props: getFieldConfigurationProps(),

    emits: [
        "update:modelValue",
        "updateConfiguration",
        "updateConfigurationValue"
    ],

    setup(props, { emit }) {
        const groupTypePurposeValueGuid = ref("");
        const groupTypePurposeDefinedTypeGuid = DefinedType.GrouptypePurpose;

        /**
         * Update the modelValue property if any value of the dictionary has
         * actually changed. This helps prevent unwanted postbacks if the value
         * didn't really change - which can happen if multiple values get updated
         * at the same time.
         *
         * @returns true if a new modelValue was emitted to the parent component.
         */
        const maybeUpdateModelValue = (): boolean => {
            const newValue: Record<string, string> = {};

            // Construct the new value that will be emitted if it is different
            // than the current value.
            newValue[ConfigurationValueKey.GroupTypePurposeValueGuid] = groupTypePurposeValueGuid.value;

            // Compare the new value and the old value.
            const anyValueChanged = newValue[ConfigurationValueKey.GroupTypePurposeValueGuid] !== (props.modelValue[ConfigurationValueKey.GroupTypePurposeValueGuid]);

            // If any value changed then emit the new model value.
            if (anyValueChanged) {
                emit("update:modelValue", newValue);
                return true;
            }
            else {
                return false;
            }
        };

        /**
         * Emits the updateConfigurationValue if the value has actually changed.
         *
         * @param key The key that was possibly modified.
         * @param value The new value.
         */
        const maybeUpdateConfiguration = (key: string, value: string): void => {
            if (maybeUpdateModelValue()) {
                emit("updateConfigurationValue", key, value);
            }
        };

        // Watch for changes coming in from the parent component and update our
        // data to match the new information.
        watch(() => [props.modelValue, props.configurationProperties], () => {
            groupTypePurposeValueGuid.value = props.modelValue[ConfigurationValueKey.GroupTypePurposeValueGuid];
        }, {
            immediate: true
        });

        // Watch for changes in properties that require new configuration
        // properties to be retrieved from the server.
        // THIS IS JUST A PLACEHOLDER FOR COPYING TO NEW FIELDS THAT MIGHT NEED IT.
        // THIS FIELD DOES NOT NEED THIS
        watch([], () => {
            if (maybeUpdateModelValue()) {
                emit("updateConfiguration");
            }
        });

        // Watch for changes in properties that only require a local UI update.
        watch(groupTypePurposeValueGuid, () => maybeUpdateConfiguration(ConfigurationValueKey.GroupTypePurposeValueGuid, groupTypePurposeValueGuid.value));

        return { groupTypePurposeValueGuid, groupTypePurposeDefinedTypeGuid };
    },

    template: `
<div>
    <DefinedValuePicker v-model="groupTypePurposeValueGuid" label="Purpose" :definedTypeGuid="groupTypePurposeDefinedTypeGuid"
        help="An optional setting to limit the selection of group types to those that have the selected purpose." />
</div>
`
});
