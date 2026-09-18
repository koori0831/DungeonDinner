// Twelve encounters, four new uses for each of the six tools. No equipment rewards.
const rows = [
 ['Meet_CampCarver','모험가가 무딘 도구로 야영지의 말뚝을 깎고 있다.','옆에는 작업을 기다리는 나무판이 쌓여 있다.','Rough-looking adventurer',
  ['knife','작업용 칼을 건넨다','칼을 받은 모험가는 말뚝을 깎고 콘치즈 두 개를 건넸다. 칼은 작업대에 남겼다.','CornCheese',2,true],
  ['hammer','말뚝을 박아 준다','말뚝을 단단히 박자 모험가가 버섯 갓 하나를 나눠 주었다. 망치를 다시 챙겼다.','MushroomCap',1]],
 ['Find_SealedSaltJar','소금 항아리의 뚜껑이 밀랍으로 봉해져 있다.','뚜껑 가장자리에는 얇은 틈이 보인다.','RockSalt',
  ['knife','밀랍을 잘라 낸다','칼로 밀랍을 걷어 내고 항아리 속 암염 두 조각을 챙겼다. 칼날을 닦았다.','RockSalt',2],
  ['hammer','뚜껑 가장자리를 두드린다','뚜껑이 기울어 작은 틈이 생겼다. 암염 한 조각을 꺼내고 망치를 챙겼다.','RockSalt',1]],
 ['Meet_BridgeWatch','노인이 끊어진 난간 옆에서 통행인을 붙잡고 있다.','어두운 다리 아래로 젖은 발판이 이어진다.','Oldman',
  ['Rope','난간에 밧줄을 설치한다','노인은 새 난간을 잡고 건너온 뒤 콘치즈 두 개를 나눠 주었다. 밧줄은 난간에 남겼다.','CornCheese',2,true],
  ['Lantern','다리 아래를 비춘다','빛으로 마른 발판을 찾았다. 노인이 길 안내의 답례로 암염 하나를 건넸다.','RockSalt',1]],
 ['Meet_NightGatherer','채집꾼이 갈림길에서 꺼진 등을 만지작거린다.','갈림길 한쪽에는 짐을 내릴 낮은 턱이 있다.','Rough-looking adventurer',
  ['Lantern','탐험 등불을 건넨다','채집꾼은 등불을 받고 버섯 갓 세 조각을 건넸다. 불빛이 어두운 길로 멀어졌다.','MushroomCap',3,true],
  ['Rope','짐을 아래로 내려 준다','밧줄로 짐을 내린 뒤 다시 감았다. 채집꾼이 납작버섯 하나를 나눠 주었다.','FlatMushroom',1]],
 ['Meet_PicklingCook','요리사가 뜨거운 냄비 옆에서 빈 용기를 찾고 있다.','돌 위에는 옮겨 담지 못한 재료가 남아 있다.','Oldman',
  ['CollectingBottle','보관할 병을 건넨다','요리사는 병에 절임물을 담고 콘치즈 두 개를 건넸다. 병은 요리사에게 맡겼다.','CornCheese',2,true],
  ['Tongs','냄비 속 재료를 건진다','긴 집게로 재료를 건져 주자 요리사가 코코넛 게살 하나를 나눠 주었다. 집게를 닦아 챙겼다.','CoconutCrabMeat',1]],
 ['Find_BubblingCrevice','바위 틈에서 뜨거운 거품이 솟아오른다.','틈 위쪽에는 마른 결정이 붙어 있다.','SlimeMucus',
  ['Tongs','결정을 집어 낸다','집게로 열기를 피해 암염 두 조각을 떼었다. 집게가 식기를 기다렸다.','RockSalt',2],
  ['CollectingBottle','식은 점액을 받는다','열기가 닿지 않는 웅덩이에서 점액 두 몫을 담았다. 재료 주머니로 옮기고 병을 헹궜다.','SlimeMucus',2]],
 ['Meet_RootClimber','머쉬룸맨이 뿌리에 걸린 짐을 끌어당긴다.','잡아당길수록 포장 끈이 더 단단히 조인다.','MushroomMan',
  ['Rope','새 포장 끈을 묶어 준다','밧줄로 짐을 다시 묶자 머쉬룸맨이 버섯 갓 두 조각을 건넸다. 밧줄은 짐에 남겼다.','MushroomCap',2,true],
  ['knife','걸린 끈을 자른다','칼로 오래된 끈만 잘라 짐을 꺼냈다. 머쉬룸맨이 콘치즈 하나를 나눠 주었다.','CornCheese',1]],
 ['Meet_StoneKitchen','요리사가 돌로 만든 화덕을 고치고 있다.','돌 사이에 빠진 받침과 꺼내지 못한 숯이 보인다.','Rough-looking adventurer',
  ['hammer','수리용 망치를 건넨다','요리사가 망치로 화덕을 고친 뒤 게살 두 덩이를 건넸다. 망치는 작업장에 남겼다.','CoconutCrabMeat',2,true],
  ['Tongs','숯을 꺼내 옮긴다','긴 집게로 숯을 안전하게 옮겼다. 요리사가 암염 하나를 건네자 집게와 함께 챙겼다.','RockSalt',1]],
 ['Meet_CaveMedic','노인이 어두운 구석에서 상처를 씻고 있다.','바닥에 놓인 그릇은 금이 가 물이 새고 있다.','Oldman',
  ['Lantern','등불을 걸어 두고 간다','노인이 밝아진 자리에서 상처를 돌보았다. 콘치즈 두 개를 받고 등불은 곁에 남겼다.','CornCheese',2,true],
  ['CollectingBottle','병으로 물을 나른다','물을 여러 번 옮겨 주자 노인이 납작버섯 하나를 건넸다. 빈 병을 회수했다.','FlatMushroom',1]],
 ['Find_ResinFruit','끈끈한 수액에 콘치즈 줄기가 달라붙어 있다.','수액이 바위 아래의 작은 홈으로 모인다.','CornCheese',
  ['knife','줄기 주변을 잘라 낸다','칼로 굳은 부분만 잘라 콘치즈 두 개를 떼었다. 칼날에 묻은 수액을 닦았다.','CornCheese',2],
  ['CollectingBottle','흐르는 점액을 모은다','홈에 모인 점액 두 몫을 병에 담았다. 주머니에 옮긴 뒤 병을 다시 비웠다.','SlimeMucus',2]],
 ['Find_EchoCabinet','빈 벽장 뒤에서 작은 울림이 들린다.','옆 판자 사이로 희미한 틈이 보인다.','Box',
  ['hammer','뒤판을 가볍게 두드린다','뒤판이 벌어지며 선반이 드러났다. 암염 두 조각을 챙기고 망치를 넣었다.','RockSalt',2],
  ['Lantern','판자 사이를 비춘다','등불로 선반 위치를 확인하고 손을 뻗었다. 버섯 갓 하나를 꺼내 챙겼다.','MushroomCap',1]],
 ['Meet_CrabKeeper','모험가가 게를 옮길 우리를 손보고 있다.','우리 입구를 닫을 끈과 먹이를 집을 도구가 필요해 보인다.','CoconutCrab',
  ['Tongs','먹이용 집게를 건넨다','모험가는 집게로 먹이를 옮기고 게살 두 덩이를 내주었다. 집게는 우리 옆에 남겼다.','CoconutCrabMeat',2,true],
  ['Rope','우리 문을 잠시 고정한다','밧줄로 문을 잡아 주는 동안 수리가 끝났다. 밧줄을 회수하고 암염 하나를 받았다.','RockSalt',1]]
];
const picture={FlatMushroom:'FlatMushoroom',SlimeNucleus:'SlimeCore'};
const line=(text,...images)=>({text,images});
const events=rows.map(([name,intro,detail,image,...choices])=>({name,intro:[line(intro,image),line(detail)],options:[
 ...choices.map(([tool,name,text,reward,count,consume=false])=>({name,description:'필요한 아이템을 사용합니다.',lines:[line(text,picture[reward]||reward)],rewards:[[reward,count]],tool,consume})),
 {name:'다음 길로 향한다',description:'필요한 아이템: 없음.',lines:[line('주변 지형을 기억해 두었다.')],rewards:[]}
]}));
for (const event of events) for (const option of event.options) option.equipment = [];
module.exports={events};
