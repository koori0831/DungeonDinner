// Explicit observation IDs are emitted on each displayed line, never on an unchosen option.
const fs = require('fs');
function ids(text) {
  const result = [];
  if (/새싹\s*슬라임/.test(text)) result.push('monster:sprout_slime');
  else if (/암염\s*슬라임/.test(text)) result.push('monster:rock_salt_slime');
  else if (/슬라임/.test(text) && !/슬라임\s*(점액|핵)만/.test(text)) result.push('monster:slime');
  if (/머쉬룸맨|머시룸맨|버섯\s*주민/.test(text)) result.push('monster:mushroom_man');
  if (/코코넛\s*게(?!살)/.test(text)) result.push('monster:coconut_crab');
  return result;
}
function annotate(paths) {
  for (const path of paths) {
    const original = fs.readFileSync(path, 'utf8');
    const text = original.replace(/^(\s*)- <Context>k__BackingField: ("[^\n]*")\r?\n(?:\1  discoveryEntryIds:.*\r?\n(?:\1  - .*\r?\n)*)?/gm,
      (line, indent, encoded) => {
        const discovered = ids(JSON.parse(encoded));
        return `${indent}- <Context>k__BackingField: ${encoded}\n${indent}  discoveryEntryIds:${discovered.length ? '\n' + discovered.map(id => indent + '  - ' + id).join('\n') : ' []'}\n`;
      });
    if (original !== text) fs.writeFileSync(path, text);
  }
}
module.exports = { ids, annotate };
