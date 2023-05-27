import './style.css';
import { routes } from './routes.ts';
import { notFound } from './errors/not-found.ts';
import { isLocalUrl, redirect } from './utils/url.ts';

((oldPushState, oldReplaceState) => {
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

const app = document.getElementById('app')!;
async function renderPage() {
    const path = location.pathname.toLowerCase();
    const pageFactory = routes[path] ?? notFound;
    const params = { location, ...Object.fromEntries(new URLSearchParams(location.search)) };
    const pageNode = await pageFactory(params);
    app.replaceChildren(pageNode);
};
window.addEventListener('locationchange', renderPage);
renderPage();

document.addEventListener('click', e => {
    const target = e.target instanceof Element
        ? e.target.closest('a')
        : null;

    if (target && isLocalUrl(target.href)) {
        e.preventDefault();
        redirect(target.href);
    }
});
