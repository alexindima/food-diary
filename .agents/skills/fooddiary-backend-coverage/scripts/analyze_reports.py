#!/usr/bin/env python3
"""Analyze Rider dotCover JSON or dotCover XML using Python's standard library."""
import argparse
import fnmatch
import hashlib
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def digest(path):
    with path.open('rb') as stream:
        value = hashlib.sha256()
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(chunk)
        return value.hexdigest()


def project_aliases(repository):
    aliases = {}
    if repository is None:
        return aliases
    repository = repository.resolve()
    solutions = sorted(repository.glob('*.slnx'))
    if len(solutions) != 1:
        raise ValueError('--repo needs a repository with one root .slnx solution')
    for entry in ET.parse(solutions[0]).getroot().iter('Project'):
        relative = entry.get('Path')
        if not relative:
            continue
        path = (repository / relative.replace('\\', '/')).resolve()
        if not path.is_relative_to(repository):
            raise ValueError(f'Solution project escapes repository: {relative}')
        if not path.is_file() or path.suffix != '.csproj':
            continue
        names = {n.text.strip() for n in ET.parse(path).getroot().iter()
                 if n.tag.split('}')[-1] == 'AssemblyName' and n.text and n.text.strip()}
        if names and (len(names) != 1 or '$(' in next(iter(names))):
            raise ValueError(f'AssemblyName requires explicit MSBuild evaluation: {path}')
        if names:
            name = next(iter(names))
            if path.stem in aliases and aliases[path.stem] != name:
                raise ValueError(f'Ambiguous project display name: {path.stem}')
            aliases[path.stem] = name
    return aliases


def kind(node):
    return node.get('Kind', '') if isinstance(node, dict) else node.tag


def children(node):
    return node.get('Children', []) if isinstance(node, dict) else list(node)


def counts(node):
    values = [node.get(key, 0) for key in ('CoveredStatements', 'TotalStatements')]
    if any(isinstance(v, bool) or not isinstance(v, (int, str))
           or (isinstance(v, str) and not v.isdecimal()) for v in values):
        raise ValueError(f'Noninteger statement counts at {node.get("Name", kind(node))}')
    covered, total = map(int, values)
    if not 0 <= covered <= total:
        raise ValueError(f'Invalid statement counts at {node.get("Name", kind(node))}')
    return covered, total


def missing_leaves(node, path=()):
    path += ((kind(node), node.get('Name') or kind(node)),)
    covered, total = counts(node)
    descendants = children(node)
    if not descendants:
        if total > covered:
            yield {'path': [list(item) for item in path], 'covered': covered,
                   'total': total, 'missing': total - covered}
        return
    for child in descendants:
        yield from missing_leaves(child, path)


def read_report(path, aliases, includes, excludes):
    if path.suffix.lower() == '.json':
        root = json.loads(path.read_text(encoding='utf-8-sig'))
        if not isinstance(root, dict) or 'Children' not in root:
            raise ValueError('Expected a Rider tree report with Kind/Children, or export XML')
        nodes = []

        def collect(node):
            if kind(node) == 'Project':
                if sum(kind(c) == 'Assembly' for c in children(node)) > 1:
                    raise ValueError(f'Multiple assembly variants: {node.get("Name")}; analyze separately')
                nodes.append(node)
                return
            for child in children(node):
                collect(child)

        collect(root)
    else:
        root = ET.parse(path).getroot()
        nodes = [root] if root.tag == 'Assembly' else list(root.iter('Assembly'))
    if not nodes:
        raise ValueError('No project/assembly nodes found in coverage report')
    rows = {}
    all_totals = [0, 0]
    for node in nodes:
        display = node.get('Name', '')
        name = aliases.get(display, display)
        if not name or name in rows:
            raise ValueError(f'Empty or duplicate assembly identity: {name!r}')
        covered, total = counts(node)
        missing = list(missing_leaves(node))
        if sum(item['missing'] for item in missing) != total - covered:
            raise ValueError(f'Leaf totals disagree for {name}; unsupported or incomplete report')
        rows[name] = {'name': name, 'displayName': display, 'covered': covered,
                      'total': total, 'missing': total - covered, 'uncoveredMembers': missing}
        all_totals[0] += covered
        all_totals[1] += total
    if tuple(all_totals) != counts(root):
        raise ValueError('Assembly/project totals disagree with report root')
    selected = {name: row for name, row in rows.items()
                if (not includes or any(fnmatch.fnmatchcase(name, p) for p in includes))
                and not any(fnmatch.fnmatchcase(name, p) for p in excludes)}
    if not selected:
        raise ValueError('No assemblies selected by the analysis filters')
    covered = sum(row['covered'] for row in selected.values())
    total = sum(row['total'] for row in selected.values())
    missing = [{**item, 'assembly': name} for name, row in selected.items()
               for item in row['uncoveredMembers']]
    return {'report': str(path.resolve()), 'sha256': digest(path),
            'observedAssemblies': len(rows), 'selectedAssemblies': len(selected),
            'analysisFilters': {'include': includes, 'exclude': excludes},
            'covered': covered, 'total': total, 'missing': total - covered,
            'percent': 100 * covered / total if total else None,
            'assemblies': [{k: v for k, v in row.items() if k != 'uncoveredMembers'}
                           for name, row in sorted(selected.items())],
            'uncoveredMembers': sorted(missing, key=lambda r: (-r['missing'], r['assembly'], str(r['path']))),
            'executionValidated': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path)
    parser.add_argument('--baseline', type=Path)
    parser.add_argument('--repo', type=Path, help='Resolve literal project-to-assembly aliases from .slnx')
    parser.add_argument('--include', action='append', default=[], help='Analysis-only assembly glob; repeatable')
    parser.add_argument('--exclude', action='append', default=[], help='Analysis-only assembly glob; repeatable')
    parser.add_argument('--output', type=Path, help='New JSON artifact; existing files are not overwritten')
    parser.add_argument('--limit', type=int, default=15)
    args = parser.parse_args()
    try:
        aliases = project_aliases(args.repo)
        result = read_report(args.report, aliases, args.include, args.exclude)
        if args.baseline:
            baseline = read_report(args.baseline, aliases, args.include, args.exclude)
            before = {r['name']: r for r in baseline['assemblies']}
            after = {r['name']: r for r in result['assemblies']}
            result['comparison'] = {
                'baseline': baseline['report'], 'baselineSha256': baseline['sha256'],
                'sameAssemblyScope': set(before) == set(after),
                'missingAssemblies': sorted(before.keys() - after.keys()),
                'addedAssemblies': sorted(after.keys() - before.keys()),
                'statementTotalChanges': [
                    {'assembly': name, 'before': before[name]['total'], 'after': after[name]['total']}
                    for name in sorted(before.keys() & after.keys())
                    if before[name]['total'] != after[name]['total']],
                'missingBefore': baseline['missing'], 'missingAfter': result['missing'],
                'binaryAndSourceIdentityValidated': False,
            }
        if args.output:
            with args.output.open('x', encoding='utf-8') as stream:
                json.dump(result, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        print(json.dumps({k: v for k, v in result.items() if k not in ('assemblies', 'uncoveredMembers')}, indent=2))
        for row in result['uncoveredMembers'][:max(0, args.limit)]:
            print(f"{row['missing']:4} {row['assembly']} :: " + ' / '.join(n for _, n in row['path']))
        return 0
    except (OSError, ValueError, ET.ParseError) as error:
        parser.exit(2, f'Coverage report error: {error}\n')


if __name__ == '__main__':
    sys.exit(main())
