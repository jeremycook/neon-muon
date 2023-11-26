import { parseJson } from './json';
import { log } from './log';

export class HttpError extends Error {
    constructor(
        message: string,
        public status: number,
        public details?: any
    ) {
        super(message);
    }
}

export const ErrorUnknownStatusCode = -1;

let AuthorizationHeader: object | null = null;

export function setBearerToken(token: string) {
    AuthorizationHeader = token
        ? {
            'Authorization': 'Bearer ' + token
        }
        : null;
}

/** Parse {@param response} content using {@link parseJson}. */
export const parseJsonResponse = async (response: Response) => {
    const json = await response.text();
    if (!json) {
        return undefined;
    }

    const result = parseJson(json);
    return result;
}

export async function jsonGet<TResult>(url: string, input?: object) {
    return await jsonFetch<TResult>({ url, method: 'GET' }, input);
}

export async function jsonPost<TResult>(url: string, input?: object) {
    return await jsonFetch<TResult>({ url, method: 'POST' }, input);
}

export async function jsonPut<TResult>(url: string, input?: object) {
    return await jsonFetch<TResult>({ url, method: 'PUT' }, input);
}

class JsonFetchResponse<TResult> {
    public ok: boolean;
    public status: number;
    public result?: TResult;
    public errorMessage?: string;
    public errorResult?: any;
    constructor(input: {
        ok: boolean;
        status: number;
        result?: TResult;
        errorMessage?: string;
        errorResult?: any;
    }) {
        this.ok = input.ok;
        this.status = input.status;
        this.result = input.result;
        this.errorMessage = input.errorMessage;
        this.errorResult = input.errorResult;
    }
    public getResultOrThrow(): TResult {
        if (this.ok) {
            return this.result as TResult;
        }
        else {
            throw new HttpError(this.errorMessage ?? 'Unknown error.', this.status, this.errorResult);
        }
    }
};

export async function jsonFetch<TResult>(init: { url: string } & RequestInit, input?: object): Promise<JsonFetchResponse<TResult>> {
    try {

        const body = input ? { body: JSON.stringify(input) } : {};
        const requestInit = {
            headers: {
                ...AuthorizationHeader,
                'Content-Type': 'application/json'
            },
            ...body,
            ...init,
        };

        const response = await fetch(init.url, requestInit);

        let result;
        let errorResult;
        let errorMessage;
        if (response.ok) {
            result = await parseJsonResponse(response);
        }
        else if (response.status === 401) {
            errorMessage = 'You must be logged in to access the requested resource.';
        }
        else if (response.status === 403) {
            errorMessage = 'You do not have permission to access the requested resource.';
        }
        else if (response.status < 500) {
            const isJsonResponse = response.headers.get('Content-Type')?.startsWith('application/json') === true;
            if (isJsonResponse) {
                const data = await parseJsonResponse(response);
                if (typeof data === 'string') {
                    errorMessage = data;
                }
                else {
                    errorMessage = 'Invalid request.';
                    errorResult = data;
                }
            }
            else {
                const text = await response.text();
                if (text) {
                    errorMessage = text;
                }
                else {
                    errorMessage = 'Invalid request.';
                }
            }
        }
        else {
            errorMessage = 'An unexpected error occurred. ';
            const text = await response.text();
            if (text) {
                errorMessage += ' Details: ' + text;
            }
            log.error('Non-OK HTTP response. {ErrorMessage}.', response.bodyUsed ? response.body : 'No response body.');
        }
        return new JsonFetchResponse<TResult>({
            ok: response.ok,
            status: response.status,
            result,
            errorMessage,
            errorResult,
        });
    }
    catch (err) {
        // Probably a transient connection issue,
        // log and provide a helpful error message.
        const errorMessage = 'Unable to connect to the server. Please check your Internet connection or try again later.';
        return new JsonFetchResponse<TResult>({
            ok: false,
            status: ErrorUnknownStatusCode,
            errorMessage,
        });
    }
}
