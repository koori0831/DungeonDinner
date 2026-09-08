const fs = require('fs');
const crypto = require('crypto');
const A = 'Assets/Work/Adventure';
const C = 'Assets/Work/Cook/Data';
const I = 'Assets/Work/Items/SO';
const read = p => fs.readFileSync(p, 'utf8');
const write = (p, s) => { if (!fs.existsSync(p) || read(p) !== s) fs.writeFileSync(p, s, 'utf8'); };
const guid = p => read(p + '.meta').match(/^guid: (\w+)/m)[1];
function meta(p, type = 'asset') {
  if (fs.existsSync(p + '.meta')) return guid(p);
  const g = crypto.randomBytes(16).toString('hex');
  const importer = type === 'asset' ? 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000' : 'PrefabImporter:\n  externalObjects: {}';
  write(p + '.meta', `fileFormatVersion: 2\nguid: ${g}\n${importer}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  return g;
}
const ref = (p, id = 11400000, type = 2) => `{fileID: ${id}, guid: ${guid(p)}, type: ${type}}`;
const q = s => JSON.stringify(s);
function header(name, script, cls) {
  return `%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: ${ref(script,11500000,3)}\n  m_Name: ${name}\n  m_EditorClassIdentifier: Assembly-CSharp::${cls}\n`;
}

const {events}=require('./adventure-equipment-content.cjs');
const toolNames={knife:'칼',hammer:'망치',Rope:'밧줄',Lantern:'탐험 등불',Tongs:'긴 집게',CollectingBottle:'채집병'};
const tools=Object.fromEntries(Object.keys(toolNames).map(n=>[n,A+'/SO/AdventureItem/'+(n==='hammer'?'Hamer':n)+'.asset']));
const materialNames={SlimeMucus:'슬라임 점액',CornCheese:'콘치즈',RockSalt:'암염'};
const items=Object.fromEntries(['CornCheese','FlatMushroom','MushroomCap','RockSalt','SlimeMucus','SlimeNucleus'].map(n=>[n,I+'/Ingredients/'+n+'IngredientItem.asset']));
items.CoconutCrabMeat=I+'/CoconutCrabMeatIngredientItem.asset';
const line=(text,...images)=>({text,images});
for(const name of ['Rope','Lantern','Tongs','CollectingBottle']) {
 const png=A+'/Graphics/Item/'+name+'.png';
 if(!fs.existsSync(png+'.meta')) write(png+'.meta',read(A+'/Graphics/Item/knife.png.meta').replace(/^guid: \w+/m,'guid: '+crypto.randomBytes(16).toString('hex')).replace(/spriteID: \w+/,'spriteID: '+crypto.randomBytes(16).toString('hex')));
 write(tools[name],header(name,A+'/Code/AdventureItemSO.cs','Work.Adventure.Code.AdventureItemSO')+'  <ItemName>k__BackingField: '+q(toolNames[name])+'\n  <ItemIcon>k__BackingField: '+ref(png,21300000,3)+'\n');
 meta(tools[name]);
 const prefab=A+'/Prefabs/Item/'+name+'.prefab';
 write(prefab,read(A+'/Prefabs/Item/CoconutCrab.prefab').replace('m_Name: CoconutCrab','m_Name: '+name).replace(/m_Sprite: \{[^}]+\}/,'m_Sprite: '+ref(png,21300000,3)));
 meta(prefab,'prefab');
}
function saveEvent(event) {
 let next = 1000; const defs = [];
 function add(cls, ns, data) {const id = ++next; defs.push({id,cls,ns,data}); return id;}
 const clear = () => add('DeleteAllImageEvent','Work.Adventure.Code.DialogMethod',[]);
 function show(images) {
   return [clear(), ...images.map((n,i) => add('CreateImageEvent','Work.Adventure.Code.DialogMethod',[
     `imagePrefab: ${ref(`${A}/Prefabs/Item/${n}.prefab`,'81474867245666639',3)}`,
     `position: {x: ${images.length===1?0:(i===0?-220:220)}, y: 0}`]))];
 }
 function dialogs(lines, indent, end = false) {
   const all = end ? [...lines, line('재료와 짐을 정리하고 다시 길을 나선다.')] : lines;
   return all.flatMap((l,i)=> {
     const ids = l.images.length ? show(l.images) : (end && i === all.length-1 ? [clear()] : []);
     return [`${indent}- <Context>k__BackingField: ${q(l.text)}`,`${indent}  method:${ids.length?'':' []'}`,...ids.map(id=>`${indent}  - rid: ${id}`)];
   });
 }
 const initial = dialogs(event.intro,'  ');
 const opts = event.options.map(o => {
   const rewards = o.rewards.map(([n,count])=>add('IngredientReward','Work.Adventure.Code.AdventureEvents',[`reward: ${ref(items[n])}`,`amount: ${count}`]));
   rewards.push(...o.equipment.map(n=>add('AdventureItemReward','Work.Adventure.Code.Rewards', [`itemSO: ${ref(tools[n])}`])));
   const data = [`<OptionName>k__BackingField: ${q(o.name)}`,`<OptionTooltip>k__BackingField: ${q(o.description)}`,`<RewardDescription>k__BackingField: ${q(o.description)}`,
     `rewardMethod:${rewards.length?'':' []'}`,...rewards.map(id=>`- rid: ${id}`),'<ResultdialogDatas>k__BackingField:',...dialogs(o.lines,'',true)];
   if(o.tool) data.push(`<LockTooltip>k__BackingField: ${q(toolNames[o.tool]+'이(가) 필요합니다.')}`,`<KeyItem>k__BackingField: ${ref(tools[o.tool])}`,'<IsUnLockOption>k__BackingField: 0',`<IsUseItemOption>k__BackingField: ${o.consume?1:0}`,'LogStatus: 3');
   if(o.cost) data.push(`<RequiredIngredient>k__BackingField: ${ref(items[o.cost[0]])}`,`<RequiredAmount>k__BackingField: ${o.cost[1]}`,`<LockTooltip>k__BackingField: ${q(materialNames[o.cost[0]]+' '+o.cost[1]+'개가 필요합니다. 사용 시 소모됩니다.')}`);
   return add(o.cost?'IngredientLockedOption':o.tool?'LockedOption':'Options','Work.Adventure.Code',data);
 });
 let out = header(event.name,`${A}/Code/AdventureEventSO.cs`,'Work.Adventure.Code.AdventureEventSO') + '  <dialogDatas>k__BackingField:\n' + initial.join('\n') + '\n  options:\n' + opts.map(id=>`  - rid: ${id}`).join('\n') + '\n  references:\n    version: 2\n    RefIds:\n';
 for(const d of defs) out += `    - rid: ${d.id}\n      type: {class: ${d.cls}, ns: ${d.ns}, asm: Assembly-CSharp}\n      data:${d.data.length?'':' '}\n` + (d.data.length?d.data.map(l=>'        '+l).join('\n')+'\n':'');
 const p = `${A}/SO/Dialog/${event.name}.asset`; write(p,out); meta(p); return p;
}
const paths = events.map(saveEvent);
require('./normalize-adventure-text.cjs').normalizePaths(paths);
require('./upgrade-adventure-branches.cjs').upgradePaths(paths);
for(const scene of [`${A}/Scene/AdventureTestScene.unity`,'Assets/Work/Cook/Scene/CookTestScene.unity']) {
 let s = read(scene); const nl = s.includes('\r\n')?'\r\n':'\n';
 s = s.replace(/  eventList:\r?\n(?:  - .*\r?\n)*/g, block => block + paths.filter(p=>!block.includes(guid(p))).map(p=>`  - ${ref(p)}${nl}`).join(''));
 write(scene,s);
}
console.log(`Created equipment expansion: ${events.length} events, ${events.reduce((n,e)=>n+e.options.length,0)} choices, 4 items and image prefabs.`);
