import { log } from './log';

export class UnexpectedError extends Error {
    constructor(...values: any) {
        log.error('Reached unexpected code with {values}.', values);
        super('Reached unexpected code with values: ' + JSON.stringify(values))
    }
}

export class UnreachableError extends Error {
    constructor(value: never) {
        log.error('Reached unreachable code with unexpected {value}.', value);
        super('Reached unreachable code with unexpected value: ' + JSON.stringify(value))
    }
}

export function unreachable(value: never) {
    throw new UnreachableError(value);
}
