// Synthetic replay evidence only. No real recordings, capture, microphone or device access.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url'),{chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/gaze-offset-report');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const session='20990101T000000000Z-abcdef01',model=(id,h,v,t)=>({id,session_id:session,calibration_id:'synthetic-mount',correction_kind:'manual_yaw_pitch',horizontal_degrees:h,vertical_degrees:v,applied_session_s:t});
 const header=['session_time_s','ray_valid','status','correction_id','calibration_id','mapping_verified','source','target_id'];
 const row=(t,id='',valid=true,status='no_target',mount='synthetic-mount')=>[String(t),String(valid),status,id,mount,'false','synthetic',''];
 const gaze=[['receive_session_time_s','aligned_session_time_s','time_alignment_status','sample_quality','eye_state_available','gaze_x_px','gaze_y_px','pupil_left_mm','pupil_right_mm','connection_epoch'],['2.07','2.05','time_echo_estimated','basic_valid_not_full_eye_quality_assessment','True','123.456','456.789','3.1','3.2','1']];
 const data={version:1,session,duration:10,events:[['session_time_s','event_type','object_id','detail']],regions:[['start_session_s','end_session_s','region']],gaze:[{source:'Neon-synthetic',table:gaze}],gazeTargets:[header,row(1),row(2.05,'manual-a'),row(2.09,'manual-a',false,'stale_or_disconnected'),row(4.5,'missing-model'),row(5.05,'manual-b'),row(6),row(7,'fitted'),row(8,'manual-a',true,'no_target','different-mount')],gazeAdjustments:{models:[model('manual-a',3.7,2.7,2),model('manual-b',-1,-.5,5),{...model('fitted',0,0,null),correction_kind:'fitted_rotation'}, {...model('foreign',9,9,1),session_id:'foreign'}],confirmations:[{id:'save-a',session_id:session,calibration_id:'synthetic-mount',saved_session_s:3,correction_id:'manual-a',mode:'manual_yaw_pitch',horizontal_degrees:3.7,vertical_degrees:2.7}],warnings:[]}};
 const write=(name,value)=>{const file=path.join(out,name);fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(value).replace(/</g,'\\u003c')));return file;};
 const file=write('synthetic-offset-review.html',data),browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1100},acceptDownloads:true}),errors=[],network=[];page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url());});
  const seek=async t=>page.locator('#cursor').evaluate((e,t)=>{e.value=String(t);e.dispatchEvent(new Event('input',{bubbles:true}));},t);
  const text=id=>page.locator('#'+id).innerText();const exportJSON=async id=>{const pending=page.waitForEvent('download');await page.locator('#'+id).click();return JSON.parse(fs.readFileSync(await(await pending).path(),'utf8'));};
  await page.goto(pathToFileURL(file).href);assert.equal(await text('error'),'');assert.match(await text('adjustmentSaved'),/3\.70° right \/ 2\.70° up/);assert.equal(await text('adjustmentHorizontal'),'Not available');
  await seek(1);assert.equal(await text('adjustmentHorizontal'),'0.00°');assert.match(await text('adjustmentActive'),/no EXTRA/);
  await seek(2.05);assert.equal(await text('adjustmentHorizontal'),'3.70° right');assert.equal(await text('adjustmentVertical'),'2.70° up');assert.match(await text('adjustmentActive'),/not verified/);assert.match(await text('status'),/Left pupil 3\.100000/);
  await page.locator('#basis').selectOption('receive');assert.equal(await text('adjustmentHorizontal'),'3.70° right','Eye trace basis does not change callback-time offsets.');
  await seek(2.09);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/stale_or_disconnected/);
  await seek(3);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/No fresh/);
  await seek(4.5);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/metadata unavailable/);
  await seek(5.05);assert.equal(await text('adjustmentHorizontal'),'1.00° left');assert.equal(await text('adjustmentVertical'),'0.50° down');
  await seek(6);assert.equal(await text('adjustmentHorizontal'),'0.00°','Reset callbacks do not inherit the last save.');
  await seek(7);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/Fitted 3D/);
  await seek(8);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/metadata unavailable/);
  const report=await exportJSON('adjustmentJson');assert.equal(report.models.length,3);assert.equal(report.models.find(m=>m.id==='manual-a').horizontal_degrees,3.7);assert.match(report.limitation,/not measured accuracy/);assert.deepEqual(report.transitions,[]);
  const pending=page.waitForEvent('download');await page.locator('#adjustmentCsv').click();const csv=fs.readFileSync(await(await pending).path(),'utf8');assert.match(csv,/3\.7/);assert.match(csv,/NOT_measured_accuracy/);
  const bundle=await exportJSON('bundle');assert.equal(bundle.gaze_adjustment_report.models.length,3);assert.equal(bundle.gaze[0].table[1][5],'123.456','Raw scene-image X remains unchanged.');
  await page.locator('#gazeAdjustmentReport details').evaluate(e=>e.open=true);await page.locator('#adjustmentHistory button').filter({hasText:'Saved'}).click();assert.equal(await page.locator('#cursor').inputValue(),'3');
  await seek(2.05);assert.equal(await page.locator('#gazeAdjustmentReport').evaluate(e=>!!e.closest('.review-left')),true);await page.evaluate(()=>{window.scrollTo(0,0);const left=document.querySelector('.review-left'),report=document.getElementById('gazeAdjustmentReport');left.scrollTop+=report.getBoundingClientRect().top-left.getBoundingClientRect().top-20;});await page.screenshot({path:path.join(out,'synthetic-offset-review.png'),fullPage:false});
  await page.setViewportSize({width:390,height:1000});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true,'Mobile layout must not overflow.');
  const reset=structuredClone(data);reset.events.push(['5.07','official_gaze_mode_selected','neon_xr','synthetic reset']);await page.goto(pathToFileURL(write('synthetic-reset.html',reset)).href);await seek(5.08);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentActive'),/changed after the last callback/);assert.equal((await exportJSON('adjustmentJson')).transitions.length,1);
  const legacy=structuredClone(data);delete legacy.gazeAdjustments;legacy.gazeTargets=[['session_time_s','ray_valid','status'],['1','true','no_target']];await page.goto(pathToFileURL(write('synthetic-legacy.html',legacy)).href);await seek(1);assert.equal(await text('adjustmentHorizontal'),'Not available');assert.match(await text('adjustmentWarning'),/older replay/);assert.match(await text('adjustmentActive'),/did not record correction IDs/);
  assert.deepEqual(errors,[]);assert.deepEqual(network,[]);console.log('PASS: offset history, per-callback signs/model/session/binding, invalid and stale gaps, official reset, fitted/legacy missing metadata, unchanged raw XY, eye-time basis separation, JSON/CSV/bundle, timeline seeking and mobile layout.');
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
