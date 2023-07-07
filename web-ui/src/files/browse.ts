import { databaseApp } from '../database/database';
import { tableApp } from '../database/table';
import { notFoundPage } from '../errors/not-found';
import { pagePage as pageApp } from '../notebooks/page';
import { fileApp, folderApp, refreshRoot, root } from './files';

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
        return folderApp(appProps);
    }
    else {
        return fileApp(appProps);
    }
}