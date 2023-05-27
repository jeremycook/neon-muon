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
