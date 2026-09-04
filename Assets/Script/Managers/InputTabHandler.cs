// InputTabHandler.cs — gắn vào LoginForm và RegisterForm
using UnityEngine;
using TMPro;

public class InputTabHandler : MonoBehaviour
{
    public TMP_InputField[] inputFields; // kéo theo thứ tự vào đây

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Tìm field đang active
            for (int i = 0; i < inputFields.Length; i++)
            {
                if (inputFields[i].isFocused)
                {
                    // Chuyển sang field tiếp theo
                    int next = (i + 1) % inputFields.Length;
                    inputFields[next].Select();
                    inputFields[next].ActivateInputField();
                    break;
                }
            }
        }
    }
}