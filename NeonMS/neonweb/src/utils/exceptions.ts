import { log } from './log';

export class Exception extends Error {
    data: any;
    constructor(message: string, ...data: any) {
        log.error('Reached unexpected code with {values}.', data);
        super(message);
        this.data = data;
    }
}

export class Panic extends Exception {
    constructor(...data: any) {
        super('Panic with {values}.', data);
    }
}

export class Unreachable extends Panic {
    constructor(data: never) {
        super('Reached unreachable code with {values}.', data);
    }
}

export function unreachable(value: never) {
    throw new Unreachable(value);
}
