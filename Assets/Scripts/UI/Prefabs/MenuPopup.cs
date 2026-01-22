using BirdCafe.Shared;
using UnityEngine;

public class MenuPopup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReturnToMainMenu ()
    {
        BirdCafeGame.Instance.ReturnToMainMenu();
    }

    public void Quit()
    {
        Application.Quit();
    }
}
