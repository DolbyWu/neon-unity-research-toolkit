// DOM integration checks without launching a browser or accessing real recordings.
// Canvas/media are inert substitutes: these checks do not validate rendering or video playback.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {JSDOM,VirtualConsole}=require(process.env.STUDY_JSDOM||'jsdom');
(async()=>{
 const root=path.resolve(__dirname,'..'),template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const header=['session_time_s','source','device_timestamp_ms','rtcp_synchronized','ray_valid','status','target_id','target_group','target_label','hit_x','hit_y','hit_z','calibration_id','timestamp_semantics'];
 const row=(t,id,valid=true,status='target_hit')=>[String(t),'synthetic','123','False',String(valid),status,id,'test','Synthetic','','','','test','unity_callback_time_NOT_device_time_or_hardware_sync'];
 const data={session:'SYNTHETIC TARGET PROTOTYPE — NOT PARTICIPANT DATA',participant:'SYNTHETIC',duration:5,events:[['event_id','session_time_s','region','event_type','puzzle','object_id','status','outcome','detail'],['1','2','Lab','grab_start','','key','','','Synthetic Left hand']],regions:[['start_session_s','end_session_s','region','source']],gaze:[],gazeTargets:[header,row(1,'key'),row(1.05,'key'),row(1.1,'',false,'stale_or_disconnected'),row(2,'door'),row(2.05,'door'),row(3,'</script><img src=x onerror=alert(1)>')]};
 function create(d){
  const errors=[],downloads=[],blobs=new Map(),vc=new VirtualConsole();vc.on('jsdomError',e=>errors.push(e.message));
  const html=template.replace('__STUDY_DATA__',JSON.stringify(d).replace(/</g,'\\u003c').replace(/>/g,'\\u003e').replace(/&/g,'\\u0026'));
  const dom=new JSDOM(html,{runScripts:'dangerously',url:'https://synthetic.invalid/local-review',virtualConsole:vc,beforeParse(w){
   w.requestAnimationFrame=()=>0;w.confirm=()=>true;w.Blob=Blob;w.HTMLElement.prototype.scrollIntoView=()=>{};
   const context=new Proxy({},{get:(o,k)=>k==='measureText'?()=>({width:0}):o[k]||(()=>{}),set:(o,k,v)=>(o[k]=v,true)});
   w.HTMLCanvasElement.prototype.getContext=()=>context;w.HTMLMediaElement.prototype.pause=()=>{};w.HTMLMediaElement.prototype.load=()=>{};
   w.URL.createObjectURL=b=>{const id='blob:synthetic-'+blobs.size;blobs.set(id,b);return id;};w.URL.revokeObjectURL=()=>{};
   w.HTMLAnchorElement.prototype.click=function(){downloads.push({name:this.download,blob:blobs.get(this.href)});};
  }});
  return {dom,w:dom.window,html,errors,downloads};
 }
 const test=create(data),{w,errors,downloads}=test,$=id=>w.document.getElementById(id);
 const change=(id,value)=>{$(id).value=value;$(id).dispatchEvent(new w.Event('change',{bubbles:true}));};
 assert.equal($('error').textContent,'');assert.equal($('targetPanel').hidden,false);assert.match($('targetQuality').textContent,/6 callbacks · 5 valid rays/);
 assert.equal(w.document.querySelectorAll('img').length,0);assert.match($('targetSummary').textContent,/<\/script><img/);
 change('targetFilter','door');assert.equal($('targetEvents').querySelectorAll('button').length,2);
 $('targetEvents').querySelector('button').click();assert.equal($('cursor').value,'2');
 $('annotationUseCursor').click();assert.equal($('annotationStart').value,'2');
 $('annotationAuthor').value='SYNTHETIC-CODER';$('annotationAuthor').dispatchEvent(new w.Event('input',{bubbles:true}));
 $('annotationLabel').value='Synthetic reflection';$('annotationLabel').dispatchEvent(new w.Event('input',{bubbles:true}));
 $('annotationSave').click();assert.match($('annotationCount').textContent,/1 saved/);
 $('annotationsExport').click();const annotation=JSON.parse(await downloads.at(-1).blob.text());assert.equal(annotation.annotations[0].start_s,2);assert.equal(annotation.annotations[0].label,'Synthetic reflection');
 $('targetExport').click();const csv=await downloads.at(-1).blob.text();assert.match(csv,/door/);assert.doesNotMatch(csv,/"key"/);
 $('bundle').click();const bundle=JSON.parse(await downloads.at(-1).blob.text());assert.equal(bundle.gaze_targets.length,7);assert.match(bundle.gaze_target_time_basis,/callback_time/);
 change('start','4');assert.match($('targetQuality').textContent,/0 callbacks/);change('end','3');assert.equal($('targetExport').disabled,true);assert.deepEqual(errors,[]);
 const out=path.resolve(root,'../../Artifacts/gaze-targets');fs.mkdirSync(out,{recursive:true});fs.writeFileSync(path.join(out,'synthetic-target-review.html'),test.html);w.close();
 const legacy={...data};delete legacy.gazeTargets;const old=create(legacy);assert.equal(old.w.document.getElementById('targetPanel').hidden,true);assert.equal(old.w.document.getElementById('error').textContent,'');assert.deepEqual(old.errors,[]);fs.writeFileSync(path.join(out,'legacy-no-targets.html'),old.html);old.w.close();
 console.log('PASS: DOM target summary/filter/seek → annotation JSON; CSV/bundle exports; inert labels; empty/invalid intervals; legacy sessions. Canvas/video rendering NOT tested.');
})().catch(e=>{console.error(e);process.exitCode=1;});
