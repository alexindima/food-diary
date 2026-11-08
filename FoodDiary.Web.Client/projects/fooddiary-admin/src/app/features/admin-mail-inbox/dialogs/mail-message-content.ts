import { type DefaultTreeAdapterTypes, parseFragment } from 'parse5';

type MailContent = { previewHtml: string; textBody: string };
const READING_TAGS = new Set([
    'a',
    'b',
    'blockquote',
    'br',
    'caption',
    'code',
    'dd',
    'del',
    'div',
    'dl',
    'dt',
    'em',
    'h1',
    'h2',
    'h3',
    'h4',
    'h5',
    'h6',
    'hr',
    'i',
    'li',
    'ol',
    'p',
    'pre',
    's',
    'small',
    'span',
    'strong',
    'sub',
    'sup',
    'table',
    'tbody',
    'td',
    'tfoot',
    'th',
    'thead',
    'tr',
    'u',
    'ul',
]);
const REMOVED_TAGS = new Set([
    'script',
    'style',
    'iframe',
    'object',
    'embed',
    'svg',
    'math',
    'template',
    'form',
    'input',
    'button',
    'select',
    'textarea',
    'img',
    'audio',
    'video',
    'source',
    'link',
    'meta',
    'base',
]);
const BLOCK_TAGS = new Set(['br', 'p', 'div', 'li', 'blockquote', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'tr', 'pre']);
const MAX_READING_DEPTH = 100;

export function mailMessageContent(html: string | null | undefined, text: string | null | undefined): MailContent {
    // Parse as data, never in a browser document: email resources cannot load during preparation.
    const content = readNodes(parseFragment(html ?? '').childNodes, 0);
    return {
        previewHtml: content.textBody.trim() === '' ? '' : content.previewHtml,
        textBody:
            text !== null && text !== undefined && text.trim() !== ''
                ? readNodes(parseFragment(text.replaceAll('<', '&lt;')).childNodes, 0).textBody
                : content.textBody.trim(),
    };
}

function readNodes(nodes: DefaultTreeAdapterTypes.ChildNode[], depth: number): MailContent {
    const contents = nodes.map(node => readNode(node, depth));
    return {
        previewHtml: contents.map(content => content.previewHtml).join(''),
        textBody: contents.map(content => content.textBody).join(''),
    };
}

function readNode(node: DefaultTreeAdapterTypes.ChildNode, depth: number): MailContent {
    if ('value' in node) {
        return { previewHtml: escapeHtml(node.value), textBody: node.value };
    }
    if (!('tagName' in node) || REMOVED_TAGS.has(node.tagName) || depth >= MAX_READING_DEPTH) {
        return { previewHtml: '', textBody: '' };
    }
    const content = readNodes(node.childNodes, depth + 1);
    if (!READING_TAGS.has(node.tagName)) {
        return content;
    }
    const attributes = readingAttributes(node);
    const closingTag = node.tagName === 'br' || node.tagName === 'hr' ? '' : `</${node.tagName}>`;
    return {
        previewHtml: `<${node.tagName}${attributes}>${content.previewHtml}${closingTag}`,
        textBody: content.textBody + (BLOCK_TAGS.has(node.tagName) ? '\n' : ''),
    };
}

function readingAttributes(node: DefaultTreeAdapterTypes.Element): string {
    if (node.tagName !== 'a') {
        return '';
    }
    const href = node.attrs.find(attribute => attribute.name === 'href')?.value;
    if (href === undefined) {
        return '';
    }
    try {
        const url = new URL(href);
        return ['https:', 'http:', 'mailto:'].includes(url.protocol)
            ? ` href="${escapeHtml(href.trim())}" target="_blank" rel="noopener noreferrer"`
            : '';
    } catch {
        return '';
    }
}

function escapeHtml(text: string): string {
    return text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
}
