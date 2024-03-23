using System.Text.RegularExpressions;
using ArabicSupport;

public class ArabicHelper {
	private static Regex arabicRanges = new Regex(@"[\u0600-\u06FF\u0750-\u076D\uFB50-\uFBFF\uFC5E-\uFC62\uFD3E-\uFDFC\uFE80-\uFEFC]");

	/// <summary>
	/// Check whether a string contains Arabic characters
	/// </summary>
	public static bool ContainsArabic(string str) {
		if (string.IsNullOrEmpty(str)) {
			return false;
		}
		return arabicRanges.IsMatch(str);
	}

	/// <summary>
	/// Determines whether arabic characters are included in the input text and
	/// if so, corrects the arabic characters (RTL text and character variants)
	/// </summary>
	public static string CorrectText(string str) {
		if (ContainsArabic(str)) {
			return ArabicFixer.Fix(str, false, false);
		} else {
			return str;
		}
	}
}
