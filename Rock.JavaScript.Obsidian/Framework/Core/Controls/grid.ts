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

import { defineComponent, PropType, reactive, ref, Ref, shallowRef, ShallowRef, unref, VNode, watch, WatchStopHandle } from "vue";
import { NumberFilterMethod } from "@Obsidian/Enums/Controls/Grid/numberFilterMethod";
import { DateFilterMethod } from "@Obsidian/Enums/Controls/Grid/dateFilterMethod";
import { ColumnFilter, ColumnDefinition, IGridState, StandardFilterProps, StandardCellProps, IGridCache, IGridRowCache, ColumnSort, SortValueFunction, FilterValueFunction, QuickFilterValueFunction, UniqueValueFunction, StandardColumnProps, StandardHeaderCellProps } from "@Obsidian/Types/Controls/grid";
import { getVNodeProp, getVNodeProps } from "@Obsidian/Utility/component";
import { DayOfWeek, RockDateTime } from "@Obsidian/Utility/rockDateTime";
import { resolveMergeFields } from "@Obsidian/Utility/lava";
import { deepEqual } from "@Obsidian/Utility/util";
import { AttributeFieldDefinitionBag } from "@Obsidian/ViewModels/Core/Grid/attributeFieldDefinitionBag";
import { Guid } from "@Obsidian/Types";

// #region Standard Component Props

/**
 * Defines the standard properties available on all columns.
 */
export const standardColumnProps: StandardColumnProps = {
    name: {
        type: String as PropType<string>,
        default: ""
    },

    title: {
        type: String as PropType<string>,
        required: false
    },

    field: {
        type: String as PropType<string>,
        required: false
    },

    quickFilterValue: {
        type: Object as PropType<QuickFilterValueFunction | string>,
        required: false
    },

    sortField: {
        type: String as PropType<string>,
        required: false
    },

    sortValue: {
        type: Object as PropType<(SortValueFunction | string)>,
        required: false
    },

    filter: {
        type: Object as PropType<ColumnFilter>,
        required: false
    },

    filterValue: {
        type: Object as PropType<(FilterValueFunction | string)>,
        required: false
    },

    uniqueValue: {
        type: Object as PropType<UniqueValueFunction>,
        required: false
    },

    headerClass: {
        type: String as PropType<string>,
        required: false
    },

    itemClass: {
        type: String as PropType<string>,
        required: false
    },

    format: {
        type: Object as PropType<VNode>,
        required: false
    },

    headerTemplate: {
        type: Object as PropType<VNode>,
        required: false
    }
};

/** The standard properties available on header cells. */
export const standardHeaderCellProps: StandardHeaderCellProps = {
    column: {
        type: Object as PropType<ColumnDefinition>,
        required: true
    },

    grid: {
        type: Object as PropType<IGridState>,
        required: true
    }
};

/** The standard properties available on cells. */
export const standardCellProps: StandardCellProps = {
    column: {
        type: Object as PropType<ColumnDefinition>,
        required: true
    },

    row: {
        type: Object as PropType<Record<string, unknown>>,
        required: true
    },

    grid: {
        type: Object as PropType<IGridState>,
        required: true
    }
};

/**
 * The standard properties that are made available to column filter
 * components.
 */
export const standardFilterProps: StandardFilterProps = {
    modelValue: {
        type: Object as PropType<unknown>,
        required: false
    },

    column: {
        type: Object as PropType<ColumnDefinition>,
        required: true
    },

    grid: {
        type: Object as PropType<IGridState>,
        required: true
    }
};

// #endregion

// #region Filter Matches Functions

/**
 * The text column filter that performs a substring search to see if the
 * `needle` is contained within the `haystack`.
 *
 * @private This is used internally by Rock and should not be used directly.
 *
 * @param needle The filter value defined in the UI component.
 * @param haystack The filter value from the row that must match the `needle`.
 *
 * @returns True if `haystack` matches the `needle` and should be included in the results.
 */
export function textFilterMatches(needle: unknown, haystack: unknown): boolean {
    if (typeof (needle) !== "string") {
        return false;
    }

    if (!needle || !haystack || typeof haystack !== "string") {
        return true;
    }

    return haystack.toLowerCase().includes(needle.toLowerCase());
}

/**
 * The column filter compares the `needle` against the `haystack` to see if
 * they match. This is a deep equality check so if they are arrays or objects
 * then all child objects and properties must match exactly.
 *
 * @private This is used internally by Rock and should not be used directly.
 *
 * @param needle The filter value defined in the UI component.
 * @param haystack The filter value from the row that must match the `needle`.
 *
 * @returns True if `haystack` matches the `needle` and should be included in the results.
 */
export function pickExistingFilterMatches(needle: unknown, haystack: unknown): boolean {
    if (!Array.isArray(needle)) {
        return false;
    }

    if (needle.length === 0) {
        return true;
    }

    return needle.some(n => deepEqual(n, haystack, true));
}

/**
 * The number column filter that performs a comparison of `haystack` and
 * the value and comparison type inside `needle` to see if it matches.
 *
 * @private This is used internally by Rock and should not be used directly.
 *
 * @param needle The filter value defined in the UI component.
 * @param haystack The filter value from the row that must match the `needle`.
 *
 * @returns True if `haystack` matches the `needle` and should be included in the results.
 */
export function numberFilterMatches(needle: unknown, haystack: unknown, column: ColumnDefinition, grid: IGridState): boolean {
    if (!needle || typeof needle !== "object") {
        return false;
    }

    // Allow undefined values and number values, but everything else is
    // considered a non-match.
    if (haystack !== undefined && typeof haystack !== "number") {
        return false;
    }

    if (needle["method"] === NumberFilterMethod.Equals) {
        return haystack === needle["value"];
    }
    else if (needle["method"] === NumberFilterMethod.DoesNotEqual) {
        return haystack !== needle["value"];
    }

    // All the remaining comparison types require a value.
    if (haystack === undefined) {
        return false;
    }

    if (needle["method"] === NumberFilterMethod.GreaterThan) {
        return haystack > needle["value"];
    }
    else if (needle["method"] === NumberFilterMethod.GreaterThanOrEqual) {
        return haystack >= needle["value"];
    }
    else if (needle["method"] === NumberFilterMethod.LessThan) {
        return haystack < needle["value"];
    }
    else if (needle["method"] === NumberFilterMethod.LessThanOrEqual) {
        return haystack <= needle["value"];
    }
    else if (needle["method"] === NumberFilterMethod.Between) {
        if (typeof needle["value"] !== "number" || typeof needle["secondValue"] !== "number") {
            return false;
        }

        return haystack >= needle["value"] && haystack <= needle["secondValue"];
    }
    else if (needle["method"] === NumberFilterMethod.TopN) {
        const nCount = needle["value"];

        if (typeof nCount !== "number" || nCount <= 0) {
            return false;
        }

        const cacheKey = grid.getColumnCacheKey(column, "number-filter", `top-${nCount}`);
        const topn = grid.cache.getOrAdd(cacheKey, () => {
            return calculateColumnTopNRowValue(nCount, column, grid);
        });

        return haystack >= topn;
    }
    else if (needle["method"] === NumberFilterMethod.AboveAverage) {
        const cacheKey = grid.getColumnCacheKey(column, "number-filter", "average");
        const average = grid.cache.getOrAdd(cacheKey, () => {
            return calculateColumnAverageValue(column, grid);
        });

        return haystack > average;
    }
    else if (needle["method"] === NumberFilterMethod.BelowAverage) {
        const cacheKey = grid.getColumnCacheKey(column, "number-filter", "average");
        const average = grid.cache.getOrAdd(cacheKey, () => {
            return calculateColumnAverageValue(column, grid);
        });

        return haystack < average;
    }
    else {
        return false;
    }
}

/**
 * The date column filter that performs a comparison of `haystack` and
 * the value and comparison type inside `needle` to see if it matches.
 *
 * @private This is used internally by Rock and should not be used directly.
 *
 * @param needle The filter value defined in the UI component.
 * @param haystack The filter value from the row that must match the `needle`.
 *
 * @returns True if `haystack` matches the `needle` and should be included in the results.
 */
export function dateFilterMatches(needle: unknown, haystack: unknown): boolean {
    if (!needle || typeof needle !== "object") {
        return false;
    }

    // Allow undefined values and number values, but everything else is
    // considered a non-match.
    if (haystack !== undefined && typeof haystack !== "string") {
        return false;
    }

    const needleFirstDate = RockDateTime.parseISO(needle["value"] ?? "")?.date.toMilliseconds() ?? 0;
    const needleSecondDate = RockDateTime.parseISO(needle["secondValue"] ?? "")?.date.toMilliseconds() ?? 0;
    const haystackDate = RockDateTime.parseISO(haystack ?? "")?.date.toMilliseconds() ?? 0;
    const today = RockDateTime.now().date;

    if (needle["method"] === DateFilterMethod.Equals) {
        return haystackDate === needleFirstDate;
    }
    else if (needle["method"] === DateFilterMethod.DoesNotEqual) {
        return haystackDate !== needleFirstDate;
    }

    // All the remaining comparison types require a value.
    if (haystackDate === 0) {
        return false;
    }

    if (needle["method"] === DateFilterMethod.Before) {
        return haystackDate < needleFirstDate;
    }
    else if (needle["method"] === DateFilterMethod.After) {
        return haystackDate > needleFirstDate;
    }
    else if (needle["method"] === DateFilterMethod.Between) {
        return haystackDate >= needleFirstDate && haystackDate <= needleSecondDate;
    }
    // SUn = 0, Mon = 1, Tue = 2, Wed = 3
    // Start = Tue = 2
    else if (needle["method"] === DateFilterMethod.ThisWeek) {
        const firstDayOfWeek = getStartOfWeek(today);
        const lastDayOfWeek = firstDayOfWeek.addDays(6);

        return haystackDate >= firstDayOfWeek.toMilliseconds()
            && haystackDate <= lastDayOfWeek.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.LastWeek) {
        const firstDayOfWeek = getStartOfWeek(today).addDays(-7);
        const lastDayOfWeek = firstDayOfWeek.addDays(6);

        return haystackDate >= firstDayOfWeek.toMilliseconds()
            && haystackDate <= lastDayOfWeek.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.NextWeek) {
        const firstDayOfWeek = getStartOfWeek(today).addDays(7);
        const lastDayOfWeek = firstDayOfWeek.addDays(6);

        return haystackDate >= firstDayOfWeek.toMilliseconds()
            && haystackDate <= lastDayOfWeek.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.ThisMonth) {
        const firstDayOfMonth = today.addDays(-(today.day - 1));
        const lastDayOfMonth = firstDayOfMonth.addMonths(1).addDays(-1);

        return haystackDate >= firstDayOfMonth.toMilliseconds()
            && haystackDate <= lastDayOfMonth.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.LastMonth) {
        const firstDayOfMonth = today.addDays(-(today.day - 1)).addMonths(-1);
        const lastDayOfMonth = firstDayOfMonth.addMonths(1).addDays(-1);

        return haystackDate >= firstDayOfMonth.toMilliseconds()
            && haystackDate <= lastDayOfMonth.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.NextMonth) {
        const firstDayOfMonth = today.addDays(-(today.day - 1)).addMonths(1);
        const lastDayOfMonth = firstDayOfMonth.addMonths(1).addDays(-1);

        return haystackDate >= firstDayOfMonth.toMilliseconds()
            && haystackDate <= lastDayOfMonth.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.ThisYear) {
        const firstDayOfYear = today.addDays(-(today.dayOfYear - 1));
        const lastDayOfYear = firstDayOfYear.addYears(1).addDays(-1);

        return haystackDate >= firstDayOfYear.toMilliseconds()
            && haystackDate <= lastDayOfYear.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.LastYear) {
        const firstDayOfYear = today.addDays(-(today.dayOfYear - 1)).addYears(-1);
        const lastDayOfYear = firstDayOfYear.addYears(1).addDays(-1);

        return haystackDate >= firstDayOfYear.toMilliseconds()
            && haystackDate <= lastDayOfYear.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.NextYear) {
        const firstDayOfYear = today.addDays(-(today.dayOfYear - 1)).addYears(1);
        const lastDayOfYear = firstDayOfYear.addYears(1).addDays(-1);

        return haystackDate >= firstDayOfYear.toMilliseconds()
            && haystackDate <= lastDayOfYear.toMilliseconds();
    }
    else if (needle["method"] === DateFilterMethod.YearToDate) {
        const firstDayOfYear = today.addDays(-(today.dayOfYear - 1));

        return haystackDate >= firstDayOfYear.toMilliseconds()
            && haystackDate <= today.toMilliseconds();
    }
    else {
        return false;
    }
}

// #endregion

// #region Functions

/**
 * Gets a new date from the passed date whose date portion is the start
 * of the week. The time value is not modified.
 *
 * @param date The original date to be used in the calculation.
 *
 * @returns A new date object whose date portion matches the start of the week.
 */
function getStartOfWeek(date: RockDateTime): RockDateTime {
    const weekStartsOn = DayOfWeek.Monday;
    let targetDate: RockDateTime = date;

    while (targetDate.dayOfWeek !== weekStartsOn) {
        targetDate = targetDate.addDays(-1);
    }

    return targetDate;
}

/**
 * Calculates the average numerical value across all rows in a grid column.
 *
 * @param column The column whose values should be considered for the average.
 * @param grid The grid that provides all the rows.
 *
 * @returns A number that represents the average value or `0` if no rows with numeric values existed.
 */
export function calculateColumnAverageValue(column: ColumnDefinition, grid: IGridState): number {
    let count = 0;
    let total = 0;

    for (const row of grid.rows) {
        const rowValue = column.filterValue(row, column, grid);

        if (typeof rowValue === "number") {
            total += rowValue;
            count++;
        }
    }

    return count === 0 ? 0 : total / count;
}

/**
 * Calculates top Nth numeric row value for a column. If the cell values are
 * `1, 2, 3, 4, 4, 5, 5` and `rowCount` is 3 then this will return
 * the value `4`. Which means the final row count displayed will be 4 because
 * there are 4 rows with values >= 4.
 *
 * @param column The column whose values should be considered for the average.
 * @param grid The grid that provides all the rows.
 *
 * @returns A number that represents the average value or `0` if no rows with numeric values existed.
 */
export function calculateColumnTopNRowValue(rowCount: number, column: ColumnDefinition, grid: IGridState): number {
    const values: number[] = [];

    for (const row of grid.rows) {
        const rowValue = column.filterValue(row, column, grid);

        if (typeof rowValue === "number") {
            values.push(rowValue);
        }
    }

    if (values.length === 0) {
        return 0;
    }

    // Sort in descending order.
    values.sort((a, b) => b - a);

    if (rowCount <= values.length) {
        return values[rowCount - 1];
    }
    else {
        return values[values.length - 1];
    }
}

/**
 * Gets the value from the row cache or creates the value and stores it in
 * cache. This is a tiny helper function to simplify the process of
 * implementing cache when constructing column definitions.
 *
 * @param row The row whose value will be cached.
 * @param column The column that the value will be associated with.
 * @param key The key that identifies the value to be cached.
 * @param grid The grid that will be used for caching.
 * @param factory The function that will return the value if it is not already in cache.
 * @returns Either the value from cache or the value returned by `factory`.
 */
function getOrAddRowCacheValue<T>(row: Record<string, unknown>, column: ColumnDefinition, key: string, grid: IGridState, factory: (() => T)): T {
    const finalKey = grid.getColumnCacheKey(column, "grid", key);

    return grid.rowCache.getOrAdd<T>(row, finalKey, () => factory());
}

/**
 * Builds the column definitions for the attributes defined on the node.
 *
 * @param columns The array of columns that the new attribute columns will be appended to.
 * @param node The node that defines the attribute fields.
 */
function buildAttributeColumns(columns: ColumnDefinition[], node: VNode): void {
    const attributes = getVNodeProp<AttributeFieldDefinitionBag[]>(node, "attributes");
    if (!attributes) {
        return;
    }

    for (const attribute of attributes) {
        if (!attribute.name) {
            continue;
        }

        columns.push({
            name: attribute.name,
            title: attribute.title ?? undefined,
            field: attribute.name,
            uniqueValue: (r, c) => c.field ? String(r[c.field]) : "",
            sortValue: (r, c) => c.field ? String(r[c.field]) : undefined,
            quickFilterValue: (r, c, g) => getOrAddRowCacheValue(r, c, "quickFilterValue", g, () => c.field ? String(r[c.field]) : undefined),
            filterValue: (r, c) => c.field ? String(r[c.field]) : undefined,
            format: getVNodeProp<VNode>(node, "format") ?? defaultCell,
            props: {},
            data: {}
        });
    }
}

/**
 * Builds a new column definition from the information provided.
 *
 * @param name The name of the column.
 * @param node The node that contains all the details about the column.
 *
 * @returns A new object that represents the column.
 */
function buildColumn(name: string, node: VNode): ColumnDefinition {
    const field = getVNodeProp<string>(node, "field");
    const title = getVNodeProp<string>(node, "title");
    const format = node.children?.["body"] ?? getVNodeProp<VNode>(node, "format") ?? defaultCell;
    const headerTemplate = node.children?.["header"] ?? getVNodeProp<VNode>(node, "headerTemplate");
    const filter = getVNodeProp<ColumnFilter>(node, "filter");
    const headerClass = getVNodeProp<string>(node, "headerClass");
    const itemClass = getVNodeProp<string>(node, "itemClass");

    // Get the function that will provide the sort value.
    let sortValue = getVNodeProp<SortValueFunction | string>(node, "sortValue");

    if (!sortValue) {
        const sortField = getVNodeProp<string>(node, "sortField") || field;

        sortValue = sortField ? (r) => String(r[sortField]) : undefined;
    }
    else if (typeof sortValue === "string") {
        const template = sortValue;

        sortValue = (row): string | undefined => {
            return resolveMergeFields(template, { row });
        };
    }

    // Get the function that will provide the quick filter value.
    let quickFilterValue = getVNodeProp<QuickFilterValueFunction | string>(node, "quickFilterValue");

    if (!quickFilterValue) {
        // One was not provided, so generate a common use one.
        quickFilterValue = (r, c): string | undefined => {
            if (!c.field) {
                return undefined;
            }

            const v = r[c.field];

            if (typeof v === "string") {
                return v;
            }
            else if (typeof v === "number") {
                return v.toString();
            }
            else {
                return undefined;
            }
        };
    }
    else if (typeof quickFilterValue === "string") {
        const template = quickFilterValue;

        quickFilterValue = (row): string | undefined => {
            return resolveMergeFields(template, { row });
        };
    }

    // Get the function that will provide the column filter value.
    let filterValue = getVNodeProp<FilterValueFunction | string>(node, "filterValue");

    if (filterValue === undefined) {
        // One wasn't provided, so do our best to infer what it should be.
        filterValue = (r, c): unknown => {
            if (!c.field) {
                return undefined;
            }

            return r[c.field];
        };
    }
    else if (typeof filterValue === "string") {
        const template = filterValue;

        filterValue = (row): unknown => {
            return resolveMergeFields(template, { row });
        };
    }

    // Get the function that will provide the unique value for a cell.
    let uniqueValue = getVNodeProp<UniqueValueFunction>(node, "uniqueValue");

    if (!uniqueValue) {
        uniqueValue = (r, c) => {
            if (!c.field || r[c.field] === undefined) {
                return undefined;
            }

            const v = r[c.field];

            if (typeof v === "string" || typeof v === "number") {
                return v;
            }

            return JSON.stringify(v);
        };
    }

    // Convert all the value functions into cached ones.
    const uniqueValueFactory = uniqueValue;
    uniqueValue = (r, c, g) => {
        return getOrAddRowCacheValue(r, c, "uniqueValue", g, () => uniqueValueFactory(r, c, g));
    };

    const sortValueFactory = sortValue;
    sortValue = (r, c, g) => {
        return sortValueFactory !== undefined
            ? getOrAddRowCacheValue(r, c, "sortValue", g, () => sortValueFactory(r, c, g))
            : undefined;
    };

    const filterValueFactory = filterValue;
    filterValue = (r, c, g) => {
        return getOrAddRowCacheValue(r, c, "filterValue", g, () => filterValueFactory(r, c, g));
    };

    const quickFilterValueFactory = quickFilterValue;
    quickFilterValue = (r, c, g) => {
        return getOrAddRowCacheValue(r, c, "quickFilterValue", g, () => quickFilterValueFactory(r, c, g));
    };

    // Build the final column definition.
    const column: ColumnDefinition = {
        name,
        title,
        field,
        format,
        headerTemplate,
        filter,
        uniqueValue,
        sortValue,
        filterValue,
        quickFilterValue,
        headerClass,
        itemClass,
        props: getVNodeProps(node),
        data: {}
    };

    return column;
}

/**
 * Builds the column definitions from the array of virtual nodes found inside
 * of a component.
 *
 * @param columnNodes The virtual nodes that contain the definitions of the columns.
 *
 * @returns An array of {@link ColumnDefinition} objects.
 */
export function getColumnDefinitions(columnNodes: VNode[]): ColumnDefinition[] {
    const columns: ColumnDefinition[] = [];

    for (const node of columnNodes) {
        const name = getVNodeProp<string>(node, "name");

        // Check if this node is the special AttributeColumns node.
        if (!name) {
            if (getVNodeProp<boolean>(node, "__attributeColumns") !== true) {
                continue;
            }

            buildAttributeColumns(columns, node);

            continue;
        }

        // Build the final column definition.
        const column = buildColumn(name, node);
        columns.push(column);
    }

    return columns;
}

/**
 * Gets the key to use on the internal cache object to load the cached data
 * for the specified row.
 *
 * @param row The row whose identifier key is needed.
 *
 * @returns The identifier key of the row or `undefined` if it could not be determined.
 */
export function getRowKey(row: Record<string, unknown>, itemIdKey?: string): string | undefined {
    if (!itemIdKey) {
        return undefined;
    }

    const rowKey = row[itemIdKey];

    if (typeof rowKey === "string") {
        return rowKey;
    }
    else if (typeof rowKey === "number") {
        return `${rowKey}`;
    }
    else {
        return undefined;
    }
}

// #endregion

// #region Classes

/**
 * Default implementation used for caching data with Grid.
 *
 * @private This class is meant for internal use only.
 */
export class GridCache implements IGridCache {
    /** The private cache data storage. */
    private cacheData: Record<string, unknown> = {};

    public clear(): void {
        this.cacheData = {};
    }

    public remove(key: string): void {
        if (key in this.cacheData) {
            delete this.cacheData[key];
        }
    }

    public get<T = unknown>(key: string): T | undefined {
        if (key in this.cacheData) {
            return <T>this.cacheData[key];
        }
        else {
            return undefined;
        }
    }

    public getOrAdd<T = unknown>(key: string, factory: () => T): T;
    public getOrAdd<T = unknown>(key: string, factory: () => T | undefined): T | undefined;
    public getOrAdd<T = unknown>(key: string, factory: () => T | undefined): T | undefined {
        if (key in this.cacheData) {
            return <T>this.cacheData[key];
        }
        else {
            const value = factory();

            if (value !== undefined) {
                this.cacheData[key] = value;
            }

            return value;
        }
    }

    public addOrReplace<T = unknown>(key: string, value: T): T {
        this.cacheData[key] = value;

        return value;
    }
}

/**
 * Default implementation used for caching grid row data.
 *
 * @private This class is meant for internal use only.
 */
export class GridRowCache implements IGridRowCache {
    /** The internal cache object used to find the cached row data. */
    private cache: IGridCache = new GridCache();

    /** The key name to use on the row objects to find the row identifier. */
    private rowItemKey?: string;

    /**
     * Creates a new grid row cache object that provides caching for each row.
     * This is used by other parts of the grid to cache expensive calculations
     * that pertain to a single row.
     *
     * @param itemIdKey The key name to use on the row objects to find the row identifier.
     */
    public constructor(itemIdKey: string | undefined) {
        this.rowItemKey = itemIdKey;
    }

    /**
     * Gets the key to use on the internal cache object to load the cached data
     * for the specified row.
     *
     * @param row The row whose identifier key is needed.
     *
     * @returns The identifier key of the row or `undefined` if it could not be determined.
     */
    private getRowKey(row: Record<string, unknown>): string | undefined {
        return getRowKey(row, this.rowItemKey);
    }

    /**
     * Sets the key that will be used when accessing a row to determine its
     * unique identifier in the grid. This will also clear all cached data.
     *
     * @param itemKey The key name to use on the row objects to find the row identifier.
     */
    public setRowItemKey(itemKey: string | undefined): void {
        if (this.rowItemKey !== itemKey) {
            this.rowItemKey = itemKey;
            this.clear();
        }
    }

    public clear(): void {
        this.cache.clear();
    }

    public remove(row: Record<string, unknown>): void;
    public remove(row: Record<string, unknown>, key: string): void;
    public remove(row: Record<string, unknown>, key?: string): void {
        const rowKey = this.getRowKey(row);

        if (!rowKey) {
            return;
        }

        const cacheRow = this.cache.get<GridCache>(rowKey);

        if (!cacheRow) {
            return;
        }

        if (!key) {
            cacheRow.clear();
        }
        else {
            cacheRow.remove(key);
        }
    }

    public get<T = unknown>(row: Record<string, unknown>, key: string): T | undefined {
        const rowKey = this.getRowKey(row);

        if (!rowKey) {
            return undefined;
        }

        return this.cache.getOrAdd(rowKey, () => new GridCache()).get<T>(key);
    }

    public getOrAdd<T = unknown>(row: Record<string, unknown>, key: string, factory: () => T): T;
    public getOrAdd<T = unknown>(row: Record<string, unknown>, key: string, factory: () => T | undefined): T | undefined;
    public getOrAdd<T = unknown>(row: Record<string, unknown>, key: string, factory: () => T | undefined): T | undefined {
        const rowKey = this.getRowKey(row);

        if (!rowKey) {
            return factory();
        }

        return this.cache.getOrAdd(rowKey, () => new GridCache()).getOrAdd<T>(key, factory);
    }

    public addOrReplace<T = unknown>(row: Record<string, unknown>, key: string, value: T): T {
        const rowKey = this.getRowKey(row);

        if (!rowKey) {
            return value;
        }

        return this.cache.getOrAdd(rowKey, () => new GridCache()).addOrReplace<T>(key, value);
    }
}

/**
 * Default implementation of the grid state for internal use.
 *
 * @private The is an internal class that should not be used by plugins.
 */
export class GridState implements IGridState {
    // #region Properties

    private internalRows: ShallowRef<Record<string, unknown>[]> = shallowRef([]);

    /** This tracks the state of each row when operating in reactive mode. */
    private rowReactiveTracker: Record<string, string> = {};

    /** The handle that can be used to stop watching row changes. */
    private internalRowsWatcher?: WatchStopHandle;

    /** Determines if we are monitoring for changes to the row data. */
    private liveUpdates: boolean;

    /** The key to get the unique identifier of each row. */
    private itemKey?: string;

    /** The current quick filter value that will be used to filter the rows. */
    private quickFilter: string = "";

    /**
     * The currently applied per-column filters that will be used to filter
     * the rows.
     */
    private columnFilters: Record<string, unknown | undefined> = {};

    /** The current column being used to sort the rows. */
    private columnSort?: ColumnSort;

    // #endregion

    // #region Constructors

    /**
     * Creates a new instance of the GridState for use with the Grid component.
     *
     * @param columns The columns to initialize the Grid with.
     * @param liveUpdates If true then the grid will monitor for live updates to rows.
     * @param itemTerm The word or phrase that describes each row.
     */
    constructor(columns: ColumnDefinition[], liveUpdates: boolean, itemTerm: string, entityTypeGuid: Guid | undefined) {
        this.rowCache = new GridRowCache(undefined);
        this.columns = columns;
        this.liveUpdates = liveUpdates;
        this.itemTerm = itemTerm;
        this.entityTypeGuid = entityTypeGuid;
    }

    /**
     * Dispose of all resources this grid state has. This includes any watchers
     * and other things that might need to be manually destroyed to free up
     * memory. A common pattern would be to call this in the onUmounted() callback.
     */
    public dispose(): void {
        if (this.internalRowsWatcher) {
            this.internalRowsWatcher();
            this.internalRowsWatcher = undefined;
        }
    }

    // #endregion

    // #region IGridState Implementation

    public readonly filteredRows: ShallowRef<Record<string, unknown>[]> = shallowRef([]);

    public readonly sortedRows: ShallowRef<Record<string, unknown>[]> = shallowRef([]);

    public readonly columns: ColumnDefinition[];

    public readonly cache: IGridCache = new GridCache();

    public readonly rowCache: IGridRowCache;

    public readonly itemTerm: string;

    public readonly entityTypeGuid?: Guid | undefined;

    public readonly selectedKeys: string[] = reactive([]);

    public isFiltered: Ref<boolean> = ref(false);

    public isSorted: Ref<boolean> = ref(false);

    get rows(): Record<string, unknown>[] {
        return this.internalRows.value;
    }

    public getColumnCacheKey(column: ColumnDefinition, component: string, key: string): string {
        return `column-${column.name}-${component}-${key}`;
    }

    public getRowKey(row: Record<string, unknown>): string | undefined {
        return getRowKey(row, this.itemKey);
    }

    // #endregion

    // #region Private Functions

    /**
     * Begins tracking all rows in the grid so that we can monitor for
     * changes and update the UI accordingly.
     */
    private initializeReactiveTracker(): void {
        const rows = unref(this.internalRows);

        this.rowReactiveTracker = {};

        for (let i = 0; i < rows.length; i++) {
            const key = getRowKey(rows[i], this.itemKey);

            if (key) {
                this.rowReactiveTracker[key] = JSON.stringify(rows[i]);
            }
        }
    }

    /**
     * Detects any changes to the row data from the last time we were called.
     * Must be called after {@link initializeReactiveTracker}.
     */
    private detectRowChanges(): void {
        const rows = unref(this.internalRows);
        const knownKeys: string[] = [];

        // Loop through all the rows we still have and check for any that
        // are new or have been modified.
        for (let i = 0; i < rows.length; i++) {
            const key = getRowKey(rows[i], this.itemKey);

            if (!key) {
                continue;
            }

            // Save the key for later.
            knownKeys.push(key);

            if (!this.rowReactiveTracker[key]) {
                console.log("Row added", rows[i]);
            }
            else if (this.rowReactiveTracker[key] !== JSON.stringify(rows[i])) {
                console.log("Row updated", rows[i]);
                this.rowReactiveTracker[key] = JSON.stringify(rows[i]);
                this.rowCache.remove(rows[i]);
            }
        }

        // Loop through all the row key values that are being tracked and
        // see if any no longer exist in our data set.
        const oldKeys = Object.keys(this.rowReactiveTracker);
        for (let i = 0; i < oldKeys.length; i++) {
            if (!knownKeys.includes(oldKeys[i])) {
                console.log("Removed row id", oldKeys[i]);
                // TODO: Remove the cache data for a row by it's key.
                delete this.rowReactiveTracker[oldKeys[i]];
            }
        }
    }

    /**
     * Performs filtering of the {@link rows} and determines which rows
     * match the filters.
     */
    private updateFilteredRows(): void {
        if (this.columns.length === 0) {
            this.filteredRows.value = [];
            this.updateSortedRows();

            return;
        }

        const start = Date.now();
        const columns = this.columns;
        const quickFilterRawValue = this.quickFilter.toLowerCase();

        const result = this.rows.filter(row => {
            // Check if the row matches the quick filter.
            const quickFilterMatch = !quickFilterRawValue || columns.some((column): boolean => {
                const value = column.quickFilterValue(row, column, this);

                if (value === undefined) {
                    return false;
                }

                return value.toLowerCase().includes(quickFilterRawValue);
            });

            // Bail out early if the quick filter didn't match.
            if (!quickFilterMatch) {
                return false;
            }

            // Check if the row matches the column specific filters.
            return columns.every(column => {
                if (!column.filter) {
                    return true;
                }

                const columnFilterValue = this.columnFilters[column.name];

                if (columnFilterValue === undefined) {
                    return true;
                }

                const value: unknown = column.filterValue(row, column, this);

                if (value === undefined) {
                    return false;
                }

                return column.filter.matches(columnFilterValue, value, column, this);
            });
        });

        this.filteredRows.value = result;

        console.log(`Filtering took ${Date.now() - start}ms.`);

        this.updateSortedRows();
    }

    /**
     * Takes the {@link filteredRows} and sorts them according to the information
     * tracked by the Grid and updates the {@link sortedRows} property.
     */
    private updateSortedRows(): void {
        const columnSort = this.columnSort;

        // Bail early if we don't have any sorting to perform.
        if (!columnSort) {
            this.sortedRows.value = this.filteredRows.value;

            return;
        }

        const start = Date.now();
        const column = this.columns.find(c => c.name === columnSort.column);
        const order = columnSort.isDescending ? -1 : 1;

        if (!column) {
            throw new Error("Invalid sort definition");
        }

        const sortValue = column.sortValue;

        // Pre-process each row to calculate the sort value. Otherwise it will
        // be calculated exponentially during sort. This provides a serious
        // performance boost when sorting Lava columns. Even though we have
        // cache we do it this way because we may not have an itemKey which
        // would disable the cache.
        const rows = this.filteredRows.value.map(r => {
            let value: string | number | undefined;

            if (sortValue) {
                value = sortValue(r, column, this);
            }
            else {
                value = undefined;
            }

            return {
                row: r,
                value
            };
        });

        rows.sort((a, b) => {
            if (a.value === undefined) {
                return -order;
            }
            else if (b.value === undefined) {
                return order;
            }
            else if (a.value < b.value) {
                return -order;
            }
            else if (a.value > b.value) {
                return order;
            }
            else {
                return 0;
            }
        });

        this.sortedRows.value = rows.map(r => r.row);

        console.log(`sortedRows took ${Date.now() - start}ms.`);
    }

    // #endregion

    // #region Public Functions

    /**
     * Sets the item key used to uniquely identify rows.
     *
     * @param value The field name that contains the item key.
     */
    public setItemKey(value: string | undefined): void {
        this.itemKey = value;
        (this.rowCache as GridRowCache).setRowItemKey(value);
    }

    /**
     * Sets the rows to be used by the Grid. This will replace all existing
     * row data.
     *
     * @param rows The array of row data to use for the Grid.
     */
    public setDataRows(rows: Record<string, unknown>[]): void {
        // Stop watching the old rows if we are currently watching for changes.
        if (this.internalRowsWatcher) {
            this.internalRowsWatcher();
            this.internalRowsWatcher = undefined;
        }

        // Update out internal rows and clear all the cache.
        this.internalRows.value = rows;
        this.cache.clear();
        this.rowCache.clear();

        // Start watching for changes if we are reactive.
        if (this.liveUpdates) {
            this.initializeReactiveTracker();

            this.internalRowsWatcher = watch(() => rows, () => {
                this.detectRowChanges();
                this.updateFilteredRows();
            }, { deep: true });
        }

        this.updateFilteredRows();
    }

    /**
     * Sets the filters to be used to filter the rows down to a limited set.
     *
     * @param quickFilter The value to use for quick filtering.
     * @param columnFilters The column filters to apply to the data.
     */
    public setFilters(quickFilter: string | undefined, columnFilters: Record<string, unknown> | undefined): void {
        this.quickFilter = quickFilter ?? "";
        this.columnFilters = columnFilters ?? {};
        this.updateFilteredRows();

        this.isFiltered.value = this.quickFilter !== "" || Object.keys(this.columnFilters).length > 0;
    }

    /**
     * Sets the column that will be used to sort the filtered rows.
     *
     * @param columnSort The column that will be used for sorting.
     */
    public setSort(columnSort: ColumnSort | undefined): void {
        this.columnSort = columnSort;
        this.updateSortedRows();

        this.isSorted.value = this.columnSort !== undefined;
    }

    // #endregion
}

// #endregion

// #region Internal Components

/**
 * This is a special cell we use when no default format cell has been defined.
 */
const defaultCell = defineComponent({
    props: standardCellProps,

    setup(props) {
        return () => props.column.field ? props.row[props.column.field] : "";
    }
});

// #endregion
