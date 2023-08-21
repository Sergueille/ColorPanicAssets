using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	const float PP_TRANS_TIME = 0.5f;
	//const int START_MONEY_AMOUNT = 100;
	const float MONEY_INCREASE_TIME = 1.5f;
	const float TIME_BTW_NEW_OBJ = 0.3f;
	const float COLOR_SELECT_TRANS = 0.2f;
	const float COLOR_SELECT_SCALE = 1.2f;
	const float MUSIC_STOP_TIME = 3f;

	const float SLOW_MOTION_DURATION = 15;
	const float SLOW_MOTION_AMOUNT = 0.3f;
	const float INVICIBILITY_DURATION = 10;

	const float BORDER_COLLIDER_RADIUS = 0.5f;

	[System.Serializable] public enum difficulty { easy, medium, hard, veryHard };
	public const int difficultyCount = 4;

    public static GameManager instance;
	public SaveData saveData;

    [HideInInspector] public List<Shape> shapes;//all shapes currently visible
	public Queue<Shape> readyToDestroy;//shapes that are big enough to be destroyed, but are waiting to be deleted by goup of 3, to avoid general color change

	[SerializeField] GameObject[] GrowingShapesPrefabs;
	[SerializeField] GameObject[] ShrinkingShapesPrefabs;
	public DifficultyRange timeBtwGrowShapes;
	public DifficultyRange timeBtwShrinkShapes;
	public Range timeBtwCoins;
	public float coinScreenDistance;
	[HideInInspector] public Rect coinRect;

	float scoreStartTime;
	public float scoreMultiplier = 1;
	public int score
	{
		get => Mathf.RoundToInt((Time.time - scoreStartTime) * scoreMultiplier);
	}
	bool newBestScore;//new best score during last game 

	public difficulty currentDifficulty;

	[Space(20)]

	[SerializeField] TextMeshProUGUI scoreText;
	[SerializeField] TextMeshProUGUI pauseScoreText;
	public PlayerController player;
	[SerializeField] TextMeshProUGUI lastScoreText;
	[SerializeField] TextMeshProUGUI bestScoreText;
	[SerializeField] PostProcessVolume rewindPP;
	[SerializeField] Image[] difficultyBtns;
	[SerializeField] RectTransform[] difficultyBtnsRects;
	[SerializeField] Color[] diffColors;
	private bool updatedDifficultyButtons = false;
	[SerializeField] float difficultyButtonAnimationDuration = 0.3f;
	[SerializeField] RectTransform scoresLayout;
	[SerializeField] GameObject rewindText;
	[SerializeField] GameObject newBestScoreLabel;
	[SerializeField] TextMeshProUGUI[] moneyText;
	[SerializeField] GameObject objDisplayPrefab;
	[SerializeField] Transform objDisplayParent;
	[SerializeField] GameObject coinPrefab;
	[SerializeField] Material displayMaterial;
	[SerializeField] public List<ImageColor> themeImages;
	[SerializeField] GameObject[] colorSelectBtns;
	[SerializeField] GameObject tutoHand;
	[SerializeField] GameObject tutoText;
	[SerializeField] GameObject tutoBtnsInsructions;
	[SerializeField] GameObject tutoBtnsInsructions2;
	[SerializeField] CanvasGroup colorBtnsCanvas;
	[SerializeField] AudioSource rewindSound;
	[SerializeField] AudioSource music;
	[SerializeField] AudioSource noise;
	[SerializeField] AudioSource blip;
	[SerializeField] AudioSource newCoin;
	[SerializeField] Range pitchRange;
	[SerializeField] ParticleSystem newCoinsPS;
	[SerializeField] GameObject ingamePowerupBtn;
	[SerializeField] CanvasGroup powerupBtnHelp;
	[SerializeField] Image ingamePowerupBtnIcon;
	[SerializeField] Sprite[] powerupIcons;
	[SerializeField] TextMeshProUGUI ingamePowerupTxt;
	[SerializeField] Slider volumeSlider;
	[SerializeField] Slider volumeSliderWebGL;
	[SerializeField] ParticleSystem noMoneyPS;
	[SerializeField] TextMeshProUGUI colorPriceText;
	[SerializeField] TMP_Dropdown controlTypeDropdown;
	[SerializeField] TMP_Dropdown controlTypeDropdownWebGL;
	[SerializeField] GameObject joystickLeft;
	[SerializeField] GameObject joystickRight;
	[SerializeField] TMP_Dropdown languageDropdown;
	[SerializeField] TMP_Dropdown languageDropdownWebGL;
	[SerializeField] GameObject screenRightCollider;
	[SerializeField] GameObject screenLeftCollider;
	[SerializeField] List<GameObject> colorButtonsGroup;
	[SerializeField] TMP_Dropdown colorBtnsPosDrop;
	[SerializeField] TMP_Dropdown colorBtnsPosDropWebGL;
	[SerializeField] GameObject randomColorBtnCross;
	[SerializeField] GameObject randomColorPopup;
	[SerializeField] Transform pauseObjParents;
	[SerializeField] Slider resolutionsSlider;
 	[HideInInspector] public Camera mainCam;

	[Space(20)]

	public float menuTransitionTime;
	public float menuTransitionSize;
	public CanvasGroup currentMenu;
	public CanvasGroup[] menus;

	List<Coroutine> gameCoroutines;

	[Space(20)]

	[SerializeField] float cameraShakeDuration = 0.5f;
	[SerializeField] float cameraShakeMoveAmpl = 0.2f;
	[SerializeField] float cameraShakeAngleAmpl = 5;

    [SerializeField] float cameraSmallShakeDuration = 0.2f;
	[SerializeField] float cameraSmallShakeMoveAmpl = 0.1f;
	[SerializeField] float cameraSmallShakeAngleAmpl = 1;

	
	[Space(20)]
	public Color[] defaultColors;//default colors, current colors in saveData
	[HideInInspector] public int currentColorSelected;//color to edit in the theme editor
	[HideInInspector] public List<Color> allColors = new List<Color>();
	[HideInInspector] public List<ThemeBtn> colorBtns = new List<ThemeBtn>();
	[Tooltip("The price of the 3 first colors")]
	[SerializeField] private int colorPriceStart = 50;
	[Tooltip("Price increment ech time you buy ONE color")]
	[SerializeField] private int colorPriceIncrement = 5;


	[Space(20)]
	[HideInInspector] public int money;
	public int minGamesForObjs;
	[System.NonSerialized] public Objective[] objectives;
	[SerializeField] private int skipObjPrice;
	private bool skippingObjs = false;
	[HideInInspector] public bool mustIgnoreCorruptedSave = false;

	public Objective[] objTemplate;

	[HideInInspector] public float lastMove;//last time player moved
	[HideInInspector] public float lastColorChange;//last time player changed color
	[HideInInspector] public float maxWithoutMove;
	[HideInInspector] public float maxWithoutColorChange;
	[HideInInspector] public difficulty lastDifficulty;
	[HideInInspector] public float lastCoinAmount;//nb of coin collected during last game

	public int currentPowerup = -1;
	public int[] powerupValue;
	public string[] ingamePowerupNames;
	[SerializeField] Transform[] powerupsBtns;
	public bool isInvincible = false;

	[SerializeField] AudioMixer mainAudioMixer;

	[HideInInspector] public List<LocalisedText> localTexts;
	[HideInInspector] public List<DropdownLocalisation> localDropdowns;

	[HideInInspector] public RectTransform btnsRect;

	// Used to hide the score in game, during rewind
	private bool scoreVisible = true;

    [Space(20)]

    [SerializeField] private float preventObjSkippingDelay;
    private bool shouldPreventObjSkipping = false;

    [Space(20)]

    [SerializeField] private Renderer startTransition;
    [SerializeField] private float startTransitionDuration;

    private float averageDeltaTime = 0.00001f;
    private int currentScreenResId = 0;
    private float lastResChangeTime = 0;

    public TextMeshProUGUI testText;

	private void Awake()
	{
		instance = this;
		shapes = new List<Shape>();
		readyToDestroy = new Queue<Shape>();
		gameCoroutines = new List<Coroutine>();
		localTexts = new List<LocalisedText>();
		localDropdowns = new List<DropdownLocalisation>();

		mainCam = gameObject.GetComponent<Camera>();

        startTransition.sharedMaterial.SetFloat("_Discard", 0);
	}

	private IEnumerator Start()
	{
		saveData = SaveData.Load();

        LeanTween.value(startTransition.gameObject, 0, 1, startTransitionDuration).setEaseInOutExpo().setOnUpdate(value => {
            startTransition.sharedMaterial.SetFloat("_Discard", value);
        }).setOnComplete(() => {
            startTransition.sharedMaterial.SetFloat("_Discard", 1);
            Destroy(startTransition);
        });

		initMenus();

		if (saveData == null)
		{
			player.gameObject.SetActive(false);
			ChangeLanguageAdvanced(Language.system, false, false);
			SwitchMenu("CorruptedSave");

			yield return new WaitUntil(() => mustIgnoreCorruptedSave);

			saveData = new SaveData();
			saveData.nbGames = 1;

			SwitchMenu("Main");
		}

		//if (PlayerPrefs.HasKey("Difficulty"))
		//	SetDifficulty(PlayerPrefs.GetInt("Difficulty"));
		//else
		//	SetDifficulty((int)difficulty.easy);
		SetDifficulty((int)difficulty.easy);

		money = 0;		
		StartCoroutine(ChangeMoneyAmount(saveData.money, true));

		UpdateMainMenuScores();
		UpdateTheme();
		SetCurrentColor(0);
		SetRandomColor(saveData.randomColors);

		ChangeLanguageAdvanced(saveData.language, false);

		LoadObjs();

        MoveFrameColliders();

        if (saveData.nbGames > 0)
		{
			player.gameObject.SetActive(false);
            gameCoroutines.Add(StartCoroutine(instantiateGrowing()));
            gameCoroutines.Add(StartCoroutine(instantiateShrinking()));
            gameCoroutines.Add(StartCoroutine(instantiateCoins()));
        }
		else
		{
			player.gameObject.SetActive(true);
			SwitchMenu("Ingame");
			yield return Tutorial();
		}        

        if (saveData.unlockedColors.Count == 0)
		{
			saveData.unlockedColors = defaultColors.ToList();
		}

		yield return new WaitForEndOfFrame();

		SetVolume(saveData.soundsVolume);
		SetControlType((int)saveData.controlType);
		ChangeColorBtnsPosition((int)saveData.buttonPosType);

		// Set coin rect
		float screenXunit = mainCam.orthographicSize / Screen.height * Screen.width;
		coinRect = Rect.MinMaxRect(-screenXunit + coinScreenDistance,
			-mainCam.orthographicSize + coinScreenDistance,
			screenXunit - coinScreenDistance,
			mainCam.orthographicSize - coinScreenDistance);
	}

	private void Update()
	{
		if (scoreVisible)
			scoreText.text = score.ToString();
		else
			scoreText.text = "";
		pauseScoreText.text = score.ToString();

		//delete growing shapes by grop of 3
		if (readyToDestroy.Count >= 3)
		{
			for (int i = 0; i < 3; i++)
			{
				var toDestroy = readyToDestroy.Dequeue();
				shapes.Remove(toDestroy);
				Destroy(toDestroy.gameObject);
			}
		}

		//check max time without moving or changing color
		if ((Time.time - lastMove) > maxWithoutMove) 
			maxWithoutMove = Time.time - lastMove;

		if ((Time.time - lastColorChange) > maxWithoutColorChange) 
			maxWithoutColorChange = Time.time - lastColorChange;

        // Compute average framerate
        averageDeltaTime += (Time.unscaledDeltaTime - averageDeltaTime) * 0.02f;
        float fps = 1 / averageDeltaTime;

        if (fps < 20 && Time.unscaledDeltaTime - lastResChangeTime > 10 && currentScreenResId < Screen.resolutions.Length - 1)
        {
            currentScreenResId++;
            lastResChangeTime = Time.unscaledTime;
            Screen.SetResolution(Screen.resolutions[currentScreenResId].width, Screen.resolutions[currentScreenResId].height, true);
        }

        testText.text = fps +  "; " + currentScreenResId;
	}

	/// <summary>
	/// Instatnitiates growing shapes based on current variables values
	/// </summary>
	public IEnumerator instantiateGrowing()
	{
		while (true)
		{
			GameObject newGO = Instantiate(GrowingShapesPrefabs[Random.Range(0, GrowingShapesPrefabs.Length)]);
			shapes.Add(newGO.GetComponent<Shape>());

			yield return new WaitForSeconds(timeBtwGrowShapes.PickRandom());
		}
	}

	public IEnumerator instantiateShrinking()
	{
		while (true)
		{
			for (int i = 0; i < 3; i++)
			{
				GameObject newGO = Instantiate(ShrinkingShapesPrefabs[Random.Range(0, ShrinkingShapesPrefabs.Length)]);
				shapes.Add(newGO.GetComponent<Shape>());

				newGO.GetComponent<ShrinkingShape>().maxSize = 21 + (i * timeBtwShrinkShapes.PickRandom());
			}

			yield return new WaitForSeconds(timeBtwShrinkShapes.PickRandom() * 3);
		}
	}

	public IEnumerator instantiateCoins()
	{
		while (true)
		{
			GameObject newCoin = Instantiate(coinPrefab);
			newCoin.transform.position = coinRect.RandomPosition();

			yield return new WaitForSeconds(timeBtwCoins.PickRandom());
		}
	}

	/// <summary>
	/// Makes all non visible menus disapear (used at game start)
	/// </summary>
	public void initMenus()
	{
		foreach (CanvasGroup canvasGroup in menus)
		{
			if (currentMenu != canvasGroup)
			{
				canvasGroup.alpha = 0;
				canvasGroup.gameObject.transform.localScale = menuTransitionSize * Vector2.one;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}
			else
			{
				canvasGroup.alpha = 1;
				canvasGroup.gameObject.transform.localScale = Vector2.one;
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}
		}
	}

	/// <summary>
	/// Switch to the first menu with the name, with transition
	/// </summary>
	public void SwitchMenu(string name)
	{
		foreach (CanvasGroup canvasGroup in menus)
		{
			if (canvasGroup.gameObject.name.Contains(name))
			{
				SwitchMenu(canvasGroup);
				break;
			}
		}
	}

	/// <summary>
	/// Switch menu, with transition
	/// </summary>
	public void SwitchMenu(CanvasGroup newMenu)
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		LeanTween.alphaCanvas(currentMenu, 0, menuTransitionTime)
			.setIgnoreTimeScale(true);
		LeanTween.scale(currentMenu.gameObject, menuTransitionSize * Vector2.one, menuTransitionTime)
			.setEase(LeanTweenType.easeInOutQuad)
			.setIgnoreTimeScale(true);
		currentMenu.interactable = false;
		currentMenu.blocksRaycasts = false;

		currentMenu = newMenu;

		LeanTween.alphaCanvas(currentMenu, 1, menuTransitionTime)
			.setIgnoreTimeScale(true);
		LeanTween.scale(currentMenu.gameObject, Vector2.one, menuTransitionTime)
			.setEase(LeanTweenType.easeInOutQuad)
			.setIgnoreTimeScale(true);
		currentMenu.interactable = true;
		currentMenu.blocksRaycasts = true;
	}

	/// <summary>
	/// Starts a new game
	/// </summary>
	public void StartGame()
		=> StartCoroutine(StartGameCoroutine());

	public IEnumerator StartGameCoroutine()
	{
		gameCoroutines.ForEach((coroutine) => StopCoroutine(coroutine));
		SwitchMenu("Ingame");
		scoreVisible = false;
        UpdateIngamePowerupBtn();
		rewindText.SetActive(true);

		// Random colors
		if (saveData.randomColors)
			RandomColors();

		bool isRight = IsJoystickRight();

		if (saveData.controlType == ContolType.fixedJoysick)
		{
			joystickLeft.SetActive(!isRight);
			joystickRight.SetActive(isRight);
		}
		else
		{
			joystickLeft.SetActive(false);
			joystickRight.SetActive(false);
		}

		foreach (Shape shape in shapes)
		{
			shape.Revert();
		}

		StartCoroutine(TweenPP(true));

		//play rewind sound
		rewindSound.Play();
		noise.Play();

		yield return new WaitForSeconds(Shape.REVERT_TIME + 0.5f);

		//reset and active player
		player.gameObject.SetActive(true);
		player.gameObject.transform.position = Vector3.zero;
		player.ChangeColor(2);

		//reset max time without move or color and coin amount
		lastColorChange = Time.time;
		lastMove = Time.time;
		maxWithoutMove = 0;
		maxWithoutColorChange = 0;
		lastCoinAmount = 0;

        //reset score and shape lists
        scoreStartTime = Time.time;
		scoreMultiplier = currentPowerup == 2 ? 1.3f : 1;
		shapes.Clear();
		readyToDestroy.Clear();

		saveData.nbGames++;
		SaveData.Save(saveData);
		
		StartCoroutine(TweenPP(false));
		rewindText.SetActive(false);

		yield return new WaitForSeconds(1);

        //play music
        scoreVisible = true;
        music.Play();
		noise.Stop();

		//Start to instantiate shapes
		gameCoroutines.Add(StartCoroutine(instantiateGrowing()));
		gameCoroutines.Add(StartCoroutine(instantiateShrinking()));
		gameCoroutines.Add(StartCoroutine(instantiateCoins()));

		// Get buttons rect to prevent touches
		btnsRect = colorButtonsGroup[(int)saveData.buttonPosType].GetComponent<RectTransform>();
	}

	/// <summary>
	/// Opens main menu and save scores
	/// </summary>
	public void StopGame()
	{
		player.gameObject.SetActive(false);

		StartCoroutine(CameraShake());

		if (music.isPlaying)
		{
			StartCoroutine(StopMusic());
		}

		//string diffTxt = currentDifficulty.ToString();
		//PlayerPrefs.SetInt($"LastScore{diffTxt}", score);
		//if (!PlayerPrefs.HasKey($"BestScore{diffTxt}") || PlayerPrefs.GetInt($"BestScore{diffTxt}") < score)
		//{
		//	PlayerPrefs.SetInt($"BestScore{diffTxt}", score);
		//	newBestScore = true;
		//}

		money += Mathf.CeilToInt(lastCoinAmount);
		StartCoroutine(ChangeMoneyAmount(money, true));

		if (currentPowerup != -1 ) 
			ChangePowerupBtn(powerupsBtns[currentPowerup], false);
		currentPowerup = -1;

		newBestScore = false;

		saveData.lastScores[(int)currentDifficulty] = score;
		if (saveData.bestScores[(int)currentDifficulty] < score)
		{
			saveData.bestScores[(int)currentDifficulty] = score;
			newBestScore = true;
		}
		SaveData.Save(saveData);

		lastDifficulty = currentDifficulty;

		if (saveData.nbGames >= minGamesForObjs)
		{
			SwitchMenu("Objectives");
		}
		else
		{
			SwitchMenu("Main");
			returnToMainMenu();
		}

		StartCoroutine(ChangeMoneyAmount(money, true));
		StartCoroutine(UpdateObjs());
	}

	/// <summary>
	/// Updates main menu interface (scores and new best score label)
	/// </summary>
	public void returnToMainMenu()
	{
		newBestScoreLabel.SetActive(false);
		if (newBestScore)
		{
			newBestScoreLabel.SetActive(true);
			newBestScore = false;
		}

		UpdateMainMenuScores();
	}

	/// <summary>
	/// Read playerPrefs and show values in interface
	/// </summary>
	public void UpdateMainMenuScores()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(scoresLayout);

		lastScoreText.text = saveData.lastScores[(int)currentDifficulty] == 0 ? "--" : saveData.lastScores[(int)currentDifficulty].ToString();
		bestScoreText.text = saveData.bestScores[(int)currentDifficulty] == 0 ? "--" : saveData.bestScores[(int)currentDifficulty].ToString();

		//string diffTxt = currentDifficulty.ToString();
		//lastScoreText.text = PlayerPrefs.HasKey($"LastScore{diffTxt}") ? PlayerPrefs.GetInt($"LastScore{diffTxt}").ToString() : "--";
		//bestScoreText.text = PlayerPrefs.HasKey($"BestScore{diffTxt}") ? PlayerPrefs.GetInt($"BestScore{diffTxt}").ToString() : "--";
	}

	/// <summary>
	/// Pauses or unpauses the game
	/// </summary>
	public void PauseGame(bool state)
	{
		if (state)
		{
			StartCoroutine(TweenPP(true));
			Time.timeScale = 0;
			SwitchMenu("Pause");
			music.Pause();
			noise.Play();
			player.canMove = false;
			UpdatePauseObjs();
		}
		else
		{
			StartCoroutine(TweenPP(false));
			Time.timeScale = 1;
			SwitchMenu("Ingame");
			music.UnPause();
			noise.Stop();
			player.canMove = true;
		}
	}

	/// <summary>
	/// Tween to rewind PP
	/// </summary>
	IEnumerator TweenPP(bool rewind)
	{
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / PP_TRANS_TIME)
		{
			rewindPP.weight = rewind? t : 1- t;
			yield return null;
		}

		rewindPP.weight = rewind ? 1 : 0;
	}

	/// <summary>
	/// Sets difficulty level and update interface
	/// </summary>
	/// <param name="d">Difficulty level</param>
	public void SetDifficulty(int d)
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

        if (!updatedDifficultyButtons) // Hide all buttons first time
        {
            for (int i = 0; i < difficultyBtns.Length; i++)
            {
                difficultyBtnsRects[i].anchorMin = new Vector2(0.5f, 0);
                difficultyBtnsRects[i].anchorMax = new Vector2(0.5f, 1);
                difficultyBtns[i].color = new Color(diffColors[i].r, diffColors[i].g, diffColors[i].b, 0);
            }
        }

        if ((int)currentDifficulty != d || !updatedDifficultyButtons) // Animate buttons
        {
            int c = (int)currentDifficulty;
            LeanTween.value(difficultyBtns[c].gameObject, 1, 0, difficultyButtonAnimationDuration).setEaseInOutExpo().setOnUpdate(value => {
                difficultyBtnsRects[c].anchorMin = new Vector2(0.5f - value / 2, 0);
                difficultyBtnsRects[c].anchorMax = new Vector2(0.5f + value / 2, 1);
                difficultyBtns[c].color = new Color(diffColors[c].r, diffColors[c].g, diffColors[c].b, value);
            });

            LeanTween.value(difficultyBtns[d].gameObject, 0, 1, difficultyButtonAnimationDuration).setEaseInOutExpo().setOnUpdate(value => {
                difficultyBtnsRects[d].anchorMin = new Vector2(0.5f - value / 2, 0);
                difficultyBtnsRects[d].anchorMax = new Vector2(0.5f + value / 2, 1);
                difficultyBtns[d].color = new Color(diffColors[d].r, diffColors[d].g, diffColors[d].b, value);
            });
        }

		currentDifficulty = (difficulty)d;
        updatedDifficultyButtons = true;

		//PlayerPrefs.SetInt("Difficulty", d);

		UpdateMainMenuScores();
		newBestScoreLabel.SetActive(false);
	}

	public void ResetAll()
	{
		File.Delete(Application.persistentDataPath + "/objectives");
		File.Delete(Application.persistentDataPath + "/save");
		Application.Quit();
	}

	public void MoveFrameColliders()
	{
		float screenXunit = mainCam.orthographicSize / Screen.height * Screen.width + BORDER_COLLIDER_RADIUS;
		screenLeftCollider.transform.position = new Vector3(-screenXunit, 0);
		screenRightCollider.transform.position = new Vector3(screenXunit, 0);
	}

	public IEnumerator CameraShake(bool small = false)
	{
#if UNITY_IOS || UNITY_ANDROID
		if (saveData.vibrationOn && !small) Handheld.Vibrate();
# endif

        float duration = small ? cameraSmallShakeDuration : cameraShakeDuration;
        float moveAmplitude = small ? cameraSmallShakeMoveAmpl : cameraShakeMoveAmpl;
        float angle = small ? cameraSmallShakeAngleAmpl : cameraShakeAngleAmpl;

		for (float t = 0; t < duration; t += Time.deltaTime)
		{
			gameObject.transform.SetPositionAndRotation(
				new Vector3(
                    Random.Range(-moveAmplitude, moveAmplitude), 
				    Random.Range(-moveAmplitude, moveAmplitude), -10
                ), 
                Quaternion.Euler(0, 0, Random.Range(-angle, angle))
            );

			yield return new WaitForEndOfFrame();
		}

		gameObject.transform.SetPositionAndRotation(
			new Vector3(0, 0, -10),
			Quaternion.identity
        );
	}

	/// <summary>
	/// Animates interface and set new value
	/// </summary>
	/// <param name="newValue">New amount of money</param>
	/// <param name="immediate">Should animate interface</param>
	public IEnumerator ChangeMoneyAmount(int newValue, bool immediate = false, bool mustSave = true)
	{
		int oldValue = money;

		money = newValue;

		if (mustSave)
		{
			saveData.money = money;
			SaveData.Save(saveData); 
		}
		//PlayerPrefs.SetInt("Money", money);

		if (!immediate)
		{
			newCoinsPS.gameObject.SetActive(true);
			newCoinsPS.Play();

			float incrTime = newCoin.clip.length;

			for (float t = 0; t <= 1; t += incrTime / MONEY_INCREASE_TIME)
			{
				foreach (TextMeshProUGUI tmp in moneyText)
				{
					tmp.text = Mathf.RoundToInt(LeanTween.easeOutQuad(oldValue, newValue, t)).ToString();
				}

				newCoin.Play();

				yield return new WaitForSeconds(incrTime);
			}

			newCoinsPS.Stop();
		}

		foreach (TextMeshProUGUI tmp in moneyText)
		{
			tmp.text = money.ToString();
		}

		yield return null;
	}

	public void IgnoreCorruptedSave()
	{
		mustIgnoreCorruptedSave = true;
	}

	///// Objs

	/// <summary>
	/// Update UI, create new obj if needed and save them
	/// </summary>
	public IEnumerator UpdateObjs()
	{
        shouldPreventObjSkipping = true;

		yield return new WaitForSeconds(TIME_BTW_NEW_OBJ);

		for (int i = 0; i < 3; i++)
		{
			var obj = objectives[i];

			if (obj != null && obj.completed)
			{
				obj.display.StartCoroutine(obj.display.Completed());
				yield return new WaitForSeconds(ObjectiveDisplay.COMPLETE_DURATION);
				StartCoroutine(ChangeMoneyAmount(money + obj.reward));
				yield return new WaitForSeconds(ObjectiveDisplay.FADE_DURATION + TIME_BTW_NEW_OBJ);

				obj = null;
			}

			if (obj == null)
			{
				objectives[i] = new Objective(objTemplate[Random.Range(0, objTemplate.Length - 1)]);
				var display = Instantiate(objDisplayPrefab, objDisplayParent).GetComponent<ObjectiveDisplay>();
				display.ShowObj(objectives[i], i);
				objectives[i].display = display;

				yield return new WaitForSeconds(TIME_BTW_NEW_OBJ);
			}
		}

		maxWithoutColorChange = 0;
		maxWithoutMove = 0;

		SaveObjs();

        yield return new WaitForSeconds(preventObjSkippingDelay);
        shouldPreventObjSkipping = false;
	}

	private void UpdatePauseObjs()
	{
		// TODO: Check this

		for (int i = 0; i < 3; i++)
		{
			ObjectiveDisplay display;

			if (pauseObjParents.childCount <= i || pauseObjParents.GetChild(i) == null)
			{
				display = Instantiate(objDisplayPrefab, pauseObjParents).GetComponent<ObjectiveDisplay>();
			}
			else
			{
				display = pauseObjParents.GetChild(i).gameObject.GetComponent<ObjectiveDisplay>();
			}

			display.ShowObj(objectives[i], i); 
		}
	}

	private void SaveObjs()
	{
		File.WriteAllText(Application.persistentDataPath + "/objectives", string.Empty);
		using (var stream = File.Open(Application.persistentDataPath + "/objectives", FileMode.OpenOrCreate))
		{
			new XmlSerializer(typeof(Objective[])).Serialize(stream, objectives);
		};
	}

	public void SkipObjs()
		=> StartCoroutine(SkipObjsCoroutine());

	private IEnumerator SkipObjsCoroutine()
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		if (skippingObjs || shouldPreventObjSkipping)
			yield break;

		if (skipObjPrice > money)
		{
			noMoneyPS.Play();
			yield break;
		}
		StartCoroutine(ChangeMoneyAmount(money - skipObjPrice, true, true));

		skippingObjs = true;

		for (int i = 0; i < 3; i++)
		{
			var obj = objectives[i];

			obj.display.SkipObj();
			yield return ObjectiveDisplay.FADE_DURATION / 2;
		}

		for (int i = 0; i < 3; i++)
		{
			objectives[i] = objTemplate[Random.Range(0, objTemplate.Length - 1)];
			var display = Instantiate(objDisplayPrefab, objDisplayParent).GetComponent<ObjectiveDisplay>();
			display.ShowObj(objectives[i], i);
			objectives[i].display = display;

			yield return ObjectiveDisplay.FADE_DURATION / 2;
		}

		SaveObjs();

		skippingObjs = false;
	}

	/// <summary>
	/// Deserializes the objectives
	/// </summary>
	public void LoadObjs()
	{
		if (File.Exists(Application.persistentDataPath + "/objectives"))
		{
			using (var stream = File.OpenRead(Application.persistentDataPath + "/objectives"))
			{
				try
				{
					objectives = new XmlSerializer(typeof(Objective[])).Deserialize(stream) as Objective[];
				}
				catch (System.Exception e)
				{
					Debug.LogError("Can't read objective's XML file :\n" + e.Message);

					objectives = new Objective[] { null, null, null };
					StartCoroutine(UpdateObjs());
					return;
				}
			};

			for (int i = 0; i < 3; i++)
			{
				var display = Instantiate(objDisplayPrefab, objDisplayParent).GetComponent<ObjectiveDisplay>();
				display.ShowObj(objectives[i], i);
				objectives[i].display = display;
			}
		}
		else
		{
			objectives = new Objective[] { null, null, null };
			StartCoroutine(UpdateObjs());
		}
	}

	///// Theme edition

	public void ChangeTheme(Color[] colors)
	{
		colors.CopyTo(saveData.colors, 0);
		SaveData.Save(saveData);
		UpdateTheme();
	}

	public void RandomColors()
	{
		List<int> usedIndexes = new List<int>();
		for (int i = 0; i < 3; i++)
		{
			int randomIndex = Random.Range(0, saveData.unlockedColors.Count);
			if (usedIndexes.Contains(randomIndex))
			{
				i--;
			}
			else
			{
				usedIndexes.Add(randomIndex);
				saveData.colors[i] = saveData.unlockedColors[randomIndex];
			}
		}

		SaveData.Save(saveData);
		UpdateTheme();
	}

	public void DefaultTheme()
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		ChangeTheme(defaultColors);
	}

	public void UpdateTheme()
	{
		for (int i = 0; i < 3; i++)
		{
			displayMaterial.SetColor("_Color" + i.ToString(), instance.saveData.colors[i]);
		}

		foreach (ImageColor item in themeImages)
		{
			item.image.color = saveData.colors[item.colorID];
		}
	}

	public void SetCurrentColor(int index)
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		LeanTween.scale(colorSelectBtns[currentColorSelected], Vector3.one, COLOR_SELECT_TRANS);

		currentColorSelected = index;

		LeanTween.scale(colorSelectBtns[index], Vector3.one * COLOR_SELECT_SCALE, COLOR_SELECT_TRANS);
	}

	public void BuyColors()
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		if (UnlockedAllColors())
			return;

		int colorPrice = GetColorPrice();

		if (colorPrice > saveData.money)
		{
			noMoneyPS.Play();
			return;
		}
		else
		{
			StartCoroutine(ChangeMoneyAmount(money - colorPrice, true, true));
		}

		for (int i = 0; i < 3; i++)
		{
			Color newColor;

			while (true)
			{
				newColor = allColors[Random.Range(0, allColors.Count)];

				if (!saveData.unlockedColors.Any(c => Utility.CompareColors(c, newColor)))
					break;
			}
			saveData.unlockedColors.Add(newColor);

			if (UnlockedAllColors())
				break;
		}

		SaveData.Save(saveData);
		RefreshThemeMenu();
	}

	public bool UnlockedAllColors()
	{
		return saveData.unlockedColors.Count >= allColors.Count;
	}

	public void RefreshThemeMenu()
	{
		colorBtns.ForEach(btn => btn.UpdateUI());
		colorPriceText.text = GetColorPrice().ToString();
	}

	public int GetColorPrice()
	{
		return colorPriceStart + colorPriceIncrement * saveData.unlockedColors.Count;
	}

	/////Powerups

	/// <summary>
	/// Switch powerup and update money
	/// </summary>
	/// <param name="type">Powerup to change</param>
	public void SwitchPowerup(int type)
	{
		if (currentPowerup == type)
		{
			currentPowerup = -1;
			StartCoroutine(ChangeMoneyAmount(money + powerupValue[type], true, false));
		}
		else
		{
			if (currentPowerup != -1)
			{
				StartCoroutine(ChangeMoneyAmount(money + powerupValue[currentPowerup], true, false));
				ChangePowerupBtn(powerupsBtns[currentPowerup], false);
			}

			if (powerupValue[type] < money)
			{
				currentPowerup = type;
				StartCoroutine(ChangeMoneyAmount(money - powerupValue[type], true, false));
			}
			else
			{
				currentPowerup = -1;
				noMoneyPS.Play();
			}
		}

		ChangePowerupBtn(powerupsBtns[type], currentPowerup == type);
	}

	/// <summary>
	/// Switch between gameObjects for a powerup button
	/// </summary>
	/// <param name="btn">Button reference</param>
	public void ChangePowerupBtn(Transform btn, bool active)
	{
		if (active)
		{
			btn.GetChild(0).gameObject.SetActive(true);
			btn.GetChild(1).gameObject.SetActive(false);
		}
		else
		{
			btn.GetChild(0).gameObject.SetActive(false);
			btn.GetChild(1).gameObject.SetActive(true);
		}
	}

	/// <summary>
	/// Enables the powerup btn ingame and change his icon
	/// </summary>
	public void UpdateIngamePowerupBtn()
	{
		if (currentPowerup == 0)
		{
			ingamePowerupBtn.SetActive(true);
			ingamePowerupBtnIcon.sprite = powerupIcons[0];

			if (saveData.firstPowerup)
			{
				saveData.firstPowerup = false;
				SaveData.Save(saveData);

				powerupBtnHelp.gameObject.SetActive(true);
				LeanTween.alphaCanvas(powerupBtnHelp, 0, 1).setDelay(8);
				Destroy(powerupBtnHelp.gameObject, 10);
			}
		}
		else if (currentPowerup == 1)
		{
			ingamePowerupBtn.SetActive(true);
			ingamePowerupBtnIcon.sprite = powerupIcons[1];

			if (saveData.firstPowerup)
			{
				saveData.firstPowerup = false;
				SaveData.Save(saveData);

				powerupBtnHelp.gameObject.SetActive(true);
				LeanTween.alphaCanvas(powerupBtnHelp, 0, 1).setDelay(8);
				Destroy(powerupBtnHelp.gameObject, 10);
			}
		}
		else
		{
			ingamePowerupBtn.SetActive(false);
		}
	}

	/// <summary>
	/// Called by the ingame powerup btn
	/// </summary>
	public void EnablePowerupIngame()
	{
		Debug.Log("Start powerup " + currentPowerup);
		StartCoroutine(EnablePowerupIngameCoroutine());
	}

	IEnumerator EnablePowerupIngameCoroutine()
	{
		ingamePowerupTxt.gameObject.SetActive(true);
		ingamePowerupTxt.text = LocalisationSystem.GetLocalisedText(ingamePowerupNames[currentPowerup]);

		ChangePowerupBtn(powerupsBtns[currentPowerup], false);
		ingamePowerupBtn.SetActive(false);

		if (currentPowerup == 0)
		{
			Time.timeScale = SLOW_MOTION_AMOUNT;
			yield return new WaitForSecondsRealtime(SLOW_MOTION_DURATION);
			Time.timeScale = 1;
		}
		else if (currentPowerup == 1)
		{
			isInvincible = true;
			yield return new WaitForSecondsRealtime(INVICIBILITY_DURATION);
			isInvincible = false;
		}

		ingamePowerupTxt.gameObject.SetActive(false);
		currentPowerup = -1;
	}

	/////

	/// <summary>
	/// Slow down the music and stop it
	/// </summary>
	IEnumerator StopMusic()
	{
		for (float t = 1; t > 0; t -= Time.deltaTime / MUSIC_STOP_TIME)
		{
			music.pitch = t;
			yield return new WaitForEndOfFrame();
		}

		music.pitch = 1;
		music.Stop();
	}

	///// Settings
		
	public void OpenSettings()
	{
		if (SystemInfo.deviceType == DeviceType.Desktop)
			SwitchMenu("SettingsWebGL");
		else SwitchMenu("Settings");
	}

	/// <summary>
	/// Enables or disables vibration, update UI, save setting
	/// </summary>
	public void ChangeVibration()
	{
		saveData.vibrationOn = !saveData.vibrationOn;
#if UNITY_IOS || UNITY_ANDROID
		if (saveData.vibrationOn) Handheld.Vibrate();
#endif

		SaveData.Save(saveData);
	}

	/// <summary>
	/// Sets the volume, update UI, save setting
	/// </summary>
	/// <param name="volume">New value</param>
	public void SetVolume(float volume)
	{
		mainAudioMixer.SetFloat("mainVolume", (volume * 60) - 60);
		saveData.soundsVolume = volume;
		SaveData.Save(saveData);

		volumeSlider.value = volume;
		volumeSliderWebGL.value = volume;
	}

	/// <summary>
	/// Sets the control type, update UI, save setting
	/// </summary>
	/// <param name="volume">New value</param>
	public void SetControlType(int id)
	{
		saveData.controlType = (ContolType)id;
		SaveData.Save(saveData);

		if (id < 4) controlTypeDropdown.SetValueWithoutNotify(id);
		controlTypeDropdownWebGL.SetValueWithoutNotify(id);
	}

	// TODO: check for errors
	/// <summary>
	/// Sets the resolution, update UI, save setting
	/// </summary>
	/// <param name="id">New value, between 0 and 1, USE A SLIDER</param>
	public void ChangeResolution(float id)
	{
		int realId = Mathf.RoundToInt(id * Screen.resolutions.Length);

		saveData.resolutionID = id;
		SaveData.Save(saveData);

#if UNITY_IOS || UNITY_ANDROID
		Resolution res = Screen.resolutions[realId];
		Screen.SetResolution(res.width, res.height, true);
#endif

		resolutionsSlider.value = id;
	}

	/// <summary>
	/// Change game language
	/// </summary>
	public void ChangeLanguageAdvanced(Language id, bool updateUI = true, bool save = true)
	{
		languageDropdown.SetValueWithoutNotify((int)id);
		languageDropdownWebGL.SetValueWithoutNotify((int)id);

		LocalisationSystem.ChangeLanguage(id);

		if (updateUI)
		{
			foreach (var text in localTexts)
			{
				text.UpdateText();
			}

			foreach (var dropdown in localDropdowns)
			{
				dropdown.UpdateText();
			} 
		}

		if (save)
		{
			saveData.language = id;
			SaveData.Save(saveData); 
		}
	}

	public void ChangeLanguage(int id)
		=> ChangeLanguageAdvanced((Language)id);

	/// <summary>
	/// Change the position of the color buttons ingame, update UI, save
	/// </summary>
	public void ChangeColorBtnsPosition(int id)
	{
		// enable or disable buttons
		for(int i = 0; i < colorButtonsGroup.Count; i++)
		{
			colorButtonsGroup[i].SetActive(i == id);
		}

		colorBtnsPosDrop.SetValueWithoutNotify(id);
		colorBtnsPosDropWebGL.SetValueWithoutNotify(id);

		saveData.buttonPosType = (ColorBtnsType)id;
		SaveData.Save(saveData);
	}

	public void SetRandomColor(bool state)
	{
		blip.pitch = pitchRange.PickRandom();
		blip.Play();

		saveData.randomColors = state;
		SaveData.Save(saveData);

		randomColorPopup.SetActive(state);
		randomColorBtnCross.SetActive(state);
	}

	public void ToggleRandomColor()
	{
		SetRandomColor(!saveData.randomColors);
	}

	public static bool IsJoystickRight()
	{
		if (instance.saveData == null)
			return false;

		return instance.saveData.buttonPosType == ColorBtnsType.left
			|| instance.saveData.buttonPosType == ColorBtnsType.leftSpaced
			|| instance.saveData.buttonPosType == ColorBtnsType.downLeft;
	}

	private IEnumerator Tutorial()
	{
		isInvincible = true;

        // Diasable color buttons
        colorButtonsGroup.ForEach(el => el.SetActive(false));
        colorBtnsCanvas.gameObject.SetActive(true);
        colorBtnsCanvas.interactable = false;
        colorBtnsCanvas.alpha = 0;

        yield return new WaitForSeconds(3);

		// Show hand
        tutoHand.SetActive(true);
        tutoHand.GetComponent<CanvasGroup>().alpha = 0;
        LeanTween.alphaCanvas(tutoHand.GetComponent<CanvasGroup>(), 1, 0.3f);
        var t = Time.time;

        yield return new WaitUntil(() => lastMove > t);
        yield return new WaitForSeconds(2);

		// Hide hand
        LeanTween.alphaCanvas(tutoHand.GetComponent<CanvasGroup>(), 0, 0.3f);

        yield return new WaitForSeconds(0.5f);
        tutoHand.SetActive(false);


		// Show color text
        tutoText.SetActive(true);
        tutoText.GetComponent<CanvasGroup>().alpha = 0;
        LeanTween.alphaCanvas(tutoText.GetComponent<CanvasGroup>(), 1, 0.3f);

        yield return new WaitForSeconds(5);

        LeanTween.alphaCanvas(tutoText.GetComponent<CanvasGroup>(), 0, 0.3f);

        yield return new WaitForSeconds(0.5f);
        tutoText.SetActive(false);


		// Show buttons instructions
		player.allowedColors = 0b010;
        tutoBtnsInsructions.SetActive(true);
        tutoBtnsInsructions.GetComponent<CanvasGroup>().alpha = 0;
        LeanTween.alphaCanvas(tutoBtnsInsructions.GetComponent<CanvasGroup>(), 1, 0.3f);

        colorBtnsCanvas.interactable = true;
        LeanTween.alphaCanvas(colorBtnsCanvas, 0.5f, 0.3f);

        yield return new WaitUntil(() => lastColorChange > t);
        yield return new WaitForSeconds(2);

        LeanTween.alphaCanvas(tutoBtnsInsructions.GetComponent<CanvasGroup>(), 0, 0.3f);
        yield return new WaitForSeconds(0.5f);
        tutoBtnsInsructions.SetActive(false);

        // Show next buttons instructions
        player.allowedColors = 0b111;
        tutoBtnsInsructions2.SetActive(true);
        tutoBtnsInsructions2.GetComponent<CanvasGroup>().alpha = 0;
        LeanTween.alphaCanvas(tutoBtnsInsructions2.GetComponent<CanvasGroup>(), 1, 0.3f);

		// Start only growing
        gameCoroutines.Add(StartCoroutine(instantiateGrowing()));
        scoreStartTime = Time.time;
        isInvincible = false;

        t = Time.time;
        yield return new WaitUntil(() => lastColorChange > t);
        yield return new WaitForSeconds(2);

        LeanTween.alphaCanvas(tutoBtnsInsructions2.GetComponent<CanvasGroup>(), 0, 0.3f);
        yield return new WaitForSeconds(0.5f);
        tutoBtnsInsructions2.SetActive(false);
    }
}
