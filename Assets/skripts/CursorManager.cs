using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private void Start()
    {
        
    }

    public void ShowCursor()
    {
        // Делаем курсор видимым
        Cursor.visible = true;

        // Открепляем курсор, чтобы он мог свободно перемещаться по экрану
        Cursor.lockState = CursorLockMode.None;
    }

    
    private void Update()
    {
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            ShowCursor();
        }
    }
    
}