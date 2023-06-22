export type EventT<TCurrentTarget = any, TTarget = any> = Event & { currentTarget: TCurrentTarget; target: TTarget };

export type TagParams<TElement> = (
    | undefined
    | null
    | false
    | string
    | Node
    | (string | Node)[]
    | { 'class': { [className: string]: boolean } }
    | { 'style': { [propertyName: string]: string } }
    | { [attributeName: string]: null | string | boolean | number | EventListener | Function }
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

    const newNodes: Node[] = [];
    for (let i = 0; i < data.length; i++) {
        const content = data[i];

        if (typeof content === 'string') {
            const child = createText(content);
            element.appendChild(child);
            newNodes.push(child);
        }
        else if (content instanceof Node) {
            element.appendChild(content);
            newNodes.push(content);
        }
        else if (content instanceof Array) {
            for (const node of content.flat(32)) {
                if (typeof node === "string") {
                    const child = createText(node);
                    element.appendChild(child);
                    newNodes.push(child);
                }
                else {
                    element.appendChild(node);
                    newNodes.push(node);
                }
            }
        }
        else if (typeof content === 'undefined' || content === null || content === false) {
            // Noop
        }
        else {
            for (const name in content) {
                const val = (<any>content)[name];
                const typeofVal = typeof val;
                if (name === "class" && typeofVal === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => val[prop]
                            ? element.classList.add(prop)
                            : element.classList.remove(prop));
                }
                else if (name === "style" && typeofVal === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => element.style.setProperty(prop, val[prop]));
                }
                else {
                    if (typeofVal === "string" || val === true || typeofVal === 'number') {
                        element.setAttribute(name, val);

                        if (name === 'autofocus' && val === true) {
                            element.addEventListener('mount', (ev: any) => {
                                console.log(ev);
                                if (_isInViewport(ev.currentTarget)) {
                                    ev.currentTarget.focus();
                                }
                            });
                        }
                        else if (name === 'autoselect' && val === true) {
                            element.addEventListener('mount', (ev: any) => {
                                console.log(ev);
                                if (_isInViewport(ev.currentTarget)) {
                                    ev.currentTarget.focus();
                                    ev.currentTarget.select();
                                }
                            });
                        }
                    }
                    else if (val === false || val === null) {
                        element.removeAttribute(name);
                    }
                    else if (typeofVal === "function" && name.startsWith('on')) {
                        element.addEventListener(name.substring(2), val);
                    }
                    else {
                        throw `The "${name}" attribute of type "${typeofVal}" is not supported. Event attributes must start with "on".`;
                    }
                }
            }
        }
    }

    // Notify children that they have been mounted once this element has been mounted
    element.addEventListener('mount', mountPropogator);

    return element as any;
}

/**
 * Create a text node.
 * @param content
 */
export function createText(content: string) {
    return document.createTextNode(content);
}

/**
 * Create a comment node.
 * @param content
 */
export function createComment(content: string) {
    return document.createComment(content);
}

/**
 * Create a document fragment.
 * @param content
 */
export function createFragment(...nodes: (Node | string)[]) {
    const fragment = document.createDocumentFragment();
    fragment.append(...nodes);
    fragment.addEventListener('mount', mountPropogator);
    return fragment;
}

/**
 * A series of nodes surrounded a begin and an end Comment.
 */
export type Segment = [Comment, ...Node[], Comment];

/**
 * Creates a series of nodes surrounded by begin and end Comments
 * that can be manipulated later.
 * @param nodes
 */
export function createSegment(...newNodes: Node[]): Segment {
    return [createComment(''), ...newNodes, createComment('')];
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
        const nextSibling = node.nextSibling;

        node.dispatchEvent(createUnmountEvent());
        node.remove();

        node = nextSibling;
    }

    // Add the new nodes and mutate the segment
    const addedNodes = newNodes.map(n => typeof n === 'string' ? createText(n) : n);
    begin.after(...addedNodes);
    segment.splice(1, segment.length - 2, ...addedNodes);

    // Notify new nodes that they have been mounted
    for (const node of addedNodes) {
        node.dispatchEvent(createMountEvent());
    }
}

/** Propogates 'mount' events to its children. */
export function mountPropogator(ev: Event): void {
    for (const child of (ev.currentTarget as Node).childNodes) {
        child.dispatchEvent(createMountEvent());
    }
}

/** Returns a new 'mount' event. */
export function createMountEvent(): Event {
    return new Event('mount', { cancelable: false, bubbles: false });
}

export function mountElement(parent: ParentNode, child: Node) {
    parent.append(child);
    child.dispatchEvent(createMountEvent());
}

export function createUnmountEvent(): Event {
    return new Event('unmount', { cancelable: false, bubbles: false });
}

export function unmountElement(node: ChildNode) {
    node.dispatchEvent(createUnmountEvent());
    node.remove();
}

function _isInViewport(element: Element) {
    const rect = element.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
}