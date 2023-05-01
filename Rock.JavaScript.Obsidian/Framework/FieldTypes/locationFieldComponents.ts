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

import { computed, defineComponent, ref, watch } from "vue";
import { getFieldEditorProps, getFieldConfigurationProps } from "./utils";
import LocationPicker from "@Obsidian/Controls/locationPicker.obs";
import CheckBoxList from "@Obsidian/Controls/checkBoxList";
import RadioButtonList from "@Obsidian/Controls/radioButtonList";
import { ConfigurationValueKey } from "./locationField.partial";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";

export const EditComponent = defineComponent({
    name: "LocationField.Edit",

    components: {
        LocationPicker
    },

    props: getFieldEditorProps(),

    setup(props, { emit }) {
        const internalValue = ref({});

        watch(() => props.modelValue, () => {
            internalValue.value = JSON.parse(props.modelValue || "{}");
        }, { immediate: true });

        watch(() => internalValue.value, () => {
            emit("update:modelValue", JSON.stringify(internalValue.value));
        });
        console.log("internal-value", internalValue.value);
        return {
            internalValue
        };
    },

    template: `
    <LocationPicker v-model="internalValue" label="Default Value" :multiple="false" />
`
});


export const ConfigurationComponent = defineComponent({
    name: "LocationField.Configuration",

    components: {
        CheckBoxList,
        RadioButtonList
    },

    props: getFieldConfigurationProps(),

    emits: [
        "update:modelValue",
        "updateConfigurationValue"
    ],

    setup(props, { emit }) {
        const allowedLocationTypes = ref<string[]>([]);
        const defaultLocationType = ref("");

        /** The options to choose from in the CheckBox and RadioButton list */
        const pickerModes = computed((): ListItemBag[] => {
            try {
                return JSON.parse(props.modelValue[ConfigurationValueKey.PickerModes] || "[]") as ListItemBag[];
            }
            catch {
                return [];
            }
        });

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
            newValue[ConfigurationValueKey.AllowedPickerModes] = allowedLocationTypes.value.join(",");
            newValue[ConfigurationValueKey.CurrentPickerMode] = defaultLocationType.value;

            // Compare the new value and the old value.
            const anyValueChanged = newValue[ConfigurationValueKey.AllowedPickerModes] !== (props.modelValue[ConfigurationValueKey.AllowedPickerModes] ?? "")
                || newValue[ConfigurationValueKey.CurrentPickerMode] !== (props.modelValue[ConfigurationValueKey.CurrentPickerMode]);

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
            allowedLocationTypes.value = props.modelValue[ConfigurationValueKey.AllowedPickerModes] ? props.modelValue[ConfigurationValueKey.AllowedPickerModes].split(",") : [];
            defaultLocationType.value = props.modelValue[ConfigurationValueKey.CurrentPickerMode];
        }, {
            immediate: true
        });

        watch(allowedLocationTypes, val => maybeUpdateConfiguration(ConfigurationValueKey.AllowedPickerModes, val.join(",")));
        watch(defaultLocationType, val => maybeUpdateConfiguration(ConfigurationValueKey.CurrentPickerMode, val));

        return {
            allowedLocationTypes,
            defaultLocationType,
            pickerModes
        };
    },

    template: `
    <CheckBoxList label="Available Location Types" v-model="allowedLocationTypes" :items="pickerModes" :horizontal="true" help="Select the location types that can be used by the Location Picker." />
    <RadioButtonList label="Default Location Type" v-model="defaultLocationType" :items="pickerModes" :horizontal="true" help="Select the location types that is initially displayed." />
    `
});