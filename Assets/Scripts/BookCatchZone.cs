using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class BookCatchZone : MonoBehaviour
{
    readonly Dictionary<HealingParticle, HashSet<Collider>> _inside = new();
    readonly Dictionary<HealingParticle, float> _lastExitTime = new();
    readonly List<HealingParticle> _remove = new();
    BoxCollider _box;
    bool _tracking;

    void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _box.isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        SetTracking(false);
    }

    void OnEnable() => HealingParticle.Unregistered += Forget;

    void OnDisable()
    {
        HealingParticle.Unregistered -= Forget;
        SetTracking(false);
    }

    public void SetTracking(bool enabled)
    {
        _tracking = enabled;
        if (_box == null) _box = GetComponent<BoxCollider>();
        _box.enabled = enabled;
        if (!enabled) ClearTracking();
    }

    public void ClearTracking()
    {
        _inside.Clear();
        _lastExitTime.Clear();
    }

    static bool Eligible(HealingParticle particle) =>
        particle != null && particle.isActiveAndEnabled && !particle.IsCaught;

    void Track(Collider other)
    {
        if (!_tracking) return;
        var particle = other.GetComponentInParent<HealingParticle>();
        if (!Eligible(particle)) return;
        if (!_inside.TryGetValue(particle, out var colliders))
            _inside.Add(particle, colliders = new HashSet<Collider>());
        colliders.Add(other);
        _lastExitTime.Remove(particle);
    }

    void OnTriggerEnter(Collider other) => Track(other);
    void OnTriggerStay(Collider other) => Track(other);

    void OnTriggerExit(Collider other)
    {
        if (!_tracking) return;
        var particle = other.GetComponentInParent<HealingParticle>();
        if (particle == null || !_inside.TryGetValue(particle, out var colliders)) return;
        colliders.Remove(other);
        if (colliders.Count != 0) return;
        _inside.Remove(particle);
        if (Eligible(particle) && other.enabled && other.gameObject.activeInHierarchy)
            _lastExitTime[particle] = Time.time;
    }

    void Forget(HealingParticle particle)
    {
        _inside.Remove(particle);
        _lastExitTime.Remove(particle);
    }

    // clean up letters or hitboxes that are gone or switched off, plus old exit times
    void Prune()
    {
        _remove.Clear();
        foreach (var entry in _inside)
        {
            entry.Value.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            if (!Eligible(entry.Key) || entry.Value.Count == 0) _remove.Add(entry.Key);
        }
        foreach (var particle in _remove) Forget(particle);
        _remove.Clear();
        foreach (var entry in _lastExitTime)
            if (!Eligible(entry.Key) || Time.time - entry.Value > 1f) _remove.Add(entry.Key);
        foreach (var particle in _remove) _lastExitTime.Remove(particle);
    }

    void FixedUpdate()
    {
        if (_tracking) Prune();
    }

    public List<HealingParticle> ResolveCatch(float graceWindow = 0.1f)
    {
        var result = new List<HealingParticle>();
        if (!_tracking) return result;
        Prune();
        foreach (var particle in _inside.Keys) result.Add(particle);
        foreach (var entry in _lastExitTime)
            if (Time.time - entry.Value <= Mathf.Clamp(graceWindow, 0f, 1f)) result.Add(entry.Key);
        // make a separate list so letters disappearing during a catch won't mess this up
        foreach (var particle in result) Forget(particle);
        return result;
    }
}
