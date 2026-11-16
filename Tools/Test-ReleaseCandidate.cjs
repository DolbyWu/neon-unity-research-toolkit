// Source-only publication audit; does not access configured study directories.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'..'),git=(...args)=>execFileSync('git',args,{cwd:root,encoding:'utf8',windowsHide:true,maxBuffer:20*1024*1024});
const staged=process.argv.includes('--staged'),split=s=>s.split('\0').filter(Boolean);
const files=[...new Set(staged?split(git('diff','--cached','--name-only','-z')):[...split(git('diff','--name-only','-z')),...split(git('ls-files','--others','--exclude-standard','-z'))])];
const read=file=>staged?git('show',':'+file):fs.readFileSync(path.join(root,file),'utf8');
if(staged)assert.ok(files.length>0,'No staged candidate files were available to audit.');
for(const file of files){
 assert.ok(/^(?:Packages\/|Tools\/|Verification(?:XR)?Project\/|docs\/|README\.md$|QUICKSTART\.md$|VALIDATION\.md$|RELEASE_NOTES\.md$|\.github\/|\.gitignore$)/.test(file),'Unexpected scope: '+file);
 assert.ok(/\.(?:cs|meta|asmdef|md|json|txt|cjs|ps1|py|yml)$/.test(file)||file==='.gitignore','Unexpected payload: '+file);
 assert.ok(!/(?:^|\/)(?:Participants|NeonConfig|UserSettings|Library|Logs|Temp|Audio|Video|Replay)\//i.test(file),'Generated/private path: '+file);
 const text=read(file);assert.ok(!text.includes('\0'),'Binary file: '+file);
 assert.ok(!/(?:sk-(?:proj-)?[A-Za-z0-9_-]{25,}|gh[pousr]_[A-Za-z0-9]{25,}|github_pat_[A-Za-z0-9_]{30,}|-----BEGIN [A-Z ]*PRIVATE KEY-----)/.test(text),'Possible credential: '+file);
 assert.ok(!/\b[A-Za-z]:[\\/](?:Users|GazeFlowTrials)[\\/]/i.test(text),'Machine-specific path: '+file);
 const ips=[...text.matchAll(/\b(?:\d{1,3}\.){3}\d{1,3}\b/g)].map(m=>m[0]).filter(ip=>ip.split('.').every(n=>Number(n)<=255)&&!['127.0.0.1','0.0.0.0','255.255.255.255'].includes(ip));assert.equal(ips.length,0,'Live address in '+file);
}
for(const file of ['Tools/Build-StorePackage.py','Tools/Test-StorePackage.py'])assert.equal(git('diff','HEAD','--',file),'','Separate store tooling changed.');
for(const directory of ['Packages/com.dolbywu.neon-research/Runtime','Packages/com.dolbywu.neon-research/Editor','Packages/com.binyanxu.gazeflow.neonxr/Runtime','Packages/com.binyanxu.gazeflow.neonxr/Editor'])for(const name of fs.readdirSync(path.join(root,directory))){if(!/\.(cs|txt)$/.test(name))continue;const text=fs.readFileSync(path.join(root,directory,name),'utf8');assert.ok(!/PlayerDataLogger|HandTutorialNextGate|EscapeRoom|LocomotionTutorial|Main Menu AR/.test(text),'Host coupling: '+directory+'/'+name);}
console.log('PASS: '+files.length+' candidate files; source-only, no detected credentials/live addresses/machine paths, no known host-script coupling; store tools unchanged.');
