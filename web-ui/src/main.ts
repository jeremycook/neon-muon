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
