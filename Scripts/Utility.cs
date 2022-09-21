using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

// Utility and enums!

[System.Serializable]
public struct Range
{
    public float min, max;

	public Range(float min, float max)
	{
		this.min = min;
		this.max = max;
	}

	public float PickRandom()
        => Random.Range(min, max);
}

[System.Serializable]
public struct DifficultyRange
{
    public Range easy;
    public Range medium;
    public Range hard;
	public Range veryHard;

	public DifficultyRange(Range easy, Range medium, Range hard, Range veryHard)
	{
		this.easy = easy;
		this.medium = medium;
		this.hard = hard;
		this.veryHard = veryHard;
	}

	public float PickRandom()
	{
		return GameManager.instance.currentDifficulty switch
		{
			GameManager.difficulty.easy => easy.PickRandom(),
			GameManager.difficulty.medium => medium.PickRandom(),
			GameManager.difficulty.hard => hard.PickRandom(),
			GameManager.difficulty.veryHard => veryHard.PickRandom(),
			_ => easy.PickRandom(),
		};
	}
}

[System.Serializable]
public class Objective
{
	public int reward;
	public ObjectiveType type;
	public float amount;
	public GameManager.difficulty difficulty;

	[HideInInspector] [XmlIgnore] public ObjectiveDisplay display;

	public Objective(Objective clone)
	{
		this.reward = clone.reward;
		this.type = clone.type;
		this.amount = clone.amount;
		this.difficulty = clone.difficulty;
	}

	public Objective() { }

	public string description
	{
		get
		{
			string difficultyString = difficulty switch
			{
				GameManager.difficulty.easy => LocalisationSystem.GetLocalisedText("easy").ToLower(),
				GameManager.difficulty.medium => LocalisationSystem.GetLocalisedText("medium").ToLower(),
				GameManager.difficulty.hard => LocalisationSystem.GetLocalisedText("hard").ToLower(),
				_ => "[error]",
			};

			return type switch
			{
				ObjectiveType.score => string.Format(LocalisationSystem.GetLocalisedText("score obj"), amount, difficultyString),
				ObjectiveType.noMove => string.Format(LocalisationSystem.GetLocalisedText("no move obj"), amount, difficultyString),
				ObjectiveType.noColor => string.Format(LocalisationSystem.GetLocalisedText("no color obj"), amount, difficultyString),
				ObjectiveType.money => string.Format(LocalisationSystem.GetLocalisedText("money obj"), amount, difficultyString),
				_ => "[Unknown]",
			};
		}
	}

	public bool completed
	{
		get
		{
			return type switch
			{
				ObjectiveType.score => GameManager.instance.saveData.lastScores[(int)GameManager.instance.currentDifficulty] >= amount
					&& GameManager.instance.lastDifficulty == difficulty,
				ObjectiveType.noMove => GameManager.instance.maxWithoutMove >= amount 
					&& GameManager.instance.lastDifficulty == difficulty,
				ObjectiveType.noColor => GameManager.instance.maxWithoutColorChange >= amount
					&& GameManager.instance.lastDifficulty == difficulty,
				ObjectiveType.money => GameManager.instance.lastCoinAmount >= amount
					&& GameManager.instance.lastDifficulty == difficulty,
				_ => false,
			};
		}
	}
}

public enum ObjectiveType { score, noMove, noColor, money }

public enum PowerupType { slowMotion, freeze, scoreMult, moneyMult }

[System.Serializable]
public enum Language
{
	system,
	english,
	french,
	russian
}

[System.Serializable]
public enum ContolType { fingerDirection, moveTowardsFinger, relativeToStart, fixedJoysick, arrows, WASD, ZQSD }

[System.Serializable]
public enum ColorBtnsType { left, leftSpaced, downLeft, downRight, right, rightSpace, numbers }

[System.Serializable]
public struct ImageColor
{
	public Image image;
	public int colorID;
}

static class Utility
{
	public static bool CompareColors(Color a, Color b)
	{
		float distR = Mathf.Abs(a.r - b.r);
		float distG = Mathf.Abs(a.g - b.g);
		float distB = Mathf.Abs(a.b - b.b);

		return distB < 0.05f && distG < 0.05f && distR < 0.05f;
	}

	public static Vector2 RandomPosition(this Rect rect)
	{
		return new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
	}
}