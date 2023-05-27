import { TagParams, createHtmlElement } from './etc';

export function a(...data: TagParams<HTMLAnchorElement>[]) {
    return createHtmlElement('a', ...data);
}

export function button(...data: TagParams<HTMLButtonElement>[]) {
    return createHtmlElement('button', ...data);
}

export function div(...data: TagParams<HTMLDivElement>[]) {
    return createHtmlElement('div', ...data);
}

export function h1(...data: TagParams<HTMLHeadingElement>[]) {
    return createHtmlElement('h1', ...data);
}

export function li(...data: TagParams<HTMLLIElement>[]) {
    return createHtmlElement('li', ...data);
}

export function p(...data: TagParams<HTMLParagraphElement>[]) {
    return createHtmlElement('p', ...data);
}

export function span(...data: TagParams<HTMLParagraphElement>[]) {
    return createHtmlElement('span', ...data);
}

export function ul(...data: TagParams<HTMLUListElement>[]) {
    return createHtmlElement('ul', ...data);
}
