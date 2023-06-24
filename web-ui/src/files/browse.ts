import { databaseApp as databaseApp } from '../database/database';
import { tableApp } from '../database/table';
import { notFoundPage } from '../errors/not-found';
import { pagePage as pageApp } from '../notebooks/page';
import { icon } from '../ui/icons';
import { a, button, div, h1, li, ul } from '../utils/html';
import { makeUrl, redirect } from '../utils/url';
import { FileNode, root, refreshRoot, promptRenameFileNode } from './files';

const appMatchers: ((props: any) => (false | ((props: any) => undefined | Node | Promise<undefined | Node>)))[] = [
    ({ path }: { path: string; }) => path.endsWith('.page') && pageApp,
    ({ path }: { path: string; }) => path.endsWith('.db') && databaseApp,
    ({ path }: { path: string; }) => path.includes('.db/') && tableApp,
];

export async function browsePage(props: { path: string; }) {
    const fileNode = root.val.get(props.path);

    if (!fileNode) {
        await refreshRoot();
    }

    if (!fileNode) {
        return notFoundPage();
    }

    const appProps = { fileNode, ...props };

    // First matching app wins.
    let app;
    for (const matcher of appMatchers) {
        app = matcher(appProps);
        if (app) {
            break;
        }
    }

    if (app) {
        const result = await app(appProps);
        if (result) {
            return result;
        }
    }

    if (fileNode.isExpandable) {
        return defaultDirectoryApp(appProps);
    }
    else {
        return defaultFileApp(appProps);
    }
}

function defaultDirectoryApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
        div({ class: 'flex gap mb' },
            button({ class: 'button' }, {
                onclick() {

                }
            },
                icon('rename-regular'), ' Rename'
            ),
            button({ class: 'button' }, {
                onclick() {

                }
            },
                icon('delete-regular'), ' Delete'
            ),
        ),
        ul(...fileNode.children!.map(item =>
            li(
                a({ href: makeUrl('/browse', { path: item.path }) }, item.name)
            )
        ))
    );
}
function defaultFileApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
        div({ class: 'flex gap' },
            a({ class: 'button', href: makeUrl('/api/file', { path: fileNode.path }) },
                icon('arrow-download-regular'), ' Download'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptRenameFileNode(fileNode);
                    await refreshRoot();
                    redirect(makeUrl('/browse', { path: newFilePath }));
                }
            },
                icon('rename-regular'), ' Rename'
            ),
            button({ class: 'button' }, {
                onclick() {

                }
            },
                icon('delete-regular'), ' Delete'
            ),
        )
    );
}
