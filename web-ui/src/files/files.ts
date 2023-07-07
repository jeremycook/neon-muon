import { currentLogin } from '../login/loginInfo';
import { icon } from '../ui/icons';
import { modalConfirm, modalPrompt } from '../ui/modals';
import { div, h1, button, ul, li, a } from '../utils/html';
import { jsonGet, jsonPost } from '../utils/http';
import { parseJson } from '../utils/json';
import { SubT, computed, val } from '../utils/pubSub';
import { makeUrl, redirect } from '../utils/url';



export function folderApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
        div({ class: 'flex gap mb' },
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptMoveFileNode(fileNode);
                    if (newFilePath) {
                        console.log(newFilePath);
                        await refreshRoot();
                        redirect(makeUrl('/browse', { path: newFilePath }));
                        return;
                    }
                }
            },
                icon('rename-regular'), ' Move'
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

export function fileApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
        div({ class: 'flex gap' },
            a({ class: 'button', href: makeUrl('/api/download-file', { path: fileNode.path }) },
                icon('arrow-download-regular'), ' Download'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptMoveFileNode(fileNode);
                    if (newFilePath) {
                        console.log(newFilePath);
                        await refreshRoot();
                        redirect(makeUrl('/browse', { path: newFilePath }));
                        return;
                    }
                }
            },
                icon('rename-regular'), ' Move'
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
    const response = await jsonGet<T>(makeUrl('/api/download-file', { path }));
    if (response.result) {
        return response.result;
    }
    else {
        return undefined;
    }
}

export async function promptMoveFileNode(fileNode: FileNode) {
    const newPath = await modalPrompt('Enter a new path:', '', fileNode.path);

    if (!newPath) {
        return undefined;
    }

    const response = await moveFileNode(fileNode.path, newPath);
    if (response.ok) {
        await refreshRoot();
        return newPath;
    }
    else {
        await modalConfirm(response.errorMessage ?? 'An error occured.');
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
    const response = await jsonGet<FileNode>(makeUrl('/api/get-file-node', { path: '' }));
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

export async function moveFileNode(path: string, newPath: string) {
    return await jsonPost('/api/move-file', {
        path,
        newPath,
    });
}

export function getFilesFromDataTransfer(dataTransfer: DataTransfer | null) {
    const files: File[] = [];
    if (dataTransfer) {
        if (dataTransfer.items) {
            // Use DataTransferItemList interface to access the file(s)
            for (const item of dataTransfer.items) {
                // If dropped items aren't files, reject them
                if (item.kind === "file") {
                    const file = item.getAsFile();
                    if (file) {
                        files.push(file);
                    }
                }
            }
        } else {
            // Use DataTransfer interface to access the file(s)
            for (const file of dataTransfer.files) {
                files.push(file);
            }
        }
    }
    return files;
}

export async function uploadFiles(path: string, files: readonly File[]) {
    const formData = new FormData();
    for (const [i, file] of Array.from(files).entries()) {
        formData.append(`files_${i}`, file);
    }
    return await fetch(
        makeUrl('/api/upload-files', { path: path }),
        {
            method: 'POST',
            body: formData
        }
    );
}
