#!/usr/bin/env python3
"""Validate test execution separately from dotCover collection and compare TRX scopes."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
FATAL = re.compile(r'Inconsistent PDB detected|CORE_API_E_UNRECOVERABLE_ERROR|'
                   r'Coverage session finished with errors|no snapshots were created|Internal CLR error', re.I)


def file_hash(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def load_tests(paths):
    files = set()
    for path in paths:
        files.update(p.resolve() for p in (path.rglob('*.trx') if path.is_dir() else [path]))
    if not files:
        raise ValueError('No TRX reports found')
    cases = []
    for path in sorted(files):
        root = ET.parse(path).getroot()
        definitions = {n.get('id'): n for n in root.findall('./t:TestDefinitions/t:UnitTest', NS)}
        results = root.findall('./t:Results/t:UnitTestResult', NS)
        counters = root.find('./t:ResultSummary/t:Counters', NS)
        summary = root.find('./t:ResultSummary', NS)
        if summary is None or summary.get('outcome') not in ('Completed', 'Passed'):
            raise ValueError(f'Incomplete or failed TRX session: {path}')
        if any(n.get('outcome') == 'Error' for n in root.findall('./t:ResultSummary/t:RunInfos/t:RunInfo', NS)):
            raise ValueError(f'TRX session reported a test-host error: {path}')
        if not results or counters is None or int(counters.get('total', 0)) != len(results):
            raise ValueError(f'Empty/incomplete TRX or inconsistent total: {path}')
        actual = Counter(r.get('outcome') for r in results)
        for field, outcome in [('passed', 'Passed'), ('failed', 'Failed')]:
            if int(counters.get(field, 0)) != actual[outcome]:
                raise ValueError(f'Inconsistent TRX {field} counter: {path}')
        # VSTest/xUnit may leave Counters.notExecuted at zero for skipped result rows.
        if int(counters.get('executed', 0)) + actual['NotExecuted'] != len(results):
            raise ValueError(f'Inconsistent TRX executed/result counts: {path}')
        for result in results:
            definition = definitions.get(result.get('testId'))
            method = definition.find('t:TestMethod', NS) if definition is not None else None
            if method is None:
                raise ValueError(f'Test definition missing: {result.get("testName")} in {path}')
            assembly = method.get('codeBase', '').replace('\\', '/').rsplit('/', 1)[-1]
            name = result.get('testName', '')
            if not assembly or not name:
                raise ValueError(f'Test identity missing in {path}')
            reason = result.find('./t:Output/t:ErrorInfo/t:Message', NS)
            cases.append({'identity': (assembly, method.get('className', ''), name),
                          'name': name, 'outcome': result.get('outcome', ''),
                          'reportedSkipReason': reason.text if reason is not None else None})
    return cases, [{'path': str(p), 'sha256': file_hash(p)} for p in sorted(files)]


def read_reasons(path):
    policy = {'skippedTests': {}, 'profileOnlySkips': {}}
    if path:
        provided = json.loads(path.read_text(encoding='utf-8-sig'))
        if not isinstance(provided, dict) or set(provided) - set(policy):
            raise ValueError('Skip reasons must contain skippedTests and/or profileOnlySkips maps')
        policy.update(provided)
    for entries in policy.values():
        if not isinstance(entries, dict) or any(not isinstance(k, str) or not k.strip()
                                              or not isinstance(v, str) or not v.strip()
                                              for k, v in entries.items()):
            raise ValueError('Each skip needs an exact display name and a nonempty reviewed reason')
    return policy


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--trx', type=Path, action='append', required=True, help='TRX file/directory; repeatable')
    parser.add_argument('--process-exit-code', type=int, required=True, help='Actual native test/collector exit code')
    parser.add_argument('--mode', choices=['tests', 'profile'], default='tests')
    parser.add_argument('--collector-log', type=Path, action='append', default=[])
    parser.add_argument('--snapshot', type=Path)
    parser.add_argument('--baseline-trx', type=Path, action='append')
    parser.add_argument('--skip-reasons', type=Path)
    parser.add_argument('--output', type=Path, help='New JSON artifact; existing files are not overwritten')
    args = parser.parse_args()
    try:
        cases, files = load_tests(args.trx)
        policy = read_reasons(args.skip_reasons)
        issues, diagnostics = [], []
        if args.process_exit_code:
            issues.append(f'Process returned {args.process_exit_code}')
        if args.mode == 'profile' and not args.collector_log:
            issues.append('Profiler log was not supplied')
        for path in args.collector_log:
            for number, line in enumerate(path.read_text(encoding='utf-8-sig', errors='replace').splitlines(), 1):
                if FATAL.search(line):
                    diagnostics.append({'path': str(path.resolve()), 'line': number, 'message': line.strip()[:300]})
        if diagnostics:
            issues.append('Collector diagnostics contain a fatal coverage error')
        snapshot = None
        if args.mode == 'profile':
            if not args.snapshot or not args.snapshot.is_file() or args.snapshot.stat().st_size == 0:
                issues.append('Nonempty profiler snapshot was not supplied')
            else:
                snapshot = {'path': str(args.snapshot.resolve()), 'sha256': file_hash(args.snapshot)}
        baseline, baseline_files = load_tests(args.baseline_trx) if args.baseline_trx else (None, [])
        for label, rows in [('current', cases), ('baseline', baseline or [])]:
            for row in rows:
                if row['outcome'] not in ('Passed', 'NotExecuted'):
                    issues.append(f'{label}: {row["outcome"]}: {row["name"]}')
                if row['outcome'] == 'NotExecuted' and row['name'] not in policy['skippedTests']:
                    profile_exception = (label == 'current' and args.mode == 'profile' and baseline is not None
                                         and row['name'] in policy['profileOnlySkips'])
                    if not profile_exception:
                        issues.append(f'Unreviewed {label} skip: {row["name"]}')
        same_scope = None
        outcome_differences = []
        if baseline is not None:
            same_scope = Counter(r['identity'] for r in cases) == Counter(r['identity'] for r in baseline)
            if not same_scope:
                issues.append('Baseline/current test identity multisets differ')
            before, after = defaultdict(Counter), defaultdict(Counter)
            for row in baseline:
                before[row['identity']][row['outcome']] += 1
            for row in cases:
                after[row['identity']][row['outcome']] += 1
            for identity in sorted(before.keys() | after.keys()):
                if before[identity] == after[identity]:
                    continue
                allowed = (args.mode == 'profile' and identity[2] in policy['profileOnlySkips']
                           and set(before[identity]) == {'Passed'} and set(after[identity]) == {'NotExecuted'}
                           and before[identity]['Passed'] == after[identity]['NotExecuted'])
                outcome_differences.append({'test': identity, 'before': dict(before[identity]),
                                            'after': dict(after[identity]), 'reviewedProfileSkip': allowed})
                if not allowed:
                    issues.append(f'Unreviewed outcome difference: {identity[2]}')
        outcomes = Counter(row['outcome'] for row in cases)
        result = {'valid': not issues, 'mode': args.mode, 'processExitCode': args.process_exit_code,
                  'total': len(cases), 'passed': outcomes['Passed'], 'failed': len(cases) - outcomes['Passed'] - outcomes['NotExecuted'],
                  'skipped': outcomes['NotExecuted'], 'testAssemblies': len({r['identity'][0] for r in cases}),
                  'skippedTests': [r for r in cases if r['outcome'] == 'NotExecuted'],
                  'sameTestScopeAsBaseline': same_scope, 'outcomeDifferences': outcome_differences,
                  'trxFiles': files, 'baselineTrxFiles': baseline_files, 'snapshot': snapshot,
                  'collectorLogs': [{'path': str(p.resolve()), 'sha256': file_hash(p)} for p in args.collector_log],
                  'skipReasons': {'path': str(args.skip_reasons.resolve()), 'sha256': file_hash(args.skip_reasons)} if args.skip_reasons else None,
                  'collectorFatalDiagnosticCount': len(diagnostics), 'collectorDiagnostics': diagnostics[:50],
                  'issues': issues, 'sourceAndCoverageScopeValidated': False}
        if args.output:
            with args.output.open('x', encoding='utf-8') as stream:
                json.dump(result, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        print(json.dumps({k: v for k, v in result.items() if k not in (
            'trxFiles', 'baselineTrxFiles', 'skippedTests', 'outcomeDifferences', 'collectorDiagnostics')}, indent=2))
        return 0 if result['valid'] else 1
    except (OSError, ValueError, ET.ParseError) as error:
        parser.exit(2, f'Run evidence error: {error}\n')


if __name__ == '__main__':
    sys.exit(main())
