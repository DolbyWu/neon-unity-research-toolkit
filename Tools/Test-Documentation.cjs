// Read-only checks for the beginner-facing documentation and menu wording.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'..'),pkg=path.join(root,'Packages/com.dolbywu.neon-research');
const files=execFileSync('git',['ls-files','--','*.md'],{cwd:root,encoding:'utf8',windowsHide:true}).trim().split(/\r?\n/).filter(Boolean);
let checkedLinks=0;
for(const file of files){
 const full=path.join(root,file),text=fs.readFileSync(full,'utf8');
 assert.ok(!/escape[\s-]?room/i.test(text),'User documentation must not require a separate game context: '+file);
 assert.equal((text.match(/^```/gm)||[]).length%2,0,'Balanced fenced code blocks: '+file);
 for(const match of text.matchAll(/\[[^\]\n]*\]\((<[^>\n]+>|[^)\s]+)\)/g)){
  let target=match[1].replace(/^<|>$/g,'');if(/^(?:[a-z]+:|#)/i.test(target))continue;
  target=decodeURIComponent(target.split('#')[0]);assert.ok(fs.existsSync(path.resolve(path.dirname(full),target)),'Broken local link in '+file+': '+target);checkedLinks++;
 }
}
const readme=fs.readFileSync(path.join(root,'README.md'),'utf8'),guide=fs.readFileSync(path.join(root,'QUICKSTART.md'),'utf8');
for(const phrase of ['Download ZIP','VerificationProject','Record on Unity Play','Manual annotations','Export annotations JSON'])assert.ok(readme.includes(phrase),'Homepage covers '+phrase);
for(const phrase of ['no Git needed','60-second','Returning participant','NO RECENT EYE DATA','Save participant and continue','Regenerate Finished Session HTML','Compute Eye State'])assert.ok(guide.includes(phrase),'Beginner guide covers '+phrase);
for(const name of ['NeonResearchSetup.cs','StudyLiveMonitor.cs','StudyParticipantStartWindow.cs']){
 const text=fs.readFileSync(path.join(pkg,'Editor',name),'utf8');assert.ok(!/escape[\s-]?room|Tools > Study >/i.test(text),'Current project-independent menu wording: '+name);
}
assert.ok(!/escape[\s-]?room/i.test(fs.readFileSync(path.join(pkg,'package.json'),'utf8')),'Package metadata has standalone wording.');
assert.equal(JSON.parse(fs.readFileSync(path.join(pkg,'package.json'),'utf8')).version,'0.1.0-preview.3');
console.log('PASS: '+files.length+' Markdown files, '+checkedLinks+' local links, beginner milestones and standalone UI wording.');
