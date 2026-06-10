namespace ClothingStore.Helpers;

public static class VietnameseStringHelper
{
    public static string NormalizeTelexForSearch(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Basic mapping for trailing telex characters that cause incomplete words
        // e.g. "As" -> "Á", "Aos" -> "Áo"
        // This is done case-insensitively, but preserving the original string's overall structure
        // Since we are searching, we can just lowercase the whole thing to simplify.
        var lowerInput = input.ToLower();

        // Dictionary of Telex combinations specifically for partial typing
        var telexMap = new Dictionary<string, string>
        {
            // A variations
            {"aas", "ấ"}, {"aaf", "ầ"}, {"aax", "ẫ"}, {"aar", "ẩ"}, {"aaj", "ậ"},
            {"aws", "ắ"}, {"awf", "ằ"}, {"awx", "ẵ"}, {"awr", "ẳ"}, {"awj", "ặ"},
            {"aos", "áo"}, {"aof", "ào"}, {"aox", "ão"}, {"aor", "ảo"}, {"aoj", "ạo"},

            // E variations
            {"ees", "ế"}, {"eef", "ề"}, {"eex", "ễ"}, {"eer", "ể"}, {"eej", "ệ"},

            // O variations
            {"oos", "ố"}, {"oof", "ồ"}, {"oox", "ỗ"}, {"oor", "ổ"}, {"ooj", "ộ"},
            {"ows", "ớ"}, {"owf", "ờ"}, {"owx", "ỡ"}, {"owr", "ở"}, {"owj", "ợ"},

            // U variations
            {"uws", "ứ"}, {"uwf", "ừ"}, {"uwx", "ữ"}, {"uwr", "ử"}, {"uwj", "ự"},

            // Basic 2-char combinations
            {"as", "á"}, {"af", "à"}, {"ax", "ã"}, {"ar", "ả"}, {"aj", "ạ"},
            {"os", "ó"}, {"of", "ò"}, {"ox", "õ"}, {"or", "ỏ"}, {"oj", "ọ"},
            {"es", "é"}, {"ef", "è"}, {"ex", "ẽ"}, {"er", "ẻ"}, {"ej", "ẹ"},
            {"us", "ú"}, {"uf", "ù"}, {"ux", "ũ"}, {"ur", "ủ"}, {"uj", "ụ"},
            {"is", "í"}, {"if", "ì"}, {"ix", "ĩ"}, {"ir", "ỉ"}, {"ij", "ị"},
            {"ys", "ý"}, {"yf", "ỳ"}, {"yx", "ỹ"}, {"yr", "ỷ"}, {"yj", "ỵ"},
            
            // Basic vowel shifts
            {"aw", "ă"}, {"aa", "â"}, {"ee", "ê"}, {"oo", "ô"}, {"ow", "ơ"}, {"uw", "ư"}, {"dd", "đ"}
        };

        // We replace longer strings first to avoid partial replacements breaking larger ones.
        // e.g. "aas" should be evaluated before "as"
        var keys = telexMap.Keys.OrderByDescending(k => k.Length).ToList();
        
        string result = lowerInput;
        foreach (var key in keys)
        {
            result = result.Replace(key, telexMap[key]);
        }

        return result;
    }
}
