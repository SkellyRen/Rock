export enum SelectionStepType {
    Begin = "Begin",
    Location = "Location",
    WeekOf = "Week Of",
    ServiceTime = "Service Time",
    End = "End",
}

export type StepperController = {
    start(): Promise<void>;
};