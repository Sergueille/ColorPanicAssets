using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Contains all the information to be saved, except objectives
/// </summary>
public class SaveData
{
	public int nbGames = 0;//nb of thimes the player played a game
	public bool firstPowerup = true;//have the user already used a powerup? (for help message)

    public int[] lastScores = new int[] { 0, 0, 0, 0 };
    public int[] bestScores = new int[] { 0, 0, 0, 0 };

    public int money = 100;

    public Color[] colors = GameManager.instance.defaultColors;
	public List<Color> unlockedColors = new List<Color>();
	public bool randomColors = false;

	public float soundsVolume = 1;
	public bool vibrationOn = true;
	public ContolType controlType = ContolType.fingerDirection;
	public ColorBtnsType buttonPosType = ColorBtnsType.left;
	public int downscaleFactorId = 0;

	public Language language = Language.system;

    public static void Save(SaveData data)
	{
		// Clear file
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
					SaveData res = new XmlSerializer(typeof(SaveData)).Deserialize(stream) as SaveData;

					// Make save compatible with new difficulty levels
					if (res.lastScores.Length != GameManager.difficultyCount)
					{
                        int[] oldLastScores = res.lastScores;
                        res.lastScores = new int[GameManager.difficultyCount];

						for (int i = 0; i < oldLastScores.Length; i++)
							res.lastScores[i] = oldLastScores[i];

                        int[] oldBestScores = res.bestScores;
                        res.bestScores = new int[GameManager.difficultyCount];

                        for (int i = 0; i < oldBestScores.Length; i++)
                            res.bestScores[i] = oldBestScores[i];
                    }

                    return res;
				}
				catch (System.Exception e)
				{
					Debug.LogError("Can't read save file :\n" + e.Message);
					return null;
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
