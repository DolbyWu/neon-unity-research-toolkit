// Synthetic fixtures only. No participant recordings are read.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.STUDY_PLAYWRIGHT||'playwright');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.resolve(root,'../../Artifacts/offline-replay');fs.mkdirSync(out,{recursive:true});
 const template=fs.readFileSync(path.join(root,'Runtime/Resources/NeonResearchOfflineReplay.txt'),'utf8');
 const header=['sample_id','connection_epoch','receive_session_time_s','aligned_session_time_s','time_alignment_status','sample_quality','eye_state_available','pupil_left_mm','pupil_right_mm','gaze_x_px','gaze_y_px'];
 const gaze=[header];for(let i=0;i<1200;i++){const t=i/100;gaze.push([String(i),'1',String(t+.02),String(t),'time_echo_estimated','basic_valid_not_full_eye_quality_assessment','True',String(.003+.0002*Math.sin(t*2)),String(.0032+.00015*Math.cos(t*2)),String(500+100*Math.sin(t)),String(350+50*Math.cos(t))]);}
 gaze.push(['unsynced','2','4.1','','unavailable','basic_valid_not_full_eye_quality_assessment','False','','','','']);
 const data={version:1,session:'SYNTHETIC DEMO / NOT PARTICIPANT DATA',duration:12,events:[['event_id','session_time_s','region','event_type','puzzle','object_id','status','outcome','detail'],['1','2','Puzzle 1','grab_start','Puzzle 1','Cup','','','Left'],['2','5','Puzzle 1','grab_end','Puzzle 1','Cup','','','Left'],['3','6','In-between','observer_marker','','','','','</script><img src=x onerror=alert(1)>'],['4','9','Puzzle 2','attempt','Puzzle 2','Lock','','wrong','']],regions:[['start_session_s','end_session_s','region','source'],['0','5','Puzzle 1','synthetic'],['5','8','In-between','synthetic'],['8','12','Puzzle 2','synthetic']],gaze:[{source:'Neon-synthetic',table:gaze}]};
 function write(name,d){const file=path.join(out,name);fs.writeFileSync(file,template.replace('__STUDY_DATA__',JSON.stringify(d).replace(/</g,'\\u003c').replace(/>/g,'\\u003e').replace(/&/g,'\\u0026')));return file;}
 const report=write('offline-replay-demo.html',data),empty=write('offline-replay-empty.html',{...data,gaze:[]});
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{const page=await browser.newPage({viewport:{width:1400,height:1200},acceptDownloads:true});const errors=[],requests=[];page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});
 await page.addInitScript(()=>{window.pupilLabels=[];const original=CanvasRenderingContext2D.prototype.fillText;CanvasRenderingContext2D.prototype.fillText=function(text,...args){if(/^0\.\d{6}$/.test(String(text)))window.pupilLabels.push(String(text));return original.call(this,text,...args);};});
 await page.goto(pathToFileURL(report).href);assert.equal(await page.locator('#error').innerText(),'');assert.match(await page.locator('#quality').innerText(),/1200 \/ 1201/);
 const pupilAxis=await page.evaluate(()=>[...new Set(window.pupilLabels)]);assert.ok(pupilAxis.length>=2);assert.ok(pupilAxis.every(label=>Number(label)>0));
 await page.locator('#cursor').evaluate(e=>{e.value='2';e.dispatchEvent(new Event('input',{bubbles:true}));});assert.match(await page.locator('#status').innerText(),/Left pupil 0\.\d{6} \/ Right pupil 0\.\d{6}/);
 await page.locator('#regions button').first().click();assert.equal(await page.locator('#start').inputValue(),'0');assert.equal(await page.locator('#end').inputValue(),'5');assert.match(await page.locator('#selection').innerText(),/500 gaze samples/);
 const downloadPromise=page.waitForEvent('download');await page.locator('#bundle').click();const download=await downloadPromise;const parsed=JSON.parse(fs.readFileSync(await download.path(),'utf8'));assert.equal(parsed.gaze[0].table.length,501);assert.equal(parsed.events.length,2);assert.equal(parsed.end_boundary_events.length,2);assert.equal(parsed.unaligned_receive_count,1);
 assert.equal(parsed.gaze[0].table[1][7],gaze[1][7]);assert.equal(parsed.gaze[0].table[1][8],gaze[1][8]);
 const csvPromise=page.waitForEvent('download');await page.locator('#gazeCsv').click();const csvDownload=await csvPromise;assert.equal(fs.readFileSync(await csvDownload.path(),'utf8').split('\r\n').length,501);
 await page.locator('#basis').selectOption('receive');assert.match(await page.locator('#selection').innerText(),/499 gaze samples/);await page.locator('#basis').selectOption('aligned');
 await page.locator('#start').fill('7');await page.locator('#start').dispatchEvent('change');assert.equal(await page.locator('#bundle').isDisabled(),true);
 await page.locator('#regions button').last().click();await page.locator('#zoom').click();await page.locator('#play').click();await page.waitForTimeout(350);await page.locator('#play').click();assert.ok(Number(await page.locator('#cursor').inputValue())>8);
 await page.locator('#all').click();
 assert.equal(await page.locator('img').count(),0);assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
 await page.goto(pathToFileURL(empty).href);assert.match(await page.locator('#quality').innerText(),/No eye data recorded/);assert.equal(await page.locator('#error').innerText(),'');assert.deepEqual(errors,[]);
 // Keep the user-facing synthetic preview readable after the injection test.
 data.events[3][8]='Observer marker (synthetic data)';write('offline-replay-demo.html',data);
 await page.goto(pathToFileURL(report).href);await page.locator('#regions button').first().click();
 await page.screenshot({path:path.join(out,'offline-replay-demo.png'),fullPage:true});
 console.log('PASS: six-decimal pupil axes/readout, unmodified exported pupil values, offline load, no network, aligned half-open extraction, end boundary context, missing data, JSON/CSV downloads, region selection, playback, invalid selection, escaped text.');
 console.log('Synthetic preview: '+report);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
