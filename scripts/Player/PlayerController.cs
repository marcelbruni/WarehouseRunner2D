using System;
using Godot;
using WarehouseRunner.Elements;

namespace WarehouseRunner.Player;

public enum PlayerState
{
    Grounded,
    Skidding,
    Airborne,
    Dashing,
    RollingOut,
    SlopeSliding,
}

public enum AirMove
{
    Fall,
    Jump,
    DoubleJump,
    SlideJump,
    WallKick,
    Dash,
}

public enum Trick
{
    Jump,
    DoubleJump,
    SlideJump,
    Dash,
    DashJump,
    WallKick,
    JumpRollout,
    DashRollout,
    GroundRoll,
    RollJump,
    PowerDash,
    SlopeSlide,
}

public partial class PlayerController : CharacterBody2D
{
    private enum RolloutKind
    {
        None,
        Jump,
        Dash,
        Ground,
    }

    private const float SkidCooldownTime = 0.25f;
    private const float FloorProbeDistance = 4f;

    private float _groundSpeed;
    private Vector2 _preMoveVelocity;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _rollBufferTimer;
    private bool _dashRequested;
    private bool _rollRequested;
    private float _postLandingRollTimer;
    private float _timeSinceLanding;
    private RolloutKind _pendingRolloutKind;

    private bool _canDoubleJump = true;
    private bool _canDash = true;
    private bool _usedDashThisAirtime;
    private bool _performedAirAction;

    private int _dashDirection = 1;
    private float _dashSpeed;
    private float _dashExitSpeed;
    private bool _isRollJumpChain;
    private bool _isGroundRollLocked;
    private float _slideJumpGraceTimer;
    private float _skidCooldownTimer;

    private float _wallNormalX;
    private float _wallCoyoteTimer;
    private float _wallKickLockTimer;

    private float _boostTimer;
    private float _boostFactor = 1f;

    public event Action<Trick, bool>? TrickPerformed;

    public event Action<float>? Landed;

    [Export]
    public MovementTuning Tuning { get; set; } = new();

    public IPlayerInputSource InputSource { get; set; } = new LocalInputSource();

    public PlayerState State { get; private set; } = PlayerState.Airborne;

    public AirMove CurrentAirMove { get; private set; } = AirMove.Fall;

    public float StateTime { get; private set; }

    public int Facing { get; private set; } = 1;

    public bool IsWallSliding { get; private set; }

    public bool IsTouchingWall { get; private set; }

    public bool CarriesPackage { get; set; }

    public bool IsBoosted => _boostTimer > 0f;

    public float BoostFactor => IsBoosted ? _boostFactor : 1f;

    public float CurrentMaxSpeed => Tuning.RunSpeed * BoostFactor;

    public float GroundSpeed => _groundSpeed;

    public bool IsOnGround => State is PlayerState.Grounded or PlayerState.Skidding or PlayerState.RollingOut or PlayerState.SlopeSliding;

    public override void _Ready()
    {
        FloorSnapLength = 12f;
        FloorConstantSpeed = true;
        FloorMaxAngle = Mathf.DegToRad(46f);
        PlatformOnLeave = PlatformOnLeaveEnum.AddVelocity;
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;
        var input = InputSource.Sample();

        TrackInputBuffers(input, dt);
        TickTimers(dt);
        StateTime += dt;

        switch (State)
        {
            case PlayerState.Grounded:
                UpdateGrounded(input, dt);
                break;
            case PlayerState.Skidding:
                UpdateSkidding(input, dt);
                break;
            case PlayerState.Airborne:
                UpdateAirborne(input, dt);
                break;
            case PlayerState.Dashing:
                UpdateDashing(input);
                break;
            case PlayerState.RollingOut:
                UpdateRollingOut();
                break;
            case PlayerState.SlopeSliding:
                UpdateSlopeSliding(input, dt);
                break;
        }

        if (IsOnGround)
        {
            Velocity = new Vector2(_groundSpeed, 0f);
        }

        _preMoveVelocity = Velocity;
        MoveAndSlide();
        ResolveContacts(input);
    }

    public void Respawn(Vector2 position)
    {
        GlobalPosition = position;
        Velocity = Vector2.Zero;
        _groundSpeed = 0f;
        _boostTimer = 0f;
        _jumpBufferTimer = 0f;
        _rollBufferTimer = 0f;
        _postLandingRollTimer = 0f;
        _canDoubleJump = true;
        _canDash = true;
        _isRollJumpChain = false;
        _isGroundRollLocked = false;
        CurrentAirMove = AirMove.Fall;
        ChangeState(PlayerState.Airborne);
        ResetPhysicsInterpolation();
    }

    private void TrackInputBuffers(PlayerInput input, float dt)
    {
        _jumpBufferTimer = input.JumpPressed ? Tuning.JumpBufferTime : _jumpBufferTimer - dt;
        ResolveActionPress(input.ActionPressed);
        _rollBufferTimer = _rollRequested ? Tuning.RollEarlyWindow : _rollBufferTimer - dt;
    }

    private void ResolveActionPress(bool pressed)
    {
        _rollRequested = pressed && (IsOnGround || IsLandingImminent());
        _dashRequested = pressed && !_rollRequested;
    }

    private bool IsLandingImminent()
    {
        var t = Tuning.RollEarlyWindow;
        var motion = new Vector2(Velocity.X * t, (Velocity.Y * t) + (0.5f * Tuning.Gravity * t * t));
        var hit = MoveAndCollide(motion, testOnly: true);
        return hit is not null && hit.GetNormal().Dot(UpDirection) >= Mathf.Cos(FloorMaxAngle);
    }

    private void TickTimers(float dt)
    {
        _coyoteTimer -= dt;
        _wallCoyoteTimer -= dt;
        _wallKickLockTimer -= dt;
        _slideJumpGraceTimer -= dt;
        _skidCooldownTimer -= dt;
        _postLandingRollTimer -= dt;
        _timeSinceLanding += dt;
        _boostTimer -= dt;
    }

    private void UpdateGrounded(PlayerInput input, float dt)
    {
        if (ConsumeJumpBuffer())
        {
            if (_slideJumpGraceTimer > 0f)
            {
                StartSlideJump(input);
            }
            else
            {
                StartJump(AirMove.Jump, Tuning.JumpSpeed, _groundSpeed, Trick.Jump);
            }

            return;
        }

        if (_rollRequested && _postLandingRollTimer > 0f)
        {
            StartRollout(_pendingRolloutKind, _timeSinceLanding <= Tuning.PerfectRollWindow, input);
            return;
        }

        if (_isGroundRollLocked && !IsBoosted && Mathf.Abs(_groundSpeed) <= Tuning.RunSpeed)
        {
            _isGroundRollLocked = false;
        }

        if (_rollRequested && IsRunning(input) && !_isGroundRollLocked)
        {
            _isGroundRollLocked = true;
            StartRollout(RolloutKind.Ground, perfect: false, input);
            return;
        }

        if (ShouldSkid(input))
        {
            ChangeState(PlayerState.Skidding);
            return;
        }

        _groundSpeed = Steer(
            _groundSpeed,
            input.MoveX,
            Tuning.GroundAcceleration,
            Tuning.TurnDeceleration,
            Tuning.OverspeedDecay,
            Tuning.GroundFriction,
            dt);

        if (!Mathf.IsZeroApprox(input.MoveX))
        {
            Facing = Mathf.Sign(input.MoveX);
        }
    }

    private bool IsRunning(PlayerInput input) =>
        !Mathf.IsZeroApprox(input.MoveX) && Mathf.Abs(_groundSpeed) >= Tuning.GroundRollMinSpeed;

    private bool ShouldSkid(PlayerInput input) =>
        _skidCooldownTimer <= 0f
        && Mathf.Abs(_groundSpeed) >= Tuning.SkidMinSpeed
        && !Mathf.IsZeroApprox(input.MoveX)
        && Mathf.Sign(input.MoveX) != Mathf.Sign(_groundSpeed);

    private void UpdateSkidding(PlayerInput input, float dt)
    {
        if (ConsumeJumpBuffer())
        {
            StartSlideJump(input);
            return;
        }

        _groundSpeed = Mathf.MoveToward(_groundSpeed, 0f, Tuning.SkidDeceleration * dt);

        if (Mathf.IsZeroApprox(_groundSpeed) || StateTime >= Tuning.SkidMaxTime)
        {
            _slideJumpGraceTimer = Tuning.SlideJumpGraceTime;
            ExitSkid(input);
        }
    }

    private void ExitSkid(PlayerInput input)
    {
        _skidCooldownTimer = SkidCooldownTime;
        if (!Mathf.IsZeroApprox(input.MoveX))
        {
            Facing = Mathf.Sign(input.MoveX);
        }

        ChangeState(PlayerState.Grounded);
    }

    private void UpdateAirborne(PlayerInput input, float dt)
    {
        if (_jumpBufferTimer > 0f && TryAirJump())
        {
            return;
        }

        if (_dashRequested && _canDash)
        {
            StartDash(input);
            return;
        }

        var move = _wallKickLockTimer > 0f ? 0f : input.MoveX;
        var vx = Steer(
            Velocity.X,
            move,
            Tuning.AirAcceleration,
            Tuning.AirTurnAcceleration,
            Tuning.AirOverspeedDecay,
            Tuning.AirFriction,
            dt);

        IsWallSliding = IsTouchingWall && Velocity.Y > 0f && input.MoveX * _wallNormalX < 0f;

        var gravity = Tuning.Gravity;
        var isCuttableJump = CurrentAirMove is AirMove.Jump or AirMove.DoubleJump;
        if (Velocity.Y < 0f && !input.JumpHeld && isCuttableJump)
        {
            gravity *= Tuning.JumpReleaseGravityFactor;
        }

        var maxFall = IsWallSliding ? Tuning.WallSlideMaxFallSpeed : Tuning.MaxFallSpeed;
        var vy = Mathf.Min(Velocity.Y + (gravity * dt), maxFall);

        Velocity = new Vector2(vx, vy);
    }

    private bool TryAirJump()
    {
        if (_wallCoyoteTimer > 0f)
        {
            StartWallKick();
            return true;
        }

        if (_coyoteTimer > 0f && CurrentAirMove == AirMove.Fall)
        {
            StartJump(AirMove.Jump, Tuning.JumpSpeed, Velocity.X, Trick.Jump);
            return true;
        }

        if (_canDoubleJump)
        {
            var fromDash = State == PlayerState.Dashing || CurrentAirMove == AirMove.Dash;
            var horizontal = fromDash ? _dashDirection * _dashExitSpeed : Velocity.X;
            _canDoubleJump = false;
            StartJump(AirMove.DoubleJump, Tuning.DoubleJumpSpeed, horizontal, fromDash ? Trick.DashJump : Trick.DoubleJump);
            return true;
        }

        return false;
    }

    private void StartJump(AirMove move, float speed, float horizontal, Trick trick)
    {
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        _performedAirAction = true;
        Velocity = new Vector2(horizontal, -speed);
        CurrentAirMove = move;
        ChangeState(PlayerState.Airborne);
        TrickPerformed?.Invoke(trick, false);
    }

    private void StartSlideJump(PlayerInput input)
    {
        var direction = !Mathf.IsZeroApprox(input.MoveX) ? Mathf.Sign(input.MoveX) : -Facing;
        Facing = direction;
        StartJump(AirMove.SlideJump, Tuning.SlideJumpSpeed, direction * Tuning.SlideJumpHorizontalSpeed, Trick.SlideJump);
    }

    private void StartWallKick()
    {
        _wallCoyoteTimer = 0f;
        _wallKickLockTimer = Tuning.WallKickInputLockTime;
        _canDash = true;
        Facing = Mathf.Sign(_wallNormalX);
        StartJump(AirMove.WallKick, Tuning.WallKickSpeed, _wallNormalX * Tuning.WallKickHorizontalSpeed, Trick.WallKick);
    }

    private void StartDash(PlayerInput input)
    {
        _dashDirection = !Mathf.IsZeroApprox(input.MoveX) ? Mathf.Sign(input.MoveX) : Facing;
        Facing = _dashDirection;
        _canDash = false;
        _usedDashThisAirtime = true;
        _performedAirAction = true;
        IsWallSliding = false;
        _dashSpeed = _isRollJumpChain ? Tuning.ChainDashSpeed : Tuning.DashSpeed;
        _dashExitSpeed = _isRollJumpChain ? Tuning.ChainDashExitSpeed : Tuning.DashExitSpeed;
        Velocity = new Vector2(_dashDirection * _dashSpeed, 0f);
        ChangeState(PlayerState.Dashing);
        TrickPerformed?.Invoke(_isRollJumpChain ? Trick.PowerDash : Trick.Dash, false);
    }

    private void UpdateDashing(PlayerInput input)
    {
        if (_jumpBufferTimer > 0f && TryAirJump())
        {
            return;
        }

        if (StateTime >= Tuning.DashDuration)
        {
            EndDash();
            return;
        }

        Velocity = new Vector2(_dashDirection * _dashSpeed, 0f);
    }

    private void EndDash()
    {
        Velocity = new Vector2(_dashDirection * _dashExitSpeed, 0f);
        CurrentAirMove = AirMove.Dash;
        ChangeState(PlayerState.Airborne);
    }

    private void StartRollout(RolloutKind kind, bool perfect, PlayerInput input)
    {
        var (boost, boostTime, trick) = kind switch
        {
            RolloutKind.Dash => (Tuning.DashRolloutBoost, Tuning.DashRolloutBoostTime, Trick.DashRollout),
            RolloutKind.Ground => (Tuning.GroundRollBoost, Tuning.GroundRollBoostTime, Trick.GroundRoll),
            _ => (Tuning.JumpRolloutBoost, Tuning.JumpRolloutBoostTime, Trick.JumpRollout),
        };
        boost += perfect ? Tuning.PerfectRollBonus : 0f;

        var direction = !Mathf.IsZeroApprox(input.MoveX) ? Mathf.Sign(input.MoveX)
            : !Mathf.IsZeroApprox(_groundSpeed) ? Mathf.Sign(_groundSpeed)
            : Facing;

        ActivateBoost(boost, boostTime);
        _groundSpeed = direction * Mathf.Max(Mathf.Abs(_groundSpeed), Tuning.RunSpeed * boost);
        Facing = direction;
        _postLandingRollTimer = 0f;
        _rollBufferTimer = 0f;
        ChangeState(PlayerState.RollingOut);
        TrickPerformed?.Invoke(trick, perfect);
    }

    private void UpdateRollingOut()
    {
        if (ConsumeJumpBuffer())
        {
            var speed = Mathf.Abs(_groundSpeed);
            var horizontal = Mathf.Max(speed, Mathf.Min(speed * Tuning.RollJumpHorizontalFactor, Tuning.RollJumpMaxSpeed));
            _isRollJumpChain = true;
            StartJump(AirMove.Jump, Tuning.JumpSpeed, Facing * horizontal, Trick.RollJump);
            return;
        }

        if (StateTime >= Tuning.RolloutDuration)
        {
            ChangeState(PlayerState.Grounded);
        }
    }

    private void UpdateSlopeSliding(PlayerInput input, float dt)
    {
        if (ConsumeJumpBuffer())
        {
            StartJump(AirMove.Jump, Tuning.JumpSpeed, _groundSpeed, Trick.Jump);
            return;
        }

        var normal = GetFloorNormal();
        var downhill = Mathf.Sign(normal.X);
        var steepness = Mathf.Abs(normal.X);

        _groundSpeed += downhill * Tuning.Gravity * steepness * Tuning.SlopeSlideGravityFactor * dt;
        _groundSpeed += input.MoveX * Tuning.SlopeSteerAcceleration * dt;
        _groundSpeed = Mathf.Clamp(_groundSpeed, -Tuning.SlopeSlideMaxSpeed, Tuning.SlopeSlideMaxSpeed);

        ActivateBoost(Tuning.SlopeSlideBoost, Tuning.SlopeSlideBoostAfterTime);

        if (!Mathf.IsZeroApprox(_groundSpeed))
        {
            Facing = Mathf.Sign(_groundSpeed);
        }
    }

    private void ResolveContacts(PlayerInput input)
    {
        IsTouchingWall = false;

        if (IsOnFloor())
        {
            _coyoteTimer = Tuning.CoyoteTime;
            if (State is PlayerState.Airborne or PlayerState.Dashing)
            {
                Land(input);
            }
            else if (State == PlayerState.Grounded && IsOnSlipperyFloor())
            {
                EnterSlopeSlide();
            }
            else if (State == PlayerState.SlopeSliding && !IsOnSlipperyFloor())
            {
                ChangeState(PlayerState.Grounded);
            }

            if (IsOnGround && IsOnWall() && Mathf.Sign(_groundSpeed) == -Mathf.Sign(GetWallNormal().X))
            {
                _groundSpeed = 0f;
            }

            return;
        }

        if (IsOnGround)
        {
            CurrentAirMove = AirMove.Fall;
            ChangeState(PlayerState.Airborne);
        }

        if (IsOnWallOnly())
        {
            IsTouchingWall = true;
            _wallNormalX = GetWallNormal().X;
            _wallCoyoteTimer = Tuning.WallCoyoteTime;
            if (State == PlayerState.Dashing)
            {
                EndDash();
                Velocity = new Vector2(0f, Velocity.Y);
            }
        }
    }

    private void Land(PlayerInput input)
    {
        var rolloutKind = _usedDashThisAirtime ? RolloutKind.Dash
            : _performedAirAction ? RolloutKind.Jump
            : RolloutKind.None;

        _groundSpeed = _preMoveVelocity.X;
        _canDoubleJump = true;
        _canDash = true;
        _usedDashThisAirtime = false;
        _performedAirAction = false;
        _isRollJumpChain = false;
        IsWallSliding = false;
        _timeSinceLanding = 0f;
        ChangeState(PlayerState.Grounded);
        Landed?.Invoke(_preMoveVelocity.Y);

        if (rolloutKind != RolloutKind.None)
        {
            if (_rollBufferTimer > 0f)
            {
                var pressAge = Tuning.RollEarlyWindow - _rollBufferTimer;
                StartRollout(rolloutKind, pressAge <= Tuning.PerfectRollWindow, input);
                return;
            }

            _pendingRolloutKind = rolloutKind;
            _postLandingRollTimer = Tuning.RollLateWindow;
        }

        if (IsOnSlipperyFloor())
        {
            EnterSlopeSlide();
        }
    }

    private void EnterSlopeSlide()
    {
        ChangeState(PlayerState.SlopeSliding);
        TrickPerformed?.Invoke(Trick.SlopeSlide, false);
    }

    private bool IsOnSlipperyFloor()
    {
        var probe = MoveAndCollide(-UpDirection * FloorProbeDistance, testOnly: true, recoveryAsCollision: true);
        return probe is not null
            && probe.GetCollider() is Crate { IsSlippery: true }
            && probe.GetNormal().Dot(UpDirection) >= Mathf.Cos(FloorMaxAngle);
    }

    private bool ConsumeJumpBuffer()
    {
        if (_jumpBufferTimer <= 0f)
        {
            return false;
        }

        _jumpBufferTimer = 0f;
        return true;
    }

    private void ActivateBoost(float factor, float duration)
    {
        if (!IsBoosted || factor >= _boostFactor)
        {
            _boostFactor = factor;
        }

        _boostTimer = Mathf.Max(_boostTimer, duration);
    }

    private float Steer(float velocity, float move, float acceleration, float turnAcceleration, float overspeedDecay, float idleFriction, float dt)
    {
        if (Mathf.IsZeroApprox(move))
        {
            return Mathf.MoveToward(velocity, 0f, idleFriction * dt);
        }

        var target = move * CurrentMaxSpeed;
        var sameDirection = Mathf.IsZeroApprox(velocity) || Mathf.Sign(move) == Mathf.Sign(velocity);
        if (!sameDirection)
        {
            return Mathf.MoveToward(velocity, target, turnAcceleration * dt);
        }

        var rate = Mathf.Abs(velocity) > Mathf.Abs(target) ? overspeedDecay : acceleration;
        return Mathf.MoveToward(velocity, target, rate * dt);
    }

    private void ChangeState(PlayerState state)
    {
        State = state;
        StateTime = 0f;
    }
}
