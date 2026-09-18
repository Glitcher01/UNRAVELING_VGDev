using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BookState { Stowed, Drawing, Held, Closing, Cooldown, Stowing }

public class BookController : MonoBehaviour
{
    public InputActionReference interactAction;
    public InputActionReference attackAction;
    public ParanoiaMeter paranoia;
    public BookCatchZone catchZone;
    public Transform catchTarget;
    public MonoBehaviour presenterComponent;
    [Min(0f)] public float cooldown = 0.6f;
    [Range(0f, 1f)] public float graceWindow = 0.1f;

    public BookState State { get; private set; } = BookState.Stowed;
    public event Action<HealingParticle> LetterCaught;
    public event Action CatchMissed;
    public event Action<BookState> StateChanged;

    IBookPresenter _presenter;
    InputAction _interact;
    InputAction _attack;
    float _nextCatchAt;
    bool _releaseDuringClose;

    void OnEnable()
    {
        _presenter = presenterComponent as IBookPresenter;
        if (_presenter == null || interactAction == null || interactAction.action == null
            || attackAction == null || attackAction.action == null || paranoia == null
            || catchZone == null || catchTarget == null)
        {
            Debug.LogError("Book controller needs input actions, presenter, catch zone/target, and paranoia.", this);
            enabled = false;
            return;
        }
        _interact = interactAction.action.Clone();
        _attack = attackAction.action.Clone();
        _interact.wantsInitialStateCheck = true;
        _interact.Enable();
        _attack.Enable();
        _presenter.Opened += OnOpened;
        _presenter.Closed += OnClosed;
        _presenter.Stowed += OnStowed;
        catchZone.SetTracking(false);
        SetState(BookState.Stowed);
    }

    void OnDisable()
    {
        if (State == BookState.Closing)
            _nextCatchAt = Mathf.Max(_nextCatchAt, Time.time + Mathf.Max(0f, cooldown));
        if (_presenter != null)
        {
            _presenter.Opened -= OnOpened;
            _presenter.Closed -= OnClosed;
            _presenter.Stowed -= OnStowed;
            _presenter.Stow();
        }
        _interact?.Dispose();
        _attack?.Dispose();
        _interact = null;
        _attack = null;
        if (catchZone != null) catchZone.SetTracking(false);
        SetState(BookState.Stowed);
    }

    void SetState(BookState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }

    void Stow()
    {
        SetState(BookState.Stowing);
        _presenter.Stow();
    }

    void Update()
    {
        if (_interact == null || Time.timeScale <= 0f) return;
        bool held = _interact.IsPressed();
        BookState atStart = State;
        if (State == BookState.Closing)
        {
            if (!held) _releaseDuringClose = true;
            return;
        }
        if (!held)
        {
            if (State != BookState.Stowed && State != BookState.Stowing) Stow();
            return;
        }
        if (State == BookState.Stowed || State == BookState.Stowing)
        {
            // putting it away and pulling it back out shouldn't skip the wait
            if (Time.time < _nextCatchAt) return;
            catchZone.SetTracking(true);
            SetState(BookState.Drawing);
            _presenter.Draw();
        }
        else if (State == BookState.Cooldown && Time.time >= _nextCatchAt)
        {
            SetState(BookState.Drawing);
            _presenter.Reopen();
        }
        if (atStart == BookState.Held && State == BookState.Held && _attack.WasPressedThisFrame())
            TryClose();
    }

    // only catch when the book is ready, even if another script asks it to close
    public bool TryClose()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || State != BookState.Held
            || _interact == null || !_interact.IsPressed() || Time.time < _nextCatchAt) return false;
        _releaseDuringClose = false;
        SetState(BookState.Closing);
        int count = 0;
        foreach (var particle in catchZone.ResolveCatch(graceWindow))
        {
            if (particle == null || !particle.TryCatch(catchTarget, paranoia)) continue;
            count++;
            LetterCaught?.Invoke(particle);
        }
        if (count == 0) CatchMissed?.Invoke();
        _presenter.Close();
        return true;
    }

    void OnOpened()
    {
        if (State != BookState.Drawing) return;
        if (_interact.IsPressed()) SetState(BookState.Held);
        else Stow();
    }

    void OnClosed()
    {
        if (State != BookState.Closing) return;
        _nextCatchAt = Time.time + Mathf.Max(0f, cooldown);
        if (_releaseDuringClose || !_interact.IsPressed()) Stow();
        else SetState(BookState.Cooldown);
    }

    void OnStowed()
    {
        if (State != BookState.Stowing) return;
        catchZone.SetTracking(false);
        SetState(BookState.Stowed);
    }
}
