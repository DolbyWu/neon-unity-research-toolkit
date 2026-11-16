// Synthetic identities and rays only; never loads participant recordings.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url'),{chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/object-ownership');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8'),session='SYNTHETIC-OWNERS';
 const header=['session_time_s','source','ray_valid','status','target_id','target_name','target_group','calibration_id','correction_id','mapping_verified'];
 const row=(t,id,valid=true)=>[String(t),'synthetic',String(valid),valid?'target_hit':'stale_or_disconnected',id,id,'props','mount','','false'];
 const data={session,duration:5,gaze:[],regions:[['start_session_s','end_session_s','region']],events:[['session_time_s','event_type','object_id'],['1.08','grab_start','diary']],
 targetCatalog:[['target_id','target_name','target_group','statistics_owner_id','statistics_owner_rule'],['diary','Diary','notes','',''],['visual','Visuals','mesh','diary','explicit_visual_owner'],['lid','Separate lid','parts','',''],['other','Visuals','other','','']],
 gazeTargets:[header,row(1,'visual'),row(1.05,'visual'),row(1.1,'diary'),row(1.15,'lid'),row(2,'other'),row(3,'visual',false),row(4,'visual')]};
 const file=path.join(out,'synthetic-object-ownership.html');const write=d=>fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(d).replace(/</g,'\\u003c')));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({acceptDownloads:true,viewport:{width:1450,height:1000}}),errors=[],network=[];page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url());});
  const table=()=>page.locator('#targetSummary tr').evaluateAll(rows=>rows.slice(1).map(r=>[...r.cells].map(c=>c.textContent)));
  const exportJSON=async id=>{const pending=page.waitForEvent('download');await page.locator('#'+id).click();return JSON.parse(fs.readFileSync(await(await pending).path(),'utf8'));};
  write(data);await page.goto(pathToFileURL(file).href);await page.locator('#targetDisclosure').evaluate(e=>e.open=true);assert.equal(await page.locator('#error').innerText(),'');
  let rows=await table(),diary=rows.find(r=>r[1]==='diary');assert.equal(diary[4],'4');assert.equal(diary[5],'2');assert.equal(diary[6],'0.100');assert.ok(!rows.some(r=>r[1]==='visual'));assert.equal(rows.find(r=>r[1]==='lid')[4],'1');assert.equal(rows.find(r=>r[1]==='other')[4],'1','Same name is never a merge key.');
  const bundle=await exportJSON('bundle');assert.equal(bundle.gaze_targets[1][4],'visual');assert.equal(bundle.target_statistics.find(r=>r.id==='diary').count,4);assert.equal(bundle.target_object_ownership.links[0].owner_id,'diary');
  await page.locator('#targetInteractionFilter').selectOption('grab_start');rows=await table();assert.equal(rows.length,1);assert.equal(rows[0][1],'diary');assert.equal(rows[0][4],'4','Parent event filters retain explicitly owned child hits.');
  const pending=page.waitForEvent('download');await page.locator('#targetExport').click();let csv=fs.readFileSync(await(await pending).path(),'utf8');assert.match(csv,/analysis_object_id/);assert.match(csv,/"visual"/);
  const summaryPending=page.waitForEvent('download');await page.locator('#objectSummaryExport').click();csv=fs.readFileSync(await(await summaryPending).path(),'utf8');assert.match(csv,/"diary","diary"/);assert.match(csv,/NOT_fixation/);
  await page.locator('#targetInteractionFilter').selectOption('');await page.locator('#targetGranularity').selectOption('raw');rows=await table();assert.equal(rows.find(r=>r[1]==='diary')[4],'1');assert.equal(rows.find(r=>r[1]==='visual')[4],'3');
  await page.locator('#targetGranularity').selectOption('object');await page.locator('#targetQualityMode').selectOption('verified');rows=await table();assert.equal(rows.find(r=>r[1]==='diary')[4],'0','Grouping cannot fabricate mapping approval.');await page.locator('#targetQualityMode').selectOption('observed');
  const legacy=structuredClone(data);legacy.targetCatalog=legacy.targetCatalog.map(r=>r.slice(0,3));write(legacy);await page.goto(pathToFileURL(file).href);rows=await table();assert.equal(rows.find(r=>r[1]==='diary')[4],'1','No historical ownership inferred from hierarchy or names.');
  legacy.targetOwnership={schema:'gazeflow-reviewed-target-ownership-v1',session_id:session,links:[{child_id:'visual',owner_id:'diary',rule:'reviewed_explicit_pair'}]};write(legacy);await page.goto(pathToFileURL(file).href);rows=await table();assert.equal(rows.find(r=>r[1]==='diary')[4],'4');
  legacy.targetOwnership.links.push({child_id:'visual',owner_id:'other',rule:'conflict'});write(legacy);await page.goto(pathToFileURL(file).href);rows=await table();assert.equal(rows.find(r=>r[1]==='diary')[4],'1');assert.match(await page.locator('#ownershipStatus').textContent(),/1 conflicting/);
  assert.deepEqual(errors,[]);assert.deepEqual(network,[]);console.log('PASS: explicit object/raw counts, no name merging, independent parts, conservative visits/coverage, no invented approval, event filter joins, JSON/CSV exports, historical opt-in and conflicting links.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
