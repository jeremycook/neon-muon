import { FileNode, getFilesFromDataTransfer, refreshRoot, root, uploadFiles } from '../files/files.ts';
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
                console.log('', files);
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
            ondragover(ev: DragEvent) { ev.preventDefault(); },
            async ondrop(ev: DragEvent) {
                ev.preventDefault();
                ev.stopPropagation();

                const files = getFilesFromDataTransfer(ev.dataTransfer);
                console.log(item.path, files);
                await uploadFiles(item.path, files);
                // TODO: Announce success/failure
                await refreshRoot();
            }
        },
            item.name
        ),
        item.isExpandable ? nestedListUI(item.children!, _fileNodeRender) : createFragment(),
    ] as Node[];
}