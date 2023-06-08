import { databasePage } from '../database/database';
import { notFoundPage } from '../errors/not-found';
import { a, div, h1, li, p, ul } from '../utils/html';
import { makeUrl } from '../utils/url';
import { FileNode, root, refreshRoot } from './files';

const appMatchers: ((props: any) => (false | ((props: any) => Promise<Node>)))[] = [
    ({ path }: { path: string; }) => path.endsWith('.db') && databasePage,
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
        return app(appProps);
    }
    else if (fileNode.isDirectory) {
        return defaultDirectoryApp(appProps);
    }
    else {
        return defaultFileApp(appProps);
    }
}

function defaultDirectoryApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
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
        p(
            a({ href: makeUrl('/api/file', { path: fileNode.path }) }, 'Download ', fileNode.name)
        )
    );
}
