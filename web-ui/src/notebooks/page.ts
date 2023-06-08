import { notFoundPage } from '../errors/not-found';
import { dynamic } from '../utils/dynamicHtml';
import { div, h1 } from '../utils/html';
import { jsonGet } from '../utils/http';
import { val } from '../utils/pubSub';
import { makeUrl } from '../utils/url';

export async function pagePage({ path }: { path: string }) {

    const response = await jsonGet<Page>(makeUrl('/api/file', { path }));
    const page = response.result;
    if (!page) {
        return notFoundPage();
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