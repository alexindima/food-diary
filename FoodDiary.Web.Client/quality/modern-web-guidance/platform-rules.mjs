// Rules describe observable platform constraints. Optional API adoption is reviewed separately.
const attr = (node, name) => {
    const item = [...(node.attributes ?? []), ...(node.inputs ?? [])].find(item => item.name.toLowerCase() === name);
    return item ? (typeof item.value === 'string' ? item.value : (item.value?.ast?.value ?? { dynamic: true })) : undefined;
};
const descendants = node => (node.children ?? []).flatMap(child => [child, ...descendants(child)]);
const rule = inspect => ({
    meta: { type: 'problem', schema: [], messages: { invalid: '{{reason}}' } },
    create(context) {
        return {
            Element(node) {
                const report = reason =>
                    context.report({
                        loc: {
                            start: { line: node.sourceSpan.start.line + 1, column: node.sourceSpan.start.col },
                            end: { line: node.sourceSpan.end.line + 1, column: node.sourceSpan.end.col },
                        },
                        messageId: 'invalid',
                        data: { reason },
                    });
                inspect(node, report);
            },
        };
    },
});

export const platformTemplateRules = {
    'media-semantics': rule((node, report) => {
        const tag = node.name.toLowerCase();
        if (tag === 'iframe' && attr(node, 'title') === undefined) report('Name each embedded frame with title.');
        if (tag === 'video' && (attr(node, 'width') === undefined || attr(node, 'height') === undefined))
            report('Reserve video dimensions with width and height.');
        if (tag === 'audio' && attr(node, 'loading') === 'lazy' && attr(node, 'controls') === undefined)
            report('A lazy audio element needs controls to intersect the viewport.');
        if (['audio', 'video'].includes(tag) && attr(node, 'autoplay') !== undefined && attr(node, 'muted') === undefined)
            report('Do not autoplay audible media.');
        const srcset = attr(node, 'srcset');
        if (
            ['img', 'source'].includes(tag) &&
            typeof srcset === 'string' &&
            /\s\d+w(?:\s*,|\s*$)/u.test(srcset) &&
            attr(node, 'sizes') === undefined
        )
            report('Width-descriptor srcset needs sizes to select the correct image.');
    }),
    'resource-hints': rule((node, report) => {
        const tag = node.name.toLowerCase();
        if (!['link', 'script'].includes(tag)) return;
        if (attr(node, 'importance') !== undefined) report('Use fetchpriority instead of the removed importance attribute.');
        if (tag === 'link' && attr(node, 'rel') === 'preload') {
            if (attr(node, 'as') === undefined) report('Declare a preload resource type with as.');
            if (attr(node, 'as') === 'font' && attr(node, 'crossorigin') === undefined)
                report('Font preloads require crossorigin even on the same origin.');
        }
    }),
    'form-transport': rule((node, report) => {
        if (node.name.toLowerCase() !== 'form') return;
        const rawMethod = attr(node, 'method');
        const method = typeof rawMethod === 'string' ? rawMethod.toLowerCase() : rawMethod;
        if (method === undefined) {
            report('Declare the form method explicitly, including forms whose controls live in child components.');
            return;
        }
        const fields = descendants(node);
        if (
            fields.some(
                child =>
                    ['password', 'new-password', 'current-password'].includes(attr(child, 'type')) ||
                    ['new-password', 'current-password'].includes(attr(child, 'autocomplete')),
            ) &&
            !['post', 'dialog'].includes(method)
        )
            report('Credential forms require method="post" so native fallback cannot expose passwords in the URL.');
    }),
    'native-disclosure': rule((node, report) => {
        if (node.name.toLowerCase() !== 'details') return;
        const children = (node.children ?? []).filter(child => child.name);
        if (children[0]?.name !== 'summary' || children.filter(child => child.name === 'summary').length !== 1)
            report('Details requires exactly one summary as its first element child.');
        if (children[0] && descendants(children[0]).some(child => ['button', 'input', 'select', 'textarea', 'a'].includes(child.name)))
            report('Keep nested interactive controls outside summary.');
    }),
    'ime-enter': rule((node, report) => {
        if (!['input', 'textarea'].includes(node.name.toLowerCase())) return;
        const handlers = (node.outputs ?? []).filter(event => /^keydown\.(?:enter|return)$/iu.test(event.name));
        for (const handler of handlers) {
            const expression = handler.handler?.source ?? '';
            if (!/isComposing|fdUiTextEnter/u.test(expression) && !/^\$event\.(?:stopPropagation|preventDefault)\(\)$/u.test(expression))
                report('Use fdUiTextEnter for text Enter actions so IME confirmation cannot submit incomplete input.');
        }
    }),
};

export function inspectPlatformStyles(root, file) {
    const findings = [];
    const fail = (node, check, message) =>
        findings.push({ check, file, line: node.source.start.line, column: node.source.start.column, message });
    root.walkAtRules('import', node =>
        fail(
            node,
            'css:loading',
            'CSS @import serializes critical stylesheet discovery; use the build-time Sass module system or an HTML stylesheet link.',
        ),
    );
    root.walkDecls(node => {
        const prop = node.prop.toLowerCase(),
            value = node.value.toLowerCase();
        const siblings = node.parent.nodes ?? [];
        const before = siblings.slice(0, siblings.indexOf(node));
        const sibling = name => siblings.some(item => item.type === 'decl' && item.prop === name);
        if (prop === 'font-size' && /\b\d*\.?\d+(?:v[wh]|cq[whib])\b/u.test(value) && !/\b(?:clamp|min|max)\(/u.test(value))
            fail(node, 'css:scalable-text', 'Constrain fluid font sizes with font-relative min/max values.');
        if (
            prop === 'content-visibility' &&
            value === 'auto' &&
            !sibling('contain-intrinsic-size') &&
            !sibling('contain-intrinsic-block-size')
        )
            fail(node, 'css:render-containment', 'Reserve deferred content size with contain-intrinsic-size.');
        if (prop === 'container-type' && /^(?:style|block-size)$/u.test(value))
            fail(node, 'css:container-syntax', 'container-type does not accept style or block-size.');
        if (
            prop === 'font-size' &&
            ['html', ':root'].includes(node.parent.selector?.trim()) &&
            !/^(?:100%|inherit|unset|revert)$/u.test(value)
        )
            fail(node, 'css:scalable-text', 'Preserve the user root font size for OS text scaling.');
        if (prop === 'animation-duration' && /^(?:0?\.0*1ms|0?\.00001s)/u.test(value) && /\*/u.test(node.parent.selector ?? ''))
            fail(node, 'css:motion-safety', 'Do not globally shorten all animations to 0.01ms; provide component motion alternatives.');
        if (prop === 'transition' && /\bscrollbar-color\b/u.test(value))
            fail(node, 'css:scrollbar-safety', 'Do not animate scrollbar-color because it flickers in WebKit.');
        if (/\b(?:calc-size|image-set|contrast-color|sibling-index|sibling-count)\(/u.test(value) && !prop.startsWith('--')) {
            let conditional = false;
            for (let parent = node.parent; parent; parent = parent.parent)
                if (parent.type === 'atrule' && parent.name === 'supports') conditional = true;
            if (
                !conditional &&
                !before.some(
                    item =>
                        item.type === 'decl' &&
                        item.prop === node.prop &&
                        !/\b(?:calc-size|image-set|contrast-color|sibling-index|sibling-count)\(/u.test(item.value),
                )
            )
                fail(
                    node,
                    'css:feature-fallback',
                    'Provide a standard declaration before this optional CSS function or guard it with @supports over a functional base.',
                );
        }
        if (
            prop === 'animation-timeline' &&
            siblings.slice(siblings.indexOf(node) + 1).some(item => item.type === 'decl' && item.prop === 'animation')
        )
            fail(node, 'css:motion-safety', 'animation shorthand resets animation-timeline; declare the timeline last.');
    });
    root.walkRules(node => {
        if (/:auto-fill\b/u.test(node.selector)) fail(node, 'css:form-states', 'The autofill pseudo-class is :autofill, not :auto-fill.');
        if (/::-webkit-/u.test(node.selector) && /::-moz-/u.test(node.selector))
            fail(
                node,
                'css:form-states',
                'Vendor pseudo-elements need separate rules; unsupported pseudo-elements invalidate a selector list.',
            );
    });
    return findings;
}
