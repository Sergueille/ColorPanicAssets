using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Contains all the information to be saved, except objectives
/// </summary>
public class SaveData
{
	public int nbGames = 0;//nb of thimes the player played a game
	public bool firstPowerup = true;//have the user already used a powerup? (for help message)

    public int[] lastScores = new int[] { 0, 0, 0 };
    public int[] bestScores = new int[] { 0, 0, 0 };

    public int money = 100;

    public Color[] colors = GameManager.instance.defaultColors;
	public List<Color> unlockedColors = new List<Color>();

	public float soundsVolume = 1;
	public bool vibrationOn = true;
	public ContolType controlType = ContolType.fingerDirection;
	public ColorBtnsType buttonPosType = ColorBtnsType.left;

	public Language language = Language.system;

    public static void Save(SaveData data)
	{
		File.WriteAllText(Application.persistentDataPath + "/save", string.Empty);
		using (var stream = File.Open(Application.persistentDataPath + "/save", FileMode.OpenOrCreate))
		{
			new XmlSerializer(typeof(SaveData)).Serialize(stream, data);
		};
	}

    public static SaveData Load()
	{
		if (File.Exists(Application.persistentDataPath + "/save"))
		{
			using (var stream = File.OpenRead(Application.persistentDataPath + "/save"))
			{
				try
				{
					return new XmlSerializer(typeof(SaveData)).Deserialize(stream) as SaveData;
				}
				catch (System.Exception e)
				{
					Debug.LogError("Can't read save XML file, Creating new save data :\n" + e.Message);
					return new SaveData();
				}
			};
		}
		else
		{
			Debug.Log("Save file does not exists! Creating new save data");
			return new SaveData();
		}
	}
}
