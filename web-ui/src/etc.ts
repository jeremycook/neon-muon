/**
 * Create an HTML element.
 * @param tag The name of the tag or a simple css-selector like:
 * 'div#some-id.some-class-name', '.some-class'.
 * @param data An array of strings, Nodes, and attribute objects.
 * @returns HTMLElement
 */
export function h(tag: string, ...data: (
    string |
    Node |
    (string | Node)[] |
    { ["style"]: { [key: string]: string } } |
    { [key: string]: string | EventListener | Function | { [key: string]: string } }
)[]): HTMLElement {
    return createElement(tag, TagNamespace.html, ...data) as HTMLElement;
}

/**
 * Create an SVG element.
 * @param tag The name of the tag or a simple css-selector like:
 * 'line#some-id.some-class', '.some-class'.
 * @param data An array of strings, Nodes, and attribute objects.
 * @returns SVGElement
 */
export function svg(tag: string, ...data: (
    string |
    Node |
    (string | Node)[] |
    { ["style"]: { [key: string]: string } } |
    { [key: string]: string | EventListener | Function | { [key: string]: string } }
)[]): SVGElement {
    return createElement(tag, TagNamespace.svg, ...data) as SVGAElement;
}

export enum TagNamespace {
    html = "html",
    svg = "http://www.w3.org/2000/svg",
}

/**
 * Create an element with attributes and content.
 * @param tag The name of the tag, or a simple css-selector style.
 * @param namespace 
 * @param data An array of strings, Nodes, and attribute objects.
 * @returns Element
 */
export function createElement(tag: string, namespace: TagNamespace, ...data: (
    string |
    Node |
    (string | Node)[] |
    { ["style"]: { [key: string]: string } } |
    { [key: string]: string | EventListener | Function | { [key: string]: string } }
)[]): Element {

    const tagNameMatch = /^[a-z][a-z-]*/i.exec(tag);

    const tagName = tagNameMatch !== null
        ? tagNameMatch[0]
        : (namespace == TagNamespace.svg ? "svg" : "div");

    const element = namespace === TagNamespace.html
        ? document.createElement(tagName)
        : document.createElementNS(namespace, tagName);

    const idMatch = /#([a-z][a-z-_]*)/i.exec(tag);
    if (idMatch !== null) {
        element.id = idMatch[1];
    }

    const classPattern = /\.([a-z][a-z-_]*)/gi;
    let classesMatch: RegExpExecArray;
    while ((classesMatch = classPattern.exec(tag)!) && classesMatch !== null) {
        element.classList.add(classesMatch[1]);
    }

    for (let i = 0; i < data.length; i++) {
        const content = data[i];

        if (typeof content === "string") {
            element.appendChild(text(content));
        }
        else if (content instanceof Node) {
            element.appendChild(content);
        }
        else if (content instanceof Array) {
            for (const node of content.flat(32)) {
                if (typeof node === "string") {
                    element.appendChild(text(node));
                }
                else {
                    element.appendChild(node);
                }
            }
        }
        else {
            for (const name in content) {
                if (name === "style") {
                    const val = content[name];
                    if (typeof val === "object") {
                        Object.getOwnPropertyNames(val).forEach(prop => element.style.setProperty(prop, val[prop]));
                    }
                    else {
                        throw `The "${name}" attribute of type "${typeof val}" is not supported.`;
                    }
                }
                else {
                    const val = (<any>content)[name];

                    if (typeof val === "string") {
                        element.setAttribute(name, val);
                    }
                    else if (typeof val === "function") {
                        element.addEventListener(name.substring(2), val);
                    }
                    else {
                        throw `The "${name}" attribute of type "${typeof val}" is not supported.`;
                    }
                }
            }
        }
    }

    return element;
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
