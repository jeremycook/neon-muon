import { TagParam, createHtmlElement } from './etc';

export interface ValueEvent extends InputEvent {
    target: EventTarget & { value: string }
}

export function a(...data: TagParam<HTMLAnchorElement>[]) {
    return createHtmlElement('a', ...data);
}

export function abbr(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('abbr', ...data);
}

export function address(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('address', ...data);
}

export function area(...data: TagParam<HTMLAreaElement>[]) {
    return createHtmlElement('area', ...data);
}

export function article(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('article', ...data);
}

export function aside(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('aside', ...data);
}

export function audio(...data: TagParam<HTMLAudioElement>[]) {
    return createHtmlElement('audio', ...data);
}

export function b(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('b', ...data);
}

export function base(...data: TagParam<HTMLBaseElement>[]) {
    return createHtmlElement('base', ...data);
}

export function bdi(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('bdi', ...data);
}

export function bdo(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('bdo', ...data);
}

export function blockquote(...data: TagParam<HTMLQuoteElement>[]) {
    return createHtmlElement('blockquote', ...data);
}

export function body(...data: TagParam<HTMLBodyElement>[]) {
    return createHtmlElement('body', ...data);
}

export function br(...data: TagParam<HTMLBRElement>[]) {
    return createHtmlElement('br', ...data);
}

export function button(...data: TagParam<HTMLButtonElement>[]) {
    return createHtmlElement('button', { type: 'button' }, ...data);
}

export function canvas(...data: TagParam<HTMLCanvasElement>[]) {
    return createHtmlElement('canvas', ...data);
}

export function caption(...data: TagParam<HTMLTableCaptionElement>[]) {
    return createHtmlElement('caption', ...data);
}

export function cite(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('cite', ...data);
}

export function code(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('code', ...data);
}

export function col(...data: TagParam<HTMLTableColElement>[]) {
    return createHtmlElement('col', ...data);
}

export function colgroup(...data: TagParam<HTMLTableColElement>[]) {
    return createHtmlElement('colgroup', ...data);
}

export function data(...data: TagParam<HTMLDataElement>[]) {
    return createHtmlElement('data', ...data);
}

export function datalist(...data: TagParam<HTMLDataListElement>[]) {
    return createHtmlElement('datalist', ...data);
}

export function dd(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('dd', ...data);
}

export function del(...data: TagParam<HTMLModElement>[]) {
    return createHtmlElement('del', ...data);
}

export function details(...data: TagParam<HTMLDetailsElement>[]) {
    return createHtmlElement('details', ...data);
}

export function dfn(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('dfn', ...data);
}

export function dialog(...data: TagParam<HTMLDialogElement>[]) {
    return createHtmlElement('dialog', ...data);
}

export function div(...data: TagParam<HTMLDivElement>[]) {
    return createHtmlElement('div', ...data);
}

export function dl(...data: TagParam<HTMLDListElement>[]) {
    return createHtmlElement('dl', ...data);
}

export function dt(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('dt', ...data);
}

export function em(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('em', ...data);
}

export function embed(...data: TagParam<HTMLEmbedElement>[]) {
    return createHtmlElement('embed', ...data);
}

export function fieldset(...data: TagParam<HTMLFieldSetElement>[]) {
    return createHtmlElement('fieldset', ...data);
}

export function figcaption(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('figcaption', ...data);
}

export function figure(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('figure', ...data);
}

export function footer(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('footer', ...data);
}

export function form(...data: TagParam<HTMLFormElement>[]) {
    return createHtmlElement('form', ...data);
}

export function h1(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h1', ...data);
}

export function h2(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h2', ...data);
}

export function h3(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h3', ...data);
}

export function h4(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h4', ...data);
}

export function h5(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h5', ...data);
}

export function h6(...data: TagParam<HTMLHeadingElement>[]) {
    return createHtmlElement('h6', ...data);
}

export function head(...data: TagParam<HTMLHeadElement>[]) {
    return createHtmlElement('head', ...data);
}

export function header(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('header', ...data);
}

export function hgroup(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('hgroup', ...data);
}

export function hr(...data: TagParam<HTMLHRElement>[]) {
    return createHtmlElement('hr', ...data);
}

export function html(...data: TagParam<HTMLHtmlElement>[]) {
    return createHtmlElement('html', ...data);
}

export function i(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('i', ...data);
}

export function iframe(...data: TagParam<HTMLIFrameElement>[]) {
    return createHtmlElement('iframe', ...data);
}

export function img(...data: TagParam<HTMLImageElement>[]) {
    return createHtmlElement('img', ...data);
}

export function input(...data: TagParam<HTMLInputElement>[]) {
    return createHtmlElement('input', ...data);
}

export function ins(...data: TagParam<HTMLModElement>[]) {
    return createHtmlElement('ins', ...data);
}

export function kbd(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('kbd', ...data);
}

export function label(...data: TagParam<HTMLLabelElement>[]) {
    return createHtmlElement('label', ...data);
}

export function legend(...data: TagParam<HTMLLegendElement>[]) {
    return createHtmlElement('legend', ...data);
}

export function li(...data: TagParam<HTMLLIElement>[]) {
    return createHtmlElement('li', ...data);
}

export function link(...data: TagParam<HTMLLinkElement>[]) {
    return createHtmlElement('link', ...data);
}

export function main(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('main', ...data);
}

export function map(...data: TagParam<HTMLMapElement>[]) {
    return createHtmlElement('map', ...data);
}

export function mark(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('mark', ...data);
}

export function menu(...data: TagParam<HTMLMenuElement>[]) {
    return createHtmlElement('menu', ...data);
}

export function meta(...data: TagParam<HTMLMetaElement>[]) {
    return createHtmlElement('meta', ...data);
}

export function meter(...data: TagParam<HTMLMeterElement>[]) {
    return createHtmlElement('meter', ...data);
}

export function nav(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('nav', ...data);
}

export function noscript(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('noscript', ...data);
}

export function object(...data: TagParam<HTMLObjectElement>[]) {
    return createHtmlElement('object', ...data);
}

export function ol(...data: TagParam<HTMLOListElement>[]) {
    return createHtmlElement('ol', ...data);
}

export function optgroup(...data: TagParam<HTMLOptGroupElement>[]) {
    return createHtmlElement('optgroup', ...data);
}

export function option(...data: TagParam<HTMLOptionElement>[]) {
    return createHtmlElement('option', ...data);
}

export function output(...data: TagParam<HTMLOutputElement>[]) {
    return createHtmlElement('output', ...data);
}

export function p(...data: TagParam<HTMLParagraphElement>[]) {
    return createHtmlElement('p', ...data);
}

export function picture(...data: TagParam<HTMLPictureElement>[]) {
    return createHtmlElement('picture', ...data);
}

export function pre(...data: TagParam<HTMLPreElement>[]) {
    return createHtmlElement('pre', ...data);
}

export function progress(...data: TagParam<HTMLProgressElement>[]) {
    return createHtmlElement('progress', ...data);
}

export function q(...data: TagParam<HTMLQuoteElement>[]) {
    return createHtmlElement('q', ...data);
}

export function rp(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('rp', ...data);
}

export function rt(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('rt', ...data);
}

export function ruby(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('ruby', ...data);
}

export function s(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('s', ...data);
}

export function samp(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('samp', ...data);
}

export function script(...data: TagParam<HTMLScriptElement>[]) {
    return createHtmlElement('script', ...data);
}

export function section(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('section', ...data);
}

export function select(...data: TagParam<HTMLSelectElement>[]) {
    return createHtmlElement('select', ...data);
}

export function slot(...data: TagParam<HTMLSlotElement>[]) {
    return createHtmlElement('slot', ...data);
}

export function small(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('small', ...data);
}

export function source(...data: TagParam<HTMLSourceElement>[]) {
    return createHtmlElement('source', ...data);
}

export function span(...data: TagParam<HTMLSpanElement>[]) {
    return createHtmlElement('span', ...data);
}

export function strong(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('strong', ...data);
}

export function style(...data: TagParam<HTMLStyleElement>[]) {
    return createHtmlElement('style', ...data);
}

export function sub(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('sub', ...data);
}

export function summary(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('summary', ...data);
}

export function sup(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('sup', ...data);
}

export function table(...data: TagParam<HTMLTableElement>[]) {
    return createHtmlElement('table', ...data);
}

export function tbody(...data: TagParam<HTMLTableSectionElement>[]) {
    return createHtmlElement('tbody', ...data);
}

export function td(...data: TagParam<HTMLTableCellElement>[]) {
    return createHtmlElement('td', ...data);
}

export function template(...data: TagParam<HTMLTemplateElement>[]) {
    return createHtmlElement('template', ...data);
}

export function textarea(...data: TagParam<HTMLTextAreaElement>[]) {
    return createHtmlElement('textarea', ...data);
}

export function tfoot(...data: TagParam<HTMLTableSectionElement>[]) {
    return createHtmlElement('tfoot', ...data);
}

export function th(...data: TagParam<HTMLTableCellElement>[]) {
    return createHtmlElement('th', ...data);
}

export function thead(...data: TagParam<HTMLTableSectionElement>[]) {
    return createHtmlElement('thead', ...data);
}

export function time(...data: TagParam<HTMLTimeElement>[]) {
    return createHtmlElement('time', ...data);
}

export function title(...data: TagParam<HTMLTitleElement>[]) {
    return createHtmlElement('title', ...data);
}

export function tr(...data: TagParam<HTMLTableRowElement>[]) {
    return createHtmlElement('tr', ...data);
}

export function track(...data: TagParam<HTMLTrackElement>[]) {
    return createHtmlElement('track', ...data);
}

export function u(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('u', ...data);
}

export function ul(...data: TagParam<HTMLUListElement>[]) {
    return createHtmlElement('ul', ...data);
}

export function varElement(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('var', ...data);
}

export function video(...data: TagParam<HTMLVideoElement>[]) {
    return createHtmlElement('video', ...data);
}

export function wbr(...data: TagParam<HTMLElement>[]) {
    return createHtmlElement('wbr', ...data);
}
