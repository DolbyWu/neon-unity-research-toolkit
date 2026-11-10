// Synthetic target callbacks only. No device, participant data or inferred fixation labels.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),vm=require('node:vm');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/gaze-targets');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 // Exercise the exact metrics function embedded in the shipped HTML.
 const code=template.slice(template.indexOf('function targetMetrics('),template.indexOf('// END TARGET METRICS'));
 const metrics=vm.runInNewContext(code+';targetMetrics');
 const sample=(t,id='key',valid=true,status='target_hit')=>({t,id,valid,status,source:'synthetic',calibration:'test'});
 let result=metrics([sample(0),sample(.05),sample(.1,'',false,'stale_or_disconnected'),sample(2),sample(2.05)],0,3);
 assert.equal(result.targets[0].count,4);assert.equal(result.targets[0].visits,2);assert.ok(Math.abs(result.targets[0].seconds-.1)<1e-9);assert.ok(Math.abs(result.covered-.1)<1e-9);
 result=metrics([sample(0),sample(.05,'door'),sample(.1,'door')],0,1);assert.equal(result.targets[0].seconds,0);assert.ok(Math.abs(result.targets[1].seconds-.05)<1e-9);
 assert.equal(metrics([sample(0),sample(1)],0,2).covered,0);
 assert.equal(metrics([sample(0),{...sample(.05),calibration:'different'}],0,1).covered,0);
 assert.equal(metrics([sample(0),sample(.05)],.01,.05).targets.length,0,'Half-open selection excludes boundary rows and pre-selection context.');
 assert.equal(metrics([sample(0,'key',false),sample(.05,'key',false)],0,1).targets.length,0);
 if(process.env.STUDY_METRICS_ONLY==='1'){console.log('PASS: 6 conservative target-metrics cases (exact shipped function).');return;}
 const header=['session_time_s','source','device_timestamp_ms','rtcp_synchronized','ray_valid','status','target_id','target_group','target_label','hit_x','hit_y','hit_z','calibration_id','timestamp_semantics'];
 const row=(t,id,valid=true,status='target_hit')=>[String(t),'synthetic','123','False',String(valid),status,id,id==='door'?'doors':'props','Synthetic','','','','test','unity_callback_time_NOT_device_time_or_hardware_sync'];
 const data={session:'SYNTHETIC TARGET PROTOTYPE — NOT PARTICIPANT DATA',participant:'SYNTHETIC',duration:5,events:[['event_id','session_time_s','region','event_type','puzzle','object_id','status','outcome','detail'],['1','2','Lab','grab_start','','key','','','Synthetic Left hand']],regions:[['start_session_s','end_session_s','region','source']],gaze:[],gazeTargets:[header,row(1,'key'),row(1.05,'key'),row(1.1,'',false,'stale_or_disconnected'),row(2,'door'),row(2.05,'door'),row(3,'</script><img src=x onerror=alert(1)>')]};
 data.targetCatalog=[['target_id','target_name','target_group','legacy_object_ids'],['key','Actual key name','props','["key"]'],['door','Actual door name','doors','[]'],['unseen','Never observed object','props','[]']];
 const file=path.join(out,'synthetic-target-review.html');
 fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(data).replace(/</g,'\\u003c').replace(/>/g,'\\u003e').replace(/&/g,'\\u0026')));
 const browser=await chromium.launch({...(process.env.STUDY_BROWSER_EXECUTABLE?{executablePath:process.env.STUDY_BROWSER_EXECUTABLE}:{channel:process.env.STUDY_BROWSER_CHANNEL||'msedge'}),headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1500,height:1200},acceptDownloads:true});const errors=[],requests=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});
  await page.goto(pathToFileURL(file).href);assert.equal(await page.locator('#error').innerText(),'');
  assert.equal(await page.locator('#targetPanel').isVisible(),true);assert.match(await page.locator('#targetQuality').innerText(),/6 callbacks · 5 valid rays/);
  assert.equal(await page.locator('img').count(),0,'Target labels must remain inert text.');
  assert.match(await page.locator('#targetSummary').innerText(),/Actual key name/);assert.match(await page.locator('#targetSummary').innerText(),/Never observed object/);
  await page.locator('#targetGroupFilter').selectOption('doors');assert.equal(await page.locator('#targetEvents button').count(),2);assert.doesNotMatch(await page.locator('#targetSummary').innerText(),/Actual key name/);
  await page.locator('#targetGroupFilter').selectOption('');await page.locator('#targetInteractionFilter').selectOption('grab_start');assert.equal(await page.locator('#targetEvents button').count(),2);assert.match(await page.locator('#targetSummary').innerText(),/Actual key name/);assert.doesNotMatch(await page.locator('#targetSummary').innerText(),/Actual door name/);
  await page.locator('#targetInteractionFilter').selectOption('');
  await page.locator('#targetFilter').selectOption('door');assert.equal(await page.locator('#targetEvents button').count(),2);
  await page.locator('#targetEvents button').first().click();assert.equal(await page.locator('#cursor').inputValue(),'2');
  await page.locator('#annotationUseCursor').click();assert.equal(await page.locator('#annotationStart').inputValue(),'2');
  await page.locator('#annotationNew').click();
  const pending=page.waitForEvent('download');await page.locator('#targetExport').click();const csv=fs.readFileSync(await(await pending).path(),'utf8');assert.match(csv,/door/);assert.doesNotMatch(csv,/"key"/);
  const bundlePending=page.waitForEvent('download');await page.locator('#bundle').click();const bundle=JSON.parse(fs.readFileSync(await(await bundlePending).path(),'utf8'));assert.equal(bundle.gaze_targets.length,7);assert.equal(bundle.filtered_gaze_targets.length,3);assert.equal(bundle.target_filters.target_id,'door');assert.equal(bundle.target_catalog.length,4);assert.match(bundle.gaze_target_time_basis,/callback_time/);
  await page.locator('#targetPanel').screenshot({path:path.join(out,'target-panel.png')});
  await page.locator('#start').fill('4');await page.locator('#start').dispatchEvent('change');assert.match(await page.locator('#targetQuality').innerText(),/0 callbacks/);
  await page.locator('#end').fill('3');await page.locator('#end').dispatchEvent('change');assert.equal(await page.locator('#targetExport').isDisabled(),true);
  assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
  const old=path.join(out,'legacy-no-targets.html');const legacy={...data};delete legacy.gazeTargets;fs.writeFileSync(old,template.replace('__STUDY_DATA__',JSON.stringify(legacy)));
  await page.goto(pathToFileURL(old).href);assert.equal(await page.locator('#targetPanel').isVisible(),false);assert.equal(await page.locator('#error').innerText(),'');assert.deepEqual(errors,[]);
  console.log('PASS: 6 conservative metrics cases; offline target review/filter/seek/annotation/CSV/bundle; inert labels; empty/invalid ranges; legacy sessions; no network requests.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
