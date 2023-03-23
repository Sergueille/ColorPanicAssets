using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] float fadeSpeed;
    [SerializeField] float rotationSpeed;
    [SerializeField] float duration;
    [SerializeField] float coinAlpha;
    [SerializeField] AudioSource coinSound;
    [SerializeField] ParticleSystem touchParticles;
    [SerializeField] float touchParticlesDuration;
    [SerializeField] Range pitchRange;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        LeanTween.alpha(gameObject, coinAlpha, fadeSpeed);

        yield return new WaitForSeconds(duration);

        LeanTween.alpha(gameObject, 0, fadeSpeed);
        yield return new WaitForSeconds(fadeSpeed);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.rotation = Quaternion.Euler(0, 0, rotationSpeed * Time.time);
    }

	private IEnumerator OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.name == "Player")
		{
            StartCoroutine(GameManager.instance.CameraShake(true));
            touchParticles.Play();

            LeanTween.alpha(gameObject, 0, fadeSpeed);
            LeanTween.scale(gameObject, new Vector3(0.7f, 0.7f), fadeSpeed);
            //GameManager.instance.money += coinValue;
            GameManager.instance.lastCoinAmount += GameManager.instance.currentPowerup == 3 ? 3 : 1;

            coinSound.pitch = pitchRange.PickRandom();
            coinSound.Play();

            yield return new WaitForSeconds(touchParticlesDuration);

            touchParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Destroy(gameObject);
        }
	}
}
