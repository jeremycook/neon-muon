export type Partial<T> = {
    [P in keyof T]?: T[P];
};

export type Primitive = string | number | boolean | Date;
