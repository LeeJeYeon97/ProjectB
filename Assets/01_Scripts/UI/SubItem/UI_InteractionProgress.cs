using UnityEngine;
using UnityEngine.UI;

public class UI_InteractionProgress : UI_Base
{
    private enum Images
    {
        Image_Bg,
        Image_Progress,
    }

    [Header("Settings")]
    [SerializeField] private bool hideWhenIdle = true;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    private IInteractable _target;
    private Transform _followTarget;

    public override void Init()
    {
        if (_init) return;

        base.Init();
        Bind<Image>(typeof(Images));

        SetProgress(0f);
        if (hideWhenIdle)
        {
            SetVisible(false);
        }
    }

    public void SetTarget(IInteractable target, Transform followTarget = null)
    {
        _target = target;
        _followTarget = followTarget ?? (target as MonoBehaviour)?.transform;
    }

    public void SetProgress(float ratio)
    {
        Init();
        Image progressImg = GetImage((int)Images.Image_Progress);
        if (progressImg != null)
        {
            progressImg.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    public void SetVisible(bool visible)
    {
        Init();
        Image bgImg = GetImage((int)Images.Image_Bg);
        Image progressImg = GetImage((int)Images.Image_Progress);

        if (bgImg != null) bgImg.enabled = visible;
        if (progressImg != null) progressImg.enabled = visible;
    }

    private void Update()
    {
        //if (_target == null) return;
        //
        //bool isInteracting = _target.IsInteracting;
        //float progress = _target.Progress;
        //
        //if (hideWhenIdle)
        //{
        //    if (isInteracting && progress > 0f)
        //    {
        //        SetVisible(true);
        //        SetProgress(progress);
        //    }
        //    else
        //    {
        //        SetVisible(false);
        //        SetProgress(0f);
        //    }
        //}
        //else
        //{
        //    SetVisible(true);
        //    SetProgress(progress);
        //}
    }

    private void LateUpdate()
    {
        // 부모가 따로 없을 때 지정된 followTarget을 따라가도록 지원
        if (_followTarget != null && transform.parent == null)
        {
            transform.position = _followTarget.position + offset;
        }
    }
}
