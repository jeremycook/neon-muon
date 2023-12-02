export function makeUrl(url: string, search: Record<string, undefined | string | string[]>) {

    const searchParams: string[][] = [];
    for (const [key, val] of Object.entries(search)) {
        if (typeof val === 'string') {
            searchParams.push([key, val]);
        }
        else if (val) {
            val.forEach(v => searchParams.push([key, v]));
        }
    }

    return url + (Object.keys(searchParams).length > 0 ? '?' + new URLSearchParams(searchParams).toString() : '');
}

export function isLocalUrl(url?: null | string | URL): boolean {
    if (url instanceof URL) {
        return url.host === location.host;
    }
    else if (typeof url === 'string') {
        return isLocalUrl(new URL(url, location.href));
    }
    else {
        return false;
    }
}

export function redirect(href: string, state?: object) {
    history.pushState(state ?? {}, '', href);
}

/** Redirect to href if it is local, otherwise redirect to the fallbackHref. */
export function redirectLocal(href?: string, fallbackHref: string = '/', state?: object) {
    const redirectHref = isLocalUrl(href)
        ? href!
        : fallbackHref;
    history.pushState(state ?? {}, '', redirectHref);
}