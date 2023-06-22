import { log } from './log';

export class UnreachableError extends Error {
    constructor(value: never) {
        log.error('Reached unreachable code with unexpected {value}.', value);
        super('Reached unreachable code with unexpected value: ' + JSON.stringify(value))
    }
}

export function unreachable(value: never) {
    log.error('Reached unreachable code with unexpected {value}.', value);
    throw new Error('Reached unreachable code with unexpected value: ' + value);
}
