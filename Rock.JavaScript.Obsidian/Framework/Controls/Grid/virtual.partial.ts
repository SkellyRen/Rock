/**
 * virtual list core calculating center
 */

const enum DirectionType {
    None = "",
    Front = "FRONT", // scroll up or left
    Behind = "BEHIND" // scroll down or right
}

const enum CalcType {
    Init = "INIT",
    Fixed = "FIXED",
    Dynamic = "DYNAMIC"
}

const leadingBuffer = 0;

export type Range = {
    start: number;
    end: number;
    padFront: number;
    padBehind: number;
};

export type VirtualParameters = {
    pageOffsetTop: number;

    keeps: number;

    estimateSize: number;

    buffer: number;

    uniqueIds: string[];
};

export default class Virtual {
    private readonly parameters: VirtualParameters;
    private readonly callUpdate: (range: Range) => void;

    public readonly sizes: Map<string, number> = new Map<string, number>();
    private firstRangeTotalSize?: number = 0;
    private firstRangeAverageSize: number = 0;
    private lastCalcIndex: number = 0;
    private fixedSizeValue?: number = 0;
    private calcType: CalcType = CalcType.Init;

    public offset: number = 0;
    private direction: DirectionType = DirectionType.None;

    private range: Range = { start: 0, end: 0, padFront: 0, padBehind: 0 };

    constructor(param: VirtualParameters, callUpdate: (range: Range) => void) {
        this.parameters = param;
        this.callUpdate = callUpdate;

        // range data
        if (param) {
            this.checkRange(0, param.keeps - 1);
        }
    }

    destroy(): void {
        //this.init(null, null);
    }

    // return current render range
    getRange(): Range {
        return {
            ...this.range
        };
    }

    isBehind(): boolean {
        return this.direction === DirectionType.Behind;
    }

    isFront(): boolean {
        return this.direction === DirectionType.Front;
    }

    // return start index offset
    getOffset(start: number): number {
        return (start < 1 ? 0 : this.getIndexOffset(start)) + this.parameters.pageOffsetTop;
    }

    updateParam(key: string, value: any): void {
        if (key in this.parameters) {
            // if uniqueIds change, find out deleted id and remove from size map
            if (key === "uniqueIds") {
                this.sizes.forEach((v, key) => {
                    if (!value.includes(key)) {
                        this.sizes.delete(key);
                    }
                });
            }
            this.parameters[key] = value;
        }
    }

    // save each size map by id
    saveSize(id: string, size: number): void {
        this.sizes.set(id, size);

        // we assume size type is fixed at the beginning and remember first size value
        // if there is no size value different from this at next comming saving
        // we think it's a fixed size list, otherwise is dynamic size list
        if (this.calcType === CalcType.Init) {
            this.fixedSizeValue = size;
            this.calcType = CalcType.Fixed;
        }
        else if (this.calcType === CalcType.Fixed && this.fixedSizeValue !== size) {
            this.calcType = CalcType.Dynamic;
            // it's no use at all
            delete this.fixedSizeValue;
        }

        // calculate the average size only in the first range
        if (this.calcType !== CalcType.Fixed && typeof this.firstRangeTotalSize !== "undefined") {
            if (this.sizes.size < Math.min(this.parameters.keeps, this.parameters.uniqueIds.length)) {
                this.firstRangeTotalSize = [...this.sizes.values()].reduce((acc, val) => acc + val, 0);
                this.firstRangeAverageSize = Math.round(this.firstRangeTotalSize / this.sizes.size);
            }
            else {
                // it's done using
                delete this.firstRangeTotalSize;
            }
        }
    }

    // in some special situation (e.g. length change) we need to update in a row
    // try goiong to render next range by a leading buffer according to current direction
    handleDataSourcesChange(): void {
        let start = this.range.start;

        if (this.isFront()) {
            start = start - leadingBuffer;
        }
        else if (this.isBehind()) {
            start = start + leadingBuffer;
        }

        start = Math.max(start, 0);

        this.updateRange(this.range.start, this.getEndByStart(start));
    }

    // when slot size change, we also need force update
    handleSlotSizeChange(): void {
        this.handleDataSourcesChange();
    }

    // calculating range on scroll
    handleScroll(offset: number): void {
        this.direction = offset < this.offset ? DirectionType.Front : DirectionType.Behind;
        this.offset = offset;

        if (!this.parameters) {
            return;
        }

        if (this.direction === DirectionType.Front) {
            this.handleFront();
        }
        else if (this.direction === DirectionType.Behind) {
            this.handleBehind();
        }
    }

    // ----------- public method end -----------

    handleFront(): void {
        const overs = this.getScrollOvers();
        // should not change range if start doesn't exceed overs
        if (overs > this.range.start) {
            return;
        }

        // move up start by a buffer length, and make sure its safety
        const start = Math.max(overs - this.parameters.buffer, 0);
        this.checkRange(start, this.getEndByStart(start));
    }

    handleBehind(): void {
        const overs = this.getScrollOvers();
        // range should not change if scroll overs within buffer
        if (overs < this.range.start + this.parameters.buffer) {
            return;
        }

        this.checkRange(overs, this.getEndByStart(overs));
    }

    // return the pass overs according to current scroll offset
    getScrollOvers(): number {
        // if slot header exist, we need subtract its size
        const offset = this.offset - this.parameters.pageOffsetTop;
        if (offset <= 0) {
            return 0;
        }

        // if is fixed type, that can be easily
        if (this.isFixedType()) {
            return Math.floor(offset / this.fixedSizeValue!);
        }

        let low = 0;
        let middle = 0;
        let middleOffset = 0;
        let high = this.parameters.uniqueIds.length;

        while (low <= high) {
            // this.__bsearchCalls++
            middle = low + Math.floor((high - low) / 2);
            middleOffset = this.getIndexOffset(middle);

            if (middleOffset === offset) {
                return middle;
            }
            else if (middleOffset < offset) {
                low = middle + 1;
            }
            else if (middleOffset > offset) {
                high = middle - 1;
            }
        }

        return low > 0 ? --low : 0;
    }

    // return a scroll offset from given index, can efficiency be improved more here?
    // although the call frequency is very high, its only a superposition of numbers
    getIndexOffset(givenIndex: number): number {
        if (!givenIndex) {
            return 0;
        }

        let offset = 0;
        for (let index = 0; index < givenIndex; index++) {
            const indexSize = this.sizes.get(this.parameters.uniqueIds[index]);
            offset = offset + (typeof indexSize === "number" ? indexSize : this.getEstimateSize());
        }

        // remember last calculate index
        this.lastCalcIndex = Math.max(this.lastCalcIndex, givenIndex - 1);
        this.lastCalcIndex = Math.min(this.lastCalcIndex, this.getLastIndex());

        return offset;
    }

    // is fixed size type
    isFixedType(): boolean {
        return this.calcType === CalcType.Fixed;
    }

    // return the real last index
    getLastIndex(): number {
        return this.parameters.uniqueIds.length - 1;
    }

    // in some conditions range is broke, we need correct it
    // and then decide whether need update to next range
    checkRange(start: number, end: number): void {
        const keeps = this.parameters.keeps;
        const total = this.parameters.uniqueIds.length;

        // datas less than keeps, render all
        if (total <= keeps) {
            start = 0;
            end = this.getLastIndex();
        }
        else if (end - start < keeps - 1) {
            // if range length is less than keeps, corrent it base on end
            start = end - keeps + 1;
        }

        if (this.range.start !== start || this.range.end !== end) {
            this.updateRange(start, end);
        }
    }

    // setting to a new range and rerender
    updateRange(start: number, end: number): void {
        this.range.start = start;
        this.range.end = end;
        this.range.padFront = this.getPadFront();
        this.range.padBehind = this.getPadBehind();
        this.callUpdate(this.getRange());
    }

    // return end base on start
    getEndByStart(start: number): number {
        const theoryEnd = start + this.parameters.keeps - 1;
        const truelyEnd = Math.min(theoryEnd, this.getLastIndex());
        return truelyEnd;
    }

    // return total front offset
    getPadFront(): number {
        if (this.isFixedType()) {
            return this.fixedSizeValue! * this.range.start;
        }
        else {
            return this.getIndexOffset(this.range.start);
        }
    }

    // return total behind offset
    getPadBehind(): number {
        const end = this.range.end;
        const lastIndex = this.getLastIndex();

        if (this.isFixedType()) {
            return (lastIndex - end) * this.fixedSizeValue!;
        }

        // if it's all calculated, return the exactly offset
        if (this.lastCalcIndex === lastIndex) {
            return this.getIndexOffset(lastIndex) - this.getIndexOffset(end);
        }
        else {
            // if not, use a estimated value
            return (lastIndex - end) * this.getEstimateSize();
        }
    }

    // get the item estimate size
    getEstimateSize(): number {
        return this.isFixedType() ? this.fixedSizeValue! : (this.firstRangeAverageSize || this.parameters.estimateSize);
    }
}
