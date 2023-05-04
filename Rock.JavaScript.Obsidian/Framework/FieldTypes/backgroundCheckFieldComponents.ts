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
import BinaryFileTypePicker from "@Obsidian/Controls/binaryFileTypePicker";
import TextBox from "@Obsidian/Controls/textBox";
import FileUploader from "@Obsidian/Controls/fileUploader";
import DropDownList from "@Obsidian/Controls/dropDownList";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { ConfigurationValueKey } from "./backgroundCheckField.partial";

export const EditComponent = defineComponent({
    name: "BackgroundCheckField.Edit",

    components: {
        TextBox,
        FileUploader
    },

    props: getFieldEditorProps(),

    setup(props, { emit }) {

        const internalValue = ref<ListItemBag>({});

        const backgroundCheckType = computed((): string | null | undefined => {
            const checkType = JSON.parse(props.configurationValues[ConfigurationValueKey.BackgroundCheckType] || "{}") as ListItemBag;
            return checkType.value;
        });

        const isFile = computed((): boolean => {
            return backgroundCheckType.value === "0";
        });

        watch(() => props.modelValue, () => {
            internalValue.value = JSON.parse(props.modelValue || "{}");
        }, { immediate: true });

        watch(() => internalValue.value, () => {
            emit("update:modelValue", JSON.stringify(internalValue.value));
        },{ deep: true });

        return {
            internalValue,
            backgroundCheckType,
            isFile
        };
    },

    template: `
    <div v-if="backgroundCheckType">
        <FileUploader v-if="isFile"
            v-model="internalValue"
            uploadAsTemporary="true"
            uploadButtonText="Upload"
            showDeleteButton="true" />
        <TextBox v-else
            v-model="internalValue.value"
            label="RecordKey"
            help="Unique key for the background check report."
            textMode="MultiLine" />
    </div>
`
});


export const ConfigurationComponent = defineComponent({
    name: "BackgroundCheckField.Configuration",

    components: {
        BinaryFileTypePicker,
        DropDownList
    },

    props: getFieldConfigurationProps(),

    emits: [
        "update:modelValue",
        "updateConfigurationValue"
    ],

    setup(props, { emit }) {
        const binaryFileType = ref<ListItemBag>({});
        const backgroundCheckType = ref<ListItemBag>({});
        const backgroundCheckTypes = computed((): ListItemBag[] => {
            try {
                return JSON.parse(props.modelValue[ConfigurationValueKey.BackgroundCheckTypes] ?? "[]") as ListItemBag[];
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
            newValue[ConfigurationValueKey.BinaryFileType] = JSON.stringify(binaryFileType.value ?? "");
            newValue[ConfigurationValueKey.BackgroundCheckTypes] = JSON.stringify(backgroundCheckTypes.value ?? "[]");
            newValue[ConfigurationValueKey.BackgroundCheckType] = JSON.stringify(backgroundCheckType.value ?? "");

            // Compare the new value and the old value.
            const anyValueChanged = newValue[ConfigurationValueKey.BinaryFileType] !== (props.modelValue[ConfigurationValueKey.BinaryFileType] ?? "")
                || newValue[ConfigurationValueKey.BackgroundCheckType] !== (props.modelValue[ConfigurationValueKey.BackgroundCheckType] ?? "");

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
            binaryFileType.value = JSON.parse(props.modelValue[ConfigurationValueKey.BinaryFileType] || "{}");
            backgroundCheckType.value = JSON.parse(props.modelValue[ConfigurationValueKey.BackgroundCheckType] || "{}");
        }, {
            immediate: true
        });

        watch(binaryFileType, val => maybeUpdateConfiguration(ConfigurationValueKey.BinaryFileType, JSON.stringify(val ?? "")));
        watch(backgroundCheckType, val => maybeUpdateConfiguration(ConfigurationValueKey.BackgroundCheckType, JSON.stringify(val ?? "")), { deep: true });

        return {
            binaryFileType,
            backgroundCheckType,
            backgroundCheckTypes
        };
    },

    template: `
    <BinaryFileTypePicker label="File Type" v-model="binaryFileType" help="File type to use to store and retrieve the file. New file types can be configured under 'Admin Tools > General Settings > File Types'" />
    <DropDownList v-model="backgroundCheckType.value" :items="backgroundCheckTypes" showBlankItem="true" />
    `
});