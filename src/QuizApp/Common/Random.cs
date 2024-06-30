namespace QuizApp.Common;

public static class UniformRandom {
  public static int RandomInt(int iMaxVal) => RandomInt(0, iMaxVal);
  public static int RandomInt(int iMinVal, int iMaxVal) => srand.Next(iMinVal, iMaxVal);

  private static readonly Random srand = new ((int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}