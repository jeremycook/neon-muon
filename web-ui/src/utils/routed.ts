import { isLocalUrl, redirect } from './url.ts';
import { SubT, val } from './pubSub.ts';

const _currentLocation = val(location);
export const currentLocation: SubT<Location> = _currentLocation;

((oldPushState, oldReplaceState) => { // Dispatch 'locationchange' events
    history.pushState = function pushState() {
        let ret = oldPushState.apply(this, arguments as any);
        window.dispatchEvent(new Event('pushstate'));
        window.dispatchEvent(new Event('locationchange'));
        return ret;
    };

    history.replaceState = function replaceState() {
        let ret = oldReplaceState.apply(this, arguments as any);
        window.dispatchEvent(new Event('replacestate'));
        window.dispatchEvent(new Event('locationchange'));
        return ret;
    };

    window.addEventListener('popstate', () => {
        window.dispatchEvent(new Event('locationchange'));
    });
})(history.pushState, history.replaceState);

window.addEventListener('locationchange', () => { // Publish locationchange events that effect the pathname
    _currentLocation.pub(location, { force: true });
});

document.addEventListener('click', e => { // Handle clicking local links
    const target = e.target instanceof Element
        ? e.target.closest('a')
        : null;

    if (target && isLocalUrl(target.href) && !target.pathname.startsWith('/api/')) {
        e.preventDefault();
        redirect(target.href);
        return;
    }
});
