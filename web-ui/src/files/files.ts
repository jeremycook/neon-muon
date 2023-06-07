import { databasePage } from '../database/database';
import { notFoundPage } from '../errors/not-found';
import { a, div, h1, li, p, ul } from '../utils/html';
import { jsonGet } from '../utils/http';
import { parseJson } from '../utils/json';
import { SubT, val } from '../utils/pubSub';
import { makeUrl } from '../utils/url';

const rootStorageKey = 'root';

export class FileNode {
    public name: string;
    public path: string;
    public isDirectory: boolean;
    public children?: FileNode[];

    constructor(props?: FileNode) {
        this.name = props?.name ?? '';
        this.path = props?.path ?? '';
        this.isDirectory = props?.isDirectory ?? true;
        if (this.isDirectory) {
            this.children = props?.children?.map(o => new FileNode(o)) ?? [];
        }
    }

    get(path: string): FileNode | undefined {
        if (path.includes('/')) {
            const [name, rest] = path.split('/', 2);
            const match = this.children?.find(x => x.name === name);
            return match?.get(rest);
        }
        else {
            return this.children?.find(x => x.name === path);
        }
    }
}

const _root = val(_getRootFromStorage());
refreshRoot();

export const root: SubT<FileNode> = _root;

export async function refreshRoot() {
    const fileNode = await _getRootFromServer();
    await _root.pub(fileNode);
    sessionStorage.setItem(rootStorageKey, JSON.stringify(fileNode));
};

async function _getRootFromServer() {
    const response = await jsonGet<FileNode>('/api/file-node');
    if (response.result) {
        return new FileNode(response.result);
    }
    else {
        return new FileNode();
    }
}

function _getRootFromStorage() {
    const json = sessionStorage.getItem(rootStorageKey);
    if (json) {
        return new FileNode(parseJson(json));
    }
    else {
        return new FileNode();
    }
}

function defaultDirectoryApp({ fileNode }: { fileNode: FileNode }) {
    return div(
        h1(fileNode.name),
        ul(...fileNode.children!.map(item =>
            li(a({ href: makeUrl('/browse', { path: item.path }) }, item.name))
        ))
    );
}

function defalutFileApp({ fileNode }: { fileNode: FileNode }) {
    return div(
        h1(fileNode.name),
        p(
            a({ href: makeUrl('/api/file', { path: fileNode.path }) }, 'Download ', fileNode.name)
        )
    );
}

const appMatchers: ((props: any) => (false | ((props: any) => Promise<Node>)))[] = [
    ({ path }: { path: string }) => path.endsWith('.db') && databasePage,
];

export async function browsePage(props: { path: string }) {
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
        return defalutFileApp(appProps);
    }
}
