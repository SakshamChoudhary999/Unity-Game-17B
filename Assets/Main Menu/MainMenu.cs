using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void EnterApartment()
    {
        SceneManager.LoadScene("Scene_01"); // your current game scene
    }

    public void LeaveGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}