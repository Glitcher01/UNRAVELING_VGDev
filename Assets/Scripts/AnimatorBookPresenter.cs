using System;
using UnityEngine;

public class AnimatorBookPresenter : MonoBehaviour, IBookPresenter
{
    public Animator bookAnimator;
    public Transform bookPose;
    public Renderer[] bookRenderers;
    public Vector3 heldPosition;
    public Vector3 stowedPosition = new Vector3(0f, -2f, 0f);
    [Min(0f)] public float drawDuration = 0.35f;
    [Min(0f)] public float stowDuration = 0.25f;

    public event Action Opened;
    public event Action Closed;
    public event Action Stowed;

    static readonly int OpenHash = Animator.StringToHash("Base Layer.Open");
    static readonly int CloseHash = Animator.StringToHash("Base Layer.Close");
    static readonly int HeldOpenHash = Animator.StringToHash("Base Layer.HeldOpen");
    static readonly int HeldClosedHash = Animator.StringToHash("Base Layer.HeldClosed");
    enum Motion { None, Drawing, Opening, Closing, Stowing }
    Motion _motion;
    Vector3 _start;
    float _elapsed;
    int _requestFrame;
    bool _ready;

    void OnEnable()
    {
        _ready = bookAnimator != null && bookPose != null && bookRenderers != null
            && bookRenderers.Length > 0 && bookAnimator.runtimeAnimatorController != null;
        if (_ready)
            _ready = bookAnimator.HasState(0, OpenHash) && bookAnimator.HasState(0, CloseHash)
                && bookAnimator.HasState(0, HeldOpenHash) && bookAnimator.HasState(0, HeldClosedHash);
        if (!_ready)
        {
            Debug.LogError("Book presenter needs its Animator, four book states, pose, and renderers.", this);
            enabled = false;
            return;
        }
        bookAnimator.applyRootMotion = false;
        bookAnimator.updateMode = AnimatorUpdateMode.Normal;
        bookAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        bookPose.localPosition = stowedPosition;
        bookAnimator.Play(HeldClosedHash, 0, 0f);
        _motion = Motion.None;
        Show(false);
    }

    void OnDisable()
    {
        _motion = Motion.None;
        if (bookPose != null) bookPose.localPosition = stowedPosition;
        Show(false);
    }

    void Show(bool visible)
    {
        if (bookRenderers == null) return;
        foreach (var renderer in bookRenderers)
            if (renderer != null) renderer.enabled = visible;
    }

    void Begin(Motion motion, int stateHash)
    {
        if (!_ready || !isActiveAndEnabled) return;
        _motion = motion;
        _requestFrame = Time.frameCount;
        _start = bookPose.localPosition;
        _elapsed = 0f;
        if (stateHash != 0) bookAnimator.Play(stateHash, 0, 0f);
    }

    public void Draw()
    {
        Show(true);
        Begin(Motion.Drawing, OpenHash);
    }

    public void Reopen() => Begin(Motion.Opening, OpenHash);
    public void Close() => Begin(Motion.Closing, CloseHash);
    public void Stow() => Begin(Motion.Stowing, 0);

    bool Finished(int hash)
    {
        var state = bookAnimator.GetCurrentAnimatorStateInfo(0);
        return Time.frameCount > _requestFrame && !bookAnimator.IsInTransition(0)
            && state.fullPathHash == hash && state.normalizedTime >= 1f;
    }

    void LateUpdate()
    {
        if (_motion == Motion.None || Time.timeScale <= 0f) return;
        _elapsed += Time.deltaTime;
        if (_motion == Motion.Drawing || _motion == Motion.Stowing)
        {
            float duration = _motion == Motion.Drawing ? drawDuration : stowDuration;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / duration);
            bookPose.localPosition = Vector3.Lerp(_start,
                _motion == Motion.Drawing ? heldPosition : stowedPosition, Mathf.SmoothStep(0f, 1f, t));
        }
        if (_motion == Motion.Stowing && _elapsed >= stowDuration)
        {
            _motion = Motion.None;
            Show(false);
            Stowed?.Invoke();
        }
        else if ((_motion == Motion.Drawing || _motion == Motion.Opening)
            && _elapsed >= (_motion == Motion.Drawing ? drawDuration : 0f) && Finished(OpenHash))
        {
            _motion = Motion.None;
            bookAnimator.Play(HeldOpenHash, 0, 0f);
            Opened?.Invoke();
        }
        else if (_motion == Motion.Closing && Finished(CloseHash))
        {
            _motion = Motion.None;
            bookAnimator.Play(HeldClosedHash, 0, 0f);
            Closed?.Invoke();
        }
    }
}
