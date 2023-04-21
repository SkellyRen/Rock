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

import { Component, PropType } from "vue";
import { Guid } from "..";

/** The purpose of the entity set. This activates special logic. */
export type EntitySetPurpose = "communication" | "export";

/**
 * The options to use when generating the entity set bag for a grid.
 */
export type EntitySetOptions = {
    /**
     * Forces the entity type to a different value then what is configured
     * on the grid. Useful when creating a set of persons from group members.
     */
    entityTypeGuid?: Guid;

    /**
     * Any additional fields that should be placed in the item merge fields.
     * The values are copied over as-is with no conversion. The key represents
     * the source field from the grid row and the value represents the name
     * of the merge field to store the value in.
     */
    mergeFields?: Record<string, string>;

    /**
     * Any columns whose values should be placed in the item merge fields.
     * The key represents the source field from the grid row and the value
     * represents the name of the merge field to store the value in. The
     * formatted value of the column is used.
     */
    mergeColumns?: Record<string, string>;

    /**
     * A function that will be called to provide additional custom merge
     * values to the entity set item.
     *
     * @param row The row that will provide additional field information.
     * @param grid The grid that is performing the operation.
     *
     * @returns An object that will be appended to the merge values.
     */
    additionalMergeFieldsFactory?: (row: Record<string, unknown>, grid: IGridState) => Record<string, unknown>;

    /**
     * The specialized purpose of this entity set. This is used to provide
     * additional context to the process about how to generate the data.
     */
    purpose?: EntitySetPurpose;
};

// #region Caching

/**
 * Defines a generic grid cache object. This can be used to store and get
 * data from a cache. The cache is unique to the grid instance so there is
 * no concern of multiple grids conflicting.
 */
export interface IGridCache {
    /**
     * Removes all values from the cache.
     */
    clear(): void;

    /**
     * Removes a single item from the cache.
     *
     * @param key The identifier of the value to be removed from the cache.
     */
    remove(key: string): void;

    /**
     * Gets an existing value from the cache.
     *
     * @param key The identifier of the value.
     *
     * @returns The value found in the cache or undefined if it was not found.
     */
    get<T = unknown>(key: string): T | undefined;

    /**
     * Gets an existing value from cache or adds it into the cache.
     *
     * @param key The identifier of the cached value.
     * @param factory The function to call when adding the value.
     *
     * @returns The existing value or the newly created value.
     */
    getOrAdd<T = unknown>(key: string, factory: () => T): T;

    /**
     * Gets an existing value form cache or adds it into the cache.
     *
     * @param key The identifier of the cached value.
     * @param factory The function to call when adding the value. If undefined is returned then the value is not added to the cache.
     *
     * @returns The existing value or the newly created value. Returns undefined if it could not be found or created.
     */
    getOrAdd<T = unknown>(key: string, factory: () => T | undefined): T | undefined;

    /**
     * Adds the value if it does not exist in cache or replaces the existing
     * value in cache with the new value.
     *
     * @param key The identifier of the cached value.
     * @param value The value that should be placed into the cache.
     *
     * @returns The value that was placed into the cache.
     */
    addOrReplace<T = unknown>(key: string, value: T): T;
}

/**
 * Defines a grid cache object used for row data. This can be used to store and
 * get data from cache for a specific row. The cache is unique to the grid
 * instance so there is no concern of multiple grids conflicting.
 */
export interface IGridRowCache {
    /**
     * Removes all values for all rows from the cache.
     */
    clear(): void;

    /**
     * Removes all the cached values for the specified row.
     *
     * @param row The row whose cached values should be removed.
     */
    remove(row: Record<string, unknown>): void;

    /**
     * Removes a single item from the cache.
     *
     * @param row The row whose cached key value should be removed.
     * @param key The identifier of the value to be removed from the row cache.
     */
    remove(row: Record<string, unknown>, key: string): void;

    /**
     * Gets an existing value from the cache.
     *
     * @param row The row whose cached key value should be retrieved.
     * @param key The identifier of the value.
     *
     * @returns The value found in the cache or undefined if it was not found.
     */
    get<T = unknown>(row: Record<string, unknown>, key: string): T | undefined;

    /**
     * Gets an existing value from cache or adds it into the cache.
     *
     * @param row The row whose cached key value should be retrieved.
     * @param key The identifier of the cached value.
     * @param factory The function to call when adding the value.
     *
     * @returns The existing value or the newly created value.
     */
    getOrAdd<T = unknown>(row: Record<string, unknown>, key: string, factory: () => T): T;

    /**
     * Gets an existing value form cache or adds it into the cache.
     *
     * @param row The row whose cached key value should be retrieved.
     * @param key The identifier of the cached value.
     * @param factory The function to call when adding the value. If undefined is returned then the value is not added to the cache.
     *
     * @returns The existing value or the newly created value. Returns undefined if it could not be found or created.
     */
    getOrAdd<T = unknown>(row: Record<string, unknown>, key: string, factory: () => T | undefined): T | undefined;

    /**
     * Adds the value if it does not exist in cache or replaces the existing
     * value in cache with the new value.
     *
     * @param row The row whose cached key value should be retrieved.
     * @param key The identifier of the cached value.
     * @param value The value that should be added into the cache.
     *
     * @returns The value that was placed into the cache.
     */
    addOrReplace<T = unknown>(row: Record<string, unknown>, key: string, value: T): T;
}

// #endregion

// #region Functions and Callbacks

/** A function that will be called in response to an action. */
export type GridActionFunction = () => void | Promise<void>;

/**
 * A function that will be called to determine the value used when filtering
 * against the quick filter text. This value will be cached by the grid until
 * the row is modified.
 *
 * @param row The data object that represents the row.
 * @param column The column definition for this operation.
 * @param grid The grid that owns this operation.
 *
 * @returns The text that will be used when performing quick filtering or `undefined` if it is not supported.
 */
export type QuickFilterValueFunction = (row: Record<string, unknown>, column: ColumnDefinition, grid: IGridState) => string | undefined;

/**
 * A function that will be called to determine the sortable value of a cell.
 * This value will be cached by the grid until the row is modified.
 *
 * @param row The data object that represents the row.
 * @param column The column definition for this operation.
 * @param grid The grid that owns this operation.
 *
 * @returns The value that will be used when sorting this column or `undefined` if no value is available.
 */
export type SortValueFunction = (row: Record<string, unknown>, column: ColumnDefinition, grid: IGridState) => string | number | undefined;

/**
 * A function that will be called to determine the value to use when
 * performing a column filter operation. This value will be cached by the
 * grid until the row is modified.
 *
 * @param row The data object that represents the row.
 * @param column The column definition for this operation.
 * @param grid The grid that owns this operation.
 *
 * @returns The value that will be used by the {@link ColumnFilterMatchesFunction} function.
 */
export type FilterValueFunction = (row: Record<string, unknown>, column: ColumnDefinition, grid: IGridState) => unknown | undefined;

/**
 * A function that will be called to determine the unique value of the cell.
 * This is used to differentiate two values that display the same but actually
 * represent two distinct things. For example, two people might have the same
 * name but a different identifier. But two dollar amounts of $10.23 and $10.23
 * should both return the same value from this function. This value will be
 * cached by the grid until the row is modified.
 *
 * @param row The data object that represents the row.
 * @param column The column definition for this operation.
 * @param grid The grid that owns this operation.
 *
 * @returns The value that uniquely identifies this cell.
 */
export type UniqueValueFunction = (row: Record<string, unknown>, column: ColumnDefinition, grid: IGridState) => string | number | undefined;

/**
 * A function that will be called in order to determine if a row matches the
 * filtering request for the column.
 *
 * @param needle The filter value entered in the column filter.
 * @param haystack The filter value provided by the row to be matched against.
 * @param column The column definition for this operation.
 * @param grid The grid that owns this operation.
 *
 * @returns True if `haystack` matches `needle`, otherwise false.
 */
export type ColumnFilterMatchesFunction = (needle: unknown, haystack: unknown, column: ColumnDefinition, grid: IGridState) => boolean;

// #endregion

// #region Component Props

/** The standard properties available on all columns. */
type StandardColumnProps = {
    /**
     * The unique name that identifies this column in the grid.
     */
    name: {
        type: PropType<string>,
        default: ""
    },

    /** The title of the column, this is displayed in the table header. */
    title: {
        type: PropType<string>,
        required: false
    },

    /**
     * The name of the field on the row that will contain the data. This is
     * used by default columns and other features to automatically display
     * the data. If you are building a completely custom column it is not
     * required.
     */
    field: {
        type: PropType<string>,
        required: false
    },

    /**
     * Overrides the default method of obtaining the value to use when matching
     * against the quick filter. If not specified then the value of of the row
     * in the `field` property will be used if it is a supported type. A
     * function may be specified which will be called with the row and column
     * definition and must return either a string or undefined. If a plain
     * string is specified then it will be used as a Lava Template which will
     * be passed the `row` object.
     */
    quickFilterValue: {
        type: PropType<QuickFilterValueFunction | string>,
        required: false
    },

    /**
     * The name of the field on the row that will contain the data to be used
     * when sorting. If this is not specified then the value from `field` will
     * be used by default. If no `title` is specified then the column will not
     * be sortable.
     */
    sortField: {
        type: PropType<string>,
        required: false
    },

    /**
     * Specifies how to get the sort value to use when sorting by this column.
     * This will override the `sortField` setting. If a function is be provided
     * then it will be called with the row and the column definition and must
     * return either a string, number or undefined. If a string is provided
     * then it will be used as a Lava Template which will be passed the `row`
     * object used to calculate the value. If no `title` is specified then the
     * column will not be sortable.
     */
    sortValue: {
        type: PropType<(SortValueFunction | string)>,
        required: false
    },

    /**
     * Enabled filtering of this column and specifies what type of filtering
     * will be done.
     */
    filter: {
        type: PropType<ColumnFilter>,
        required: false
    },

    /**
     * Specifies how to get the value to use when filtering by this column.
     * This is used on combination with the `filter` setting only. If a
     * function is be provided then it will be called with the row and the
     * column definition and must return a value recognized by the filter.
     * If a string is provided then it will be used as a Lava Template which
     * will be passed the `row` object used to calculate the value.
     */
    filterValue: {
        type: PropType<(FilterValueFunction | string)>,
        required: false
    },

    /**
     * Specifies how to determine the unique value for cells in this column.
     * This is used by some operations to find only the distinct values of
     * the entire dataset. For example, two people might have the same name
     * and thus look the same when displayed, but in reality they are different
     * people so this method should return a unique value for each. If
     * specified then the function will be passed the row and column definition
     * and must return either a string, number or undefined. Otherwise the
     * value of the row in `field` will be used to determine this value.
     */
    uniqueValue: {
        type: PropType<UniqueValueFunction>,
        required: false
    },

    /**
     * Additional CSS class to apply to the header cell.
     */
    headerClass: {
        type: PropType<string>,
        required: false
    },

    /**
     * Additional CSS class to apply to the data item cell.
     */
    itemClass: {
        type: PropType<string>,
        required: false
    }

    /**
     * Provides a custom component that will be used to format and display
     * the cell. This is rarely needed as you can usually accomplish the same
     * with a template that defines the body content.
     */
    format: {
        type: PropType<Component>,
        required: false
    },

    /**
     * Provides a custom component that will be used to render the header
     * cell. This is rarely needed as you can usually accomplish the same
     * with a template that defines the header content.
     */
    headerTemplate: {
        type: PropType<Component>,
        required: false
    },

    /**
     * If `true` then the column will not ever be rendered on screen. It may
     * still be included in exports and other operations.
     */
    hideOnScreen: {
        type: PropType<boolean>,
        required: false
    },

    /**
     * If `true` then the column will not be included when exporting data to
     * be downloaded by the individual.
     */
    excludeFromExport: {
        type: PropType<boolean>,
        required: false
    }
};

/** The standard properties available on header cells. */
export type StandardHeaderCellProps = {
    /** The column definition that this cell is being displayed in. */
    column: {
        type: PropType<ColumnDefinition>,
        required: true
    },

    /** The grid this cell is being displayed inside of. */
    grid: {
        type: PropType<IGridState>,
        required: true
    }
};

/** The standard properties available on cells. */
export type StandardCellProps = {
    /** The column definition that this cell is being displayed in. */
    column: {
        type: PropType<ColumnDefinition>,
        required: true
    },

    /** The data object that represents the row for this cell. */
    row: {
        type: PropType<Record<string, unknown>>,
        required: true
    },

    /** The grid this cell is being displayed inside of. */
    grid: {
        type: PropType<IGridState>,
        required: true
    }
};

/**
 * The standard properties that are made available to column filter
 * components.
 */
export type StandardFilterProps = {
    /** The currently selected filter value. */
    modelValue: {
        type: PropType<unknown>,
        required: false
    },

    /** The column that this filter will be applied to. */
    column: {
        type: PropType<ColumnDefinition>,
        required: true
    },

    /** The gird that this filter is being displayed inside of. */
    grid: {
        type: PropType<IGridState>,
        required: true
    }
};

// #endregion

/** Defines a single action related to a Grid control. */
export type GridAction = {
    /**
     * The title of the action, this should be a very short (one or two words)
     * description of the action that will be performed, such as "Delete".
     */
    title?: string;

    /**
     * The tooltip to display for this action.
     */
    tooltip?: string;

    /**
     * The CSS class for the icon used when displaying this action.
     */
    iconCssClass?: string;

    /**
     * Additional CSS classes to add to the button. This is primarily used
     * to mark certain buttons as danger or success.
     */
    buttonCssClass?: string;

    /** The callback function that will handle the action. */
    handler?: GridActionFunction;

    /** If true then the action will be disabled and not respond to clicks. */
    disabled?: boolean;
};

/**
 * Defines the structure and properties of a column in the grid.
 */
export type ColumnDefinition = {
    /** The unique name of this column. */
    name: string;

    /** The title to display in the column header. */
    title?: string;

    /** The name of the field in the row object. */
    field?: string;

    /**
     * Defines the content that will be used in the header cell. This will
     * override any title value provided.
     */
    headerTemplate?: Component;

    /**
     * Formats the value for display in the cell. Should return HTML safe
     * content, meaning if you intend to display the < character you need
     * to HTML encode it as &lt;.
     */
    format: Component;

    /** Gets the value to use when filtering on the quick filter. */
    quickFilterValue: QuickFilterValueFunction;

    /**
     * Gets the unique value representation of the cell value. For example,
     * two people might have the same display name so they will look identical.
     * But in reality they have a different identifier and are two different
     * people.
     */
    uniqueValue: UniqueValueFunction;

    /** Gets the value to use when sorting. */
    sortValue?: SortValueFunction;

    /** Gets the value to use when performing column filtering. */
    filterValue: FilterValueFunction;

    /** Gets the filter to use to perform column filtering. */
    filter?: ColumnFilter;

    /** The additional CSS class to apply to the header cell. */
    headerClass?: string;

    /** The additional CSS class to apply to the data item cell. */
    itemClass?: string;

    /**
     * If `true` then the column will not ever be rendered on screen. It may
     * still be included in exports and other operations.
     */
    hideOnScreen: boolean;

    /**
     * If `true` then the column will not be included when exporting data to
     * be downloaded by the individual.
     */
    excludeFromExport: boolean;

    /** All properties and attributes that were defined on the column. */
    props: Record<string, unknown>;

    /** Custom data that the column and cells can use any way they desire. */
    data: Record<string, unknown>;
};

/**
 * Defines a column filter. This contains the information required to display
 * the column filter UI as well as perform the row filtering.
 */
export type ColumnFilter = {
    /** The component that will handle displaying the UI for the filter. */
    component: Component;

    /**
     * The function that will be called on each row to determine if it
     * matches the filter value.
     */
    matches: ColumnFilterMatchesFunction;
};

/**
 * Defines the information required to handle sorting on a single column.
 */
export type ColumnSort = {
    /** The name of the column to be sorted. */
    column: string;

    /** True if the column should be sorted in descending order. */
    isDescending: boolean;
};

export type GridPropertyChangedEvents =
    "rowsChanged" |
    "filteredRowsChanged" |
    "sortedRowsChanged" |
    "visibleColumnsChanged" |
    "selectedKeysChanged" |
    "isFilteredChanged" |
    "isSortedChanged";

/**
 * Defines the public interface for tracking the state of a grid.
 * Implementations are in charge of all the heavy lifting of a grid to handle
 * filtering, sorting and other operations that don't require a direct UI.
 */
export interface IGridState {
    /**
     * The cache object for the grid. This can be used to store custom data
     * related to the grid as a whole.
     */
    readonly cache: IGridCache;

    /**
     * The cache object for specific rows. This can be used to store custom
     * data related to a single row of the grid.
     */
    readonly rowCache: IGridRowCache;

    /** The defined columns on the grid. */
    readonly columns: ReadonlyArray<ColumnDefinition>;

    /**
     * The columns that are currently visible in the DOM. The columns might
     * still be hidden via CSS.
     */
    readonly visibleColumns: ReadonlyArray<ColumnDefinition>;

    /** The set of all rows that are known by the grid. */
    readonly rows: ReadonlyArray<Record<string, unknown>>;

    /** The current set of rows that have passed the filters. */
    readonly filteredRows: ReadonlyArray<Record<string, unknown>>;

    /** The current set of rows that have been filtered and sorted. */
    readonly sortedRows: ReadonlyArray<Record<string, unknown>>;

    /** The word or phrase that describes the individual row items.  */
    readonly itemTerm: string;

    /** Will be `true` if the grid rows currently have any filtering applied. */
    readonly isFiltered: boolean;

    /** Will be `true` if the grid rows currently have any sorting applied. */
    readonly isSorted: boolean;

    /**
     * The unique identifier of the entity type that the rows represent. If the
     * rows do not represent an entity then this will be undefined.
     */
    readonly entityTypeGuid?: Guid;

    /**
     * The currently selected row keys. This is a reactive array so it can
     * be watched for changes.
     */
    selectedKeys: ReadonlyArray<string>;

    /**
     * Gets the cache key to use for storing column specific data for a
     * component.
     *
     * @param column The column that will determine the cache key prefix.
     * @param component The identifier or name of the component wanting to access the cache.
     * @param key The key that the component wishes to access or store data in.
     *
     * @returns A string should be used as the cache key.
     */
    getColumnCacheKey(column: ColumnDefinition, component: string, key: string): string;

    /**
     * Gets the key of the specified row in the grid.
     *
     * @param row The row whose key should be returned.
     *
     * @returns The unique key of the row or `undefined` if it could not be determined.
     */
    getRowKey(row: Record<string, unknown>): string | undefined;

    /**
     * Gets all rows in the grid and sorts them according to the current
     * sorting rules. The {@link sortedRows} property only contains the rows
     * that match the filter. But this function will return all rows.
     *
     * @returns An array of all rows in the grid that has been sorted.
     */
    getSortedRows(): Record<string, unknown>[];

    /**
     * Registers a function that will be called whenever the value of the
     * property changes.
     *
     * @param event The event to listen for.
     * @param callback The function to be called.
     */
    on(event: GridPropertyChangedEvents, callback: (grid: IGridState) => void): void;

    /**
     * Removes a function from the specified event. The function will no longer
     * be called when the event is raised.
     *
     * @param event The event that was previously listened to.
     * @param callback The callback that was previously registered.
     */
    off(event: GridPropertyChangedEvents, callback: (grid: IGridState) => void): void;
}
