export class Exception {
    data: any[];
    constructor(public message: string, ...data: any[]) {
        this.data = data;
    }
}
