import { currentLogin } from '../login/loginInfo';
import { jsonGet } from '../utils/http';
import { parseJson } from '../utils/json';
import { SubT, computed, val } from '../utils/pubSub';
import { makeUrl } from '../utils/url';

function _rootStorageKey() {
    return 'root:' + currentLogin.val.sub;
};

export class FileNode {
    public name: string;
    public path: string;
    public isExpandable: boolean;
    public children?: FileNode[];

    constructor(props?: FileNode) {
        this.name = props?.name ?? '';
        this.path = props?.path ?? '';
        this.isExpandable = props?.isExpandable ?? true;
        if (this.isExpandable) {
            this.children = props?.children?.map(o => new FileNode(o)) ?? [];
        }
    }

    get(path: string): FileNode | undefined {
        if (path.includes('/')) {
            const slash = path.indexOf('/');
            const name = path.substring(0, slash);
            const rest = path.substring(slash + 1);
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
computed(currentLogin, () => refreshRoot());

export async function refreshRoot() {
    const fileNode = await _getRootFromServer();
    await _root.pub(fileNode);
    sessionStorage.setItem(_rootStorageKey(), JSON.stringify(fileNode));
};

export async function getJsonFile<T>(path: string) {
    const response = await jsonGet<T>(makeUrl('/api/file', { path }));
    if (response.result) {
        return response.result;
    }
    else {
        return undefined;
    }
}

export function getDirectoryName(path: string) {
    const slash = path.lastIndexOf('/');
    if (slash > -1) {
        return path.substring(0, slash);
    }
    else {
        return '';
    }
}

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
    const json = sessionStorage.getItem(_rootStorageKey());
    if (json) {
        return new FileNode(parseJson(json));
    }
    else {
        return new FileNode();
    }
}
