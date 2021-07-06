using System.Text.RegularExpressions;

public class StringTools
{
    /// <summary>
    /// 获取第一个匹配
    /// </summary>
    /// <param name="str"></param>
    /// <param name="regexStr"></param>
    /// <returns></returns>
    static public string GetFirstMatch(string str, string regexStr)
    {
        Match m = Regex.Match(str, regexStr);
        if (!string.IsNullOrEmpty(m.ToString()))
        {
            return m.ToString();
        }
        return null;
    }
}

