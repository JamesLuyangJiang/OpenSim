using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void LoadDLT()
    {
        SceneManager.LoadScene("DLT Traffic");
    }

    public void LoadRCI()
    {
        SceneManager.LoadScene("RCI Traffic");
    }

    public void LoadRBI()
    {
        SceneManager.LoadScene("RBI Traffic");
    }

    public void LoadDDI()
    {
        SceneManager.LoadScene("DDI Traffic");
    }

    public void LoadRAB()
    {
        SceneManager.LoadScene("RAB Traffic");
    }

    public void ExitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
