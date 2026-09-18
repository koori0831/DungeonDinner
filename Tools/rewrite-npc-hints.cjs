// Author the pre-order conversation from sensory clues, keeping result dialogue and evaluator data intact.
const fs = require('fs');
function csv(text) {
  text = text.replace(/^\uFEFF/, '');
  const rows = []; let row = [], value = '', quoted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '"') { if (quoted && text[i + 1] === '"') { value += '"'; i++; } else quoted = !quoted; }
    else if (!quoted && c === ',') { row.push(value); value = ''; }
    else if (!quoted && c === '\n') { row.push(value.replace(/\r$/, '')); rows.push(row); row = []; value = ''; }
    else value += c;
  }
  if (value || row.length) { row.push(value.replace(/\r$/, '')); rows.push(row); }
  const header = rows.shift();
  return { header, rows: rows.filter(r => r.length === header.length).map(r => Object.fromEntries(header.map((h, i) => [h, r[i]]))) };
}
const dialoguePath = 'Assets/Resources/NPCData/DialogueLines.csv';
const dialogue = csv(fs.readFileSync(dialoguePath, 'utf8'));
const visits = csv(fs.readFileSync('Assets/Resources/NPCData/VisitEvents.csv', 'utf8')).rows;
const requests = {
  Odin: {
    mushroom_soup_pane: ['동굴의 흙내음이 은근히 밴 따뜻한 한 끼가 생각나는군.', '숟가락으로 천천히 떠먹고 싶네. 마지막에는 담긴 그릇까지 부드럽게 뜯어 먹을 수 있다면 재미있겠어.'],
    corn_cheese_fondue: ['긴 이야기를 나누며 조금씩 찍어 먹던 식사가 그립군.', '노란 알갱이가 고소하게 씹히고, 따뜻할 때 부드럽게 늘어나는 것이었네. 달기보다는 깊은 맛이면 좋겠어.'],
    slime_nucleus_dango: ['산책길에 손으로 집어 먹던 작고 동그란 간식이 생각나는군.', '몇 알을 나란히 꿰어 놓았지. 달콤한 첫맛 뒤에 은근히 고소한 맛이 남았으면 하네.']
  },
  Nari: {
    mushroom_soup_pane: ['손이 좀 시리네요. 김이 오르는 걸 두 손으로 감싸고 싶어요.', '숟가락으로 떠먹다가 가장자리도 조금씩 뜯어 먹는 재미가 있으면 좋겠어요. 숲의 향도 은근하게요.'],
    corn_cheese_fondue: ['오늘은 한입씩 푹 찍어 먹으면서 쉬고 싶어요.', '따뜻하고 고소한 것 속에 노란 알갱이가 톡톡 씹히면 좋겠어요. 들었을 때 부드럽게 따라오는 느낌도요.'],
    slime_nucleus_dango: ['작고 동글동글해서 손으로 집기 편한 간식이 당겨요.', '나란히 꿰어 들고 걷고 싶어요. 달콤하면서도 한입 뒤에는 은근히 고소한 맛이 남는 걸로요.']
  },
  Boram: {
    mushroom_soup_pane: ['속부터 따뜻해지는 한 그릇이 필요해요. 숟가락을 들고 느긋하게 먹고 싶거든요.', '버섯 숲 같은 향이 나고, 다 먹은 뒤에는 가장자리까지 뜯어 먹을 수 있으면 딱 좋겠어요.'],
    corn_cheese_fondue: ['한입거리를 따뜻한 것에 푹 담갔다 먹는 게 생각났어요.', '노란 알갱이가 씹히는 고소한 맛이요. 들어 올리면 부드럽게 늘어나는 모습도 보고 싶네요.'],
    slime_nucleus_dango: ['오늘은 들고 다니며 먹을 만한 걸 부탁드릴게요.', '작은 구슬 몇 알을 나란히 꿴 모양이면 좋겠어요. 달콤하고 가볍게, 뒤에는 고소한 맛이 살짝 남도록요.']
  },
  Rook: {
    mushroom_soup_pane: ['김을 불어 가며 숟가락으로 먹을 수 있는 것이 좋겠어요.', '부드러운 속을 비우고 나면 바깥도 조금씩 뜯어 먹을 수 있으면 해요. 숲에서 맡던 향이 은근하게 나고요.'],
    corn_cheese_fondue: ['한입씩 찍어 먹으며 잠깐 쉬고 싶군요.', '따뜻하고 고소한 것 속에 노란 알갱이가 씹히던 기억이 있어요. 들어 올리면 천천히 늘어나던 모습도요.'],
    slime_nucleus_dango: ['길 위에서 손에 들고 먹던 작은 간식이 생각나요.', '동그란 것 몇 알을 나란히 꿰었지요. 달콤하게 시작해서 고소한 여운이 남았으면 해요.']
  }
};
const replacements = new Map();
for (const visit of visits) {
  const event = visit.EventId;
  const npc = visit.NpcId || event.split('_')[0];
  const recipe = Object.keys(requests.Odin).find(id => Object.values(visit).includes(id));
  if (!requests[npc] || !recipe) throw Error('No sensory request for ' + event);
  const add = (group, category, lines) => replacements.set(event + '|' + group,
    lines.map(([Speaker, Text], i) => ({ EventId: event, Group: group, QuestionCategory: category, LineOrder: String(i + 1), Speaker, Text })));
  add('OrderIntent', '', [[npc, requests[npc][recipe][0]], [npc, requests[npc][recipe][1]],
    ['Player', '말씀하신 모습과 느낌을 떠올리며 준비해 볼게요.']]);
  const dragon = npc === 'Odin';
  const sweet = recipe === 'slime_nucleus_dango';
  add('Question_Taste', 'Taste', [['Player', '입에 넣었을 때 어떤 맛이 먼저 느껴지면 좋을까요?'],
    [npc, sweet ? (dragon ? '첫맛은 달콤하게, 뒤에는 고소한 여운이 남으면 좋겠네.' : '첫맛은 달콤하게, 뒤에는 고소한 맛이 살짝 남으면 좋겠어요.')
      : (dragon ? '감칠맛이 충분했으면 하네. 단맛이 앞서지는 않았으면 좋겠군.' : '감칠맛과 고소한 풍미가 충분했으면 해요. 단맛이 앞서지는 않게요.')]]);
  add('Question_TextureTemp', 'TextureTemp', [['Player', '온도나 씹는 느낌도 말씀해 주세요.'],
    [npc, sweet ? (dragon ? '부담 없이 한입에 먹기 좋으면 되네. 너무 묵직하지만 않게 부탁하네.' : '한입에 먹기 편하고 부담 없이 가벼우면 좋겠어요.')
      : (dragon ? '식기 전에 따뜻하게 먹고 싶네. 부드럽고 촉촉하게 넘어가는 것을 좋아한다네.' : '따뜻할 때 먹고 싶어요. 부드럽고 촉촉하게 넘어가면 더 좋아요.')]]);
  add('Question_Condition', 'Condition', [['Player', '먹을 때 특히 신경 써 드릴 점이 있을까요?'],
    [npc, sweet ? (dragon ? '입안을 너무 무겁게 채우지만 않으면 되네.' : '먹고 나서 입안이 너무 무겁지 않았으면 해요.')
      : (dragon ? '부드럽게 넘어가면 좋겠네. 이에 오래 달라붙는 느낌은 피하고 싶군.' : '부드럽게 넘어가도록 부탁해요. 이에 오래 달라붙는 느낌은 피하고 싶어요.')]]);
  add('Question_Avoid', 'Avoid', [['Player', '피하고 싶은 맛이나 느낌은요?'],
    [npc, sweet ? (dragon ? '아무런 맛도 남지 않는 밋밋한 것은 아쉽지.' : '아무 맛도 남지 않는 밋밋한 건 조금 아쉬워요.')
      : (dragon ? '맛과 향이 너무 옅은 것은 아쉽네. 이에 질척하게 붙는 것도 피하고 싶군.' : '맛과 향이 너무 옅거나 이에 질척하게 달라붙지는 않았으면 좋겠어요.')]]);
}
const used = new Set(), output = [];
for (const row of dialogue.rows) {
  const key = row.EventId + '|' + row.Group;
  if (replacements.has(key)) { if (!used.has(key)) output.push(...replacements.get(key)); used.add(key); }
  else output.push(row);
}
for (const [key, rows] of replacements) if (!used.has(key)) output.push(...rows);
const answers = /버섯\s*수프\s*빠네|콘치즈\s*퐁듀|슬라임\s*핵\s*당고|slime_nucleus_dango|mushroom_soup_pane|corn_cheese_fondue/;
for (const row of output) if (row.Group === 'Intro')
  row.Text = row.Text.replace(new RegExp(answers.source, 'g'), '지난번 음식');
for (const row of output) if (!row.Group.startsWith('Result_') && answers.test(row.Text)) throw Error('Answer leaked: ' + row.EventId + '/' + row.Group);
const quote = s => '"' + String(s ?? '').replaceAll('"', '""') + '"';
fs.writeFileSync(dialoguePath, [dialogue.header, ...output.map(r => dialogue.header.map(h => r[h]))].map(r => r.map(quote).join(',')).join('\r\n') + '\r\n');
console.log(`Authored sensory clues for ${visits.length} visits; preserved result dialogue.`);
