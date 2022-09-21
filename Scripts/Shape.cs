using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Shape : MonoBehaviour
{
	public readonly DifficultyRange sizeSpeedRange = new DifficultyRange(
		new Range(0.7f, 0.7f),
		new Range(0.8f, 1.1f),
		new Range(0.3f, 2),
		new Range(0.2f, 3)
		);

	public readonly DifficultyRange rotationSpeedRange = new DifficultyRange(
		new Range(5, 20),
		new Range(5, 20),
		new Range(10, 30),
		new Range(10, 30)
		);

	public const float REVERT_TIME = 2;
	public const float MAX_SIZE = 21;

	protected bool isReverting;

	public abstract void Revert();
}
