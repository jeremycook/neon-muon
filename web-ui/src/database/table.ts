import { FileNode } from '../files/files';
import { siteCard } from '../site/siteCard';
import { div, h1 } from '../utils/html';

export function tableApp({ fileNode }: { fileNode: FileNode }) {
    return siteCard(
        h1(fileNode.name),
        div(
            'TODO: Data table will go here!'
        )
    )
}