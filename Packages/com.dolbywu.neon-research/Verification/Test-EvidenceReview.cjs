// Synthetic-only evidence review and provenance acceptance; no participant files or uploads.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/evidence-review');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const data={session:'SYNTHETIC-REVIEW',participant:'SYNTHETIC-ONLY',duration:12,events:[['event_id','session_time_s','event_type']],regions:[['start_session_s','end_session_s','region']],gaze:[],spatialValidations:[{id:'synthetic-validation',calibration_id:'synthetic-mount',passed:false,aborted:false,max_mean_error_deg:2,min_coverage:.7,targets:[{target_index:0,mean_error_deg:null,coverage:0}]}]};
 const file=path.join(out,'evidence-review.html');fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(data)));
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({acceptDownloads:true});const errors=[],requests=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});page.on('dialog',d=>d.accept());
  await page.goto(pathToFileURL(file).href);await page.locator('#annotationAdvanced').evaluate(e=>e.open=true);
  assert.match(await page.locator('#spatialValidationReview').innerText(),/MEASURED FAIL.*mean missing/);
  const exportJson=async()=>{const pending=page.waitForEvent('download');await page.locator('#annotationsExport').click();return JSON.parse(fs.readFileSync(await(await pending).path(),'utf8'));};
  const importJson=async(b,id='annotationsImport')=>page.locator('#'+id).setInputFiles({name:'synthetic.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(b))});
  const create=async(author,label,intensity)=>{
   await page.locator('#annotationStart').fill('1');await page.locator('#annotationEnd').fill('4');await page.locator('#annotationLabel').fill(label);await page.locator('#annotationAuthor').fill(author);await page.locator('#annotationSource').selectOption('participant_self_report');await page.locator('#annotationIntensity').fill(intensity);await page.locator('#annotationReview').selectOption('reviewed');await page.locator('#annotationNotes').fill('Synthetic evidence');await page.locator('#annotationSave').click();
  };
  await create('P','Confusion','0');let bundle=await exportJson();assert.equal(bundle.schema,'neon-study-annotations-v2');assert.equal(bundle.annotations[0].participant_intensity,0);assert.equal(bundle.annotations[0].confidence,'unspecified');
  const id=bundle.annotations[0].id;await page.locator(`[data-annotation-id="${id}"]`).getByRole('button',{name:'Edit',exact:true}).click();assert.equal(await page.locator('#annotationIntensity').inputValue(),'0');assert.equal(await page.locator('#annotationReview').inputValue(),'reviewed');await page.locator('#annotationIntensity').fill('3');await page.locator('#annotationNotes').fill('Revised, original report retained');await page.locator('#annotationSave').click();
  bundle=await exportJson();assert.equal(bundle.annotations[0].record_revision,2);assert.equal(bundle.annotations[0].revisions[0].participant_intensity,0);assert.equal(bundle.annotations[0].participant_intensity,3);
  const other=JSON.parse(JSON.stringify(bundle));other.annotations[0].annotator_id='R';other.annotations[0].label='Stuck';other.annotations[0].participant_intensity=null;other.annotations[0].source='researcher_observation';
  await importJson(other,'annotationsMerge');assert.match(await page.locator('#annotationMessage').innerText(),/Merged annotations/);bundle=await exportJson();assert.equal(bundle.annotations.length,2);assert.equal(bundle.annotations[0].id,id);assert.equal(bundle.annotations[0].label,'Confusion');assert.equal(bundle.annotations[1].review_status,'disputed');assert.ok(bundle.annotations[1].related_ids.includes(id));assert.equal(bundle.annotations[1].revisions.length,1);
  await page.locator('#annotationComparison').evaluate(e=>e.parentElement.open=true);assert.match(await page.locator('#annotationComparison').innerText(),/DIFFERENT LABELS/);
  await page.locator('#annotationComparison button').first().click();assert.match(await page.locator('#annotationRelated').inputValue(),new RegExp(id));await page.locator('#annotationLabel').fill('Uncertain');await page.locator('#annotationAuthor').fill('Reconciler');await page.locator('#annotationSave').click();bundle=await exportJson();assert.equal(bundle.annotations.length,3);assert.equal(bundle.annotations[2].related_ids.length,2);assert.equal(bundle.annotations[0].label,'Confusion');
  await page.reload();await importJson(bundle);assert.match(await page.locator('#annotationMessage').innerText(),/Loaded 3 annotations/);let restored=await exportJson();assert.deepEqual(restored.annotations,bundle.annotations);
  await page.locator(`[data-annotation-id="${id}"]`).getByRole('button',{name:'Delete',exact:true}).click();restored=await exportJson();assert.equal(restored.annotations.length,2);assert.equal(restored.deleted_annotations[0].record.id,id);assert.equal(restored.deleted_annotations[0].record.revisions.length,1);await importJson(restored);assert.equal((await exportJson()).deleted_annotations.length,1);
  const bad=JSON.parse(JSON.stringify(restored));bad.annotations[0].participant_intensity=11;await importJson(bad);assert.match(await page.locator('#annotationMessage').innerText(),/Import rejected/);assert.equal((await exportJson()).annotations.length,2);
  const legacy=JSON.parse(JSON.stringify(bundle));legacy.schema='neon-study-annotations-v1';delete legacy.deleted_annotations;for(const a of legacy.annotations)for(const k of ['participant_intensity','review_status','record_revision','revisions','related_ids','intensity_source'])delete a[k];await importJson(legacy);const migrated=await exportJson();assert.equal(migrated.annotations[0].review_status,'draft');assert.equal(migrated.annotations[0].participant_intensity,null);
  assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
  await page.screenshot({path:path.join(out,'review.png'),fullPage:true});
  console.log('PASS: v2 intensity/confidence separation, history, conflict-preserving merge, comparison/reconciliation, deletion audit, JSON roundtrip, invalid-value rejection, v1 migration, calibration review; no network.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
