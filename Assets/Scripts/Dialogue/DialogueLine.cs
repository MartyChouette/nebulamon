using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula
{
    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public string chirpProfile; // optional
    }

    [CreateAssetMenu(menuName = "Nebula/Dialogue/CSV Database")]
    public class DialogueDatabaseCSV : ScriptableObject
    {
        public TextAsset csvFile;

        private Dictionary<string, List<DialogueLine>> _convos;

        public void BuildIfNeeded()
        {
            if (_convos != null) return;
            _convos = new Dictionary<string, List<DialogueLine>>();

            if (csvFile == null)
            {
                Debug.LogError("DialogueDatabaseCSV: csvFile is null.");
                return;
            }

            string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return; // header only

            // header: conversation_id,line_index,speaker,text,chirp_profile
            for (int i = 1; i < lines.Length; i++)
            {
                string raw = lines[i];
                var cols = ParseCsvRow(raw);
                if (cols.Count < 4) continue;

                string convoId = cols[0].Trim();
                // cols[1] line_index (we can ignore if file is sorted; still parse to keep format stable)
                string speaker = cols[2].Trim();
                string text = cols[3];
                string chirp = (cols.Count >= 5) ? cols[4].Trim() : "";

                if (!_convos.TryGetValue(convoId, out var list))
                {
                    list = new List<DialogueLine>();
                    _convos.Add(convoId, list);
                }

                list.Add(new DialogueLine { speaker = speaker, text = text, chirpProfile = chirp });
            }
        }

        public bool TryGetConversation(string convoId, out List<DialogueLine> lines)
        {
            lines = null;

            BuildIfNeeded();
            if (_convos == null) return false;

            return _convos.TryGetValue(convoId, out lines);
        }



        // Minimal CSV parsing with quoted fields.
        private static List<string> ParseCsvRow(string row)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var cur = new System.Text.StringBuilder();

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];

                if (c == '"')
                {
                    // double-quote escape
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        cur.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(cur.ToString());
                    cur.Length = 0;
                }
                else
                {
                    cur.Append(c);
                }
            }

            result.Add(cur.ToString());
            return result;
        }
    }
}
