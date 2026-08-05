using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : BaseController
{
    public int money = 100;

    private float throwTimer = 0f;
    public float throwInterval = 0.1f; // 0.1초마다 1번씩 돈을 날림
    public int throwAmount = 1;       // 한 번에 1원(또는 지정된 금액)씩 날림

    public StackingSystem Stacking;

    private void Awake()
    {
        Stacking = GetComponent<StackingSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        BaseStation station = other.GetComponent<BaseStation>();
        if (station != null && station.isOperational)
        {
            station.OnCharacterEnter(this.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 부딪힌 오브젝트가 상호작용 발판(BaseStation)인지 확인
        BaseStation station = other.GetComponent<BaseStation>();

        if (station != null && station.isOperational)
        {
            throwTimer += Time.deltaTime;

            // 지정된 시간(0.1초)마다 한 번씩 상호작용
            if (throwTimer >= throwInterval)
            {
                station.OnCharacterStay(this.gameObject);
                throwTimer = 0f; // 타이머 초기화
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BaseStation station = other.GetComponent<BaseStation>();
        if (station != null)
        {
            station.OnCharacterExit(this.gameObject);
        }
    }

    public void UpdateMoney(int addMoney)
    {
        money += addMoney;
    }
}
