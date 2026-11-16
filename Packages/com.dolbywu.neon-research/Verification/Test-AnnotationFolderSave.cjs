// Synthetic sessions only. Picker/permission failures are injected; accepted writes use a
// bounded local fixture directory. A second test uses native Chromium filesystem handles.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url'),{chromium}=require('playwright');
(async()=>{
 const core=path.resolve(__dirname,'..'),base=path.resolve(process.env.GAZEFLOW_TEST_OUTPUT||path.join(core,'../../Artifacts/annotation-folder-save'));
 fs.mkdirSync(base,{recursive:true});const out=fs.mkdtempSync(path.join(base,'synthetic-'));
 const data={session:'SYNTHETIC-FOLDER-SAVE',participant:'SYNTHETIC-PARTICIPANT',duration:12,gaze:[],events:[['session_time_s','event_type','object_id']],regions:[['start_session_s','end_session_s','region']]};
 const manifest={closed:true,sessionId:data.session,participantId:data.participant,durationSeconds:data.duration};
 const folders={};
 for(const [key,change]of Object.entries({good:{},wrong:{sessionId:'OTHER'},participant:{participantId:'OTHER'},duration:{durationSeconds:13},open:{closed:false}})){
  const dir=path.join(out,key);fs.mkdirSync(path.join(dir,'Replay'),{recursive:true});fs.writeFileSync(path.join(dir,'export_manifest.json'),JSON.stringify({...manifest,...change}));folders[key]=dir;
 }
 const sentinel=path.join(folders.good,'Replay','earlier-annotations.json');fs.writeFileSync(sentinel,'{"keep":"earlier version"}');
 const template=fs.readFileSync(path.join(core,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8'),report=path.join(out,'synthetic-folder-replay.html');
 fs.writeFileSync(report,template.replace('__STUDY_DATA__',JSON.stringify(data)));
 const annotationFiles=key=>fs.readdirSync(path.join(folders[key],'Replay')).filter(n=>n.startsWith(data.session+'-annotations-'));
 const latest=()=>annotationFiles('good').flatMap(name=>{try{return[{name,bundle:JSON.parse(fs.readFileSync(path.join(folders.good,'Replay',name),'utf8'))}];}catch{return[];}}).sort((a,b)=>b.bundle.revision-a.bundle.revision)[0];
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1450,height:1200},acceptDownloads:true}),errors=[],network=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))network.push(r.url());});page.on('dialog',d=>d.accept());
  await page.exposeFunction('annotationFixtureFs',({op,key,name,text})=>{
   assert.ok(Object.hasOwn(folders,key));assert.ok(!name||/^[A-Za-z0-9_.-]+$/.test(name));
   const file=path.join(folders[key],'Replay',name||'');
   if(op==='manifest')return fs.readFileSync(path.join(folders[key],'export_manifest.json'),'utf8');
   if(op==='exists')return fs.existsSync(file);
   if(op==='read')return fs.readFileSync(file,'utf8');
   if(op==='create'){fs.writeFileSync(file,'',{flag:'wx'});return;}
   if(op==='write'){fs.writeFileSync(file,text);return;}
   throw Error('Unknown fixture operation');
  });
  await page.addInitScript(()=>{
   window.nativeAnnotationPickerAvailable=typeof window.showDirectoryPicker==='function';
   window.annotationPickerKey='good';window.annotationWriteDelay=0;window.annotationFailWrite=false;window.annotationCorruptRead=false;window.annotationPickerCalls=0;
   const missing=()=>{throw new DOMException('Synthetic entry not found','NotFoundError');};
   const fileHandle=(key,name)=>({kind:'file',name,async getFile(){const text=await window.annotationFixtureFs({op:'read',key,name});return new File([window.annotationCorruptRead?text+'!':text],name);},async createWritable(){if(window.annotationFailWrite)throw new DOMException('Synthetic permission denied','NotAllowedError');let pending='';return {async write(text){await new Promise(r=>setTimeout(r,window.annotationWriteDelay));pending=text;},async close(){await window.annotationFixtureFs({op:'write',key,name,text:pending});},async abort(){}};}});
   const replay=key=>({kind:'directory',name:'Replay',async getFileHandle(name,opts={}){if(!await window.annotationFixtureFs({op:'exists',key,name})){if(!opts.create)return missing();await window.annotationFixtureFs({op:'create',key,name});}return fileHandle(key,name);}});
   const root=key=>({kind:'directory',name:'synthetic-'+key,async getFileHandle(name){if(name!=='export_manifest.json')return missing();const text=await window.annotationFixtureFs({op:'manifest',key});return {async getFile(){return new File([text],name);}};},async getDirectoryHandle(name){if(name!=='Replay')return missing();return replay(key);}});
   window.showDirectoryPicker=async options=>{window.annotationPickerCalls++;if(options.mode!=='readwrite')throw Error('Write permission must be explicit.');if(window.annotationPickerKey==='cancel')throw new DOMException('Cancelled','AbortError');if(window.annotationNativeRoot)return window.annotationNativeRoot;return root(window.annotationPickerKey);};
  });
  await page.goto(pathToFileURL(report).href);assert.equal(await page.locator('#error').textContent(),'');
  assert.equal(await page.evaluate(()=>window.nativeAnnotationPickerAvailable),true,'Local file in installed Edge exposes the native folder picker.');
  const idle=()=>page.waitForFunction(()=>!document.getElementById('annotationsSaveFolder').disabled);
  const status=fragment=>page.waitForFunction(s=>document.getElementById('annotationFolderStatus').textContent.includes(s),fragment);
  const add=async(t,note)=>{
   await page.locator('#cursor').evaluate((e,v)=>{e.value=v;e.dispatchEvent(new Event('input'));},String(t));
   await page.locator('#annotationUseCursor').click();await page.locator('#annotationAuthor').fill('R-SYNTHETIC');await page.locator('#annotationNotes').fill(note);await page.locator('[data-rating="7"]').click();await page.locator('#annotationSave').click();
  };
  await add(1,'First player explanation');assert.equal(annotationFiles('good').length,0,'Adding before folder authorization never writes.');
  await page.evaluate(()=>window.annotationPickerKey='cancel');await page.locator('#annotationsSaveFolder').click();await idle();assert.equal(annotationFiles('good').length,0);
  for(const key of ['wrong','participant','duration','open']){
   await page.evaluate(k=>window.annotationPickerKey=k,key);await page.locator('#annotationsSaveFolder').click();await status('does not match');await idle();assert.equal(annotationFiles(key).length,0);
  }
  await page.evaluate(()=>window.annotationPickerKey='good');await page.locator('#annotationsSaveFolder').click();await status('Last verified save:');await idle();
  assert.equal(annotationFiles('good').length,1);let saved=latest().bundle;
  assert.equal(saved.session,data.session);assert.equal(saved.participant_id,data.participant);assert.equal(saved.annotations.length,1);assert.equal(saved.annotations[0].participant_intensity,7);assert.equal(saved.annotations[0].notes,'First player explanation');
  assert.equal(fs.readFileSync(sentinel,'utf8'),'{"keep":"earlier version"}');
  const pickerCalls=await page.evaluate(()=>window.annotationPickerCalls);
  await add(2,'Second player explanation');await idle();assert.equal(annotationFiles('good').length,2);assert.equal(latest().bundle.annotations.length,2);assert.equal(await page.evaluate(()=>window.annotationPickerCalls),pickerCalls,'Subsequent annotation save reuses explicit permission.');
  await page.locator('#annotationList').getByRole('button',{name:'Edit',exact:true}).first().click();await page.locator('#annotationNotes').fill('Edited explanation');await page.locator('#annotationSave').click();await idle();assert.equal(latest().bundle.annotations[0].notes,'Edited explanation');assert.equal(latest().bundle.annotations[0].revisions.length,1);
  await page.locator('#annotationList').getByRole('button',{name:'Delete',exact:true}).last().click();await idle();assert.equal(latest().bundle.annotations.length,1);assert.equal(latest().bundle.deleted_annotations.length,1);
  // Failure never reports success, loses the current list, or overwrites an earlier JSON.
  const beforeFailure=annotationFiles('good').length;
  await page.evaluate(()=>window.annotationFailWrite=true);await add(3,'Retained after denied write');await status('NOT SAVED');await idle();assert.equal(await page.locator('#annotationList tr[data-annotation-id]').count(),2);
  await page.evaluate(()=>window.annotationFailWrite=false);await page.locator('#annotationsSaveFolder').click();await idle();assert.doesNotMatch(await page.locator('#annotationFolderStatus').textContent(),/NOT SAVED/);assert.equal(latest().bundle.annotations.length,2);
  assert.ok(annotationFiles('good').length>beforeFailure);
  await page.evaluate(()=>window.annotationCorruptRead=true);await page.locator('#annotationsSaveFolder').click();await status('read-back check');await idle();
  await page.evaluate(()=>window.annotationCorruptRead=false);await page.locator('#annotationsSaveFolder').click();await idle();
  // Rapid accepted changes are serialized as complete independent snapshots.
  await page.evaluate(()=>window.annotationWriteDelay=450);await add(4,'Queued first');await add(5,'Queued second');await idle();assert.equal(latest().bundle.annotations.length,4);assert.equal(latest().bundle.annotations.at(-1).notes,'Queued second');
  await page.locator('#annotationsFolderAuto').uncheck();const beforeOptOut=annotationFiles('good').length;await add(6,'Manual save only');await page.waitForTimeout(200);assert.equal(annotationFiles('good').length,beforeOptOut);await page.locator('#annotationsSaveFolder').click();await idle();assert.equal(latest().bundle.annotations.length,5);
  // A pending draft is not represented as saved. Explicit save refuses it.
  await page.locator('#annotationNotes').fill('Unapplied draft');const beforeDraft=annotationFiles('good').length;await page.locator('#annotationsSaveFolder').click();assert.match(await page.locator('#annotationMessage').textContent(),/Save or cancel/);assert.equal(annotationFiles('good').length,beforeDraft);await page.locator('#annotationNew').click();
  saved=latest().bundle;await page.reload();assert.match(await page.locator('#annotationFolderStatus').textContent(),/Select this session folder/);assert.equal(await page.locator('#annotationList tr[data-annotation-id]').count(),0,'Reload does not silently load or persist annotations.');
  await page.locator('#annotationsImport').setInputFiles({name:'saved-annotations.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(saved))});assert.equal(await page.locator('#annotationList tr[data-annotation-id]').count(),5);
  // Real browser-native handles exercise createWritable, close and read-back (OPFS
  // is test storage only; the production code exclusively uses the chosen folder).
  // OPFS deliberately rejects file: origins. A fulfilled loopback page supplies a
  // secure synthetic origin without running a server or making a network request.
  const nativePage=await browser.newPage({viewport:{width:1450,height:1200}});nativePage.on('pageerror',e=>errors.push(e.message));
  await nativePage.route('http://127.0.0.1:17777/**',route=>route.fulfill({contentType:'text/html',body:template.replace('__STUDY_DATA__',JSON.stringify(data))}));
  await nativePage.goto('http://127.0.0.1:17777/synthetic');
  await nativePage.locator('#annotationsImport').setInputFiles({name:'saved-annotations.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(saved))});
  await nativePage.waitForFunction(()=>document.querySelectorAll('#annotationList tr[data-annotation-id]').length===5);
  const native=await nativePage.evaluate(async m=>{
   const root=await navigator.storage.getDirectory(),dir=await root.getDirectoryHandle('synthetic-annotation-'+crypto.randomUUID(),{create:true});
   const handle=await dir.getFileHandle('export_manifest.json',{create:true}),writer=await handle.createWritable();await writer.write(JSON.stringify(m));await writer.close();await dir.getDirectoryHandle('Replay',{create:true});window.annotationNativeRoot=dir;window.showDirectoryPicker=async()=>dir;return dir.name;
  },manifest);
  await nativePage.locator('#annotationsSaveFolder').click();await nativePage.waitForFunction(()=>document.getElementById('annotationFolderStatus').textContent.includes('Last verified save:')&&!document.getElementById('annotationsSaveFolder').disabled);
  const nativeSaved=await nativePage.evaluate(async()=>{const replay=await window.annotationNativeRoot.getDirectoryHandle('Replay'),result=[];for await(const [name,handle]of replay.entries())result.push({name,text:await(await handle.getFile()).text()});return result;});
  assert.equal(nativeSaved.length,1);assert.equal(JSON.parse(nativeSaved[0].text).annotations.length,5);assert.match(await nativePage.locator('#annotationFolderStatus').textContent(),new RegExp(native));
  // Unsupported browser retains an explicit download fallback.
  await page.evaluate(()=>{window.showDirectoryPicker=undefined;document.getElementById('annotationsFolderAuto').checked=false;});
  await page.locator('#videoOffset').fill('1');await page.locator('#applyVideoOffset').click();assert.equal(await page.locator('#annotationsSaveFolder').isDisabled(),true);assert.match(await page.locator('#annotationFolderStatus').textContent(),/unavailable/);
  const downloaded=page.waitForEvent('download');await page.locator('#annotationsExport').click();const backup=JSON.parse(fs.readFileSync(await(await downloaded).path(),'utf8'));assert.equal(backup.annotations.length,5);assert.equal(backup.video.offset_s,1);
  assert.deepEqual(errors,[]);assert.deepEqual(network,[]);assert.equal(fs.readFileSync(sentinel,'utf8'),'{"keep":"earlier version"}');
  console.log('PASS: folder permission/cancel, session/participant/duration/closed checks, real fixture writes, JSON/ratings/revisions/deletion audit, automatic and manual saves, denial/retry/read-back failure, serialized changes, draft/reload/import protection, native filesystem-handle write/read, no-overwrite, download fallback and no network.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
