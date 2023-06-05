import './styles/index.css';
import { routes } from './routes.ts';
import { notFound } from './errors/not-found.ts';
import { error } from './errors/error.ts';
import { div } from './utils/html.ts';
import { mainLayout } from './site/mainLayout.ts';
import { siteNavbarUI } from './site/siteNavbar.ts';
import { currentLogin } from './login/loginInfo.ts';
import { dynamic } from './utils/dynamicHtml.ts';
import { currentPath } from './utils/routed.ts';

document.getElementById('app')!.replaceChildren(
    div({ class: 'site' },
        div({ class: 'site-desktop' },
            div({ class: 'site-navbar' },
                siteNavbarUI(currentLogin)
            ),
            div({ class: 'site-router' },
                mainLayout(
                    ...dynamic(currentPath, async () => {

                        const pageFactory = routes[currentPath.val] ?? notFound;
                        const params = { location, ...Object.fromEntries(new URLSearchParams(location.search)) };

                        try {
                            const pageNode = await pageFactory(params);
                            return pageNode;
                        } catch (ex) {
                            console.error('Page render exception', ex);
                            const errorPage = error();
                            return errorPage;
                        }
                    })
                )
            )
        )
    )
);
