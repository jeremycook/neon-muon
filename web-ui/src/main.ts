import './styles/index.css';
import { routes } from './routes.ts';
import { notFound } from './errors/not-found.ts';
import { isLocalUrl, redirect } from './utils/url.ts';
import { error } from './errors/error.ts';
import { mutateSegment, createSegment } from './utils/etc.ts';
import { div } from './utils/html.ts';
import { mainLayout } from './site/main-layout.ts';

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

const pageSegment = createSegment();
document.getElementById('app')!.replaceChildren(
    div({ class: 'site' },
        div({ class: 'site-desktop' },
            div({ class: 'site-router' },
                mainLayout(
                    ...pageSegment
                )
            )
        )
    )
);

async function renderPage() {
    const path = location.pathname.toLowerCase();
    const pageFactory = routes[path] ?? notFound;
    const params = { location, ...Object.fromEntries(new URLSearchParams(location.search)) };

    try {
        const pageNode = await pageFactory(params);
        mutateSegment(pageSegment, pageNode);
    } catch (ex) {
        console.error('Page render exception', ex);
        const errorPage = error();
        mutateSegment(pageSegment, errorPage);
        setTimeout(() => console.log('focus', document.querySelector<HTMLElement>('[autofocus]')), 100);
    }
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
        return;
    }
});
