import { TagParams, createHtmlElement } from './etc';

export interface ValueEvent extends InputEvent {
    target: EventTarget & { value: string }
}

export function a(...data: TagParams<HTMLAnchorElement>[]) {
    return createHtmlElement('a', ...data);
}

export function abbr(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('abbr', ...data);
}

export function address(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('address', ...data);
}

export function area(...data: TagParams<HTMLAreaElement>[]) {
    return createHtmlElement('area', ...data);
}

export function article(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('article', ...data);
}

export function aside(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('aside', ...data);
}

export function audio(...data: TagParams<HTMLAudioElement>[]) {
    return createHtmlElement('audio', ...data);
}

export function b(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('b', ...data);
}

export function base(...data: TagParams<HTMLBaseElement>[]) {
    return createHtmlElement('base', ...data);
}

export function bdi(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('bdi', ...data);
}

export function bdo(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('bdo', ...data);
}

export function blockquote(...data: TagParams<HTMLQuoteElement>[]) {
    return createHtmlElement('blockquote', ...data);
}

export function body(...data: TagParams<HTMLBodyElement>[]) {
    return createHtmlElement('body', ...data);
}

export function br(...data: TagParams<HTMLBRElement>[]) {
    return createHtmlElement('br', ...data);
}

export function button(...data: TagParams<HTMLButtonElement>[]) {
    return createHtmlElement('button', ...data);
}

export function canvas(...data: TagParams<HTMLCanvasElement>[]) {
    return createHtmlElement('canvas', ...data);
}

export function caption(...data: TagParams<HTMLTableCaptionElement>[]) {
    return createHtmlElement('caption', ...data);
}

export function cite(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('cite', ...data);
}

export function code(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('code', ...data);
}

export function col(...data: TagParams<HTMLTableColElement>[]) {
    return createHtmlElement('col', ...data);
}

export function colgroup(...data: TagParams<HTMLTableColElement>[]) {
    return createHtmlElement('colgroup', ...data);
}

export function data(...data: TagParams<HTMLDataElement>[]) {
    return createHtmlElement('data', ...data);
}

export function datalist(...data: TagParams<HTMLDataListElement>[]) {
    return createHtmlElement('datalist', ...data);
}

export function dd(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('dd', ...data);
}

export function del(...data: TagParams<HTMLModElement>[]) {
    return createHtmlElement('del', ...data);
}

export function details(...data: TagParams<HTMLDetailsElement>[]) {
    return createHtmlElement('details', ...data);
}

export function dfn(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('dfn', ...data);
}

export function dialog(...data: TagParams<HTMLDialogElement>[]) {
    return createHtmlElement('dialog', ...data);
}

export function div(...data: TagParams<HTMLDivElement>[]) {
    return createHtmlElement('div', ...data);
}

export function dl(...data: TagParams<HTMLDListElement>[]) {
    return createHtmlElement('dl', ...data);
}

export function dt(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('dt', ...data);
}

export function em(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('em', ...data);
}

export function embed(...data: TagParams<HTMLEmbedElement>[]) {
    return createHtmlElement('embed', ...data);
}

export function fieldset(...data: TagParams<HTMLFieldSetElement>[]) {
    return createHtmlElement('fieldset', ...data);
}

export function figcaption(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('figcaption', ...data);
}

export function figure(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('figure', ...data);
}

export function footer(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('footer', ...data);
}

export function form(...data: TagParams<HTMLFormElement>[]) {
    return createHtmlElement('form', ...data);
}

export function h1(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h1', ...data);
}

export function h2(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h2', ...data);
}

export function h3(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h3', ...data);
}

export function h4(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h4', ...data);
}

export function h5(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h5', ...data);
}

export function h6(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h6', ...data);
}

export function head(...data: TagParams<HTMLHeadElement>[]) {
    return createHtmlElement('head', ...data);
}

export function header(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('header', ...data);
}

export function hgroup(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('hgroup', ...data);
}

export function hr(...data: TagParams<HTMLHRElement>[]) {
    return createHtmlElement('hr', ...data);
}

export function html(...data: TagParams<HTMLHtmlElement>[]) {
    return createHtmlElement('html', ...data);
}

export function i(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('i', ...data);
}

export function iframe(...data: TagParams<HTMLIFrameElement>[]) {
    return createHtmlElement('iframe', ...data);
}

export function img(...data: TagParams<HTMLImageElement>[]) {
    return createHtmlElement('img', ...data);
}

export function input(...data: TagParams<HTMLInputElement>[]) {
    return createHtmlElement('input', ...data);
}

export function ins(...data: TagParams<HTMLModElement>[]) {
    return createHtmlElement('ins', ...data);
}

export function kbd(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('kbd', ...data);
}

export function label(...data: TagParams<HTMLLabelElement>[]) {
    return createHtmlElement('label', ...data);
}

export function legend(...data: TagParams<HTMLLegendElement>[]) {
    return createHtmlElement('legend', ...data);
}

export function li(...data: TagParams<HTMLLIElement>[]) {
    return createHtmlElement('li', ...data);
}

export function link(...data: TagParams<HTMLLinkElement>[]) {
    return createHtmlElement('link', ...data);
}

export function main(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('main', ...data);
}

export function map(...data: TagParams<HTMLMapElement>[]) {
    return createHtmlElement('map', ...data);
}

export function mark(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('mark', ...data);
}

export function menu(...data: TagParams<HTMLMenuElement>[]) {
    return createHtmlElement('menu', ...data);
}

export function meta(...data: TagParams<HTMLMetaElement>[]) {
    return createHtmlElement('meta', ...data);
}

export function meter(...data: TagParams<HTMLMeterElement>[]) {
    return createHtmlElement('meter', ...data);
}

export function nav(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('nav', ...data);
}

export function noscript(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('noscript', ...data);
}

export function object(...data: TagParams<HTMLObjectElement>[]) {
    return createHtmlElement('object', ...data);
}

export function ol(...data: TagParams<HTMLOListElement>[]) {
    return createHtmlElement('ol', ...data);
}

export function optgroup(...data: TagParams<HTMLOptGroupElement>[]) {
    return createHtmlElement('optgroup', ...data);
}

export function option(...data: TagParams<HTMLOptionElement>[]) {
    return createHtmlElement('option', ...data);
}

export function output(...data: TagParams<HTMLOutputElement>[]) {
    return createHtmlElement('output', ...data);
}

export function p(...data: TagParams<HTMLParagraphElement>[]) {
    return createHtmlElement('p', ...data);
}

export function picture(...data: TagParams<HTMLPictureElement>[]) {
    return createHtmlElement('picture', ...data);
}

export function pre(...data: TagParams<HTMLPreElement>[]) {
    return createHtmlElement('pre', ...data);
}

export function progress(...data: TagParams<HTMLProgressElement>[]) {
    return createHtmlElement('progress', ...data);
}

export function q(...data: TagParams<HTMLQuoteElement>[]) {
    return createHtmlElement('q', ...data);
}

export function rp(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('rp', ...data);
}

export function rt(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('rt', ...data);
}

export function ruby(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('ruby', ...data);
}

export function s(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('s', ...data);
}

export function samp(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('samp', ...data);
}

export function script(...data: TagParams<HTMLScriptElement>[]) {
    return createHtmlElement('script', ...data);
}

export function section(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('section', ...data);
}

export function select(...data: TagParams<HTMLSelectElement>[]) {
    return createHtmlElement('select', ...data);
}

export function slot(...data: TagParams<HTMLSlotElement>[]) {
    return createHtmlElement('slot', ...data);
}

export function small(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('small', ...data);
}

export function source(...data: TagParams<HTMLSourceElement>[]) {
    return createHtmlElement('source', ...data);
}

export function span(...data: TagParams<HTMLSpanElement>[]) {
    return createHtmlElement('span', ...data);
}

export function strong(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('strong', ...data);
}

export function style(...data: TagParams<HTMLStyleElement>[]) {
    return createHtmlElement('style', ...data);
}

export function sub(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('sub', ...data);
}

export function summary(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('summary', ...data);
}

export function sup(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('sup', ...data);
}

export function table(...data: TagParams<HTMLTableElement>[]) {
    return createHtmlElement('table', ...data);
}

export function tbody(...data: TagParams<HTMLTableSectionElement>[]) {
    return createHtmlElement('tbody', ...data);
}

export function td(...data: TagParams<HTMLTableCellElement>[]) {
    return createHtmlElement('td', ...data);
}

export function template(...data: TagParams<HTMLTemplateElement>[]) {
    return createHtmlElement('template', ...data);
}

export function textarea(...data: TagParams<HTMLTextAreaElement>[]) {
    return createHtmlElement('textarea', ...data);
}

export function tfoot(...data: TagParams<HTMLTableSectionElement>[]) {
    return createHtmlElement('tfoot', ...data);
}

export function th(...data: TagParams<HTMLTableCellElement>[]) {
    return createHtmlElement('th', ...data);
}

export function thead(...data: TagParams<HTMLTableSectionElement>[]) {
    return createHtmlElement('thead', ...data);
}

export function time(...data: TagParams<HTMLTimeElement>[]) {
    return createHtmlElement('time', ...data);
}

export function title(...data: TagParams<HTMLTitleElement>[]) {
    return createHtmlElement('title', ...data);
}

export function tr(...data: TagParams<HTMLTableRowElement>[]) {
    return createHtmlElement('tr', ...data);
}

export function track(...data: TagParams<HTMLTrackElement>[]) {
    return createHtmlElement('track', ...data);
}

export function u(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('u', ...data);
}

export function ul(...data: TagParams<HTMLUListElement>[]) {
    return createHtmlElement('ul', ...data);
}

export function varElement(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('var', ...data);
}

export function video(...data: TagParams<HTMLVideoElement>[]) {
    return createHtmlElement('video', ...data);
}

export function wbr(...data: TagParams<HTMLElement>[]) {
    return createHtmlElement('wbr', ...data);
}
