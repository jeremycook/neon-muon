import { jsonGet } from '../utils/http';
import { parseJson } from '../utils/json';
import { SubT, val } from '../utils/pubSub';

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
