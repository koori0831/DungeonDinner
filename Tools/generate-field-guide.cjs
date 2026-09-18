// Rebuild the shared guide from production asset references. Run from the project root.
const fs = require('fs');
const crypto = require('crypto');
const read = p => fs.readFileSync(p, 'utf8');
const write = (p, text) => { if (!fs.existsSync(p) || read(p) !== text) fs.writeFileSync(p, text); };
const guid = p => read(p + '.meta').match(/^guid: (\w+)/m)[1];
const ref = (p, id = 11400000, type = 2) => `{fileID: ${id}, guid: ${guid(p)}, type: ${type}}`;
const q = JSON.stringify;
function meta(p, importer) {
  if (!fs.existsSync(p + '.meta')) write(p + '.meta', `fileFormatVersion: 2\nguid: ${crypto.randomBytes(16).toString('hex')}\n${importer}:\n  externalObjects: {}\n`);
}
const script = 'Assets/Work/Cook/Code/Info/FieldGuideCatalogSO.cs';
meta(script, 'MonoImporter');
const A = 'Assets/Work/Adventure';
const ingredientRows = [
  ['corn_cheese', '콘치즈 군락과 탐험 중 만나는 식량 상자에서 구할 수 있다. 새싹 슬라임을 따라가면 군락을 발견하기도 한다.', '가열해 녹이는 손질을 선택할 수 있다. 탐험에서는 물물교환에 쓰이기도 하므로 여분을 챙겨 두자.'],
  ['flat_mushroom', '던전의 버섯과 식량 상자를 살펴보자. 가시덤불 속 상자에서는 긴 집게가 도움이 된다.', '통째로 쓰거나 썰고 다질 수 있다. 칼질의 완성도에 따라 요리의 식감이 달라진다.'],
  ['mushroom_cap', '큰 버섯과 머쉬룸맨에게서 얻는 재료. 곤란한 머쉬룸맨을 도와주고 받는 경우도 있다.', '갓 모양을 살려 통으로 쓰거나 한입 크기로 손질해 보자. 같은 재료도 손질에 따라 다른 요리로 이어진다.'],
  ['rock_salt', '던전의 소금 결정, 저장고와 암염을 품은 슬라임을 살펴보자.', '굽거나 곱게 갈아 사용하는 손질이 있다. 탐험 중 거래에 필요한 경우도 있다.'],
  ['slime_mucus', '슬라임을 상대하거나 지나간 자리의 점액을 모아 얻는다. 채집병을 쓰면 웅덩이와 좁은 틈에서도 채집할 수 있다.', '끓이기와 살짝 얼리기는 서로 다른 식감을 만든다. 도구 없이 그대로 사용하는 방법도 있다.'],
  ['slime_nucleus', '슬라임의 몸 안쪽에 남는 핵. 탐험에서는 칼로 점액을 걷어 내거나 집게로 웅덩이를 뒤져 얻기도 한다.', '씻기, 그을리기, 삶기를 선택할 수 있다. 겉보기만 보고 맛을 단정하지 말고 손질 결과를 기록해 보자.'],
  ['coconut_crab_meat', '코코넛 게의 껍질을 공략하거나 탈피한 껍질에서 꺼낸다. 망치·칼·긴 집게를 사용할 기회가 있다.', '그대로 사용하거나 굽고 데칠 수 있다. 은은한 단맛과 고소한 풍미를 살려 보자.']
];
// Descriptions are shared with ingredient selection as well as the guide.
const descriptions = {
  flat_mushroom: '넓고 납작한 갓을 가진 던전 버섯. 통째로 쓰기에도, 한입 크기로 썰거나 잘게 다지기에도 알맞은 재료다.',
  mushroom_cap: '그릇처럼 오목한 버섯의 갓 부분. 모양을 살려 통으로 사용하거나 썰고 다져 요리에 넣을 수 있다.',
  rock_salt: '던전의 바위와 틈새에 맺힌 소금 결정. 덩어리째 쓰거나 굽고 갈아 요리의 간을 더하는 재료다.',
  slime_mucus: '슬라임이 남긴 끈끈한 점액. 끓이거나 살짝 얼리는 손질에 따라 식감이 달라지는 독특한 던전 식재료다.',
  slime_nucleus: '슬라임의 몸속에서 꺼낸 작은 핵. 씻거나 불에 그을리고 삶는 등, 손질에 따라 서로 다른 풍미를 낸다.'
};
for (const [id, description] of Object.entries(descriptions)) {
  const p = `Assets/Work/Cook/Data/Ingredients/${id}.asset`;
  write(p, read(p).replace(/^  description:.*(?:\r?\n    [^\r\n]*)*/m, '  description: ' + q(description)));
}
const section = (title, text) => `\n\n<b><color=#81552F>${title}</color></b>\n${text}`;
const tools = [
  ['knife', '잘라 내고 틈을 공략하는 탐험용 칼.', '슬라임의 점액을 걷어 내거나 코코넛 게의 껍질 틈을 공략할 때 사용한다. 버섯 채집에도 쓰인다.', '일부 선택에서는 칼이 부러지거나 소모된다. 선택지의 소모 안내를 확인하자.'],
  ['Hamer', '단단한 껍질과 돌을 두드리는 묵직한 망치.', '코코넛 게의 껍질을 깨거나 돌에 끼인 물건을 꺼내는 데 사용한다.', '탐험 중 공구를 나눠 주는 모험가에게서 구할 수 있다.'],
  ['Rope', '몸과 짐을 단단히 고정하는 튼튼한 밧줄.', '절벽을 내려가고, 매달린 상자를 내리고, 덫이나 문을 고정할 때 사용한다.', '직접 사용한 뒤 회수하기도 하지만 구조용으로 남겨 두는 선택에서는 1개가 소모된다.'],
  ['Lantern', '어두운 갈림길과 손이 닿지 않는 틈을 비추는 탐험 등불.', '발판·발자국·매듭을 살피고 저장고와 둥지 안쪽을 확인할 때 사용한다.', '등불을 손보는 노인의 짐 정리를 돕거나 물품을 교환해 얻을 수 있다.'],
  ['Tongs', '맨손이 닿기 어려운 곳을 대신 잡아 주는 긴 집게.', '뜨거운 물가의 게, 가시덤불, 점액 웅덩이와 좁은 걸쇠를 살필 때 사용한다.', '야영지 도구 상자와 물물교환에서 구할 수 있다. 머쉬룸맨의 가시를 빼 주는 데도 쓰인다.'],
  ['CollectingBottle', '점액과 샘물을 담을 수 있는 마개 달린 채집병.', '점액을 모으거나 샘물을 담아 껍질과 상처를 씻는 데 사용한다.', '내용물을 옮긴 뒤 다시 쓰는 경우가 많다. 병째 건네주는 선택에서는 1개가 소모된다.']
];
const monsters = [
  ['슬라임', 'HurbSlime', '말랑한 점액으로 몸을 이루고 길목을 막아서는 던전의 익숙한 주민.', '몸 바깥의 점액과 안쪽의 핵을 구분해 살펴보자. 탐험에서는 공격 방법과 상황에 따라 남는 재료가 달라진다.', '슬라임 점액 · 슬라임 핵'],
  ['암염 슬라임', 'RockSaltSlime', '몸속에 하얀 암염을 품은 슬라임. 반투명한 몸 안의 결정을 눈여겨보자.', '길가에서 만나면 몸속 암염을 확인할 수 있다. 지나치거나 도구를 사용해 상대하는 선택이 있다.', '암염 · 슬라임 계열 재료'],
  ['새싹 슬라임', 'HurbSlime', '머리에 작은 새싹이 돋은 슬라임. 몇 걸음 움직이고 뒤돌아보며 길을 안내하기도 한다.', '조용히 따라가면 콘치즈 군락을 발견하는 만남이 있다. 공격하면 안내를 끝까지 볼 수 없다.', '콘치즈 군락 안내 · 슬라임 점액'],
  ['머쉬룸맨', 'MushroomMan', '커다란 갓을 머리에 쓴 버섯 주민. 길가에서 서성이거나 잠든 모습을 볼 수 있다.', '공격하는 것 외에도 길을 돌아가거나 곤란한 주민을 돕는 만남이 있다. 도구로 도움을 주면 식재료를 나눠 받기도 한다.', '버섯 갓 · 콘치즈 등 만남별 보답'],
  ['코코넛 게', 'CoconutCrab', '코코넛 껍질을 집 삼아 움직이는 게. 단단한 껍질 사이로 틈과 집게발이 보인다.', '망치로 껍질을 깨거나 칼로 틈을 공략할 수 있다. 덫과 탈피 흔적을 만났을 때는 밧줄·집게·채집병도 도움이 된다.', '코코넛 게살']
];
function field(text, key) { const match = text.match(new RegExp('^  ' + key + ': (.+)$', 'm')); if (!match) throw Error(key); return match[1]; }
const itemEntries = tools.map(([id, intro, use, note]) => {
  const s = read(`${A}/SO/AdventureItem/${id}.asset`);
  const entryId = "tool:" + id.toLowerCase();
  const itemPath = `${A}/SO/AdventureItem/${id}.asset`;
  write(itemPath, s.replace(/^  discoveryEntryId:.*\r?\n/gm, "") + `  discoveryEntryId: ${entryId}\n`);
  return { entryId, name: JSON.parse(field(s, '<ItemName>k__BackingField')), icon: field(s, '<ItemIcon>k__BackingField'), description: intro };
});
const monsterIds = ['slime', 'rock_salt_slime', 'sprout_slime', 'mushroom_man', 'coconut_crab'];
const monsterEntries = monsters.map(([name, icon, intro, note, material], index) => ({entryId: 'monster:' + monsterIds[index], name, icon: ref(`${A}/Graphics/Item/${icon}.png`, 21300000, 3), description: intro}));
let output = `%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: ${ref(script, 11500000, 3)}\n  m_Name: DungeonFieldGuide\n  m_EditorClassIdentifier: Assembly-CSharp::Work.Cook.Code.Info.FieldGuideCatalogSO\n  ingredients:\n`;
for (const [id, source, notes] of ingredientRows) output += `  - ingredient: ${ref(`Assets/Work/Cook/Data/Ingredients/${id}.asset`)}\n    source: ${q(source)}\n    notes: ${q(notes)}\n`;
output += `  cookingCatalog: ${ref('Assets/Work/Cook/SO/CookingDataCatalog.asset')}\n  categories:\n`;
for (const [name, marker, entries] of [['도구', 5, itemEntries], ['몬스터', 3, monsterEntries]]) {
  output += `  - <DisplayName>k__BackingField: ${q(name)}\n    <MarkIcon>k__BackingField: ${entries[0].icon}\n    <Marker>k__BackingField: ${marker}\n    <ViewType>k__BackingField: 7\n    <Entries>k__BackingField:\n`;
  for (const e of entries) output += `    - <EntryId>k__BackingField: ${q(e.entryId)}\n      <IsDiscovered>k__BackingField: 1\n      <DisplayName>k__BackingField: ${q(e.name)}\n      <Icon>k__BackingField: ${e.icon}\n      <Description>k__BackingField: ${q(e.description)}\n`;
}
const target = 'Assets/Resources/DungeonFieldGuide.asset';
write(target, output);
meta(target, 'NativeFormatImporter');
console.log(`Field guide: ${ingredientRows.length} ingredients, ${itemEntries.length} tools, ${monsterEntries.length} monsters.`);
