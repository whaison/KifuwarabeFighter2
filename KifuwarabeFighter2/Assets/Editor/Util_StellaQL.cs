﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StellaQL
{
    /// <summary>
    /// 解説: 「UnityEditorを使って2D格闘(2D Fighting game)作るときのモーション遷移図作成の半自動化に挑戦しよう＜その４＞」 http://qiita.com/muzudho1/items/baf4b06cdcda96ca9a11
    /// </summary>
    public abstract class Util_StellaQL
    {
        /// <summary>
        /// 列挙型の扱い方：「文字列を列挙体に変換する」（DOBON.NET） http://dobon.net/vb/dotnet/programing/enumparse.html
        /// </summary>
        /// <param name="expression">例えば "( [ ( Alpha Cee ) ( Beta Dee ) ] { Eee } )" といった式。</param>
        /// <returns></returns>
        public static List<int> Execute(string expression, Type enumration)
        {
            List<int> result = new List<int>();
            List<string> tokens = new List<string>();

            // 字句解析
            {
                Util_StellaQL.String_to_tokens(expression, tokens);
                //StringBuilder sb = new StringBuilder();
                //foreach (string token in tokens)
                //{
                //    sb.Append("token: ");
                //    sb.AppendLine(token);
                //}
                //Debug.Log( sb.ToString() );
            }

            Stack<char> stack = new Stack<char>();

            // スキャン（XMLのSAX風）
            StellaQLParenthesisScanner scanner = new StellaQLParenthesisScanner(enumration);
            scanner.Scan(tokens);
            {
            }

            object enumElement = Enum.Parse(enumration, "Num"); // 変換できなかったら例外を投げる
            Debug.Log("enumElement = " + enumElement.ToString() + " typeof = " + enumElement.GetType());

            return result;
        }

        private static void String_to_tokens(string expression, List<string> tokens)
        {
            StringBuilder word = new StringBuilder();

            for (int iCaret = 0; iCaret < expression.Length; iCaret++)
            {
                char ch = expression[iCaret];
                switch (ch)
                {
                    case ' ':
                        if (0<word.Length)
                        {
                            tokens.Add(word.ToString());
                            word.Length = 0;
                        }
                        break;
                    case '(':
                    case '[':
                    case '{':
                    case ')':
                    case ']':
                    case '}':
                        tokens.Add(ch.ToString());
                        break;
                    default: word.Append(ch); break;
                }
            }

            if (0 < word.Length)//構文エラー
            {
                tokens.Add(word.ToString());
                word.Length = 0;
            }
        }
    }

    public class StructuredQuery
    {
        public StructuredQuery()
        {
            Target = "";
            Manipulation = "";
            Set = new Dictionary<string, string>();
            From_Fullname = "";
            From_Attr = "";
            To_Fullname = "";
            To_Attr = "";
        }

        public const string TRANSITION = "TRANSITION";
        public const string INSERT = "INSERT";
        public const string UPDATE = "UPDATE";
        public const string DELETE = "DELETE";
        public const string SELECT = "SELECT";
        public const string SET = "SET";
        public const string FROM = "FROM";
        public const string TO = "TO";
        public const string ATTR = "ATTR";

        /// <summary>
        /// Transition の１つ。大文字小文字は区別しない。
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// Insert、Update、Delete、Selectのいずれか。大文字小文字は区別しない。
        /// </summary>
        public string Manipulation { get; set; }
        /// <summary>
        /// SET部。大文字小文字は区別したい。
        /// </summary>
        public Dictionary<string,string> Set { get; set; }
        /// <summary>
        /// ステート・フルネーム が入る。
        /// </summary>
        public string From_Fullname { get; set; }
        /// <summary>
        /// 括弧を使った式 が入る。
        /// </summary>
        public string From_Attr { get; set; }
        /// <summary>
        /// ステート・フルネーム が入る。
        /// </summary>
        public string To_Fullname { get; set; }
        /// <summary>
        /// 括弧を使った式 が入る。
        /// </summary>
        public string To_Attr { get; set; }
    }

    public class StellaQLAggregater
    {
        public static Dictionary<int, AstateRecordable> Filtering_StateFullNameRegex(string pattern, Dictionary<int, AstateRecordable> universe)
        {
            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();

            Regex regex = new Regex(pattern);
            foreach (KeyValuePair<int, AstateRecordable> pair in universe)
            {
                if (regex.IsMatch(pair.Value.BreadCrumb + pair.Value.Name))
                {
                    hitRecords.Add(pair.Key, pair.Value);
                }
            }

            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_ElementsAnd(List<List<int>> lockers, Dictionary<int, AstateRecordable> universe)
        {
            List<int> recordIndexes = new List<int>();// レコード・インデックスを入れたり、削除したりする
            int iLocker = 0;
            foreach (List<int> locker in lockers)
            {
                if (0 == iLocker) // 最初のロッカーは丸ごと入れる。
                {
                    foreach (int recordIndex in locker) { recordIndexes.Add(recordIndex); }
                }
                else // ２つ目以降のロッカーは、全てのロッカーに共通する要素のみ残るようにする。
                {
                    for (int iElem = recordIndexes.Count - 1; -1 < iElem; iElem--)// 後ろから指定の要素を削除する。
                    {
                        if (!locker.Contains(recordIndexes[iElem])) { recordIndexes.RemoveAt(iElem); }
                    }
                }
                iLocker++;
            }

            HashSet<int> distinctRecordIndexes = new HashSet<int>();// 一応、重複を消しておく
            foreach (int recordIndex in recordIndexes) { distinctRecordIndexes.Add(recordIndex); }

            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();
            foreach (int recordIndex in distinctRecordIndexes) { hitRecords.Add(recordIndex, universe[recordIndex]); }
            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_OrElements(List<List<int>> lockers, Dictionary<int, AstateRecordable> universe)
        {
            HashSet<int> recordIndexes = new HashSet<int>();// どんどんレコード・インデックスを追加していく
            foreach (List<int> locker in lockers)
            {
                foreach (int recordIndex in locker)
                {
                    recordIndexes.Add(recordIndex);
                }
            }

            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();
            foreach (int recordIndex in recordIndexes) { hitRecords.Add(recordIndex, universe[recordIndex]); }
            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_ElementsNotAndNot(List<List<int>> lockers, Dictionary<int, AstateRecordable> universe)
        {
            HashSet<int> recordIndexes = new HashSet<int>();// どんどんレコード・インデックスを追加していく
            foreach (List<int> locker in lockers)
            {
                foreach (int recordIndex in locker)
                {
                    recordIndexes.Add(recordIndex);
                }
            }

            List<int> complementRecordIndexes = new List<int>();// 補集合を取る
            {
                foreach (int recordIndex in universe.Keys) { complementRecordIndexes.Add(recordIndex); }// 列挙型の中身をリストに移動。
                for (int iComp = complementRecordIndexes.Count - 1; -1 < iComp; iComp--)// 後ろから指定の要素を削除する。
                {
                    if (recordIndexes.Contains(complementRecordIndexes[iComp])) // 集合にある要素を削除
                    {
                        complementRecordIndexes.RemoveAt(iComp);
                    }
                }
            }

            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();
            foreach (int recordIndex in complementRecordIndexes) { hitRecords.Add(recordIndex, universe[recordIndex]); }
            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_AndAttributes(List<int> attrs, Dictionary<int, AstateRecordable> universe)
        {
            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>(universe);
            foreach (int attr in attrs)
            {
                Dictionary<int, AstateRecordable> records_empty = new Dictionary<int, AstateRecordable>();
                foreach (KeyValuePair<int, AstateRecordable> pair in hitRecords)
                {
                    if (pair.Value.HasFlag_attr(attr)) { records_empty.Add(pair.Key, pair.Value); }// 該当したもの
                }
                hitRecords = records_empty;
            }
            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_OrAttributes(List<int> attrs, Dictionary<int, AstateRecordable> universe)
        {
            HashSet<int> distinctAttr = new HashSet<int>();// まず属性の重複を除外
            foreach (int attr in attrs) { distinctAttr.Add(attr); }

            HashSet<int> hitRecordIndexes = new HashSet<int>();// レコード・インデックスを属性検索（重複除外）
            foreach (KeyValuePair<int, AstateRecordable> pair in universe)
            {
                foreach (int attr in distinctAttr)
                {
                    if (pair.Value.HasFlag_attr(attr)) { hitRecordIndexes.Add(pair.Key); }
                }
            }

            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();
            foreach (int recordIndex in hitRecordIndexes) { hitRecords.Add(recordIndex, universe[recordIndex]); }
            return hitRecords;
        }

        public static Dictionary<int, AstateRecordable> Filtering_NotAndNotAttributes(List<int> attrs, Dictionary<int, AstateRecordable> universe)
        {
            HashSet<int> distinctAttr = new HashSet<int>();// まず属性の重複を除外
            foreach (int attr in attrs) { distinctAttr.Add(attr); }

            HashSet<int> hitRecordIndexes = new HashSet<int>();// レコード・インデックスを属性検索（重複除外）
            foreach (KeyValuePair<int, AstateRecordable> pair in universe)
            {
                foreach (int attr in distinctAttr)
                {
                    if (pair.Value.HasFlag_attr(attr)) { hitRecordIndexes.Add(pair.Key); }
                }
            }

            List<int> complementRecordIndexes = new List<int>();// 補集合を取る
            {
                foreach (int recordIndex in universe.Keys) { complementRecordIndexes.Add(recordIndex); }// 列挙型の中身をリストに移動。
                for (int iComp = complementRecordIndexes.Count - 1; -1 < iComp; iComp--)// 後ろから指定の要素を削除する。
                {
                    if (hitRecordIndexes.Contains(complementRecordIndexes[iComp]))
                    {
                        // Debug.Log("Remove[" + iComp + "] (" + complementRecordIndexes[iComp] + ")");
                        complementRecordIndexes.RemoveAt(iComp);
                    }
                    // else { Debug.Log("Tick[" + iComp + "] (" + complementRecordIndexes[iComp] + ")"); }
                }
            }

            Dictionary<int, AstateRecordable> hitRecords = new Dictionary<int, AstateRecordable>();
            foreach (int recordIndex in complementRecordIndexes) { hitRecords.Add(recordIndex, universe[recordIndex]); }
            return hitRecords;
        }

        public static List<int> Keyword_to_locker(List<int> set, Type enumration)
        { // 列挙型要素を OR 結合して持つ。
            List<int> attrs = new List<int>();
            int sum = (int)Enum.GetValues(enumration).GetValue(0);//最初の要素は 0 にしておくこと。 列挙型だが、int 型に変換。
            foreach (object elem in set) { sum |= (int)elem; }// OR結合
            attrs.Add(sum); // 列挙型の要素を結合したものを int型として入れておく。
            return attrs;
        }

        public static List<int> KeywordList_to_locker(List<int> set, Type enumration)
        { // 列挙型要素を １つ１つ　ばらばらに持つ。
            List<int> attrs = new List<int>();
            foreach (int elem in set) { attrs.Add(elem); }// 列挙型の要素を１つ１つ入れていく。
            return attrs;
        }

        public static List<int> NGKeywordList_to_locker(List<int> set, Type enumration)
        {
            return Complement(set, enumration); // 補集合を返すだけ☆
        }

        /// <summary>
        /// 補集合
        /// </summary>
        public static List<int> Complement(List<int> set, Type enumration)
        {
            List<int> complement = new List<int>();
            {
                // 列挙型の中身をリストに移動。
                foreach (int elem in Enum.GetValues(enumration)) { complement.Add(elem); }
                // 後ろから指定の要素を削除する。
                for (int iComp = complement.Count - 1; -1 < iComp; iComp--)
                {
                    if (set.Contains(complement[iComp]))
                    {
                        Debug.Log("Remove[" + iComp + "] (" + complement[iComp] + ")");
                        complement.RemoveAt(iComp);
                    }
                    else
                    {
                        Debug.Log("Tick[" + iComp + "] (" + complement[iComp] + ")");
                    }
                }
            }
            return complement;
        }
    }

    public abstract class AbstractStellaQLParenthesisScanner
    {
        public void Scan(List<string> tokens)
        {
            string openParen = ""; // 閉じ括弧に対応する、「開きカッコ」
            int iCursor = 0;
            int lockerIndex = 0; // 部室のロッカー番号。スタートは 0 番から。
            while (iCursor < tokens.Count)
            {
                string token = tokens[iCursor];
                if ("" == openParen)
                {
                    this.OnGo(iCursor, token);
                    switch (token)
                    {
                        case ")": openParen = "("; tokens[iCursor] = ""; break;
                        case "]": openParen = "["; tokens[iCursor] = ""; break;
                        case "}": openParen = "{"; tokens[iCursor] = ""; break;
                        default: break; // 無視して進む
                    }
                }
                else // 後ろに進む☆
                {
                    this.OnBack(iCursor, token);
                    switch (token)
                    {
                        case "(":
                        case "[":
                        case "{": if (openParen == token) { openParen = ""; this.OnOpenParen(iCursor, token); lockerIndex++; } break;
                        default: this.OnKeywordGet(iCursor, token, lockerIndex); break;
                    }
                    tokens[iCursor] = ""; // 後ろに進んだ先では、その文字を削除する
                }

                if ("" == openParen) { iCursor++; }
                else { iCursor--; }
            }
        }

        abstract public void OnGo(int iCursor, string token);
        abstract public void OnBack(int iCursor, string token);
        abstract public void OnKeywordGet(int iCursor, string token, int locker);
        abstract public void OnOpenParen(int iCursor, string token);
    }

    public class StellaQLParenthesisScanner : AbstractStellaQLParenthesisScanner
    {
        /// <summary>
        /// ほんとは列挙型の要素として持っておきたいが、型指定できないので int 型として持っておく。
        /// </summary>
        private List<int> bufferKeywordEnums;
        private Type enumration;
        /// <summary>
        /// ほんとは列挙型の要素として持っておきたいが、型指定できないので int 型として持っておく。
        /// ２重のリストになっており、[ロッカー番号][要素番号] となる。
        /// </summary>
        private List<List<int>> lockerAttr;

        public StellaQLParenthesisScanner(Type enumration)
        {
            this.enumration = enumration;
            this.bufferKeywordEnums = new List<int>();
            this.lockerAttr = new List<List<int>>();
        }

        public override void OnGo(int iCursor, string token)
        {
            StringBuilder sb1 = new StringBuilder();
            sb1.Append("go["); sb1.Append(iCursor); sb1.Append("]: "); sb1.Append(token); sb1.AppendLine();
            Debug.Log(sb1.ToString());
        }

        public override void OnBack(int iCursor, string token)
        {
            StringBuilder sb2 = new StringBuilder();
            sb2.Append("back["); sb2.Append(iCursor); sb2.Append("]: "); sb2.Append(token); sb2.AppendLine();
            Debug.Log(sb2.ToString());
        }

        public override void OnKeywordGet(int iCursor, string token, int locker)
        {
            object enumElement = Enum.Parse(enumration, token); // 変換できなかったら例外を投げる
            this.bufferKeywordEnums.Add((int)enumElement);// 列挙型だが、int 型に変換。
        }

        public override void OnOpenParen(int iCursor, string token)
        {
            switch (token)
            {
                case "(":
                    this.lockerAttr.Add(
                        StellaQLAggregater.Keyword_to_locker(this.bufferKeywordEnums, enumration));
                    break;
                case "[":
                    this.lockerAttr.Add(
                        StellaQLAggregater.KeywordList_to_locker(this.bufferKeywordEnums, enumration));
                    break;
                case "{":
                    this.lockerAttr.Add(
                        StellaQLAggregater.NGKeywordList_to_locker(this.bufferKeywordEnums, enumration));
                    break;
            }
            this.bufferKeywordEnums.Clear();
        }
    }

    /// <summary>
    /// 正規表現の参考：http://smdn.jp/programming/netfx/regex/2_expressions/
    /// </summary>
    public class StellaQLScanner
    {
        public static void SkipSpace(string query, ref int caret)
        {
            while (caret < query.Length && ' ' == query[caret]) { caret++; }
        }
        private static Regex regexSpaces = new Regex(@"^(\s+)");
        public static bool HitSpaces(string query, ref int caret)
        {
            Match match = regexSpaces.Match(query.Substring(caret));
            if (match.Success) { caret += match.Groups[1].Value.Length; return true; }
            return false;
        }
        public static bool HitWordAndSpace_IgnoreCase(string word, string query, ref int caret)
        {
            int oldCaret = caret;
            if (caret == query.IndexOf(word, caret, StringComparison.OrdinalIgnoreCase))
            {
                caret += word.Length;
                if (caret == query.Length || HitSpaces(query, ref caret)) { return true; }
            }
            caret = oldCaret; return false;
        }
        /// <summary>
        /// "bear)" など後ろに半角スペースが付かないケースもあるので、スペースは 0 個も OK とする。
        /// </summary>
        private static Regex regexWordAndSpaces = new Regex(@"^(\w+)(\s*)", RegexOptions.IgnoreCase);
        public static bool GetWordAndSpace_IgnoreCase(string query, ref int caret, out string word)
        {
            Match match = regexWordAndSpaces.Match(query.Substring(caret));
            if (match.Success) { word = match.Groups[1].Value; caret += word.Length + match.Groups[2].Value.Length; return true; }
            word = ""; return false;
        }
        /// <summary>
        /// 浮動小数点の「.」もOKとする。
        /// </summary>
        private static Regex regexValueAndSpaces = new Regex(@"^((?:\w|\.)+)(\s*)", RegexOptions.IgnoreCase);
        public static bool GetValueAndSpace_IgnoreCase(string query, ref int caret, out string word)
        {
            Match match = regexValueAndSpaces.Match(query.Substring(caret));
            if (match.Success) { word = match.Groups[1].Value; caret += word.Length + match.Groups[2].Value.Length; return true; }
            word = ""; return false;
        }
        private static Regex regexStringAndSpaces = new Regex(@"^""((?:(?:\\"")|[^""])*)""(\s*)", RegexOptions.IgnoreCase);
        public static bool GetStringAndSpace_IgnoreCase(string query, ref int caret, out string stringWithoutDoubleQuotation)
        {
            Match match = regexStringAndSpaces.Match(query.Substring(caret));
            // ダブルクォーテーションの２文字分を足す
            if (match.Success) {
                stringWithoutDoubleQuotation = match.Groups[1].Value; caret += stringWithoutDoubleQuotation.Length + 2 + match.Groups[2].Value.Length;
                Debug.Log("stringWithoutDoubleQuotation=["+ stringWithoutDoubleQuotation + "] match.Groups[2].Value=[" + match.Groups[2].Value + "] match.Groups[2].Value.Length=["+ match.Groups[2].Value.Length + "]");
                return true;
            }
            stringWithoutDoubleQuotation = ""; return false;
        }
        public static bool GetParentesisAndSpace_IgnoreCase(string query, ref int caret, out string parentesis)
        {
            int oldCaret = caret;
            string word;
            Stack<char> closeParen = new Stack<char>();

            // 開始時
            switch (query[caret])
            {
                case '(': closeParen.Push(')'); caret++; break;
                case '[': closeParen.Push(']'); caret++; break;
                case '{': closeParen.Push('}'); caret++; break;
                default: goto gt_Failure;
            }
            HitSpaces(query, ref caret);

            while (caret < query.Length)
            {
                switch (query[caret])
                {
                    case '(': closeParen.Push(')'); caret++; break;
                    case '[': closeParen.Push(']'); caret++; break;
                    case '{': closeParen.Push('}'); caret++; break;
                    case ')':
                    case ']':
                    case '}':
                        if (query[caret] != closeParen.Peek()) { goto gt_Failure; }
                        closeParen.Pop(); caret++; if (0 == closeParen.Count) { goto gt_Finish; } break;
                    default: if (!GetWordAndSpace_IgnoreCase(query, ref caret, out word)) { goto gt_Failure; } break;
                }
            }

            gt_Finish:
            Debug.Log("oldCaret=["+ oldCaret + "] caret=["+ caret + "] query.Length=[" + query.Length+ "] query=["+ query+"]");
            if (caret==query.Length)
            {
                parentesis = query.Substring(oldCaret);
            }
            else
            {
                parentesis = query.Substring(oldCaret, caret);
            }
            HitSpaces(query, ref caret);
            return true;
        gt_Failure:
            parentesis = "";
            return false;
        }

        /// <summary>
        /// 例
        /// TRANSITION INSERT
        /// SET Duration 0 ExitTime 1
        /// FROM “Base Layer.SMove”
        /// TO ATTR (BusyX Block)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static StructuredQuery Parser_InsertStatement(string query)
        {
            StructuredQuery sq = new StructuredQuery();
            int caret = 0;
            string propertyName;
            string propertyValue;
            string stringWithoutDoubleQuotation;
            string parenthesis;
            SkipSpace(query, ref caret);

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TRANSITION, query, ref caret)) { goto gt_endMethod; }
            sq.Target = StructuredQuery.TRANSITION;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.INSERT, query, ref caret)) { goto gt_endMethod; }
            sq.Manipulation = StructuredQuery.INSERT;

            if (HitWordAndSpace_IgnoreCase(StructuredQuery.SET, query, ref caret))
            {
                // 「項目名、スペース、値、スペース」の繰り返し。項目名が FROM だった場合終わり。
                while (caret < query.Length && !HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret))
                {
                    if (!GetWordAndSpace_IgnoreCase(query, ref caret, out propertyName)) { goto gt_endMethod; }
                    if (!GetValueAndSpace_IgnoreCase(query, ref caret, out propertyValue)) { goto gt_endMethod; }
                    sq.Set.Add(propertyName, propertyValue);
                }
            }
            else
            {
                if (!HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret)) { goto gt_endMethod; }
            }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.From_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.From_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TO, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.To_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.To_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            gt_endMethod:
            return sq;
        }

        /// <summary>
        /// 例
        /// TRANSITION UPDATE
        /// SET Duration 0.25 ExitTime 0.75
        /// FROM “Base Layer.SMove”
        /// TO ATTR (BusyX Block)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static StructuredQuery Parser_UpdateStatement(string query)
        {
            StructuredQuery sq = new StructuredQuery();
            int caret = 0;
            string propertyName;
            string propertyValue;
            string stringWithoutDoubleQuotation;
            string parenthesis;
            SkipSpace(query, ref caret);

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TRANSITION, query, ref caret)) { goto gt_endMethod; }
            sq.Target = StructuredQuery.TRANSITION;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.UPDATE, query, ref caret)) { goto gt_endMethod; }
            sq.Manipulation = StructuredQuery.UPDATE;

            if (HitWordAndSpace_IgnoreCase(StructuredQuery.SET, query, ref caret))
            {
                // 「項目名、スペース、値、スペース」の繰り返し。項目名が FROM だった場合終わり。
                while (caret < query.Length && !HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret))
                {
                    if (!GetWordAndSpace_IgnoreCase(query, ref caret, out propertyName)) { goto gt_endMethod; }
                    if (!GetValueAndSpace_IgnoreCase(query, ref caret, out propertyValue)) { goto gt_endMethod; }
                    sq.Set.Add(propertyName, propertyValue);
                }
            }
            else
            {
                if (!HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret)) { goto gt_endMethod; }
            }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.From_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.From_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TO, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.To_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.To_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            gt_endMethod:
            return sq;
        }

        /// <summary>
        /// 例
        /// TRANSITION DELETE
        /// FROM “Base Layer.SMove”
        /// TO ATTR (BusyX Block)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static StructuredQuery Parser_DeleteStatement(string query)
        {
            StructuredQuery sq = new StructuredQuery();
            int caret = 0;
            string stringWithoutDoubleQuotation;
            string parenthesis;
            SkipSpace(query, ref caret);

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TRANSITION, query, ref caret)) { goto gt_endMethod; }
            sq.Target = StructuredQuery.TRANSITION;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.DELETE, query, ref caret)) { goto gt_endMethod; }
            sq.Manipulation = StructuredQuery.DELETE;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.From_Fullname = stringWithoutDoubleQuotation;
            }
            else if(HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.From_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TO, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.To_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.To_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            gt_endMethod:
            return sq;
        }

        /// <summary>
        /// TRANSITION SELECT
        /// FROM “Base Layer.SMove”
        /// TO ATTR (BusyX Block)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static StructuredQuery Parser_SelectStatement(string query)
        {
            StructuredQuery sq = new StructuredQuery();
            int caret = 0;
            string stringWithoutDoubleQuotation;
            string parenthesis;
            SkipSpace(query, ref caret);

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TRANSITION, query, ref caret)) { goto gt_endMethod; }
            sq.Target = StructuredQuery.TRANSITION;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.SELECT, query, ref caret)) { goto gt_endMethod; }
            sq.Manipulation = StructuredQuery.SELECT;

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.FROM, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.From_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.From_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            if (!HitWordAndSpace_IgnoreCase(StructuredQuery.TO, query, ref caret)) { goto gt_endMethod; }

            // 「"文字列"」か、「ATTR ～」のどちらか。
            if (GetStringAndSpace_IgnoreCase(query, ref caret, out stringWithoutDoubleQuotation))
            {
                sq.To_Fullname = stringWithoutDoubleQuotation;
            }
            else if (HitWordAndSpace_IgnoreCase(StructuredQuery.ATTR, query, ref caret))
            {
                if (!GetParentesisAndSpace_IgnoreCase(query, ref caret, out parenthesis)) { goto gt_endMethod; }
                sq.To_Attr = parenthesis;
            }
            else
            {
                goto gt_endMethod;
            }

            gt_endMethod:
            return sq;
        }
    }

}
