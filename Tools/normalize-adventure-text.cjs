// Text-only migration. Never changes managed references, event methods or rewards.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const revisions = require('./adventure-text-revisions.json');
const directory = 'Assets/Work/Adventure/SO/Dialog';
const scalar = '"(?:\\\\.|[^"\\\\])*"|[^\\r\\n]*';
const field = name => new RegExp(`(<(?:${name})>k__BackingField:[ \\t]*)(${scalar})`, 'g');
const decode = value => value.startsWith('"') ? JSON.parse(value.replace(/\r?\n\s*/g, ' ')) : value.trim();
const get = (s, name) => { const m = field(name).exec(s); return m ? decode(m[2]) : null; };
const set = (s, name, value) => s.replace(field(name), (_, prefix) => prefix + JSON.stringify(value));
const names = {
  '무시하기':'그냥 지나간다', '망치로 부수기':'망치로 부순다', '칼로 자물쇠따기':'칼로 자물쇠를 딴다',
  '지나치기':'그냥 지나간다', '가져가기':'칼로 줄기를 자른다', '가져간다':'칼을 챙긴다',
  '지나간다':'그냥 지나간다', '칼로 긁어본다':'칼로 이끼를 긁어 낸다', '다른길로 피해간다':'옆길로 돌아간다',
  '망치로 공격합니다':'망치로 공격한다', '"제가 조금 바빠서요;;"':'도움을 거절한다', '"숨겨드릴게요"':'노인을 숨겨 준다',
  '칼을 주고 식재료를 받는다':'칼을 건넨다', '망치를 주고 식재료를 받는다':'망치를 건넨다',
  '기도하고 공물을 하나 받는다':'조각상 앞에서 기도한다', '샘물을 날라 준다':'샘물을 나른다'
};
function polish(text) {
  if(text.includes('<size=') && !text.includes('</size>')) text += '</size>';
  return text.trim()
    .replaceAll('재료와 짐을 정리하고 다시 길을 나선다.', '짐을 정리하고 다시 길을 나섰다.')
    .replaceAll('주변 지형을 기억해 두고 다음 길로 향했다.', '주변 지형을 기억해 두었다.')
    .replaceAll('위치를 기억해 두고 다시 길을 나섰다.', '위치를 기억해 두었다.')
    .replaceAll('점액만 챙긴다.', '점액만 챙겼다.')
    .replaceAll('선물인 듯하다.', '선물인 듯했다.')
    .replaceAll('두 개는 무사하다.', '두 개는 무사했다.')
    .replaceAll('아직 튼튼하다.', '아직 튼튼했다.')
    .replaceAll('작은 암염을 챙기고 자리를 떠난다.', '작은 암염을 챙겼다.')
    .replaceAll('암염을 두드려 본다.', '암염을 두드려 보았다.')
    .replaceAll('다음 행인을 기다린다.', '다음 행인을 기다렸다.')
    .replaceAll('머쉬룸맨이 고개를 숙인다.', '머쉬룸맨이 고개를 숙였다.')
    .replaceAll('머쉬룸맨이 돌 위에 갓 끝을 올린다.', '머쉬룸맨이 돌 위에 갓 끝을 올렸다.')
    .replaceAll('꽤 든든하다.', '꽤 든든했다.')
    .replaceAll('손에 잘 맞는다.', '손에 잘 맞았다.')
    .replaceAll('게는 계속 자고 있다.', '게는 계속 자고 있었다.');
}
function itemNames() {
  const result = new Map();
  for (const root of ['Assets/Work/Adventure/SO/AdventureItem','Assets/Work/Items/SO']) {
    for (const p of fs.readdirSync(root, {recursive:true}).filter(p=>p.endsWith('.asset'))) {
      const file = path.join(root,p), s = fs.readFileSync(file,'utf8');
      const name = get(s,'ItemName') ?? (()=>{ const m = new RegExp(`^  displayName: (${scalar})`, 'm').exec(s); return m && decode(m[1]); })();
      if(name) result.set(fs.readFileSync(file+'.meta','utf8').match(/^guid: (\w+)/m)[1],name);
    }
  }
  return result;
}
const optionBlocks = /(^    - rid: [^\r\n]+\r?\n      type: \{class: (?:Options|LockedOption|IngredientLockedOption),[^]*?)(?=^    - rid: |$(?![^]))/gm;
function tooltip(block, items) {
  const ingredient = /class: IngredientLockedOption,/.test(block);
  const locked = /class: LockedOption,/.test(block);
  if (!ingredient && !locked) return '필요한 아이템: 없음.';
  const ref = get(block,ingredient?'RequiredIngredient':'KeyItem');
  const guid = ref && ref.match(/guid: (\w+)/)?.[1];
  assert(items.has(guid), 'Missing requirement item: '+ref);
  const name = items.get(guid);
  if(locked && get(block,'IsUnLockOption')==='1') return `선택 조건: ${name} 미보유.`;
  const count = ingredient ? Number(get(block,'RequiredAmount')) : 1;
  assert(count > 0);
  const consumed = ingredient || get(block,'IsUseItemOption')==='1';
  return `필요한 아이템: ${name} ${count}개.` + (consumed ? ` 선택 시 ${count}개 소모됩니다.` : '');
}
const stripText = s => s.replace(field('Context|OptionName|OptionTooltip|LockTooltip'),(_,prefix)=>prefix+'"TEXT"');
function normalizePaths(paths, check = false) {
  const items = itemNames();
  let choices=0, lines=0, changed=0;
  const report=[];
  for(const file of paths) {
    const before=fs.readFileSync(file,'utf8'), name=path.basename(file,'.asset');
    const legacyRevision = /        followUpOptions:\r?\n        - rid:/.test(before) ? null : revisions[name];
    const choiceStart=choices;
    let index=0;
    let after=before.replace(field('Context'),(_,prefix,value)=>{
      const text=legacyRevision?.[index] ?? polish(decode(value)); index++; lines++;
      return prefix+JSON.stringify(text);
    });
    if(legacyRevision) assert.equal(index,legacyRevision.length,name+': dialogue count');
    after=after.replace(field('OptionName'),(_,prefix,value)=>{
      const original=decode(value).trim().replace(/\.$/,'');
      return prefix+JSON.stringify(names[original]??original.replaceAll('모아두고','모아 두고').replaceAll('뽑아본다','뽑아 본다').replaceAll('비켜준다','비켜 준다').replaceAll('내려준다','내려 준다').replaceAll('걷어낸다','걷어 낸다').replaceAll('베어낸다','베어 낸다'));
    });
    after=after.replace(optionBlocks,block=>{
      choices++;
      const tip=tooltip(block,items);
      report.push({event:name,option:get(block,'OptionName'),tooltip:tip});
      return set(set(block,'OptionTooltip',tip),'LockTooltip',tip);
    });
    assert.equal(choices-choiceStart,[...after.matchAll(field('OptionName'))].length,name+': unhandled option type');
    assert.equal(choices-choiceStart,[...after.matchAll(field('OptionTooltip'))].length,name+': tooltip count');
    for(const m of after.matchAll(field('OptionName'))) {
      assert(decode(m[2]).endsWith('다'),name+': option must describe an action');
    }
    assert.equal(stripText(after),stripText(before),name+': non-text content changed');
    for(const m of after.matchAll(field('Context'))) {
      const text=decode(m[2]);
      assert(text.length && (text.match(/"/g)||[]).length%2===0,name+': invalid dialogue quotes');
      assert.equal((text.match(/<size=/g)||[]).length,(text.match(/<\/size>/g)||[]).length,name+': size markup');
    }
    if(after!==before) { changed++; if(!check) fs.writeFileSync(file,after); }
  }
  if(check) assert.equal(changed,0,'Run text normalization first');
  return {events:paths.length,choices,lines,changed,report};
}
module.exports={normalizePaths};
if(require.main===module) {
  const paths=fs.readdirSync(directory).filter(p=>p.endsWith('.asset')).sort().map(p=>path.join(directory,p));
  const result=normalizePaths(paths,process.argv.includes('--check'));
  fs.writeFileSync('Temp/AdventureTextValidation.json',JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify({...result,report:undefined}));
}

