import { FileNode, getFilesFromDataTransfer, moveFile, refreshRoot, root, uploadFiles } from '../files/files.ts';
import { isAuthenticated } from '../login/loginInfo.ts';
import { nestedListUI } from '../ui/nestedUI.ts';
import { dynamic, when } from '../utils/dynamicHtml.ts';
import { createFragment } from '../utils/etc.ts';
import { a, div } from '../utils/html.ts';
import { makeUrl } from '../utils/url.ts';

export function siteMenu() {

    const view = when(isAuthenticated,
        () => div({ class: 'site-menu' }, {
            ondragover(ev: DragEvent) { ev.preventDefault(); },
            async ondrop(ev: DragEvent) {
                ev.preventDefault();
                ev.stopPropagation();

                const files = getFilesFromDataTransfer(ev.dataTransfer);
                await uploadFiles('', files);
                // TODO: Announce success/failure
                await refreshRoot();
            }
        },
            ...dynamic(root, () => nestedListUI(root.val.children!, _fileNodeRender))
        )
    );

    return view;
}

function _fileNodeRender(item: FileNode): Node[] {
    return [
        a({ href: makeUrl('/browse', { path: item.path }) }, {
            draggable: true,
            ondragstart(ev: DragEvent) {
                ev.dataTransfer!.setData('text/x-file-node', JSON.stringify(item));
            },
            ondragover(ev: DragEvent) { ev.preventDefault(); },
            async ondrop(ev: DragEvent) {
                ev.preventDefault();
                ev.stopPropagation();

                const destinationDirectory = item.isExpandable
                    ? item.path
                    : item.path.split('/').slice(0, -1).join('/');

                const json = ev.dataTransfer!.getData('text/x-file-node');
                if (json) {
                    const fileNode = <FileNode>JSON.parse(ev.dataTransfer!.getData('text/x-file-node'));
                    const newPath = (destinationDirectory ? destinationDirectory + '/' : '') + fileNode.name;
                    const response = await moveFile(fileNode.path, newPath);
                    if (response.errorMessage) {
                        alert(response.errorMessage);
                    }
                }

                const files = getFilesFromDataTransfer(ev.dataTransfer);
                if (files.length > 0) {
                    const response = await uploadFiles(destinationDirectory, files);
                    if (!response.ok) {
                        alert('An error occurred while trying to upload files.');
                    }
                }

                await refreshRoot();
            }
        },
            item.name
        ),
        item.isExpandable ? nestedListUI(item.children!, _fileNodeRender) : createFragment(),
    ] as Node[];
}