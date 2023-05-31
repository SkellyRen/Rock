import { Ref, UnwrapRef, computed, isRef, ref } from "vue";

/** Returns a function that will return reactive values with a default. */
export function useDefaultRef<T>(defaultValue: T) {
    return (valueRef: Ref<T | null | undefined>): Ref<T> => {
        return defaultRef<T>(valueRef, defaultValue);
    };
}

/** Converts an optional string reactive value into a required string ref that defaults to "". */
export const defaultStringRef = useDefaultRef("");

/** Returns a reactive value with a default. */
export function defaultRef<T>(valueRef: Ref<T | null | undefined>, defaultValue: T): Ref<T> {
    return computed({
        get(): T {
            return valueRef.value ?? defaultValue;
        },
        set(newValue: T) {
            valueRef.value = newValue;
        }
    });
}
// export function defaultRef<T, U extends NonNullable<UnwrapRef<T>>>(value: Ref<UnwrapRef<U> | null>, defaultValue: UnwrapRef<U>): Ref<UnwrapRef<U>>;
// export function defaultRef<T, U extends NonNullable<UnwrapRef<T>>>(value: Ref<UnwrapRef<U> | undefined>, defaultValue: UnwrapRef<U>): Ref<UnwrapRef<U>>;
// export function defaultRef<T, U extends NonNullable<UnwrapRef<T>>>(value: Ref<UnwrapRef<U> | null | undefined>, defaultValue: UnwrapRef<U>): Ref<UnwrapRef<U>>;
// export function defaultRef<T, U extends NonNullable<UnwrapRef<T>>>(value: UnwrapRef<U> | null | undefined, defaultValue: UnwrapRef<U>): Ref<UnwrapRef<U>>;
// export function defaultRef<T, U extends NonNullable<UnwrapRef<T>>>(value: Ref<UnwrapRef<U> | null | undefined> | U | null | undefined, defaultValue: UnwrapRef<U>): Ref<UnwrapRef<U>> {
//     let valueRef: Ref<UnwrapRef<U> | null | undefined>;

//     function isValueType(v: unknown): v is UnwrapRef<U> | null | undefined {
//         return typeof v === undefined || v === null || true;
//     }

//     if (isRef<UnwrapRef<U> | null | undefined>(value)) {
//         valueRef = value;
//     }
//     else if (isValueType(value)) {
//         valueRef = ref<U | null | undefined>(value);
//     }

//     return computed({
//         get(): UnwrapRef<U> {
//             return valueRef.value ?? defaultValue;
//         },
//         set(newValue: UnwrapRef<U>) {
//             valueRef.value = newValue;
//         }
//     });
// }