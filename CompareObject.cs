using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace CompareObject
{
    /// <summary>
    /// Model class lưu trữ kết quả so sánh
    /// </summary>
    public class Difference
    {
        public object Current { get; set; }
        public object Expect { get; set; }
        public Dictionary<string, Difference> RowDifference { get; set; }
    }

    public class ObjectComparer
    {
        public static Dictionary<string, Difference> CompareObjects(object currentObject, object expectObject, string[] ignoreKeys = null, bool ignoreFalsy = false, bool ignoreArrayEmpty = false, bool isIgnoreType = false)
        {
            var differences = new Dictionary<string, Difference>();


            //if (!ignoreFalsy && (currentObject == null || expectObject == null))
            //{
            // throw new Exception("currentObject or expectObject is null or undefined");
            //}
            if (currentObject == null && expectObject == null)
            {
                return differences;

            }

            if ((currentObject == null || expectObject == null) && !currentObject.Equals(expectObject))
            {
                differences[""] = new Difference { Current = currentObject, Expect = expectObject };
                return differences;
            }

            CompareProperties("", currentObject, expectObject, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, differences);

            return differences;

        }

        private static void CompareProperties(string path, object currentObject, object expectObject, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences)
        {
            var currentDict = currentObject as IDictionary;
            var expectDict = expectObject as IDictionary;

            // so sánh nếu kiểu dữ liệu là nguyên thủy
            if (IsPrimaryValueType(currentObject, currentDict))
            {
                if (!currentObject.Equals(expectObject))
                {
                    resultDictionaryDifferences[path] = new Difference { Current = currentObject, Expect = expectObject };
                }
                return;
            }
            // so sánh nếu kiểu dữ liệu là phức hợp
            else if (currentDict != null && expectDict != null)
            {
                // nếu cả 2 object đều không null => so sánh kiểu

                CompareDictionary(path, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences, currentDict, expectDict);
            }
            // nếu currentDict hoặc expectDict null => Kiểu phức hợp và ko parse dc thành dictionary
            else
            {
                // nếu currentDict hoặc expectDict null => Kiểu phức hợp và ko parse dc thành dictionary
                CompareComplexData(path, currentObject, expectObject, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }
        }

        private static void CompareComplexData(string path, object currentObject, object expectObject, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences)
        {
            // kiểm tra currentObject
            if (IsJObject(currentObject))
            {
                CompareJSONObject(path, currentObject, expectObject, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }
            else if (IsComplexType(currentObject))
            {
                ComprareNormalObject(path, currentObject, expectObject, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }
            else if (IsListType(currentObject))
            {
                CompareListObject(path, currentObject, expectObject, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }
        }

        private static void CompareDictionary(string path, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences, IDictionary currentDict, IDictionary expectDict)
        {
            foreach (DictionaryEntry entry in currentDict)
            {
                var prop = (string)entry.Key;
                if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == prop))
                {
                    continue;
                }

                var propSubPath = String.IsNullOrWhiteSpace(path) ? prop : $"{path}.{prop}";
                if (expectDict.Contains(prop))
                {
                    // nếu expectDict cũng có prop đó thì so sánh sâu hơn
                    CompareProperties(propSubPath, entry.Value, expectDict[prop], ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
                }
                else
                {
                    // nếu currentDict có mà expectDict không có => Property này bị xóa
                    resultDictionaryDifferences[$"{propSubPath}"] = new Difference { Current = entry.Value, Expect = null };
                }
            }

            foreach (DictionaryEntry entry in expectDict)
            {
                var prop = (string)entry.Key;
                if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == prop))
                {
                    continue;
                }
                if (!currentDict.Contains(prop))
                {
                    // nếu currentDict ko có mà expectDict lại có => Property này được thêm
                    var propSubPath = String.IsNullOrWhiteSpace(path) ? prop : $"{path}.{prop}";

                    resultDictionaryDifferences[propSubPath] = new Difference { Current = null, Expect = entry.Value };
                }
            }
        }

        private static void CompareJSONObject(string path, object currentObject, object expectObject, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences)
        {

            // nếu dữ liệu currentObject là JObject
            var currentJSONObject = currentObject as JObject;
            var currentJSONObjectProperties = currentJSONObject.Properties();
            // so sánh current với expect
            foreach (var currentJSONObjectProperty in currentJSONObjectProperties)
            {
                // kiểm tra ignoreKey thì ko so sánh nữa
                if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == currentJSONObjectProperty.Name))
                {
                    continue;
                }

                object currentValue = currentJSONObjectProperty.Value;
                object expectValue = null;

                if (expectObject != null)
                {
                    var expectJSONObject = expectObject as JObject;
                    if (expectJSONObject != null && expectJSONObject.ContainsKey(currentJSONObjectProperty.Name))
                    {
                        expectValue = expectJSONObject[currentJSONObjectProperty.Name].Value<object>();
                    }
                }
                var propSubPath = String.IsNullOrWhiteSpace(path) ? currentJSONObjectProperty.Name : $"{path}.{currentJSONObjectProperty.Name}";

                CompareProperties(propSubPath, currentValue, expectValue, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }

            // trường hợp thêm mới
            if (IsJObject(expectObject))
            {
                var expectJSONObject = expectObject as JObject;
                // lặp qua expectedObject xem có key nào mới được thêm hay không
                var expectJSONObjectProperties = expectJSONObject.Properties();


                foreach (var expectJSONObjectProperty in expectJSONObjectProperties)
                {
                    if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == expectJSONObjectProperty.Name))
                    {
                        continue;
                    }

                    if (!currentJSONObject.ContainsKey(expectJSONObjectProperty.Name))
                    {
                        // nếu currentObjectProperty ko có mà expectObjectProperty lại có => Property này được thêm
                        var propSubPath = String.IsNullOrWhiteSpace(path) ? expectJSONObjectProperty.Name : $"{path}.{expectJSONObjectProperty.Name}";
                        resultDictionaryDifferences[propSubPath] = new Difference { Current = null, Expect = expectJSONObject[expectJSONObjectProperty.Name].Value<object>() };
                    }
                }
            }
            else
            {
                // nếu currentObject ko có mà ExpectObject lại có => Property này được thêm
                var propSubPath = String.IsNullOrWhiteSpace(path) ? "" : $"{path}";
                resultDictionaryDifferences[propSubPath] = new Difference { Current = currentObject, Expect = expectObject };
            }

        }

        private static void ComprareNormalObject(string path, object currentObject, object expectObject, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences)
        {
            // lấy danh sách props ở currentDict ra
            PropertyInfo[] currentObjectProperties = currentObject.GetType().GetProperties();

            foreach (var currentObjectProperty in currentObjectProperties)
            {
                if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == currentObjectProperty.Name))
                {
                    continue;
                }

                object currentValue = currentObjectProperty.GetValue(currentObject);
                object expectValue = null;

                if (expectObject != null)
                {
                    PropertyInfo expectProperty = expectObject.GetType().GetProperty(currentObjectProperty.Name);
                    if (expectProperty != null)
                    {
                        expectValue = expectProperty.GetValue(expectObject);
                    }
                }
                var propSubPath = String.IsNullOrWhiteSpace(path) ? currentObjectProperty.Name : $"{path}.{currentObjectProperty.Name}";

                CompareProperties(propSubPath, currentValue, expectValue, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, resultDictionaryDifferences);
            }
            if (IsComplexType(expectObject))
            {
                // lặp qua expectedObject xem có key nào mới được thêm hay không
                PropertyInfo[] expectObjectProperties = expectObject.GetType().GetProperties();
                foreach (var expectObjectProperty in expectObjectProperties)
                {
                    if (ignoreKeys != null && Array.Exists(ignoreKeys, k => k == expectObjectProperty.Name))
                    {
                        continue;
                    }

                    PropertyInfo currentObjectProperty = currentObject.GetType().GetProperty(expectObjectProperty.Name);
                    if (currentObjectProperty == null)
                    {
                        // nếu currentObjectProperty ko có mà expectObjectProperty lại có => Property này được thêm
                        var propSubPath = String.IsNullOrWhiteSpace(path) ? expectObjectProperty.Name : $"{path}.{expectObjectProperty.Name}";
                        resultDictionaryDifferences[propSubPath] = new Difference { Current = null, Expect = expectObjectProperty.GetValue(expectObject) };
                    }
                }
            }
            else
            {
                // nếu currentObject ko có mà ExpectObject lại có => Property này được thêm
                var propSubPath = String.IsNullOrWhiteSpace(path) ? "" : $"{path}";
                resultDictionaryDifferences[propSubPath] = new Difference { Current = currentObject, Expect = expectObject };
            }
        }

        private static void CompareListObject(string path, object currentObject, object expectObject, string[] ignoreKeys, bool ignoreFalsy, bool ignoreArrayEmpty, bool isIgnoreType, Dictionary<string, Difference> resultDictionaryDifferences)
        {
            // nếu là định dạng list => quy về bài toán so sánh list
            // convert object ra list
            // Giả định cả 2 list đã cùng được sắp xếp
            IList currentList = (IList)currentObject;
            IList expectList = (IList)expectObject;
            var isListDifferent = false;
            Dictionary<string, Difference> tableDifferences = new Dictionary<string, Difference>();
            for (int itemIndex = 0; itemIndex < Math.Max(currentList.Count, expectList.Count); itemIndex++)
            {
                var currentListItem = itemIndex < currentList.Count ? currentList[itemIndex] : null;
                var expectListItem = itemIndex < expectList.Count ? expectList[itemIndex] : null;
                var differentListItem = new Dictionary<string, Difference>();

                CompareProperties(path, currentListItem, expectListItem, ignoreKeys, ignoreFalsy, ignoreArrayEmpty, isIgnoreType, differentListItem);
                if (differentListItem.Count > 0)
                {

                    tableDifferences[itemIndex.ToString()] = new Difference { RowDifference = differentListItem };
                    isListDifferent = true;
                }
            }

            if (isListDifferent)
            {
                resultDictionaryDifferences[path] = new Difference { RowDifference = tableDifferences };
            }
        }

        private static bool IsPrimaryValueType(object currentObject, IDictionary currentDict)
        {
            return currentDict == null && currentObject != null && !IsComplexType(currentObject) && !IsListType(currentObject);
        }

        /// <summary>
        /// check dữ liệu dạng phức hợp (object, dicitonary)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool IsComplexType(object obj)
        {
            return obj != null && !obj.GetType().IsPrimitive && !obj.GetType().IsEnum && obj.GetType() != typeof(string) && obj.GetType() != typeof(List<object>) && !obj.GetType().IsArray && obj.GetType() != typeof(JArray) && obj.GetType() != typeof(JValue);
        }
        /// <summary>
        /// check dữ liệu JObject
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool IsJObject(object obj)
        {
            return obj != null && obj.GetType() == typeof(JObject);
        }

        /// <summary>
        /// check dữ liệu dạng Mảng
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool IsListType(object obj)
        {
            return obj != null && (obj.GetType() == typeof(List<object>) || obj.GetType() == typeof(JArray));
        }
    }

}
