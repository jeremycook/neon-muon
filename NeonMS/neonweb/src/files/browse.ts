import { databaseApp } from '../database/database';
import { tableApp } from '../database/table';
import { notFoundPage } from '../errors/not-found';
import { fileApp, folderApp, refreshRoot, root, textApp } from './files';

const appMatchers: ((props: any) => (false | ((props: any) => undefined | Node | Promise<undefined | Node>)))[] = [
    ({ path }: { path?: string; }) => !path && folderApp,
    ({ path }: { path: string; }) => path.endsWith('.db') && databaseApp,
    ({ path }: { path: string; }) => path.includes('.db/') && tableApp,
    ({ path }: { path: string; }) => /\.(md)$/i.test(path) && textApp,
];

export async function browsePage(props: { path: string; }) {
    let fileNode = root.val.get(props.path ?? '');

    if (!fileNode) {
        // Make sure the tree is up-to-date
        await refreshRoot();
        fileNode = root.val.get(props.path ?? '');
    }

    const appProps = { fileNode, path: props.path };

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

    if (typeof fileNode === 'undefined') {
        return notFoundPage();
    }

    if (fileNode.isExpandable) {
        return folderApp({ fileNode });
    }
    else {
        return fileApp({ fileNode });
    }
}