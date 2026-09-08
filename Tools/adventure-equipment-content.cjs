// Authoring source for the equipment expansion. Tooltips describe actions/costs, never rewards.
const line = (text, ...images) => ({text, images});
const option = (name, description, lines, rewards = [], tool = null, consume = false, cost = null, equipment = []) => ({name, description, lines, rewards, tool, consume, cost, equipment});
const leave = () => option('다음 길로 향한다', '주변을 건드리지 않고 길을 이어갑니다.', [line('위치를 기억해 두고 다시 길을 나섰다.')]);
const events = [
 {name:'Find_RopeCache', intro:[line('바위 아래에 묶인 짐 꾸러미를 발견했다.','Box'),line('겉에는 [남는 물건입니다. 필요한 만큼만!]이라고 적혀 있다.'),line('튼튼한 밧줄 옆에는 돌 틈에 끼인 칼이 보인다.')], options:[
  option('밧줄을 풀어 챙긴다','꾸러미의 매듭을 풀고 밧줄을 감습니다.',[line('해진 곳이 없는지 살피고 밧줄을 둘둘 감았다.','Rope'),line('"던전에서도 줄은 잘 잡아야지."')],[],null,false,null,['Rope']),
  option('망치로 칼을 꺼낸다','칼 주변의 돌을 두드립니다. 망치가 필요합니다.',[line('날을 피해서 돌만 두드리자 칼이 빠져나왔다.','knife'),line('칼날을 닦아 짐에 넣었다.')],[],'hammer',false,null,['knife']), leave()]},
 {name:'Meet_LanternKeeper', intro:[line('노인이 어두운 갈림길에서 등불을 손보고 있다.','Oldman','Lantern'),line('"짐 정리를 거들어 주겠나? 여분의 등이 무거워서 말이야."')], options:[
  option('짐 정리를 돕는다','노인의 짐을 나눠 들고 밝은 길까지 함께 걷습니다.',[line('갈림길을 지나자 노인이 여분의 등불을 건넸다.','Lantern'),line('"불씨는 챙겨 뒀네. 발밑부터 비추게나."')],[],null,false,null,['Lantern']),
  option('채집병을 건넨다','기름을 옮겨 담을 병을 건넵니다. 채집병 1개가 소모됩니다.',[line('노인은 병에 기름을 담고 예비 등불을 내주었다.','Lantern'),line('"딱 맞는 병이로군. 이 등은 자네가 쓰게."')],[],'CollectingBottle',true,null,['Lantern']), leave()]},
 {name:'Find_CookKit', intro:[line('식어 버린 야영지에 작은 도구 상자가 남아 있다.','Box'),line('[다음 요리사에게. 하나만 가져가세요.]'),line('안에는 긴 집게와 깨끗한 채집병이 놓여 있다.','Tongs','CollectingBottle')], options:[
  option('긴 집게를 챙긴다','손잡이와 맞물림을 확인하고 집게를 챙깁니다.',[line('집게를 몇 번 벌려 보았다. 아직 튼튼하다.','Tongs'),line('"손보다 긴 손이 생겼네."')],[],null,false,null,['Tongs']),
  option('채집병을 챙긴다','금이 간 곳이 없는지 살피고 마개를 닫습니다.',[line('병 안을 헹구고 허리끈에 걸었다.','CollectingBottle'),line('"흐르는 것도 이제 담을 수 있겠어."')],[],null,false,null,['CollectingBottle']), leave()]},
 {name:'Meet_SupplyPorter', intro:[line('모험가가 무거운 공구 상자를 내려놓았다.','Rough-looking adventurer','Box'),line('"수리하고 남은 도구야. 짐 좀 줄이게 하나 가져가."')], options:[
  option('망치를 고른다','자루가 단단히 고정된 망치를 골라 챙깁니다.',[line('망치의 자루를 쥐고 흔들어 보았다. 손에 잘 맞는다.','Hamer'),line('"필요할 때마다 없더라니까."')],[],null,false,null,['hammer']),
  option('칼을 고른다','날과 손잡이를 살펴보고 칼을 챙깁니다.',[line('녹을 닦아낸 칼을 받았다.','knife'),line('"이번에는 오래 써야겠네."')],[],null,false,null,['knife']), leave()]},
 {name:'Meet_RopeWeaver', intro:[line('머쉬룸맨이 풀섬유를 꼬며 매듭을 만들고 있다.','MushroomMan','Rope'),line('옆에는 손질을 기다리는 등불도 놓여 있다.'),line('머쉬룸맨은 벌어진 섬유 끝과 등불 속 부러진 심지를 번갈아 가리킨다.')], options:[
  option('점액으로 끝을 고정한다','풀섬유 끝에 점액을 바릅니다. 슬라임 점액 1개가 소모됩니다.',[line('섬유가 풀리지 않게 고정하자 머쉬룸맨이 완성된 밧줄을 건넸다.','Rope'),line('접착제 값으로는 꽤 든든하다.')],[],null,false,['SlimeMucus',1],['Rope']),
  option('집게로 심지를 손본다','집게로 심지를 꺼내 바로잡습니다. 긴 집게가 필요합니다.',[line('심지를 바로 세우니 작은 불꽃이 살아났다.','Lantern'),line('머쉬룸맨은 환하게 웃으며 등불을 내 쪽으로 밀었다.')],[],'Tongs',false,null,['Lantern']), leave()]},
 {name:'Meet_BottleTrader', intro:[line('노인이 공구와 빈 병을 펼쳐 놓고 점심을 기다린다.','Oldman','CollectingBottle'),line('"물건은 남는데 식재료가 모자라는군. 바꿀 생각 있나?"')], options:[
  option('콘치즈를 건넨다','노인과 물품을 교환합니다. 콘치즈 1개가 소모됩니다.',[line('노인은 콘치즈를 받고 깨끗한 채집병을 골라 주었다.','CollectingBottle'),line('"마개까지 잘 맞춰 뒀네."')],[],null,false,['CornCheese',1],['CollectingBottle']),
  option('암염을 건넨다','노인과 물품을 교환합니다. 암염 1개가 소모됩니다.',[line('노인은 소금 맛을 보더니 긴 집게를 내주었다.','Tongs'),line('"간도 맞췄고, 거래도 맞았군."')],[],null,false,['RockSalt',1],['Tongs']), leave()]},
 {name:'Find_LedgePantry', intro:[line('낮은 절벽 아래에 버려진 식재료 상자가 걸려 있다.','Box'),line('내려가는 길은 어둡고, 상자 주변에는 굵은 뿌리가 뻗어 있다.')], options:[
  option('밧줄을 타고 내려간다','바위에 밧줄을 고정하고 내려갑니다. 밧줄이 필요합니다.',[line('상자를 끌어올린 뒤 밧줄을 회수했다.'),line('상자에는 콘치즈 두 개가 남아 있었다.','CornCheese')],[['CornCheese',2]],'Rope'),
  option('등불로 발판을 찾는다','절벽 가장자리의 발판을 비춥니다. 탐험 등불이 필요합니다.',[line('빛을 비추자 옆으로 이어지는 좁은 길이 보였다.'),line('길 끝의 선반에서 버섯 갓 하나를 챙겼다.','MushroomCap')],[['MushroomCap',1]],'Lantern'), leave()]},
 {name:'Meet_PitAdventurer', intro:[line('구덩이 아래에서 익숙한 목소리가 들린다.','Rough-looking adventurer'),line('"내려온 건 계획이었는데, 올라가는 건 계획에 없었어."'),line('모험가는 다친 발을 가리킨다. 밧줄을 고정해 두거나 씻을 물을 내려줘야겠다.')], options:[
  option('밧줄을 고정해 준다','스스로 올라올 수 있도록 밧줄을 남깁니다. 밧줄 1개가 소모됩니다.',[line('모험가가 밧줄을 잡고 천천히 올라왔다.'),line('"덕분에 살았어. 이걸 받아 줘."'),line('구조 사례로 콘치즈와 버섯 갓을 받았다.','CornCheese','MushroomCap')],[['CornCheese',2],['MushroomCap',1]],'Rope',true),
  option('물을 담아 내려준다','샘물을 담은 병을 내려줍니다. 채집병 1개가 소모됩니다.',[line('모험가는 상처를 씻고 쉴 자리를 잡았다.'),line('"다음 구조대까지 버틸 수 있겠어. 이건 가져가."'),line('그가 올려 준 작은 망치와 암염을 챙겼다.','Hamer','RockSalt')],[['RockSalt',1]],'CollectingBottle',true,null,['hammer']), leave()]},
 {name:'Find_DarkNest', intro:[line('큰 버섯 아래에서 무언가 바스락거린다.','Mushroom'),line('깊은 그늘 때문에 안쪽이 보이지 않는다. 좁은 틈으로 손을 넣기에는 꺼림칙하다.')], options:[
  option('등불로 안을 살핀다','그늘 속을 천천히 비춥니다. 탐험 등불이 필요합니다.',[line('빛에 놀란 작은 벌레들이 흩어졌다.'),line('빈 둥지 주변의 멀쩡한 갓 두 조각을 챙겼다.','MushroomCap')],[['MushroomCap',2]],'Lantern'),
  option('집게로 틈을 뒤진다','손 대신 집게를 넣어 조심스럽게 살핍니다. 긴 집게가 필요합니다.',[line('집게 끝에 눌린 버섯 하나가 걸려 나왔다.','FlatMushoroom'),line('"적어도 손을 물리지는 않았네."')],[['FlatMushroom',1]],'Tongs'), leave()]},
 {name:'Find_HotSpringBasket', intro:[line('뜨거운 물이 흐르는 돌 틈에 게가 끼어 있다.','CoconutCrab'),line('근처 바위에는 끈끈한 점액도 고여 있다. 맨손으로 만지기엔 물이 너무 뜨겁다.')], options:[
  option('집게로 껍질을 잡는다','뜨거운 물을 피해 집게를 뻗습니다. 긴 집게가 필요합니다.',[line('집게로 끌어낸 게가 빛이 되어 사라졌다.'),line('온전한 코코넛 게살 두 덩이를 챙겼다.','CoconutCrabMeat')],[['CoconutCrabMeat',2]],'Tongs'),
  option('병으로 점액을 뜬다','물가에 고인 점액을 담습니다. 채집병이 필요합니다.',[line('점액을 병에 담아 건져 낸 뒤 재료 주머니로 옮겼다.','SlimeMucus'),line('빈 병은 물에 헹궈 다시 챙겼다.')],[['SlimeMucus',2]],'CollectingBottle'), leave()]},
 {name:'Find_StickyPool', intro:[line('슬라임이 지나간 자리에 두꺼운 점액 웅덩이가 남았다.','SlimeMucus'),line('가운데에 단단한 무언가가 반짝인다. 손을 넣으면 한참 끈적거리겠지.')], options:[
  option('병으로 점액을 모은다','웅덩이 가장자리부터 떠 담습니다. 채집병이 필요합니다.',[line('깨끗한 점액만 골라 재료 주머니에 옮겨 담았다.','SlimeMucus'),line('병을 닦고 마개를 닫았다.')],[['SlimeMucus',2]],'CollectingBottle'),
  option('집게로 빛나는 것을 집는다','가운데의 단단한 물체를 집어 올립니다. 긴 집게가 필요합니다.',[line('끈끈한 실을 끊어 내자 작은 슬라임 핵이 드러났다.','SlimeCore'),line('"젓가락으로는 무리였겠는데."')],[['SlimeNucleus',1]],'Tongs'), leave()]},
 {name:'Find_RootCellar', intro:[line('나무뿌리가 얽힌 오래된 저장고를 발견했다.','Box'),line('문은 반쯤 내려앉았고, 안쪽에서는 마른 흙냄새가 난다.')], options:[
  option('밧줄로 문을 당긴다','문고리에 밧줄을 걸어 당깁니다. 밧줄이 필요합니다.',[line('문을 비켜 세우고 밧줄을 풀어 회수했다.'),line('선반 위에서 암염 두 덩이를 발견했다.','RockSalt')],[['RockSalt',2]],'Rope'),
  option('등불로 선반을 살핀다','문틈으로 저장고 안을 비춥니다. 탐험 등불이 필요합니다.',[line('가까운 선반에 멀쩡한 콘치즈가 하나 보였다.','CornCheese'),line('손을 뻗어 챙긴 뒤 문틈에서 물러났다.')],[['CornCheese',1]],'Lantern'), leave()]}
];
module.exports = {events};
