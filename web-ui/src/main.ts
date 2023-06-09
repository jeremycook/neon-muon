import './styles/index.css';
import { routes } from './routes.ts';
import { notFoundPage } from './errors/not-found.ts';
import { errorPage } from './errors/error.ts';
import { div, main } from './utils/html.ts';
import { siteNavbar } from './site/siteNavbar.ts';
import { currentLogin } from './login/loginInfo.ts';
import { dynamic } from './utils/dynamicHtml.ts';
import { currentLocation } from './utils/routed.ts';
import { log } from './utils/log.ts';
import { siteMenu } from './site/siteMenu.ts';

const view = div({ class: 'site' },
    div({ class: 'site-desktop' },
        div({ class: 'site-navbar' },
            siteNavbar(currentLogin)
        ),
        div({ class: 'site-body' },

            siteMenu(),

            main({ class: 'site-main' }, ...dynamic(currentLocation, async () => {

                const pageFactory = routes[currentLocation.val.pathname.toLowerCase()] ?? notFoundPage;
                const params = { location, ...Object.fromEntries(new URLSearchParams(location.search)) };

                try {
                    const page = await pageFactory(params);
                    return page;
                } catch (ex) {
                    log.error('Page render exception', ex);
                    const page = errorPage();
                    return page;
                }
            }))
        )
    )
);

document.getElementById('site')!.replaceWith(view);
view.dispatchEvent(new Event('mount', { cancelable: false }));

document.addEventListener('keyup', ev => {

    const el = ev.target;
    if (el instanceof HTMLTableCellElement && document.activeElement === el) {

        // TODO: Account for shift, alt, ctrl, meta keys

        switch (ev.key) {
            case 'ArrowUp':
                (el.closest('tr')?.previousElementSibling?.children[el.cellIndex] as any)?.focus?.();
                break;

            case 'ArrowDown':
                (el.closest('tr')?.nextElementSibling?.children[el.cellIndex] as any)?.focus?.();
                break;

            case 'ArrowLeft':
                (el.previousElementSibling as any)?.focus?.();
                break;

            case 'ArrowRight':
                (el.nextElementSibling as any)?.focus?.();
                break;

            default:
                break;
        }
    }
})

if (import.meta.env.DEV) {
    // Development
    document.addEventListener('keyup', ev => {
        if (ev.target instanceof HTMLInputElement && ev.target.type === 'password') {
            return;
        }

        console.debug({ key: ev.key, altKey: ev.altKey, ctrlKey: ev.ctrlKey, metaKey: ev.metaKey, shiftKey: ev.shiftKey, code: ev.code });
    });
}
