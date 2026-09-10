export function englishMorphologicalVariants(term) {
  if (!/^[a-z]+$/.test(term)) return [];
  const variants = [];
  if (term.length > 4 && term.endsWith('ies')) variants.push(`${term.slice(0, -3)}y`);
  else if (term.length > 3 && term.endsWith('s') && !term.endsWith('ss')) variants.push(term.slice(0, -1));
  // Preserve the old alternative, but also recognize plurals such as indexes,
  // boxes, classes and watches before bounded FTS candidate selection.
  if (term.length > 4 && /(?:xes|ches|shes|sses|zes)$/.test(term)) variants.push(term.slice(0, -2));
  if (term.length > 5 && term.endsWith('ing')) {
    const stem = term.slice(0, -3);
    variants.push(stem, `${stem}e`);
    if (stem.length > 2 && stem.at(-1) === stem.at(-2)) variants.push(stem.slice(0, -1));
  }
  if (term.length > 4 && term.endsWith('ied')) variants.push(`${term.slice(0, -3)}y`);
  if (term.length > 4 && term.endsWith('ed')) variants.push(term.slice(0, -2), term.slice(0, -1));
  return variants;
}
