using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    private static string Name;
    private static string AvatarChoice;

    void Start()
    {
        Debug.Log("Created PlayerData object");
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        
    }

    public string GetName()
    {
        return Name;
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public string GetAvatarChoicee()
    {
        return AvatarChoice;
    }

    public void SetAvatarChoice(string avatarChoice)
    {
        Debug.Log("SetAvatarChoice test");
        AvatarChoice = avatarChoice;
    }




    // Start is called before the first frame update

}
