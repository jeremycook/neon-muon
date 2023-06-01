export type TagParams<TElement> = (
    null
    | string
    | Node
    | (string | Node)[]
    | { 'class': { [className: string]: boolean } }
    | { 'style': { [propertyName: string]: string } }
    | { [attributeName: string]: null | string | boolean | EventListener | Function }
    | TElement
);

/**
 * Create an HTML element.
 * @param tagSelector The name of the tag or a simple css-selector like:
 * 'div#some-id.some-class-name', '.some-class'.
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function h(tagSelector: string, ...data: TagParams<HTMLElement>[]) {
    const tagNameMatch = /^[a-z][a-z-]*/i.exec(tagSelector);

    const tagName = tagNameMatch !== null
        ? tagNameMatch[0]
        : "div";

    const idMatch = /#([a-z][a-z0-9-_]*)/i.exec(tagSelector);
    if (idMatch) {
        data = [{ id: idMatch[1] }, ...data];
    }

    const classPattern = /\.([a-z][a-z0-9-_]*)/gi;
    const classes = [];
    let classesMatch: RegExpExecArray;
    while ((classesMatch = classPattern.exec(tagSelector)!) && classesMatch !== null) {
        classes.push(classesMatch[1]);
    }
    if (classes.length > 0) {
        data = [{ class: classes.join(' ') }, ...data];
    }

    return createElement<HTMLElement>(tagName, TagNamespace.html, ...data);
}

/**
 * Create an HTML element.
 * @param tag The name of the tag.
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createHtmlElement<K extends keyof HTMLElementTagNameMap, TElement extends HTMLElementTagNameMap[K]>(tag: K, ...data: TagParams<TElement>[]) {
    return createElement<TElement>(tag, TagNamespace.html, ...data);
}

/**
 * Create an SVG element.
 * @param tag The name of the tag.
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createSvgElement<K extends keyof SVGElementTagNameMap, TElement extends SVGElementTagNameMap[K]>(tag: K, ...data: TagParams<TElement>[]) {
    return createElement<TElement>(tag, TagNamespace.svg, ...data);
}

export enum TagNamespace {
    html = "html",
    svg = "http://www.w3.org/2000/svg",
}

/**
 * Create an element with attributes and content.
 * @param tag The name of the tag.
 * @param namespace 
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createElement<TElement extends Element>(tag: string, namespace: TagNamespace, ...data: TagParams<TElement>[]): TElement {

    const element = namespace === TagNamespace.html
        ? document.createElement(tag)
        : document.createElementNS(namespace, tag);

    for (let i = 0; i < data.length; i++) {
        const content = data[i];

        if (typeof content === "string") {
            const child = text(content);
            element.appendChild(child);
            child.dispatchEvent(new Event('mount', { cancelable: false }));
        }
        else if (content instanceof Node) {
            element.appendChild(content);
            content.dispatchEvent(new Event('mount', { cancelable: false }));
        }
        else if (content instanceof Array) {
            for (const node of content.flat(32)) {
                if (typeof node === "string") {
                    const child = text(node);
                    element.appendChild(child);
                    child.dispatchEvent(new Event('mount', { cancelable: false }));
                }
                else {
                    element.appendChild(node);
                    node.dispatchEvent(new Event('mount', { cancelable: false }));
                }
            }
        }
        else if (content === null) {
            // Noop
        }
        else {
            for (const name in content) {
                const val = (<any>content)[name];
                if (name === "class" && typeof val === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => val[prop]
                            ? element.classList.add(prop)
                            : element.classList.remove(prop));
                }
                else if (name === "style" && typeof val === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => element.style.setProperty(prop, val[prop]));
                }
                else {
                    if (typeof val === "string" || val === true) {
                        element.setAttribute(name, val);
                    }
                    else if (val === false || val === null) {
                        element.removeAttribute(name);
                    }
                    else if (typeof val === "function" && name.startsWith('on')) {
                        element.addEventListener(name.substring(2), val);
                    }
                    else {
                        throw `The "${name}" attribute of type "${typeof val}" is not supported. Event attributes must start with "on".`;
                    }
                }
            }
        }
    }

    return element as any;
}

/**
 * Create a text node.
 * @param content
 */
export function text(content: string) {
    return document.createTextNode(content);
}

/**
 * Create a comment node.
 * @param content
 */
export function comment(content: string) {
    return document.createComment(content);
}

/**
 * A series of nodes surrounded by begin and end Comments.
 */
export type Segment = [Comment, ...(string | Node)[], Comment];

/**
 * Creates a series of nodes surrounded by begin and end Comments
 * that can be manipulated later.
 * @param nodes
 */
export function createSegment(...nodes: (string | Node)[]): Segment {
    return [comment(''), ...nodes, comment('')];
}

/**
 * Replace the nodes within the segment with new nodes.
 * @param segment 
 * @param newNodes 
 */
export function mutateSegment(segment: Segment, ...newNodes: (string | Node)[]) {
    const begin = segment[0];
    const end = segment[segment.length - 1] as Comment;

    if (!(begin instanceof Comment) || !(end instanceof Comment)) {
        throw 'Segments must begin and end with Comments.';
    }

    let node = begin.nextSibling;
    while (node && node !== end) {
        let nextSibling = node.nextSibling;
        
        node.dispatchEvent(new Event('unmount', { cancelable: false }));
        node.remove();

        node = nextSibling;
    }

    begin.after(...newNodes);
    newNodes
        .filter(n => n instanceof Node)
        .forEach(n => (n as Node).dispatchEvent(new Event('mount', { cancelable: false })));

    segment.splice(1, segment.length - 2, ...newNodes);
}
