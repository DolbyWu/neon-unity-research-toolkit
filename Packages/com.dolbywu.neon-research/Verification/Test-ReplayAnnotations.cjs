// Synthetic fixtures only: no participant records, desktop capture or microphone access.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url'),{spawnSync}=require('node:child_process');
const {chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const project=path.resolve(__dirname,'..'),root=path.resolve(project,'../../Artifacts/replay-label-unit');
 const replay=path.join(root,'Replay'),videos=path.join(root,'Video');fs.mkdirSync(replay,{recursive:true});fs.mkdirSync(videos,{recursive:true});
 const template=fs.readFileSync(path.join(project,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const data={version:1,session:'20990101T000000000Z-abcdef01',participant:'SYNTHETIC-ONLY',duration:12,media:{video_offset_s:2},
  events:[['event_id','session_time_s','event_type','object_id','detail'],['1','3','grab_start','synthetic-cup','Synthetic event']],
  regions:[['start_session_s','end_session_s','region','source'],['0','12','Synthetic room','synthetic']],gaze:[]};
 const report=path.join(replay,'study-replay.html');fs.writeFileSync(report,template.replace('__STUDY_DATA__',JSON.stringify(data).replace(/</g,'\\u003c')));
 const ffmpeg=process.env.STUDY_FFMPEG;if(!ffmpeg)throw Error('Set STUDY_FFMPEG to an installed FFmpeg executable; this test never downloads one.');
 const videoPath=path.join(videos,'quest-view.mp4');
 const generated=spawnSync(ffmpeg,['-hide_banner','-loglevel','error','-y','-f','lavfi','-i','testsrc2=size=320x180:rate=30','-t','8','-c:v','libx264','-pix_fmt','yuv420p',videoPath],{encoding:'utf8',windowsHide:true});
 assert.equal(generated.status,0,generated.stderr);
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1100},acceptDownloads:true});
  const errors=[],requests=[];let acceptDialogs=true;
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});
  page.on('dialog',d=>acceptDialogs?d.accept():d.dismiss());
  const message=fragment=>page.waitForFunction(s=>document.getElementById('annotationMessage').textContent.includes(s),fragment);
  const count=()=>page.locator('#annotationList tr[data-annotation-id]').count();
  const seek=async t=>{await page.locator('#cursor').evaluate((e,value)=>{e.value=value;e.dispatchEvent(new Event('input',{bubbles:true}));},String(t));};
  const upload=async bundle=>{await page.locator('#annotationsImport').setInputFiles({name:'synthetic-annotations.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(bundle))});};
  const exportJson=async()=>{const pending=page.waitForEvent('download');await page.locator('#annotationsExport').click();const file=await pending;return JSON.parse(fs.readFileSync(await file.path(),'utf8'));};
  await page.goto(pathToFileURL(report).href);assert.equal(await page.locator('#error').innerText(),'');assert.equal(await count(),0);
  assert.deepEqual(await page.locator('.review-data > .panel').evaluateAll(panels=>panels.map(p=>p.id)),['timelinePanel','annotationPanel','intervalExportPanel','targetPanel','gameLogPanel'],'Annotations immediately follow eye data; targets are available and game log stays last.');
  await page.locator('#annotationAdvanced').evaluate(e=>{e.open=true;});
  // Missing required fields and invalid bounds cannot create a record.
  await page.locator('#annotationSave').click();await message('required');assert.equal(await count(),0);
  await page.locator('#annotationLabel').fill('Confusion');await page.locator('#annotationAuthor').fill('R001');
  await page.locator('#annotationStart').fill('4');await page.locator('#annotationEnd').fill('3');await page.locator('#annotationSave').click();await message('0 <= start');
  // Interval, provenance and exact quotation survive JSON round-trip.
  await page.locator('#start').fill('1');await page.locator('#end').fill('4');await page.locator('#end').dispatchEvent('change');
  await page.locator('#annotationUseSelection').click();await page.locator('#annotationSource').selectOption('retrospective_interview');await page.locator('#annotationConfidence').selectOption('medium');
  await page.locator('#annotationQuote').fill('=HYPERLINK("example","not a formula")');
  await page.locator('#annotationEvidence').fill('Repeated grasp of synthetic cup.');await page.locator('#annotationNotes').fill('Participant reports uncertainty; interpretation remains provisional.');
  await page.locator('#annotationSave').click();await message('Annotation added');assert.equal(await count(),1);
  await page.locator('#basis').selectOption('receive');let original=await exportJson();assert.equal(original.time_base,'session_seconds');assert.equal(original.annotations[0].start_s,1);assert.equal(original.annotations[0].end_s,4);assert.equal(original.annotations[0].source,'retrospective_interview');assert.equal(original.video.offset_s,2);
  const firstId=original.annotations[0].id,created=original.annotations[0].created_utc;
  // Point and injection strings are literal text, never markup.
  await seek(6);await page.locator('#annotationUseCursor').click();await page.locator('#annotationLabel').fill('<img src=x onerror=alert(1)>');await page.locator('#annotationSave').click();assert.equal(await count(),2);assert.equal(await page.locator('img').count(),0);
  let two=await exportJson();assert.equal(two.annotations[1].start_s,6);assert.equal(two.annotations[1].end_s,6);
  const first=page.locator(`tr[data-annotation-id="${firstId}"]`);
  await first.getByRole('button',{name:'Edit',exact:true}).click();await page.locator('#annotationNotes').fill('Edited interpretation');await page.locator('#annotationSave').click();
  let edited=await exportJson();assert.equal(edited.annotations[0].created_utc,created);assert.equal(edited.annotations[0].id,firstId);assert.equal(edited.annotations[0].notes,'Edited interpretation');
  assert.ok(edited.revision>original.revision);
  await page.locator(`tr[data-annotation-id="${two.annotations[1].id}"]`).getByRole('button',{name:'Delete',exact:true}).click();assert.equal(await count(),1);
  // A valid import needs explicit replacement; cancellation preserves current records.
  acceptDialogs=false;await upload(original);await page.waitForTimeout(100);assert.match(await first.innerText(),/Edited interpretation/);
  acceptDialogs=true;await upload(original);await message('Loaded 1 annotations');assert.match(await first.innerText(),/interpretation remains provisional/);
  // Reject wrong sessions, schema, malformed records and duplicate IDs atomically.
  const invalid=[{...original,session:'OTHER'},{...original,participant_id:'OTHER'},{...original,duration_s:13},{...original,schema:'future-format'},
   {...original,annotations:[{...original.annotations[0],start_s:'1'}]},
   {...original,annotations:[{...original.annotations[0],end_s:13}]},
   {...original,annotations:[original.annotations[0],original.annotations[0]]}];
  for(const bad of invalid){await upload(bad);await message('Import rejected');assert.equal(await count(),1);assert.match(await first.innerText(),/interpretation remains provisional/);}
  const csvPending=page.waitForEvent('download');await page.locator('#annotationsCsv').click();const csv=fs.readFileSync(await (await csvPending).path(),'utf8');assert.match(csv,/"'=HYPERLINK/);
  // Unapplied form edits must never be mistaken for exported annotations.
  await page.locator('#annotationLabel').fill('Unsaved draft');await page.locator('#annotationsExport').click();await message('Save or cancel');await page.locator('#annotationNew').click();
  // Both the known relative path and manual file picker work offline.
  await page.locator('#loadSessionVideo').click();await page.waitForFunction(()=>document.getElementById('sessionVideo').readyState>=1);
  await seek(5);await page.waitForFunction(()=>Math.abs(document.getElementById('sessionVideo').currentTime-3)<.12);
  await page.locator('#sessionVideo').evaluate(v=>{v.currentTime=6;});await page.waitForFunction(()=>Math.abs(Number(document.getElementById('cursor').value)-8)<.15);
  await page.locator('#annotationUseCursor').click();assert.ok(Math.abs(Number(await page.locator('#annotationStart').inputValue())-8)<.15);
  await page.locator('#annotationNew').click();
  await page.locator('#play').click();await page.waitForTimeout(400);await page.locator('#play').click();assert.ok(Number(await page.locator('#cursor').inputValue())>8.15);
  await seek(11);assert.match(await page.locator('#videoStatus').innerText(),/NO VIDEO COVERAGE/);
  await page.locator('#videoOffset').fill('3');await page.locator('#applyVideoOffset').click();await seek(5);await page.waitForFunction(()=>Math.abs(document.getElementById('sessionVideo').currentTime-2)<.12);
  let timingSaved=await exportJson();assert.equal(timingSaved.video.offset_s,3);assert.equal(timingSaved.video.timing_method,'operator_adjusted_not_hardware_verified');
  const timingUpload=async sid=>page.locator('#videoMetadata').setInputFiles({name:'media_capture.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify({sessionId:sid,channels:{video:{estimatedSessionStartSeconds:2}}}))});
  await timingUpload('OTHER');await page.waitForFunction(()=>document.getElementById('videoStatus').textContent.includes('different session'));assert.equal(await page.locator('#videoOffset').inputValue(),'3');
  await timingUpload(data.session);await page.waitForFunction(()=>document.getElementById('videoOffset').value==='2');
  await page.locator('#videoFile').setInputFiles(videoPath);await page.waitForFunction(()=>document.getElementById('sessionVideo').readyState>=1);assert.match(await page.locator('#sessionVideo').getAttribute('src'),/^blob:/);
  await seek(4);await page.waitForFunction(()=>Math.abs(document.getElementById('sessionVideo').currentTime-2)<.12);
  // User-pause is authoritative: neither RAF nor seeking may implicitly restart playback.
  await page.waitForFunction(()=>{const v=document.getElementById('sessionVideo');return !v.seeking&&v.readyState>=2;});
  await page.locator('#videoTogglePlay').click();await page.waitForTimeout(200);
  const nativePaused=await page.locator('#sessionVideo').evaluate(v=>{v.pause();return v.currentTime;});
  await page.waitForTimeout(400);assert.equal(await page.locator('#sessionVideo').evaluate(v=>v.paused),true);
  assert.ok(Math.abs(await page.locator('#sessionVideo').evaluate(v=>v.currentTime)-nativePaused)<.05,'Native pause stays paused.');
  await page.locator('#videoTogglePlay').click();await page.waitForTimeout(200);await page.locator('#videoTogglePlay').click();
  const explicitPaused=await page.locator('#sessionVideo').evaluate(v=>v.currentTime);await page.waitForTimeout(300);
  assert.equal(await page.locator('#sessionVideo').evaluate(v=>v.paused),true);assert.ok(Math.abs(await page.locator('#sessionVideo').evaluate(v=>v.currentTime)-explicitPaused)<.05);
  await page.locator('#annotationAdvanced').evaluate(e=>{e.open=false;});
  const scrub=async t=>{await page.locator('#videoReviewSeek').evaluate((e,time)=>{e.value=time;e.dispatchEvent(new Event('input',{bubbles:true}));},String(t));await page.waitForFunction(()=>{const v=document.getElementById('sessionVideo');return !v.seeking&&v.readyState>=2;});};
  await scrub(3.456);assert.equal(await page.locator('#sessionVideo').evaluate(v=>v.paused),true);
  assert.match(await page.locator('#reviewClocks').innerText(),/Video 00:03\.456 → Experiment 00:05\.456/);
  await page.locator('#stampVideo').click();assert.equal(await page.locator('#annotationStart').inputValue(),'5.456');assert.equal(await page.locator('#annotationEnd').inputValue(),'5.456');
  assert.match(await page.locator('#annotationDraftTime').innerText(),/00:05\.456.*Video 00:03\.456/);
  await page.locator('#annotationQuickLabel').selectOption('Insight');await page.locator('#annotationNotes').fill('Synthetic frame-linked insight');await page.locator('#annotationSave').click();
  const frameLabel=await exportJson();assert.equal(frameLabel.annotations[1].video_time_s,3.456);assert.equal(frameLabel.annotations[1].video_offset_s,2);assert.equal(frameLabel.annotations[1].label,'Insight');
  await scrub(4.125);await page.locator('#stampVideo').click();await page.locator('#annotationNotes').fill('Description only: synthetic player inspects the cup.');await page.locator('#annotationSave').click();
  const frameNotes=await exportJson();assert.equal(frameNotes.annotations[2].label,'Unlabeled');assert.equal(frameNotes.annotations[2].start_s,6.125);assert.equal(frameNotes.annotations[2].video_time_s,4.125);
  await upload(frameNotes);await message('Loaded 3 annotations');assert.equal(await count(),3);
  const legacy=JSON.parse(JSON.stringify(original));for(const a of legacy.annotations){delete a.video_time_s;delete a.video_offset_s;}
  await upload(legacy);await message('Loaded 1 annotations');assert.equal(await count(),1);
  await upload({...original,annotations:[{...original.annotations[0],start_s:4,end_s:4,video_time_s:3,video_offset_s:2}]});await message('Import rejected');assert.equal(await count(),1);
  // Save several new points consecutively WITHOUT exporting in between; never replace the first.
  for(const [time,label]of [[1.1,'Confusion'],[2.2,'Engagement'],[3.3,'Insight']]){
   await scrub(time);await page.locator('#annotationNext').click();assert.match(await page.locator('#annotationMode').innerText(),/NEW annotation/);
   await page.locator('#annotationQuickLabel').selectOption(label);await page.locator('#annotationSave').click();
  }
  assert.equal(await count(),4);const consecutive=await exportJson();assert.equal(consecutive.annotations[0].id,firstId);assert.equal(consecutive.annotations[0].notes,original.annotations[0].notes);
  assert.equal(new Set(consecutive.annotations.map(a=>a.id)).size,4);assert.deepEqual(consecutive.annotations.slice(1).map(a=>a.label),['Confusion','Engagement','Insight']);
  await first.getByRole('button',{name:'Edit',exact:true}).click();assert.match(await page.locator('#annotationMode').innerText(),/EDITING/);
  await scrub(4.4);await page.locator('#annotationNext').click();await page.locator('#annotationNotes').fill('New note after leaving edit mode');await page.locator('#annotationSave').click();
  assert.equal(await count(),5);const afterEdit=await exportJson();assert.equal(afterEdit.annotations[0].notes,original.annotations[0].notes);assert.equal(afterEdit.annotations[4].label,'Unlabeled');
  // No browser cache required: reload loses the in-memory list; JSON restores it and the offset.
  await page.reload();assert.equal(await count(),0);await upload(timingSaved);await message('Loaded 1 annotations');assert.equal(await page.locator('#videoOffset').inputValue(),'3');
  assert.equal(await page.locator('#sessionVideo').getAttribute('src'),null);assert.match(await first.innerText(),/interpretation remains provisional/);
  assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
  await page.locator('#loadSessionVideo').click();await page.waitForFunction(()=>document.getElementById('sessionVideo').readyState>=1);await seek(5);
  await page.waitForFunction(()=>{const v=document.getElementById('sessionVideo');return v.readyState>=2&&!v.seeking&&Math.abs(v.currentTime-2)<.12;});
  await page.screenshot({path:path.join(root,'annotated-video-replay.png'),fullPage:true});
  await page.locator('#annotationPanel').scrollIntoViewIfNeeded();
  const videoBounds=await page.locator('#sessionVideo').boundingBox();assert.ok(videoBounds.y>=0&&videoBounds.y+videoBounds.height<=1100,'Video stays visible beside the annotation form.');
  await page.screenshot({path:path.join(root,'annotated-video-review-desktop.png')});
  await page.setViewportSize({width:390,height:844});
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1),'Mobile layout does not overflow horizontally.');
  await page.locator('#annotationPanel').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(root,'annotated-video-review-mobile.png')});
  console.log('PASS: consecutive additions without export, five independent IDs/records, first annotation preserved, explicit exit from edit mode, authoritative pause, paused scrubbing, frame-linked clocks/labels, description-only notes, legacy import, local video, JSON reload, validation, CSV escaping, no network.');
  console.log('Synthetic preview: '+report);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
