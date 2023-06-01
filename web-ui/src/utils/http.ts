import { Exception } from './exceptions';
import { parseJson } from './json';
import { log } from './log';

export class HttpException extends Exception {
    constructor(
        public message: string,
        public status?: number,
        public statusText?: string,
        public details?: any
    ) {
        super(message, status, statusText, details);
    }
}

/** Parse {@param response} content using {@link parseJson}. */
export const parseJsonResponse = async (response: Response) => {
    const json = await response.text();
    const result = parseJson(json);
    return result;
}

export async function jsonGet<TResult>(url: string, input?: object) {
    return await jsonFetch<TResult>({ url, method: 'GET' }, input);
}

export async function jsonPost<TResult>(url: string, input?: object) {
    return await jsonFetch<TResult>({ url, method: 'POST' }, input);
}

export async function jsonFetch<TResult>(init: { url: string } & RequestInit, input?: object): Promise<{ ok: boolean, status: number, result?: TResult, errorMessage?: string, errorResult?: any }> {
    try {

        const body = input ? { body: JSON.stringify(input) } : {};
        const requestInit = {
            headers: { 'Content-Type': 'application/json' },
            ...body,
            ...init,
        };

        const response = await fetch(init.url, requestInit);

        const isJsonResponse = response.headers.get('Content-Type')?.startsWith('application/json') === true;

        let result;
        let errorResult;
        let errorMessage;
        if (response.ok) {
            if (isJsonResponse) {
                result = await parseJsonResponse(response);
            }
        }
        else if (response.status === 401) {
            errorMessage = 'You must be logged in to access the requested resource.';
        }
        else if (response.status === 403) {
            errorMessage = 'You do not have permission to access the requested resource.';
        }
        else if (response.status < 500) {
            errorResult = await parseJsonResponse(response);
            errorMessage = 'Bad request: ' + JSON.stringify(errorResult);
        }
        else {
            errorMessage = 'An unexpected error occurred while trying retrive the requested resource.';
            log.warn('Non-OK response in {MemberName}: {ErrorMessage}.', 'jsonFetch', errorMessage);
        }
        return {
            ok: response.ok,
            status: response.status,
            result,
            errorMessage,
            errorResult,
        };
    }
    catch (err) {
        if (err instanceof Exception) {
            // Let it by
            throw err;
        }
        else {
            // Probably a transient connection issue,
            // log and provide a helpful error message.
            const errorMessage = 'Unable to connect to the server. Please check your Internet connection or try again later.';
            return {
                ok: false,
                status: -1,
                errorMessage,
            };
        }
    }
}
