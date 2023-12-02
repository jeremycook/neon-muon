type Action<T> = ((value: T) => void);

export class ObservableSet<T> extends Set<T>
{
    constructor() {
        super();
    }

    private callbacks = {
        "add": new Set<Action<T>>(),
        "added": new Set<Action<T>>(),
        "delete": new Set<Action<T>>(),
        "deleted": new Set<Action<T>>(),
    }

    addEventListener(event: "add" | "added" | "delete" | "deleted", callback: Action<T>) {
        this.callbacks[event].add(callback);
    }

    protected emit(event: "add" | "added" | "delete" | "deleted", value: T) {
        for (const callback of this.callbacks[event]) {
            callback(value);
        }
    }

    override add(value: T) {
        this.emit("add", value);
        const result = super.add(value);
        this.emit("added", value);

        return result;
    }

    override delete(value: T) {
        this.emit("delete", value);
        const result = super.delete(value);
        this.emit("deleted", value);

        return result;
    }
}
