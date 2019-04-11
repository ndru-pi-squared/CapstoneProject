using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraAnimationHandler : MonoBehaviour
{
    public static CameraAnimationHandler CA;
    public Animator anim;

    void Awake() {
        CA = this;
        anim = this.GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start() {}

    // Update is called once per frame
    void Update() {
        if (anim.IsInTransition(0)) {
            ResetTriggers();
        }

    }

    public void MoveLeft() {
        anim.SetTrigger("MoveLeft");
    }

    public void MoveRight() {
        Text text = GameObject.Find("/Canvas/Login and Register (Front Panel)/Status Panel/Progress Label").GetComponent<Text>();///Login and Register(Front Panel)/Status Panel/Progress Label")
        text.text = "Waiting for user to do something...";
        anim.SetTrigger("MoveRight");
    }

    public void MoveBack() {
        anim.SetTrigger("MoveBack");
    }

    public void MoveUp() {
        anim.SetTrigger("MoveUp");
    }

    public void MoveDown() {
        anim.SetTrigger("MoveDown");
    }

    public void ResetTriggers() {
        anim.ResetTrigger("MoveLeft");
        anim.ResetTrigger("MoveRight");
        anim.ResetTrigger("MoveBack");
        anim.ResetTrigger("MoveUp");
        anim.ResetTrigger("MoveDown");
    }
}
