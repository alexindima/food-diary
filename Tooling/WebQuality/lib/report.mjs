const escape = value =>
    String(value ?? '').replace(
        /[&<>"']/gu,
        character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character],
    );

export function coverage(lock, policy) {
    return lock.guides.map(guide => ({
        ...guide,
        status:
            policy.guideReviews?.find(review => review.guide === guide.id)?.status ??
            (policy.requirements.some(requirement => requirement.source.guide === guide.id) ? 'partial' : 'unreviewed'),
        review: policy.guideReviews?.find(review => review.guide === guide.id),
        requirements: [...new Set([
            ...policy.requirements.filter(requirement => requirement.source.guide === guide.id).map(requirement => requirement.id),
            ...(policy.guideReviews?.find(review => review.guide === guide.id)?.requirements ?? []),
        ])],
    }));
}

export function renderReport(report, policy) {
    const counts = Object.fromEntries(
        ['pass', 'fail', 'error', 'not-run', 'manual', 'exception', 'not-applicable'].map(status => [
            status,
            report.results.filter(result => result.status === status).length,
        ]),
    );
    const rows = report.results
        .map(result => {
            const requirement = policy.requirements.find(item => item.id === result.requirement);
            const guide = report.catalogue.find(item => item.id === requirement.source.guide);
            const source = `https://github.com/${report.upstream.repository}/blob/${report.upstream.commit}/${guide.path}`;
            const relatedGuides = report.catalogue.filter(item => item.requirements.includes(requirement.id)).map(item => item.id).join(' ');
            return `<article class="requirement" data-guides="${escape(relatedGuides)}" data-status="${escape(result.status)}" data-engine="${escape(result.engine)}"><header><span class="badge ${escape(result.status)}">${escape(result.status)}</span><span class="engine">${escape(result.engine)} · severity: ${escape(result.severity)}</span></header>
      <h3>${escape(requirement.statement)}</h3><p class="muted">${escape(requirement.id)} · ${escape(requirement.scopes.join(', '))}</p>
      <p>${escape(result.detail)}</p><ul>${result.findings.map(finding => `<li><strong>${escape(finding.file ?? finding.case ?? '')}${finding.line ? `:${escape(finding.line)}` : ''}</strong> ${escape(finding.message)}${finding.targets ? `<p class="muted">${escape(finding.targets.join(', '))}</p>` : ''}</li>`).join('')}</ul>
      ${result.cases ? `<details><summary>Execution evidence (${result.cases.length} cases)</summary><pre>${escape(JSON.stringify(result.cases, null, 2))}</pre></details>` : ''}
      ${result.exception ? `<p>Exception: ${escape(result.exception.reason)} · ${escape(result.exception.owner)} · until ${escape(result.exception.expires)}</p>` : ''}
      <footer><a href="${escape(source)}" target="_blank" rel="noreferrer">Google guide: ${escape(guide.id)}</a><span>${escape(result.observations)} observations</span></footer></article>`;
        })
        .join('');
    const catalogueRows = report.catalogue
        .map(
            guide =>
                `<tr data-guide-status="${escape(guide.status)}"><td>${escape(guide.id)}${guide.review ? `<details><summary>Applicability and evidence</summary><p>${escape(guide.review.reason)}</p><p>Assertions: ${escape(guide.requirements.join(', '))}</p><p>Current source: ${escape(guide.review.evidence?.join(', '))}</p>${guide.review.whenAbsent ? `<p>Conditional absence: ${escape(guide.review.whenAbsent.reason)}. CI rejects introduction of a matching feature until this decision is replaced.</p>` : ''}</details>` : ''}</td><td>${escape(guide.category)}</td><td>${escape(guide.status)}</td><td>${guide.requirements.length}</td></tr>`,
        )
        .join('');
    return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>FoodDiary · Web quality</title>
  <style>
  :root{color-scheme:light dark;--bg:#f6f8fc;--surface:#fff;--text:#172338;--muted:#53627a;--border:#dce3ee;--blue:#214bd3}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:1rem/1.55 system-ui,sans-serif}main{max-width:90rem;margin:auto;padding:clamp(1rem,4vw,3rem)}h1{font-size:clamp(2rem,5vw,3.5rem);line-height:1.1;margin:.5rem 0 1rem}h2{margin-top:2rem}h3{font-size:1.1rem;margin:.8rem 0}.eyebrow{color:var(--blue);font-weight:700;letter-spacing:.08em}.muted,.engine{color:var(--muted);font-size:.88rem}.intro{max-width:65rem}.stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(7rem,1fr));gap:.6rem;margin:1.5rem 0}.stat,.requirement,.panel{background:var(--surface);border:1px solid var(--border);border-radius:.8rem;padding:1rem}.stat strong{font-size:1.7rem;display:block}.toolbar{display:flex;gap:.7rem;flex-wrap:wrap;align-items:end;margin:1.5rem 0}label{display:grid;gap:.3rem;font-size:.9rem}input,select{font:inherit;padding:.65rem;border:1px solid var(--border);border-radius:.4rem;background:var(--surface);color:var(--text)}input{min-width:16rem}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,30rem),1fr));gap:1rem;align-items:start}.requirement header,.requirement footer{display:flex;justify-content:space-between;gap:.5rem;align-items:center}.requirement footer{border-top:1px solid var(--border);padding-top:.8rem;margin-top:1rem;font-size:.85rem}.badge{border-radius:1rem;padding:.15rem .65rem;font-size:.8rem;font-weight:700;background:#e9eef6;color:#34445d}.pass{background:#dcf5e9;color:#075c35}.fail,.error{background:#ffe1df;color:#8b1914}.manual,.not-run,.exception{background:#fff0cd;color:#735000}a{color:var(--blue)}ul{padding-left:1.2rem}li{overflow-wrap:anywhere;margin:.5rem 0}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:.78rem;max-height:24rem;overflow:auto}summary{cursor:pointer}table{width:100%;border-collapse:collapse;font-size:.85rem}th,td{text-align:left;padding:.6rem;border-bottom:1px solid var(--border);overflow-wrap:anywhere}.table-scroll{overflow-x:auto}[hidden]{display:none!important}:focus-visible{outline:3px solid var(--blue);outline-offset:3px}@media(prefers-color-scheme:dark){:root{--bg:#111827;--surface:#1b2639;--text:#e8eef9;--muted:#b2c1d9;--border:#34435b;--blue:#a4baff}}
  </style></head><body><main><div class="eyebrow">FOODDIARY / ENGINEERING QUALITY</div><h1>Modern web guidance</h1>
  <p class="intro">A versioned record of what was checked, what failed, and what still needs evidence. Passing implemented checks does not establish compliance with every Google recommendation.</p>
  <p class="muted">Google ${escape(report.upstream.version)} · ${escape(report.upstream.commit.slice(0, 12))} · checkout ${escape(report.provenance.revision.slice(0, 12))}${report.provenance.dirty ? ' + local changes' : ''} · ${escape(report.startedAt)}</p>
  <div class="stats">${Object.entries(counts)
      .map(([status, count]) => `<div class="stat"><strong>${count}</strong><span>${escape(status)}</span></div>`)
      .join('')}</div>
  <div class="panel"><strong>Catalogue coverage</strong><p>${report.catalogue.length} published guides · ${report.catalogue.filter(item => item.status === 'reviewed').length} reviewed as applicable · ${report.catalogue.filter(item => item.status === 'not-applicable').length} conditionally inapplicable · ${report.catalogue.filter(item => item.status === 'partial').length} partially mapped · ${report.catalogue.filter(item => item.status === 'unreviewed').length} unreviewed. Manual assessment, browser scope and real-user performance remain explicit.</p><a href="report.json">Download full JSON evidence</a></div>
  <div class="toolbar"><label>Search requirements<input id="search" type="search" placeholder="Guide, requirement or source file"></label><label>Status<select id="status"><option value="">All statuses</option>${Object.keys(
      counts,
  )
      .map(status => `<option>${status}</option>`)
      .join(
          '',
      )}</select></label><label>Engine<select id="engine"><option value="">All engines</option>${['source', 'contracts', 'browser', 'journeys', 'performance', 'manual'].map(engine => `<option>${engine}</option>`).join('')}</select></label><span id="visible" role="status" aria-live="polite"></span></div>
  <section class="grid" aria-label="Requirement results">${rows}</section><h2>All published guides</h2><p>Reviewed records an applicability decision and linked assertions. It does not certify every recommendation. Conditional absence is rechecked on every source audit. Human and field requirements remain explicit.</p><details><summary>Browse the guide inventory (${report.catalogue.length})</summary><div class="table-scroll"><table><thead><tr><th scope="col">Guide</th><th scope="col">Category</th><th scope="col">Review status</th><th scope="col">Requirements</th></tr></thead><tbody>${catalogueRows}</tbody></table></div></details>
  <h2>Run provenance</h2><details><summary>Revision, source fingerprints and execution environment</summary><pre>${escape(JSON.stringify({ ...report.provenance, executions: report.executions }, null, 2))}</pre></details></main>
  <script>const search=document.getElementById('search'),status=document.getElementById('status'),engine=document.getElementById('engine'),cards=[...document.querySelectorAll('.requirement')];function filter(){let count=0;for(const card of cards){card.hidden=!!((status.value&&card.dataset.status!==status.value)||(engine.value&&card.dataset.engine!==engine.value)||!(card.textContent+' '+card.dataset.guides).toLowerCase().includes(search.value.toLowerCase()));if(!card.hidden)count++}document.getElementById('visible').textContent=count+' requirements shown'}for(const control of [search,status,engine])control.addEventListener('input',filter);filter();</script></body></html>`;
}
