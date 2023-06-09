import { FileNode, getJsonFile } from '../files/files';
import { dynamic } from '../utils/dynamicHtml';
import { div, h1 } from '../utils/html';
import { val } from '../utils/pubSub';

export async function pagePage({ fileNode }: { fileNode: FileNode }) {

    const page = await getJsonFile<Page>(fileNode.path);

    if (!page) {
        return;
    }

    const title = val(page.title);

    const view = div(
        h1(dynamic(title)),

        div(

        )
    );

    return view;
}

export interface Page {
    title: string;
}