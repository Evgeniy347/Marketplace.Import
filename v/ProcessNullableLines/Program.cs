using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProcessNullableLines
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            string value = @"Тестовое оповещение
1 кейс
<?>Тип отпуска: {Тип отпуска}.
2 кейс
<?>Тип отпуска: {Тип отпуска} {Вид документа}.
3 кейс
Вид документа: {Вид документа}<?> Тип отпуска: {Тип отпуска}.
Инициатор: {Инициатор}.
4 кейс
Вид документа: {Вид документа}<?> Тип отпуска: {Тип отпуска}<?> Тип отпуска: {Тип отпуска}.
Инициатор: {Инициатор}.
5 кейс
Вид документа: [Вид документа]<?> Тип отпуска: [Тип отпуска]<?> Тип отпуска: [Тип отпуска].
Инициатор: [Инициатор]. 

";

            string result = ProcessNullableLines(value);
            result = ProcessLines(result);

            Console.WriteLine(result);
            Console.ReadLine();
        }

        public static string ProcessLines(string sourceMessage)
        {
            string resultMessage = sourceMessage;
            while (true)
            {
                StringExtensions.SubstringInfo fieldInfo = resultMessage.Substring(0, StringExtensions.SymbolPara.SquareBrackets, StringExtensions.SymbolPara.CurlyBrackets);
                if (fieldInfo.NotFind)
                    break;

                string fieldVal = GetFieldValueObjWithLookups(fieldInfo.Result, false);

                resultMessage = resultMessage.Replace(fieldVal, fieldInfo.StartIndex, fieldInfo.EndIndex + 1);
            }

            return resultMessage;
        }

        public static string ProcessNullableLines(string sourceMessage)
        {
            string resultMessage = sourceMessage;
            string specValue = "<?>";

            int index = resultMessage.IndexOf(specValue);

            while (index != -1)
            {
                StringExtensions.SubstringInfo fieldInfo = resultMessage.Substring(index, StringExtensions.SymbolPara.SquareBrackets, StringExtensions.SymbolPara.CurlyBrackets);

                if (!fieldInfo.NotFind)
                {
                    bool hasHeaderValue = HasHeaderValue(resultMessage, index);
                    int endPhrase;
                    if (hasHeaderValue)
                    {
                        //Если перед "<?>" есть значение то заменяем только выражение, а не всю строку
                        endPhrase = fieldInfo.EndIndex + 1;
                    }
                    else
                    {
                        //Если перед "<?>" отсутствует значение и после выражения отсутствуют символы "[", "{", "<?>", Environment.NewLine
                        //то заменяем всю строку, включая символ переноса

                        endPhrase = resultMessage.IndexOf(fieldInfo.EndIndex, "[", "{", specValue, Environment.NewLine);
                        if (endPhrase == -1)
                            endPhrase = resultMessage.Length;

                        bool removeEndSymbol = !hasHeaderValue && resultMessage.Substring(endPhrase).StartsWith(Environment.NewLine);

                        if (removeEndSymbol)
                            endPhrase += Environment.NewLine.Length;
                    }

                    string fieldVal = GetFieldValueObjWithLookups(fieldInfo.Result, true);

                    //Удаляем только выражение "<?>" или всю фразу целиком
                    int count = string.IsNullOrEmpty(fieldVal?.Trim()) ?
                        endPhrase - index :
                        specValue.Length;

                    resultMessage = resultMessage.Remove(index, count);
                }

                index = resultMessage.IndexOf(specValue);
            }

            return resultMessage;
        }

        private static bool HasHeaderValue(string sourceMessage, int index)
        {
            int startIndex = sourceMessage.LastIndexOf(Environment.NewLine, index);
            int addIndex = Environment.NewLine.Length;
            if (startIndex == -1)
            {
                startIndex = 0;
                addIndex = 0;
            }

            int count = index - startIndex - addIndex;
            if (count < 0)
                return true;

            string headerValue = sourceMessage.Substring(startIndex + addIndex, index - startIndex - addIndex);
            bool hasHeaderValue = !string.IsNullOrEmpty(headerValue.Trim());
            return hasHeaderValue;
        }

        public static string GetFieldValueObjWithLookups(string fieldName, bool hide)
        {

            if (fieldName == "Вид документа")
                return "DocType";

            if (fieldName == "Инициатор")
                return "Initiator";

            if (fieldName == "Тип отпуска")
                return "Holiday";


            //if (hide)
            return null;
            //else
            //    return $"testValue#{fieldName}#";
        }
    }

    internal static class StringExtensions
    {
        public static SubstringInfo Substring(this string value, int start, params SymbolPara[] symbolParas)
        {
            return symbolParas.Select(x => value.Substring(start, x.StartSymbol, x.EndSymbol))
                   .Where(x => !x.NotFind)
                   .OrderBy(x => x.StartIndex)
                   .FirstOrDefault() ?? new SubstringInfo();
        }

        public static int IndexOf(this string source, int start, params string[] values)
        {
            if (string.IsNullOrEmpty(source))
                return -1;

            string resultValue = null;
            int index = -1;

            foreach (string value in values)
            {
                int indexValue = source.IndexOf(value, start);
                if (index == -1 || indexValue != -1 && indexValue < index)
                    index = indexValue;

                resultValue = value;
            }

            return index;
        }

        public static SubstringInfo Substring(this string value, int start, string startSymbol, string endSymbol)
        {
            SubstringInfo result = new SubstringInfo();

            result.StartIndex = value.IndexOf(startSymbol, start);
            result.EndIndex = value.IndexOf(endSymbol, result.StartIndex + startSymbol.Length);

            if (result.NotFind)
                return result;

            result.Result = value.Substring(result.StartIndex + startSymbol.Length, result.EndIndex - result.StartIndex - startSymbol.Length);

            return result;
        }

        public static string Replace(this string value, string newValue, int startIndex, int endIndex)
        {
            string resultValue = value
                    .Remove(startIndex, endIndex - startIndex)
                    .Insert(startIndex, newValue ?? string.Empty);

            return resultValue;
        }

        public class SubstringInfo
        {
            public string Result { get; set; }

            public int StartIndex { get; set; } = -1;

            public int EndIndex { get; set; } = -1;

            public bool NotFind => EndIndex == -1 || StartIndex == -1;
        }

        public readonly struct SymbolPara
        {
            public readonly string StartSymbol;
            public readonly string EndSymbol;

            public SymbolPara(string startSymbol, string endSymbol)
            {
                StartSymbol = startSymbol;
                EndSymbol = endSymbol;
            }

            public SymbolPara(string symbol)
            {
                StartSymbol = symbol;
                EndSymbol = symbol;
            }

            public static SymbolPara SquareBrackets { get; } = new SymbolPara("[", "]");
            public static SymbolPara RoundBrackets { get; } = new SymbolPara("(", ")");
            public static SymbolPara CurlyBrackets { get; } = new SymbolPara("{", "}");
        }
    }
}
