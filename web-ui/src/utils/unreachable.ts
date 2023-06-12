import { log } from './log';

export function unreachable(value: never) {
    log.error('Reached unreachable code with unexpected {value}.', value);
    throw new Error('Reached unreachable code with unexpected value: ' + value);
}