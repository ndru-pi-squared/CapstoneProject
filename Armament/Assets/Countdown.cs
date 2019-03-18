using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Countdown : MonoBehaviour
{
    float time;
    int time2;
    public TextMeshPro tm;
    public AudioSource audioSource;
    public AudioClip countdown;
    public AudioClip start;
    public AudioClip door; 
    // Start is called before the first frame update
    void Start()
    {
        time = 60f;
        tm = GetComponent<TextMeshPro>();
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(SoundDelay());
    }

    // Update is called once per frame
    void Update()
    {
        if (time >= 0)
        {
            time -= 1 * Time.deltaTime;
            time2 = (int)time;
            tm.text = time2.ToString();
        }
        else {
            tm.text = "Fight";
        }
    }

    IEnumerator SoundDelay()
    {
        yield return new WaitForSeconds(49);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(countdown, 1f);
        yield return new WaitForSeconds(1);
        audioSource.PlayOneShot(start, 1f);
        audioSource.PlayOneShot(door, 1f);
    }
}
