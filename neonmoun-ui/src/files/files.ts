import { currentLogin } from '../login/loginInfo';
import { icon } from '../ui/icons';
import { modalConfirm, modalPrompt } from '../ui/modals';
import { when } from '../utils/dynamicHtml';
import { EventT } from '../utils/etc';
import { a, button, div, h1, input, li, textarea, ul } from '../utils/html';
import { jsonGet, jsonPost } from '../utils/http';
import { parseJson } from '../utils/json';
import { Val, computed, val } from '../utils/pubSub';
import { makeUrl, redirect } from '../utils/url';

export function folderApp({ fileNode }: { fileNode: FileNode; }) {
    return div(
        h1(fileNode.name),
        div({ class: 'flex mb' },

            div({ class: 'dropdown' },
                button({ id: 'files-app-new-dropdown', class: 'button' },
                    icon('sparkle-regular'), ' New',
                ),
                div({ class: 'dropdown-anchor', 'aria-labelledby': 'files-app-new-dropdown' },
                    div({ class: 'dropdown-content' },
                        button({
                            async onclick() {
                                const newFilePath = await promptCreateFile(fileNode);
                                if (newFilePath) {
                                    redirect(makeUrl('/browse', { path: newFilePath }));
                                    return;
                                }
                            }
                        },
                            icon('text-add-regular'), ' New File'
                        ),
                        button({
                            async onclick() {
                                const newFolderPath = await promptCreateFolder(fileNode);
                                if (newFolderPath) {
                                    redirect(makeUrl('/browse', { path: newFolderPath }));
                                    return;
                                }
                            }
                        },
                            icon('folder-add-regular'), ' New Folder'
                        ),
                    ),
                )
            ),


            button({ class: 'button' }, {
                async onclick() {
                    const success = await promptUpload(fileNode);
                    if (success) {
                        return;
                    }
                }
            },
                icon('arrow-upload-regular'), ' Upload'
            ),
            ...(fileNode.path === ''
                ? []
                : [
                    button({ class: 'button' }, {
                        async onclick() {
                            const newFilePath = await promptMoveFile(fileNode);
                            if (newFilePath) {
                                redirect(makeUrl('/browse', { path: newFilePath }));
                                return;
                            }
                        }
                    },
                        icon('rename-regular'), ' Move'
                    ),
                    button({ class: 'button' }, {
                        async onclick() {
                            const success = await promptDeleteFile(fileNode);
                            if (success) {
                                redirect(makeUrl('/browse', { path: getParentPath(fileNode.path) }));
                                return;
                            }
                        }
                    },
                        icon('delete-regular'), ' Delete'
                    )]),
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
        div({ class: 'flex' },
            a({ class: 'button', href: makeUrl('/api/download-file', { path: fileNode.path }) },
                icon('arrow-download-regular'), ' Download'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptMoveFile(fileNode);
                    if (newFilePath) {
                        redirect(makeUrl('/browse', { path: newFilePath }));
                        return;
                    }
                }
            },
                icon('rename-regular'), ' Move'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const success = await promptDeleteFile(fileNode);
                    if (success) {
                        redirect(makeUrl('/browse', { path: getParentPath(fileNode.path) }));
                        return;
                    }
                }
            },
                icon('delete-regular'), ' Delete'
            ),
        )
    );
}

export async function textApp({ fileNode }: { fileNode: FileNode; }) {

    let text = await downloadTextFile(fileNode.path) ?? '';
    const changed = val(false);

    return div({ class: 'flex flex-down h-fill' },
        h1(fileNode.name),

        div({ class: 'flex mb' },
            a({ class: 'button', href: makeUrl('/api/download-file', { path: fileNode.path }) },
                icon('arrow-download-regular'), ' Download'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptMoveFile(fileNode);
                    if (newFilePath) {
                        redirect(makeUrl('/browse', { path: newFilePath }));
                        return;
                    }
                }
            },
                icon('rename-regular'), ' Move'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const success = await promptDeleteFile(fileNode);
                    if (success) {
                        redirect(makeUrl('/browse', { path: getParentPath(fileNode.path) }));
                        return;
                    }
                }
            },
                icon('delete-regular'), ' Delete'
            ),
            when(changed, () =>
                button({ class: 'button' }, {
                    async onclick() {
                        const file = new File([text], fileNode.path, { type: "text/plain;charset=utf-8" });
                        const response = await uploadContent(file);
                        if (response.ok) {
                            changed.val = false;
                        }
                        else {
                            alert(await response.text() || 'Unknown error.');
                        }
                    }
                },
                    icon('document-save-regular'), ' Save'
                )
            )
        ),

        textarea({ class: 'flex-grow resize-0' }, {
            oninput(ev: EventT<HTMLTextAreaElement>) {
                text = ev.currentTarget.value;
                changed.val = true;
            }
        },
            text
        ),
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
        if (path === '') {
            return this;
        }
        else if (path.includes('/')) {
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

// TODO? let _lastGetRootFromServer = 0;
const _root = val(_getRootFromStorage());
export const root: Readonly<Val<FileNode>> = _root;

refreshRoot();

/** Refresh root when the current login changes. */
computed(currentLogin, () => refreshRoot());

/** Last refresh in UNIX epoch ms. */
export async function refreshRoot() {
    const fileNode = await _getRootFromServer();
    _root.val = fileNode;
    sessionStorage.setItem(_rootStorageKey(), JSON.stringify(fileNode));
};

export async function downloadTextFile(path: string) {
    const response = await fetch(makeUrl('/api/download-file', { path }));

    if (response.ok) {
        return await response.text();
    }
    else {
        return undefined;
    }
}

export async function downloadJsonFile<T>(path: string) {
    const response = await jsonGet<T>(makeUrl('/api/download-file', { path }));
    if (response.result) {
        return response.result;
    }
    else {
        return undefined;
    }
}

export async function promptCreateFile(parentNode: FileNode) {
    const fileName = await modalPrompt('Name of new file:');

    if (!fileName) {
        return undefined;
    }

    const path = (parentNode.path ? (parentNode.path + '/') : '') + fileName;

    const response = await createFile(path);
    if (response.ok) {
        await refreshRoot();
        return path;
    }
    else {
        await modalConfirm(response.errorMessage ?? 'An error occured.');
    }
}

export async function promptCreateFolder(parentNode: FileNode) {
    const folderName = await modalPrompt('Name of new folder:');

    if (!folderName) {
        return undefined;
    }

    const path = (parentNode.path ? (parentNode.path + '/') : '') + folderName;

    const response = await createFolder(path);
    if (response.ok) {
        await refreshRoot();
        return path;
    }
    else {
        await modalConfirm(response.errorMessage ?? 'An error occured.');
    }
}

export async function promptDeleteFile(node: FileNode): Promise<boolean> {
    const confirm = await modalConfirm(`Delete ${node.path} and all of its children? This operation cannot be undone.`);

    if (!confirm) {
        return false;
    }

    const response = await deleteFile(node.path);
    if (response.ok) {
        await refreshRoot();
        return true;
    }
    else {
        await modalConfirm(response.errorMessage ?? 'An error occured.');
        return false;
    }
}

export async function promptMoveFile(fileNode: FileNode) {
    const newPath = await modalPrompt('Enter a new path:', '', fileNode.path);

    if (!newPath) {
        return undefined;
    }

    const response = await moveFile(fileNode.path, newPath);
    if (response.ok) {
        await refreshRoot();
        return newPath;
    }
    else {
        await modalConfirm(response.errorMessage ?? 'An error occured.');
    }
}

export async function promptUpload(fileNode: FileNode): Promise<boolean> {

    let ref: HTMLInputElement = null!;
    const confirm = await modalConfirm(
        'Select the files you want to upload:',
        input({ type: 'file', multiple: true, required: true }, {
            onmount(ev: EventT<HTMLInputElement>) {
                ref = ev.currentTarget;
            }
        })
    );

    if (!confirm || !ref.files) {
        return false;
    }

    const response = await uploadFiles(fileNode.path, [...ref.files]);
    if (response.ok) {
        await refreshRoot();
        return true;
    }
    else {
        await modalConfirm('An error occured.');
        return false;
    }
}

/** Returns the parent's path. */
export function getParentPath(path: string) {
    const slash = path.lastIndexOf('/');
    if (slash > -1) {
        return path.substring(0, slash);
    }
    else {
        return '';
    }
}

/** Returns the filename of this path or empty string if it is root. */
export function getFilename(path: string) {
    const slash = path.lastIndexOf('/');
    if (slash > -1) {
        return path.substring(slash + 1);
    }
    else {
        return '';
    }
}

async function _getRootFromServer() {
    // TODO? const now = Date.now();
    // const elapsed = now - _lastGetRootFromServer;
    // if (elapsed < 100) {
    //     console.log('Getting root skipped', elapsed)
    //     return _root.val;
    // }
    // console.log('Getting root', elapsed, now, _lastGetRootFromServer)

    const response = await jsonGet<FileNode>(makeUrl('/api/get-file-node', { path: '' }));
    if (response.result) {
        // _lastGetRootFromServer = now;
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

export async function createFile(path: string) {
    return await jsonPost('/api/create-file', {
        path,
    });
}

export async function createFolder(path: string) {
    return await jsonPost('/api/create-folder', {
        path,
    });
}

export async function deleteFile(path: string) {
    return await jsonPost('/api/delete-file', {
        path,
    });
}

export async function moveFile(path: string, newPath: string) {
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

export async function uploadContent(...files: readonly File[]) {
    const formData = new FormData();
    for (const [i, file] of Array.from(files).entries()) {
        formData.append(`files_${i}`, file);
    }
    return await fetch('/api/upload-content', {
        method: 'POST',
        body: formData
    });
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
