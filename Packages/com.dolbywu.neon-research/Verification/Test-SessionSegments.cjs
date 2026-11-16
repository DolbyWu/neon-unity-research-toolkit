// Synthetic only. Original session clocks and full archive remain available.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url'),{chromium}=require('playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/session-segments');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const header=['session_time_s','source','ray_valid','status','target_id','target_name','target_group','mapping_verified'];
 const data={session:'SYNTHETIC',participant:'SYNTHETIC',duration:10,gaze:[],events:[['session_time_s','event_type','object_id'],['1','grab_start','tutorial'],['5','grab_start','study']],regions:[['start_session_s','end_session_s','region','source'],['0','4','Tutorial','test'],['4','10','Pre-room','test']],gazeTargets:[header,['1','synthetic','True','target_hit','tutorial','Tutorial object','props','False'],['5','synthetic','True','target_hit','study','Study object','props','False']],targetCatalog:[['target_id','target_name','target_group'],['tutorial','Tutorial object','props'],['study','Study object','props']],analysisSegments:{schema:'gazeflow-session-segments-v1',session_id:'SYNTHETIC',enabled:true,study_start_s:4,session_end_s:10,boundary_region:'Pre-room',primary_analysis_available:true,manifest_relative:'../Analysis/SessionSegments-synthetic/segments.json'}};
 const file=path.join(out,'synthetic-scopes.html');fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(data)));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1500,height:1100},acceptDownloads:true}),errors=[],requests=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});await page.goto(pathToFileURL(file).href);
  assert.equal(await page.locator('#error').innerText(),'');assert.equal(await page.locator('#start').inputValue(),'4');assert.equal(await page.locator('#cursor').inputValue(),'4');
  await page.locator('#targetDisclosure>summary').click();assert.match(await page.locator('#targetQuality').innerText(),/1 callbacks/);assert.match(await page.locator('#targetSummary').innerText(),/Study object/);assert.doesNotMatch(await page.locator('#targetSummary').innerText(),/Tutorial object/);
  async function bundle(){const pending=page.waitForEvent('download');await page.locator('#bundle').click();return JSON.parse(fs.readFileSync(await(await pending).path(),'utf8'));}
  let b=await bundle();assert.equal(b.analysis_scope.segment,'study');assert.equal(b.analysis_scope.primary_analysis_eligible,true);assert.equal(b.gaze_targets.length,2);assert.equal(b.start_s,4);
  await page.locator('#scopeTutorial').click();b=await bundle();assert.equal(b.analysis_scope.segment,'tutorial');assert.equal(b.analysis_scope.primary_analysis_eligible,false);assert.equal(b.gaze_targets[1][0],'1');
  await page.locator('#annotationUseCursor').click();assert.equal(await page.locator('#annotationStart').inputValue(),'0');
  await page.locator('#scopeFull').click();b=await bundle();assert.equal(b.analysis_scope.segment,'mixed_tutorial_and_study');assert.equal(b.gaze_targets.length,3);assert.equal(b.analysis_scope.primary_analysis_eligible,false);
  await page.locator('#scopeStudy').click();assert.equal(await page.locator('#cursor').inputValue(),'4');
  await page.screenshot({path:path.join(out,'study-default.png'),fullPage:false});
  data.analysisSegments.study_start_s=10;data.analysisSegments.primary_analysis_available=false;const only=path.join(out,'tutorial-only.html');fs.writeFileSync(only,template.replace('__STUDY_DATA__',JSON.stringify(data)));await page.goto(pathToFileURL(only).href);assert.equal(await page.locator('#scopeStudy').isDisabled(),true);assert.equal(await page.locator('#bundle').isDisabled(),true);await page.locator('#scopeTutorial').click();assert.equal((await bundle()).gaze_targets.length,3);
  assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);console.log('PASS: Study default, half-open boundary, Tutorial retained, original annotation time, full-session diagnostics, bundle provenance and tutorial-only safeguard.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
