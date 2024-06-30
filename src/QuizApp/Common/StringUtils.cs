namespace QuizApp.Common;

public static class StringUtils {
  public static bool IsEmpty(this string str) => str == null || str.Trim() == "";

  public static int ParseInt(this string str, int defaultValue = 0) => double.TryParse(str, out double value) ? Convert.ToInt32(value) : defaultValue;
}