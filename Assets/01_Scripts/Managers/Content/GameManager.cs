using System;
using System.Collections.Generic;
using System.Text;


[Serializable]
public class GameManager
{
    public bool IsOpen = false;

    public Counter Counter;

    /// <summary>가게 상태를 열림으로 바꾸고, 이미 열린 경우 다시 처리하지 않습니다.</summary>
    public void OpenShop()
    {
        if (IsOpen)
            return;

        IsOpen = true;
    }
}

