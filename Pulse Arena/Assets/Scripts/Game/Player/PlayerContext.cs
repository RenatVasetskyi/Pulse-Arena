using System;
using Data;
using Game.Common;
using Game.Common.Interfaces;
using Game.Player.Interfaces;
using Game.Visuals;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    ///     Lean handle the player states use to reach their collaborators + tuning data directly, so all per-frame
    ///     work lives in the states. Exposes the shared gameplay ticks (<see cref="TickCommon" />/
    ///     <see cref="FixedTickCommon" />), the dash-input probe, the transition callbacks (kept on the controller
    ///     so they guard the dead flag), and the Rigidbody/Visual the pause state freezes. Mirrors
    ///     <see cref="Game.Enemy.EnemyContext" />.
    /// </summary>
    public sealed class PlayerContext
    {
        private readonly Action _changeToDash;
        private readonly Action _changeToHit;
        private readonly Action _changeToIdle;
        private readonly Action _changeToRun;
        private readonly Action _die;
        private readonly Func<bool> _hasFallenOff;
        private readonly Func<bool> _hasMoveInput;
        private readonly IActorHealth _health;
        private readonly HitFlash _hitFlash;
        private readonly Action _startDash;

        public IPlayerDash Dash { get; }
        public PlayerData Data { get; }
        public bool HasMoveInput => _hasMoveInput();
        public IPlayerMovement Movement { get; }
        public Rigidbody Rigidbody { get; }
        public IPlayerVisual Visual { get; }

        public PlayerContext(
            IPlayerMovement movement,
            IPlayerDash dash,
            IPlayerVisual visual,
            Rigidbody rigidbody,
            IActorHealth health,
            HitFlash hitFlash,
            PlayerData data,
            Func<bool> hasMoveInput,
            Func<bool> hasFallenOff,
            Action changeToIdle,
            Action changeToRun,
            Action changeToDash,
            Action changeToHit,
            Action startDash,
            Action die)
        {
            Movement = movement;
            Dash = dash;
            Visual = visual;
            Rigidbody = rigidbody;
            Data = data;
            _health = health;
            _hitFlash = hitFlash;
            _hasMoveInput = hasMoveInput;
            _hasFallenOff = hasFallenOff;
            _changeToIdle = changeToIdle;
            _changeToRun = changeToRun;
            _changeToDash = changeToDash;
            _changeToHit = changeToHit;
            _startDash = startDash;
            _die = die;
        }

        // Every gameplay state runs this; the pause + dead states don't, so the countdowns freeze under pause and
        // stop at death for free. Rings out if the player has fallen off the arena.
        public void TickCommon()
        {
            float deltaTime = Time.deltaTime;
            _health.Tick(deltaTime);
            Dash.Tick(deltaTime);
            _hitFlash.Tick(deltaTime);

            if (_hasFallenOff())
                _die();
        }

        // The Rigidbody leaves Y rotation free (RotateToInput turns the player), so kill physics-induced spin each
        // step or a collision pinwheels the player.
        public void FixedTickCommon()
        {
            Movement.KillAngularVelocity();
        }

        // Dash-input probe the grounded states run: if the dash is ready and pressed, start it and report a
        // transition so the caller bails out of its own logic this frame.
        public bool TryStartDash()
        {
            if (!Dash.IsReady || !Dash.WantsDash())
                return false;

            _startDash();
            return true;
        }

        public void ChangeToDashState()
        {
            _changeToDash();
        }

        public void ChangeToHitState()
        {
            _changeToHit();
        }

        public void ChangeToIdleState()
        {
            _changeToIdle();
        }

        public void ChangeToRunState()
        {
            _changeToRun();
        }
    }
}
