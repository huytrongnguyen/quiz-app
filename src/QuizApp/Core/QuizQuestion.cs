using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuizApp.Common;

namespace QuizApp.Core;

public class QuizQuestion {
  [Key] public string QuestionID { get; set; }
  public string Content { get; set; }
  public int MaxResponseTime { get; set; } // in second
  public string CorrectChoice { get; set; }
  public string Choice1 { get; set; }
  public string Choice2 { get; set; }
  public string Choice3 { get; set; }

  [NotMapped] public List<string> Choices = [];

  public void ShuffleChoice() {
    var choiceMap = new Dictionary<int, string> {
      { 1, Choice1 },
      { 2, Choice2 },
      { 3, Choice3 },
      { 4, CorrectChoice },
    };

    var choiceList = new List<int> { 1, 2, 3, 4 };
    for (var i = 0; i < 4; ++i) {
      var randomIndex = UniformRandom.RandomInt(4);
      (choiceList[randomIndex], choiceList[i]) = (choiceList[i], choiceList[randomIndex]);
    }

    Choices = choiceList.Select(index => choiceMap[index]).ToList();
  }
}