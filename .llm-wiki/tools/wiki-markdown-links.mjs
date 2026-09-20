// A conservative, position-preserving destination scanner. Block and inline
// literals are removed from the scanning view, never from the source text.
// Unsupported/ambiguous syntax is left alone rather than rewritten speculatively.
export function markdownDestinations(text) {
  // Offsets must use UTF-16 code units, including when prose contains emoji.
  const masked = text.split('');
  const hide = (start, end) => { for (let i = start; i < end; i++) if (masked[i] !== '\n') masked[i] = ' '; };
  let offset = 0, fence, frontMatter = false;
  for (const line of text.split(/(?<=\n)/)) {
    const content = line.replace(/^(?: {0,3}>[ \t]?)+/, '');
    if (offset === 0 && line.trim() === '---') frontMatter = true;
    else if (frontMatter && /^(?:---|\.\.\.)\s*$/.test(line)) { frontMatter = false; hide(offset, offset + line.length); offset += line.length; continue; }
    if (frontMatter) hide(offset, offset + line.length);
    else if (fence) {
      hide(offset, offset + line.length);
      const end = /^ {0,3}(`{3,}|~{3,})\s*$/.exec(content);
      if (end && end[1][0] === fence[0] && end[1].length >= fence.length) fence = undefined;
    } else {
      const start = /^ {0,3}(`{3,}|~{3,})/.exec(content);
      if (start) { fence = start[1]; hide(offset, offset + line.length); }
      else if (/^(?: {4}|\t)/.test(content)) hide(offset, offset + line.length);
    }
    offset += line.length;
  }
  let view = masked.join('');
  for (const match of view.matchAll(/<!--[\s\S]*?(?:-->|$)/g)) hide(match.index, match.index + match[0].length);
  for (const match of view.matchAll(/<(pre|code|script|style)(?:\s[^>]*)?>[\s\S]*?(?:<\/\1\s*>|$)/gi)) hide(match.index, match.index + match[0].length);
  view = masked.join('');
  for (let i = 0; i < view.length; i++) {
    if (view[i] === '\\') { i++; continue; }
    if (view[i] !== '`') continue;
    let end = i;
    while (view[end] === '`') end++;
    const delimiter = view.slice(i, end);
    let close = view.indexOf(delimiter, end);
    while (close >= 0 && (view[close - 1] === '`' || view[close + delimiter.length] === '`')) close = view.indexOf(delimiter, close + delimiter.length);
    if (close >= 0) { hide(i, close + delimiter.length); i = close + delimiter.length - 1; }
    else i = end - 1;
  }
  view = masked.join('');
  const destination = (start) => {
    while (/[ \t\r\n]/.test(view[start] ?? '') && start < view.length) start++;
    const angle = view[start] === '<';
    if (angle) start++;
    let end = start, depth = 0;
    for (; end < view.length; end++) {
      const char = view[end];
      if (char === '\\' && end + 1 < view.length) { end++; continue; }
      if (angle) { if (char === '>') break; if (char === '\n' || char === '<') return; }
      else {
        if (/\s/.test(char)) break;
        if (char === '(') depth++;
        if (char === ')') { if (depth === 0) break; depth--; }
      }
    }
    if (end === start || depth !== 0 || (angle && view[end] !== '>')) return;
    return { start, end, href: text.slice(start, end), angle, after: end + (angle ? 1 : 0) };
  };
  const nodes = [];
  for (const match of view.matchAll(/^ {0,3}\[(?:\\.|[^\]\\\n])+\]:[ \t]*/gm)) {
    const node = destination(match.index + match[0].length);
    // A reference definition must end after its destination or a valid title.
    // Inspect original text: masking inline literals must not turn garbage into whitespace.
    const lineEnd = node ? text.indexOf('\n', node.after) : -1;
    const tail = node ? text.slice(node.after, lineEnd < 0 ? text.length : lineEnd) : '';
    if (node && /^(?:[ \t]+(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|\((?:\\.|[^)\\])*\)))?[ \t]*\r?$/.test(tail)) {
      nodes.push(node);
      hide(match.index, lineEnd < 0 ? view.length : lineEnd);
    }
  }
  view = masked.join('');
  const brackets = [];
  for (let i = 0; i < view.length; i++) {
    if (view[i] === '\\') { i++; continue; }
    if (view[i] === '[') brackets.push(i);
    if (view[i] !== ']' || brackets.length === 0) continue;
    brackets.pop();
    if (view[i + 1] !== '(') continue;
    const node = destination(i + 2);
    if (!node) continue;
    // Validate the closing delimiter, including an optional quoted title.
    const tail = view.slice(node.after);
    const closing = /^[ \t\r\n]*(?:(?:"[^"\n]*"|'[^'\n]*'|\([^\n)]*\))[ \t\r\n]*)?\)/.exec(tail);
    if (!closing) continue;
    nodes.push(node);
    i = node.after + closing[0].length - 1;
  }
  return nodes.sort((a, b) => a.start - b.start);
}
