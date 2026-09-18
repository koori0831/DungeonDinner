const fs = require('node:fs');
const path = require('node:path');
const specs = {
  Find_Box: {name:'상자를 살핀다', lines:['상자의 모서리와 자물쇠를 자세히 살펴보았다.','나무판은 낡았지만 자물쇠는 단단하다. 틈을 건드리거나 상자를 부술 수 있을 것 같다.'], order:[1,2,0], leave:0},
  Meet_AdventurerTrade: {name:'거래 조건을 묻는다', lines:['모험가가 내 짐을 보며 공구를 가리켰다.','"칼이나 망치가 필요해. 내줄 수 있는 물건이 있나?"'], order:[0,1,2], leave:2},
  Meet_PitAdventurer: {name:'구덩이 아래에 말을 건다', lines:['가장자리에 엎드려 모험가의 상태를 살펴보았다.','"발을 디디기가 어렵네. 잡고 올라갈 줄이나 상처를 씻을 물이 있겠어?"'], order:[0,1,2], leave:2}
};
function upgradePaths(paths) {
  for(const file of paths) {
    const spec=specs[path.basename(file,'.asset')]; if(!spec) continue;
    let s=fs.readFileSync(file,'utf8');
    if(/        followUpOptions:\r?\n        - rid:/.test(s)) continue;
    const nl=s.includes('\r\n')?'\r\n':'\n';
    const block=s.match(/^  options:\r?\n(?:  - rid: [^\r\n]+\r?\n)+/m);
    if(!block) throw new Error('Missing root options: '+file);
    const ids=[...block[0].matchAll(/rid: (\d+)/g)].map(m=>m[1]);
    if(ids.length!==3) throw new Error('Expected three existing choices: '+file);
    const id='900000000000000001';
    if(s.includes('rid: '+id)) throw new Error('Reference collision: '+file);
    s=s.replace(block[0],`  options:${nl}  - rid: ${id}${nl}  - rid: ${ids[spec.leave]}${nl}`);
    s += [
      `    - rid: ${id}`,
      '      type: {class: Options, ns: Work.Adventure.Code, asm: Assembly-CSharp}',
      '      data:',
      `        <OptionName>k__BackingField: ${JSON.stringify(spec.name)}`,
      '        <OptionTooltip>k__BackingField: "필요한 아이템: 없음."',
      '        <RewardDescription>k__BackingField: ""',
      '        rewardMethod: []',
      '        <ResultdialogDatas>k__BackingField:',
      ...spec.lines.flatMap(t=>[`        - <Context>k__BackingField: ${JSON.stringify(t)}`,'          method: []']),
      '        followUpOptions:',...spec.order.map(i=>`        - rid: ${ids[i]}`),''
    ].join(nl);
    fs.writeFileSync(file,s);
  }
}
module.exports={upgradePaths};
if(require.main===module) upgradePaths(Object.keys(specs).map(n=>`Assets/Work/Adventure/SO/Dialog/${n}.asset`));
