
export class Val<TValue> {
    private _subscritions: (() => (void | Promise<void>))[] = [];

    constructor(private _value: TValue) { }

    public get value(): TValue {
        return this._value;
    }

    public sub(subscription: () => (void | Promise<void>)) {
        subscription();
        this._subscritions.push(subscription);
        return () => { this._subscritions.splice(this._subscritions.indexOf(subscription, 1)) };
    }

    public async pub(v?: TValue) {
        if (typeof v === 'undefined') {
            await this._dispatch();
        }
        else if (v !== this._value) {
            this._value = v;
            await this._dispatch();
        }
    }

    private async _dispatch() {
        for (const sub of this._subscritions) {
            await sub();
        }
    }
}
