# DungeonDinner UI 에셋 이미지 생성 명세

## 1. 목적

`Assets/Work/Cook/Graphics/UIAsset`을 DungeonDinner의 공통 UI 아트 기준으로 사용한다.
이 문서는 Codex의 내장 이미지 생성 기능으로 에셋을 한 종씩 제작할 때 사용할 명세와 프롬프트를 정의한다.

현재 제작 범위는 신규·교체 41종과 기존 에셋 수정 1종이다. 이후 시스템 확장용 후보 8종은 별도 항목으로 둔다.

## 2. 제작 원칙

1. 한 번의 이미지 생성 호출에서는 하나의 오브젝트, 아이콘, 초상화 또는 배경만 생성한다.
2. 생성 결과를 먼저 화면에 보여주고 승인받는다.
3. 승인 전에는 프로젝트 에셋을 추가하거나 기존 에셋을 교체하지 않는다.
4. 승인본은 기존 파일을 덮어쓰지 않고 새 PNG로 저장한다.
5. UI 아이콘과 도구는 실제 알파 채널이 있는 투명 배경이어야 한다.
6. 이미지 내부에는 글자, 숫자, UI 라벨을 넣지 않는다. 날짜, 시간, 금액은 TMP로 표시한다.
7. 64~128px 표시 크기에서도 실루엣과 의미가 구분되어야 한다.
8. hover, pressed, disabled 상태는 별도 이미지를 만들지 않고 Unity 색상·크기·투명도 변화로 처리한다.

## 3. 기준 이미지

### 아이콘·도구용 스타일 참조

- `Assets/Work/Cook/Graphics/UIAsset/SlicingIcon.png`
  - 역할: 선 굵기, 해칭, 적색 강조, 큰 실루엣 기준
- `Assets/Work/Cook/Graphics/UIAsset/ChoppingIcon.png`
  - 역할: 작은 조각 표현, 점묘와 세부 묘사 기준

### UI 프레임·배너용 스타일 참조

- `Assets/Work/Cook/Graphics/UIAsset/Pannel.png`
- `Assets/Work/Cook/Graphics/UIAsset/Btn1.png`
- `Assets/Work/Cook/Graphics/UIAsset/Receipt.png`
  - 역할: 짙은 코코아색 면, 손으로 그린 불규칙한 테두리, 장식 밀도 기준

### 캐릭터 디자인 참조

- `Assets/Work/Cook/Graphics/TempImage/Odin.png`
  - 역할: 오딘의 머리카락, 뿔, 의상과 인상만 참고
  - 주의: 애니메이션풍 렌더링과 흰색 스티커 테두리는 따라 하지 않는다.

### 스타일 참조로 사용하지 않을 이미지

- `Assets/Work/Cook/Graphics/UIPresentation/Icons/*`
- `Assets/Work/Cook/Graphics/TempImage/image-Photoroom.png`
- `Assets/Work/Cook/Graphics/TempImage/FREE Casual Game SFX Pack/*`
- `Assets/Work/Cook/Graphics/TempImage/Humble Gift - v1.3/*`

위 파일은 교체 대상을 파악하는 용도로만 사용한다. 금속 광택, 이모지 표정, 네온 발광, 현대적인 그라데이션, 픽셀 UI 스타일을 새 에셋에 옮기지 않는다.

## 4. 공통 비주얼 규격

- 스타일: 따뜻한 전통 판타지 요리 게임, 빈티지 요리책 삽화, 손그림 에칭
- 선: 강한 짙은 갈색 외곽선, 일정하지 않은 자연스러운 펜선
- 명암: 부드러운 3D 그라데이션 대신 해칭과 점묘
- 기본색: 크림, 베이지, 갈색
- 강조색: 적색, 녹색, 황금색, 재료 고유색을 제한적으로 사용
- 형태: 하나의 큰 중심 피사체, 명확한 실루엣, 충분한 외곽 여백
- 금지: 사진풍, 매끈한 3D, 플라스틱 광택, 네온, 강한 림라이트, 검은 배경, 과도한 발광
- 공통 금지 요소: 텍스트, 숫자, 로고, 워터마크, 외부 UI 프레임, 불필요한 소품

## 5. Codex 실행용 지시문

아래 문장을 Codex 요청의 머리말로 사용한다.

```text
이 프로젝트의 `Docs/UIAsset_ImageGen_Spec.md`를 읽고 지정한 에셋 한 종만 제작해줘.
명세에 적힌 기준 이미지는 편집 대상이 아니라 스타일 참조로 사용해줘.
내장 imagegen을 사용하고, 먼저 결과만 보여줘. 승인 전에는 프로젝트 폴더로 복사하거나 기존 에셋을 교체하지 마.
투명 스프라이트는 실제 알파 채널을 유지하고, 텍스트와 워터마크를 넣지 마.
이번 제작 대상: <ASSET_ID>
```

Codex는 `<ASSET_ID>`에 해당하는 개별 요청문을 아래 공통 프롬프트와 결합한다.

## 6. 공통 이미지 생성 프롬프트

### A. 투명 UI 아이콘

```text
Use case: illustration-story
Asset type: Unity 2D UI Sprite
Primary request: <개별 에셋 요청문>
Input images: Image 1 is `SlicingIcon.png`, a visual-style reference for outline, hatching, palette, and readability; Image 2 is `ChoppingIcon.png`, a supporting style reference for stippling and small-detail treatment. Do not copy their objects or composition.
Style/medium: warm traditional fantasy cooking-game illustration; vintage cookbook engraving; hand-drawn etching
Composition/framing: near-square canvas; one large centered subject; clear silhouette; 10–14 percent transparent padding; readable at 64–128 px
Lighting/mood: warm, restrained, friendly fantasy tavern mood; no dramatic cinematic lighting
Color palette: cream, beige, and brown base; dark-brown outlines; only the specified limited accent color
Materials/textures: visible pen hatching and stippling; lightly worn handmade surfaces
Constraints: genuinely transparent background with preserved alpha; no text, numbers, UI panels, border, frame, watermark, shadow plate, or extra props
Avoid: photorealism, anime rendering, smooth 3D gradients, glossy metal, glossy plastic, emoji styling, neon, heavy bloom, harsh rim lighting, tiny decorative clutter
```

### B. 투명 조리 도구·상호작용 오브젝트

```text
Use case: illustration-story
Asset type: Unity 2D interactive cooking-object Sprite
Primary request: <개별 에셋 요청문>
Input images: Image 1 is `SlicingIcon.png`, a visual-style reference for tool construction, outline, hatching, and red-handle accents; Image 2 is `ChoppingIcon.png`, a supporting reference for texture and readability. Do not copy their complete scene.
Style/medium: warm traditional fantasy cooking-game illustration; vintage cookbook engraving; hand-drawn etching
Composition/framing: near-square canvas; one isolated usable tool or interaction marker; centered; full silhouette visible; generous transparent margin; readable at 64–128 px
Lighting/mood: warm neutral light, friendly and practical
Color palette: cream, beige, brown, and dark iron; limited muted-red handle accent; only use blue for water or frost
Materials/textures: etched wood grain, hand-hatched metal or ceramic, restrained stippling
Constraints: genuinely transparent background with preserved alpha; no hands, character, countertop, room, text, UI frame, watermark, or unrelated prop
Avoid: photorealism, glossy 3D game-item rendering, modern plastic, neon, oversized particle effects, complex background
```

### C. 캐릭터 초상화

```text
Use case: illustration-story
Asset type: Unity 2D NPC portrait Sprite
Primary request: <개별 에셋 요청문>
Input images: Image 1 is `SlicingIcon.png`, a visual-style reference for linework, hatching, cream-and-brown palette, and limited red accents. If an explicit character reference is listed in the individual request, use it only for identity and costume details, not for rendering style.
Style/medium: warm traditional fantasy cookbook character illustration; hand-drawn ink etching with restrained watercolor wash
Composition/framing: near-square; centered chest-up portrait; face and species features clearly readable; full hair, horns, ears, or feather silhouette inside the canvas
Lighting/mood: welcoming tavern warmth; approachable expression appropriate to the character
Color palette: cream, beige, brown base; restrained character-specific colors
Materials/textures: visible hand-inked hatching on hair, cloth, scales, and feathers
Constraints: genuinely transparent background with preserved alpha; no circular portrait frame, text, logo, watermark, white sticker outline, weapon, or unrelated prop
Avoid: modern anime cel shading, glossy gacha rendering, photorealism, 3D, neon, excessive jewelry, extreme facial expression
```

### D. 지역 삽화

```text
Use case: illustration-story
Asset type: Unity 2D dispatch-region illustration
Primary request: <개별 에셋 요청문>
Input images: Image 1 is `SlicingIcon.png`, a visual-style reference for dark-brown ink, hatching, stippling, and limited accent colors; Image 2 is `Pannel.png`, a palette reference for the dark cocoa UI surrounding the illustration. Do not include the panel itself.
Scene/backdrop: <개별 지역 설명>
Style/medium: warm traditional fantasy field-guide and cookbook engraving; hand-drawn ink with restrained watercolor wash
Composition/framing: landscape 16:9; one clear region focal point; foreground, midground, and background separated; readable when cropped into a dispatch card
Lighting/mood: mysterious but inviting adventure tone; restrained lighting
Color palette: cream, beige, brown base with the specified biome accent
Materials/textures: visible cross-hatching and stippling on rock, soil, plants, smoke, or mineral surfaces
Constraints: no characters, UI, border, frame, title, labels, logo, or watermark; keep important features away from extreme edges
Avoid: photorealism, cinematic 3D concept art, neon lava or crystals, excessive bloom, highly saturated sky, cluttered composition
```

### E. 던전 식당 배경

```text
Use case: illustration-story
Asset type: Unity 2D game scene background
Primary request: a warm underground fantasy restaurant interior built inside an old dungeon, intended to replace the current photographic placeholder background
Input images: Image 1 is `Pannel.png`, a palette and shape-language reference; Image 2 is `Receipt.png`, a reference for parchment warmth and handmade line texture; Image 3 is `SlicingIcon.png`, a reference for engraved hatching. Do not place these UI assets inside the scene.
Scene/backdrop: old stone dining room with dark timber beams, a modest open kitchen and hearth, wooden tables, shelves with simple cookware, and a softly lit passage leading deeper into the dungeon
Subject: the dungeon restaurant interior itself; no characters
Style/medium: warm traditional fantasy storybook background; vintage cookbook engraving with restrained watercolor wash
Composition/framing: landscape 16:9; eye-level wide view; visually quiet central and upper-middle zones for UI; clear foreground and depth without clutter
Lighting/mood: warm amber hearth and candlelight against muted brown stone; cozy, slightly mysterious, welcoming
Color palette: cream, beige, cocoa brown, charcoal brown; limited muted red and moss green accents
Materials/textures: hand-inked stone, worn wood grain, aged plaster, soft smoke drawn with sparse hatching
Constraints: no people, monsters, food close-up, text, signs, logos, UI panels, frames, or watermark
Avoid: photography, realistic 3D rendering, modern restaurant furniture, neon, strong bloom, extreme darkness, bright blue light, busy center composition
```

## 7. 개별 에셋 요청문 — 현재 제작 범위 41종

### 7.1 요리 품질 아이콘 — 4종

#### `quality_perfect`

- 파일명: `ui_quality_perfect.png`
- 저장 후보: `Assets/Work/Cook/Graphics/UIAsset/Feedback/`
- 강조색: 제한된 따뜻한 금색과 작은 적색 보석
- 개별 요청문:

```text
an elegant small fantasy cook's crown representing perfect dish quality, symmetrical and unmistakable, with one tiny muted-red central jewel, engraved rather than shiny
```

#### `quality_good`

- 파일명: `ui_quality_good.png`
- 저장 후보: `Assets/Work/Cook/Graphics/UIAsset/Feedback/`
- 강조색: 황토색과 약한 녹색
- 개별 요청문:

```text
a well-made three-point quality star carved from warm wood and parchment-toned material, balanced and intact, representing a good but not perfect dish
```

#### `quality_normal`

- 파일명: `ui_quality_normal.png`
- 저장 후보: `Assets/Work/Cook/Graphics/UIAsset/Feedback/`
- 강조색: 없음
- 개별 요청문:

```text
a simple closed serving cloche with an ordinary, dependable silhouette, clean and undamaged, representing normal dish quality without celebration or failure
```

#### `quality_poor`

- 파일명: `ui_quality_poor.png`
- 저장 후보: `Assets/Work/Cook/Graphics/UIAsset/Feedback/`
- 강조색: 제한된 탁한 적갈색
- 개별 요청문:

```text
a slightly dented serving cloche with one small crack and a restrained curl of unpleasant steam, clearly representing poor dish quality while remaining non-gory and suitable for a cozy cooking game
```

### 7.2 손님 반응 아이콘 — 5종

반응 아이콘은 동일한 단순한 후드 차림 판타지 손님 머리를 사용한다. 첫 승인본을 다음 반응 생성 시 추가 스타일·캐릭터 일관성 참조로 제공한다.

#### `reaction_delighted`

- 파일명: `ui_reaction_delighted.png`
- 개별 요청문:

```text
a simple friendly hooded fantasy diner head showing delighted amazement, bright open eyes and a broad but restrained smile, hand-etched character expression rather than an emoji
```

#### `reaction_satisfied`

- 파일명: `ui_reaction_satisfied.png`
- 개별 요청문:

```text
the same simple hooded fantasy diner head showing calm satisfaction, gently closed smiling eyes and a small content smile, readable and restrained
```

#### `reaction_interested`

- 파일명: `ui_reaction_interested.png`
- 개별 요청문:

```text
the same simple hooded fantasy diner head showing curious interest, one eyebrow slightly raised and eyes focused, neither clearly happy nor unhappy
```

#### `reaction_disappointed`

- 파일명: `ui_reaction_disappointed.png`
- 개별 요청문:

```text
the same simple hooded fantasy diner head showing mild disappointment, lowered eyes and a small downturned mouth, sympathetic rather than angry
```

#### `reaction_repulsed`

- 파일명: `ui_reaction_repulsed.png`
- 개별 요청문:

```text
the same simple hooded fantasy diner head recoiling with clear but family-friendly disgust, squinting eyes and wrinkled nose, no vomit, gore, or exaggerated emoji styling
```

### 7.3 주문 조건 태그 — 4종

#### `tag_required`

- 파일명: `ui_tag_required.png`
- 강조색: 황금색
- 개별 요청문:

```text
a compact parchment checklist icon with one large hand-drawn check mark, representing a required order condition, with no written words or tiny list text
```

#### `tag_preferred`

- 파일명: `ui_tag_preferred.png`
- 강조색: 제한된 녹색
- 개별 요청문:

```text
a single heart-shaped culinary herb leaf, wholesome and appetizing, representing a preferred ingredient or trait without looking romantic or glossy
```

#### `tag_avoid`

- 파일명: `ui_tag_avoid.png`
- 강조색: 탁한 주황색
- 개별 요청문:

```text
a small serving fork crossed by one bold diagonal prohibition stroke, representing an ingredient or trait to avoid, with a simple highly readable silhouette
```

#### `tag_danger`

- 파일명: `ui_tag_danger.png`
- 강조색: 제한된 진한 적색
- 개별 요청문:

```text
a compact triangular warning charm containing a simple poisonous mushroom silhouette, representing a dangerous food condition, no letters or punctuation marks
```

### 7.4 보상·보조 표시 — 3종

#### `reward_coin`

- 파일명: `ui_reward_coin.png`
- 개별 요청문:

```text
a single thick, slightly irregular fantasy tavern coin embossed with a tiny crossed fork-and-spoon symbol, warm aged gold with brown etched shading, not shiny
```

#### `cooking_sparkle`

- 파일명: `ui_cooking_sparkle.png`
- 개별 요청문:

```text
a single four-point magical cooking sparkle drawn like an old cookbook ornament, cream center with a restrained warm-gold edge, crisp at small size and not glowing excessively
```

#### `npc_placeholder`

- 파일명: `ui_npc_placeholder.png`
- 개별 요청문:

```text
a neutral anonymous hooded fantasy guest bust with the face mostly in soft shadow, friendly rather than ominous, designed as a fallback NPC portrait with a clear simple silhouette
```

### 7.5 공통 내비게이션·임시 에셋 교체 — 6종

#### `nav_back`

- 파일명: `ui_nav_back.png`
- 개별 요청문:

```text
a single sturdy curved back arrow carved from dark warm wood, pointing left, with a broad readable arrowhead and restrained etched grain
```

#### `chapter_dungeon_emblem`

- 파일명: `ui_chapter_dungeon_emblem.png`
- 개별 요청문:

```text
a compact dungeon-chapter emblem shaped like a mushroom-cap cave entrance, combining a rounded cave mouth and a simple mushroom silhouette into one readable symbol
```

#### `select_confirm`

- 파일명: `ui_select_confirm.png`
- 개별 요청문:

```text
a bold hand-drawn confirmation check mark pressed into a small irregular parchment wax seal, simple and readable, with a restrained moss-green accent
```

#### `action_start`

- 파일명: `ui_action_start.png`
- 개별 요청문:

```text
a single right-pointing triangular start marker carved from warm dark wood with a narrow muted-red inset, simple and readable at small size
```

#### `action_return`

- 파일명: `ui_action_return.png`
- 개별 요청문:

```text
a single circular hooked return arrow made from a hand-inked dark-brown stroke with subtle wood texture, clearly indicating return or retry
```

#### `chapter_bookmark_banner`

- 파일명: `ui_chapter_bookmark_banner.png`
- 참조 이미지: `Pannel.png`, `Btn1.png`, `Receipt.png`
- 공통 프롬프트 A 대신 아래 프롬프트 사용:

```text
Use case: illustration-story
Asset type: Unity 2D UI bookmark-tab Sprite
Primary request: a single tall hanging chapter bookmark tab made from dark cocoa leather and aged parchment, with a pointed lower end and a clean empty center reserved for a separate icon and TMP label
Input images: Image 1 is `Pannel.png`, Image 2 is `Btn1.png`, and Image 3 is `Receipt.png`; use them only as references for dark-brown shape language, handmade borders, and parchment texture
Style/medium: warm traditional fantasy UI; hand-drawn vintage cookbook ornament
Composition/framing: near-square transparent canvas containing one centered tall vertical tab; complete silhouette visible; symmetrical enough for 9-slice use
Color palette: dark cocoa brown, muted parchment beige, subtle warm-brown decorative line
Constraints: genuinely transparent background; empty center; no crown, sword, icon, text, number, logo, watermark, glow, or shadow plate
Avoid: red-blue esports banner design, smooth gradients, glossy fabric, metallic trim, modern vector styling
```

### 7.6 날짜·시간 — 2종

#### `status_date`

- 파일명: `ui_status_date.png`
- 개별 요청문:

```text
a small torn parchment calendar page with a simple sun emblem at the top, completely blank center for a separate day number, no written text or numerals
```

#### `status_time`

- 파일명: `ui_status_time.png`
- 개별 요청문:

```text
a compact old tavern hourglass with dark wooden caps and pale golden sand, sturdy silhouette, clear upper and lower chambers, no magical glow
```

### 7.7 조리 미니게임 오브젝트 — 10종

#### `tool_knife`

- 파일명: `cook_tool_knife.png`
- 개별 요청문:

```text
a single sturdy fantasy kitchen chef knife shown in side view, muted-red wooden handle on the left and broad cream-metal blade extending right, clean cutting edge and practical proportions
```

#### `tool_brush`

- 파일명: `cook_tool_brush.png`
- 개별 요청문:

```text
a single short basting brush with a muted-red wooden handle and thick cream natural bristles, clearly a cooking brush rather than a painting brush
```

#### `tool_pan`

- 파일명: `cook_tool_pan.png`
- 개별 요청문:

```text
a single shallow dark-iron frying pan in a slight top-down three-quarter view, empty clean cooking surface, stout wooden grip, complete silhouette visible
```

#### `tool_plate`

- 파일명: `cook_tool_plate.png`
- 개별 요청문:

```text
a single empty off-white ceramic serving plate viewed from above, slightly handmade irregular rim with sparse brown etched decoration, clean center
```

#### `tool_pestle`

- 파일명: `cook_tool_pestle.png`
- 개별 요청문:

```text
a single heavy wooden-and-stone kitchen pestle shown diagonally, rounded crushing end clearly visible, practical grip and strong simple silhouette
```

#### `tool_pitcher`

- 파일명: `cook_tool_pitcher.png`
- 개별 요청문:

```text
a single small ceramic water pitcher with one handle and a pronounced pouring lip, a small visible surface of pale blue water at the opening, no water stream
```

#### `tool_mortar`

- 파일명: `cook_tool_mortar.png`
- 개별 요청문:

```text
a single empty heavy stone mortar bowl in a top-down three-quarter view, wide stable base and clearly visible inner grinding surface, no pestle
```

#### `interaction_frost`

- 파일명: `cook_interaction_frost.png`
- 개별 요청문:

```text
a single compact frost emblem formed from one bold snowflake and a few attached ice facets, pale blue accent with dark-brown engraved outline, no flame or steam
```

#### `interaction_flip`

- 파일명: `cook_interaction_flip.png`
- 개별 요청문:

```text
a single bold curved double-ended motion arrow describing an upward flipping arc, hand-inked and immediately readable, no pan, food, hand, or text
```

#### `interaction_foam_discard`

- 파일명: `cook_interaction_foam_discard.png`
- 개별 요청문:

```text
a small kitchen skimming bowl holding a simple mound of pale cooking foam, clearly representing discarded surface foam, clean and non-gory, no trash-can symbol or text
```

### 7.8 NPC 초상화 — 4종

#### `portrait_odin`

- 파일명: `npc_portrait_odin.png`
- 추가 참조: `Assets/Work/Cook/Graphics/TempImage/Odin.png`을 캐릭터 디자인 참조로 제공
- 개별 요청문:

```text
Odin, an adult humanoid golden dragon lord with long pale-golden hair, short swept horns, amber eyes, and a dignified black long coat with restrained gold embroidery; calm warm confidence and a slight knowing smile; preserve the recognizable hair, horns, and formal costume from the supplied Odin design reference while completely changing the rendering into the shared etched cookbook style
```

#### `portrait_nari`

- 파일명: `npc_portrait_nari.png`
- 개별 요청문:

```text
Nari, a young human apprentice adventurer with practical short travel hair, a modest leather-and-cloth outfit, a small notebook tucked against the chest, and a bright but cautious expression; clearly inexperienced, observant, and approachable
```

#### `portrait_boram`

- 파일명: `npc_portrait_boram.png`
- 개별 요청문:

```text
Boram, an adult lizardfolk cave gatherer with muted moss-green and brown scales, calm attentive eyes, a practical woven gathering strap, and simple cave-working clothes; thoughtful, grounded, and interested in ingredient preparation
```

#### `portrait_rook`

- 파일명: `npc_portrait_rook.png`
- 개별 요청문:

```text
Rook, an adult harpy scout with gray-brown feathers, a compact feather crest, keen eyes, and a practical wind-worn scarf over light scouting clothes; alert, concise, and reliable rather than aggressive
```

### 7.9 파견 지역 삽화 — 2종

#### `dispatch_moss_cave`

- 파일명: `dispatch_region_moss_cave.png`
- 지역 설명:

```text
an expansive damp cave with thick moss, broad edible fantasy mushrooms, shallow green pools, hanging roots, and a winding safe path disappearing into the cavern; pale lime-green biological accents kept muted and natural
```

- 개별 요청문:

```text
the Moss Cave dispatch region, an inviting but mysterious resource-gathering cavern whose moss, mushrooms, shallow pools, and navigable route are immediately readable
```

#### `dispatch_volcano`

- 파일명: `dispatch_region_volcano.png`
- 지역 설명:

```text
a rugged volcanic cavern with dark basalt terraces, thin restrained lava channels, mineral salt deposits, drifting smoke, and a narrow traversable route; limited burnt-red and orange accents
```

- 개별 요청문:

```text
the Volcano dispatch region, a hazardous but traversable resource-gathering area whose basalt, restrained lava, mineral deposits, and route are immediately readable
```

### 7.10 식당 배경 — 1종

#### `dungeon_restaurant_background`

- 파일명: `bg_dungeon_restaurant_interior.png`
- 프롬프트: 6장 E의 전체 프롬프트를 그대로 사용

## 8. 기존 에셋 수정 — 1종

### `GrindingIcon` 알파 복구

원본 `Assets/Work/Cook/Graphics/UIAsset/GrindingIcon.png`은 24-bit RGB 파일이다. 새 그림을 생성하는 작업이 아니라 배경 추출 편집으로 처리한다.

```text
Use case: background-extraction
Asset type: Unity 2D cooking-process Sprite repair
Input images: Image 1 is the existing `GrindingIcon.png`, the edit target
Primary request: remove only the flat background and produce a clean cutout on a genuinely transparent background
Constraints: preserve the mortar, pestle, crushed ingredient, exact composition, colors, outlines, hatching, stippling, dimensions, and scale; change only the background; preserve fine dark-brown line edges; no white halo; no restyling; no added object, text, frame, logo, or watermark
```

승인본은 기존 파일에 덮어쓰지 않고 `GrindingIcon-alpha-v2.png`처럼 저장한 뒤 Unity에서 교체한다.

## 9. 추후 시스템 확장 후보 — 8종

다음 항목은 현재 기능 완성에 즉시 필요하지 않으므로 앞의 41종 이후에 제작한다.

### 흐름 단계 아이콘 — 4종

| ASSET_ID | 파일명 | 개별 요청문 |
|---|---|---|
| `phase_adventure` | `ui_phase_adventure.png` | a compact rolled dungeon map with one winding cave route and a small mushroom landmark, no labels or letters |
| `phase_service` | `ui_phase_service.png` | a single welcoming serving cloche with one small heart-shaped curl of steam, representing guest hospitality |
| `phase_cooking` | `ui_phase_cooking.png` | a single sturdy bubbling cookpot with restrained steam, representing active cooking rather than serving |
| `phase_dispatch` | `ui_phase_dispatch.png` | a single tied leather expedition satchel with a small rolled map secured under its strap, representing dispatch preparation |

### 유지비·실패 아이콘 — 4종

| ASSET_ID | 파일명 | 개별 요청문 |
|---|---|---|
| `economy_bill` | `ui_economy_bill.png` | a curled parchment bill secured by a small coin-shaped seal, blank paper with no writing or numbers |
| `economy_upkeep` | `ui_economy_upkeep.png` | a single worn wooden gear with an aged coin embedded at its center, representing regular maintenance cost |
| `economy_insufficient` | `ui_economy_insufficient.png` | a single visibly cracked tavern coin split through the center, representing insufficient funds, no punctuation mark |
| `economy_game_over` | `ui_economy_game_over.png` | a closed hanging tavern sign with a small extinguished lantern beneath it, no written words, sad but not frightening |

이 8종은 공통 프롬프트 A를 사용한다. 파견 비용은 `ui_reward_coin.png`과 TMP의 마이너스 금액 표시를 재사용한다.

## 10. 생성 순서

1. `quality_perfect` — 전체 스타일 마스터
2. `quality_good`, `quality_normal`, `quality_poor`
3. `reaction_satisfied` — 반응 캐릭터 마스터
4. 나머지 반응 아이콘
5. 주문 태그, 동전, 반짝임, NPC 기본 이미지
6. 날짜와 시간
7. 공통 내비게이션과 챕터 북마크
8. 조리 미니게임 도구와 동작 표시
9. NPC 초상화
10. 파견 지역 삽화
11. 던전 식당 배경
12. 추후 흐름·경제 아이콘

각 패밀리의 첫 승인본은 같은 패밀리의 다음 이미지를 생성할 때 추가 참조 이미지로 사용한다. 수정 요청은 한 번에 한 가지 변경만 지정한다.

## 11. 승인 후 Unity 반입 규격

### 아이콘·도구

- 파일: PNG, 실제 알파 채널 유지
- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Pixels Per Unit: 100
- Mip Maps: Off
- Wrap Mode: Clamp
- Filter Mode: Bilinear
- Max Size: 256 또는 512
- Alpha Is Transparency: On

### NPC 초상화

- 권장 원본: 1024×1024
- Unity Max Size: 512
- 나머지 설정은 아이콘과 동일

### 지역·식당 배경

- 권장 비율: 16:9
- 권장 원본: 최소 1536×864, 가능하면 2048×1152 이상
- Texture Type: 용도에 따라 `Sprite (2D and UI)`
- Mip Maps: Off
- Wrap Mode: Clamp
- Max Size: 2048

## 12. 검수 체크리스트

- 대상이 요청한 하나의 오브젝트 또는 장면인가?
- UIAsset의 짙은 갈색 선, 크림색 면, 해칭이 보이는가?
- 기존 `UIPresentation`의 광택 금속·이모지 스타일이 남지 않았는가?
- 투명 에셋에 실제 알파 채널이 존재하는가?
- 흰색 또는 검은색 배경과 가장자리 halo가 없는가?
- 텍스트, 숫자, 워터마크, 불필요한 프레임이 없는가?
- 128px와 64px 축소 상태에서 의미가 구분되는가?
- 같은 패밀리의 선 굵기, 여백, 시점, 색 사용량이 통일되어 있는가?
- 승인 전 기존 에셋을 덮어쓰지 않았는가?

