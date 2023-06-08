import { a, div } from '../utils/html.ts';
import { dynamic, when } from '../utils/dynamicHtml.ts';
import { FileNode, root } from '../files/files.ts';
import { nestedListUI } from '../ui/nestedUI.ts';
import { makeUrl } from '../utils/url.ts';
import { isAuthenticated } from '../login/loginInfo.ts';

export function siteMenu() {

    const view = when(isAuthenticated,
        () => div({ class: 'site-menu' },
            ...dynamic(root, () => nestedListUI(root.val.children!, _fileNodeRender))
        )
    );
    
    return view;
}

function _fileNodeRender(item: FileNode): Node | Node[] {
    if (item.isExpandable) {
        return [
            a({ href: makeUrl('/browse', { path: item.path }) }, item.name),
            nestedListUI(item.children!, _fileNodeRender),
        ] as Node[];
    }
    else {
        return a({ href: makeUrl('/browse', { path: item.path }) }, item.name);
    }
}
