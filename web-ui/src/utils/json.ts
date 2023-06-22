const isoRegex = /^(\d{4})-(\d{2})-(\d{2})(T(\d{2}):(\d{2}):(\d{2}(?:\.\d*))(?:Z|(\+|-)([\d|:]*))?)?$/;

/** Converts ISO date strings into Date objects. Leaves other values as-is. */
export function parseValue(value: string | number | null) {
    if (typeof value === 'string') {
        if (isoRegex.test(value))
            return new Date(value);
    }
    return value;
}

/** Revive JSON with {@link parseValue}.
 * @example JSON.parse(someJson, jsonParseValueReviver)
 */
export function parseValueJsonReviver(this: any, _: string, value: any) {
    return parseValue(value);
}

/** Parse JSON with the {@link parseValueJsonReviver}. */
export function parseJson(json: string) {
    return JSON.parse(json, parseValueJsonReviver);
}
