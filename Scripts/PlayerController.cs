using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles color change, color verification.
/// </summary>
public class PlayerController : MonoBehaviour
{
	public int currentColorID;

	[SerializeField] SpriteRenderer mySR;
	[SerializeField] Rigidbody2D myRB;
	[SerializeField] Collider2D myCol;
	[SerializeField] TrailRenderer myTR;
	[SerializeField] GameObject colorChangePS;
	[SerializeField] GameObject diePS;
	[SerializeField] GameObject slideStartPrefab;
	[SerializeField] AudioSource colorChange;
	[SerializeField] GameObject joystickLeft;
	[SerializeField] GameObject joystickRight;

	[SerializeField] Range minMaxPitch;

	[Space(10)]

	public float maxSpeed;//max speed for the player
	public float maxSpeedDist;//slide distance before reaching max speed
	public float minDistToMove;//slide distance before changing direction
	public float maxJoystickDistance;//Max distance of the joysick to move in world units
	public float minTimeToMove;//Min time to assume real move (for objectives)

	public bool canMove = true;

	private float startMovingTime = 0;

	Vector2 relativePos;
	Vector2 slideStart;
	Vector2 lastPos;

	GameObject slideStartMarker;

	ContactFilter2D contactFilter;

	private void Start()
	{
		Input.simulateMouseWithTouches = false;
		Input.multiTouchEnabled = true;

		contactFilter = new ContactFilter2D();
		contactFilter.useLayerMask = true;
		contactFilter.SetLayerMask(LayerMask.GetMask("ColorIncrement"));

		ChangeColor(2);
	}

	private void Update()
	{
		// Select joystick
		GameObject joystick = GameManager.IsJoystickRight() ? joystickRight : joystickLeft;
		// Reset movement
		relativePos = Vector2.zero;

		// For computer testing
		if (SystemInfo.deviceType == DeviceType.Desktop)
		{
			//check mouse
			relativePos = Vector2.zero;

			if (Input.GetMouseButtonDown(0))
			{
				slideStart = GameManager.instance.mainCam.ScreenToWorldPoint(Input.mousePosition);
				slideStartMarker = Instantiate(slideStartPrefab, (Vector3)slideStart + Vector3.forward, Quaternion.Euler(0, 0, 45));
			}
			else if (Input.GetMouseButtonUp(0) && slideStartMarker != null)
			{
				Destroy(slideStartMarker);
				slideStartMarker = null;
			}
			else if (Input.GetMouseButton(0))
			{
				relativePos = (Vector2)GameManager.instance.mainCam.ScreenToWorldPoint(Input.mousePosition) - slideStart;
			}
		}

		// Get movement if phone
		if (SystemInfo.deviceType == DeviceType.Handheld && Time.timeScale != 0)
		{
			switch (GameManager.instance.saveData.controlType)
			{
				case ContolType.fingerDirection:
					if (Input.touchCount > 0)
					{
						Touch touch = Input.GetTouch(0);
						Vector2 touchPos = GameManager.instance.mainCam.ScreenToWorldPoint(touch.position);

						if (touch.phase != TouchPhase.Began
							&& (touchPos - lastPos).magnitude > minDistToMove
							&& !IsTouchInFrobiddenRect(touchPos))
						{
							relativePos = (touchPos - lastPos);
						}

						lastPos = touchPos;

						//Vector2 touchPos = GameManager.instance.mainCam.ScreenToWorldPoint(touch.position);
						//relativePos = touchPos - slideStart;
						//slideStart = touchPos;
					}
					else
					{
						relativePos = Vector2.zero;
					}
					break;


				case ContolType.relativeToStart:
					if (Input.touchCount > 0)
					{
						Touch touch = Input.GetTouch(0);

						if (!IsTouchInFrobiddenRect(touchPos))
						{
							if (touch.phase == TouchPhase.Began)
							{
								slideStart = GameManager.instance.mainCam.ScreenToWorldPoint(touch.position);
								slideStartMarker = Instantiate(slideStartPrefab, (Vector3)slideStart + Vector3.forward, Quaternion.Euler(0, 0, 45));
							}
							else if (touch.phase == TouchPhase.Ended)
							{
								if (slideStartMarker != null)
								{
									Destroy(slideStartMarker);
									slideStartMarker = null;
								}
							}
							else
							{
								relativePos = (Vector2)GameManager.instance.mainCam.ScreenToWorldPoint(touch.position) - slideStart;
							}
						}
					}
					else
					{
						if (slideStartMarker != null)
						{
							Destroy(slideStartMarker);
							slideStartMarker = null;
						}
						relativePos = Vector2.zero;
					}
					break;

				case ContolType.moveTowardsFinger:
					if (Input.touchCount > 0)
					{
						Touch touch = Input.GetTouch(0);

						if (!IsTouchInFrobiddenRect(touchPos))
						{
							Vector2 worldPos = GameManager.instance.mainCam.ScreenToWorldPoint(touch.position);
							relativePos = worldPos - (Vector2)gameObject.transform.position;
						}
					}
					else
					{
						relativePos = Vector2.zero;
					}
					break;


				case ContolType.fixedJoysick:
					if (Input.touchCount > 0)
					{
						Touch touch = Input.GetTouch(0);

						if (!IsTouchInFrobiddenRect(touchPos))
						{
							Vector2 worldPos = GameManager.instance.mainCam.ScreenToWorldPoint(touch.position);

							if (relativePos.magnitude < maxJoystickDistance)
								relativePos = worldPos - (Vector2)joystick.transform.position;
						}
					}
					else
					{
						relativePos = Vector2.zero;
					}
					break;

			}
		}

		//if movement, move player
		if (relativePos != Vector2.zero && canMove)
		{
			Vector2 normalised = relativePos.normalized;
			//float distance = relativePos.magnitude;
			//distance = distance > maxSpeedDist ? 1 : distance / maxSpeedDist;
			float distance = 1;

			gameObject.transform.position += (Vector3)(distance * maxSpeed * Time.unscaledDeltaTime * normalised);

			if (startMovingTime < 0)
			{
				startMovingTime = Time.unscaledTime;
			}
			else if (startMovingTime > minTimeToMove + Time.time)
			{
				// Notify gameManager for objectives
				GameManager.instance.lastMove = Time.time;
			}
		}
		else
		{
			startMovingTime = -1;
		}

		// Chech if player dies
		if (IsTouchingWrongColor() || myRB.IsTouchingLayers(LayerMask.GetMask("ScreenBorder")))
		{
			if (!GameManager.instance.isInvincible)
			{
				GameManager.instance.StopGame();
				Instantiate(diePS, gameObject.transform.position, Quaternion.identity);
			}
		}
	}

	private void OnDisable()
	{
		Destroy(slideStartMarker);
		slideStartMarker = null;
	}

	/// <summary>
	/// Sets the color of the player
	/// </summary>
	/// <param name="index">Color index from 0 to 2</param>
	public void ChangeColor(int index)
	{
		currentColorID = index;
		mySR.color = GameManager.instance.saveData.colors[currentColorID];
		myTR.startColor = myTR.endColor = GameManager.instance.saveData.colors[currentColorID];

#pragma warning disable CS0618 // Le champ est obsol�te mais flemme de recr�er le PS a cause de la propri�t�
		Instantiate(colorChangePS, gameObject.transform).GetComponent<ParticleSystem>().startColor = GameManager.instance.saveData.colors[currentColorID];
#pragma warning restore CS0618

		colorChange.pitch = minMaxPitch.PickRandom();
		colorChange.Play();

		GameManager.instance.lastColorChange = Time.time;
	}

	/// <summary>
	/// Determines if the player is touching the wrong color
	/// </summary>
	/// <returns>True if is touching wrong color</returns>
	public bool IsTouchingWrongColor()
	{
		int touch = 0;//how many shapes are touching me
		//int overlap = 0;//how many shapes are overlaping me (not working yet, used if the player touches two colors)
		foreach (Shape item in GameManager.instance.shapes)
		{
			var itemCollider = item.gameObject.GetComponent<Collider2D>();

			if (myRB.IsTouching(itemCollider))
			{
				//var results = new Collider2D[100];
				//Physics2D.OverlapCollider(myCol, contactFilter, results);

				//if (results.Select(col => col == itemCollider) != null)
				//{
				//	overlap++;
				//}

				touch++;
			}
		}

		touch %= 3;
		//overlap %= 3;

		return currentColorID == touch; //|| currentColorID == overlap;
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("ScreenBorder"))
		{
			GameManager.instance.StopGame();
			Instantiate(diePS, gameObject.transform.position, Quaternion.identity);
		}
	}

	private void IsTouchInFrobiddenRect(Vector2 pos)
	{
		Rect rect = GameManager.instance.btnsRect;

		return pos.x < rect.max.x && pos.x > rect.min.x && pos.y < rect.max.y && pos.y > rect.min.y
	}
}
