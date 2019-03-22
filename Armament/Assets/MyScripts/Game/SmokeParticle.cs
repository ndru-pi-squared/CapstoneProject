using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeParticle : MonoBehaviour
{
    private ParticleSystem ps;
    public float hSliderValueR = 0.0F;
    public float hSliderValueG = 0.0F;
    public float hSliderValueB = 0.0F;
    public float hSliderValueA = 1.0F;
    public Gradient g;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        ParticleSystem.MainModule psMain = ps.main;
        hSliderValueR++;
        hSliderValueG++;
        hSliderValueB++;
        hSliderValueA++;
        psMain.startColor = g.Evaluate(Random.Range(0f, 1f));
        ps.Emit(1);
    }
}
