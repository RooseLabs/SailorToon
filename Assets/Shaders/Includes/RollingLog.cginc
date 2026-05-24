#ifndef ROLLING_LOG_INCLUDED
#define ROLLING_LOG_INCLUDED

float  _RL_Amount;
float3 _RL_Center;   // Used in sphere mode.

// Pre-displacement anchor point. Camera in AC mode, fixed world point in sphere mode.
float3 GetRollingLogAnchor()
{
#ifdef _ROLLING_LOG_SPHERE
    return _RL_Center;
#else
    return _WorldSpaceCameraPos;
#endif
}

// Returns (drop, ddrop/dx, ddrop/dz) for the current mode.
// Log mode:    drop = -A * dz^2            (depth curve only — cylinder along x)
// Sphere mode: drop = -A * (dx^2 + dz^2)   (paraboloid — depth + horizontal curve)
float3 RollingLogDisplacement(float dx, float dz)
{
#ifdef _ROLLING_LOG_SPHERE
    float drop = -_RL_Amount * (dx * dx + dz * dz);
    return float3(drop, -2.0 * _RL_Amount * dx, -2.0 * _RL_Amount * dz);
#else
    float drop = -_RL_Amount * dz * dz;
    return float3(drop, 0.0, -2.0 * _RL_Amount * dz);
#endif
}

// Drops world-Y to follow the current bend mode.
// Caller is responsible for gating with `#ifdef _ENABLE_ROLLING_LOG`.
float3 ApplyRollingLog(float3 worldPos)
{
    float3 d = worldPos - GetRollingLogAnchor();
    worldPos.y += RollingLogDisplacement(d.x, d.z).x;
    return worldPos;
}

// Tilts world-space normal to match the curved surface. Inverse-transpose Jacobian
// of ApplyRollingLog: since only world-Y is displaced, only n.x and n.z get perturbed
// by -(d drop / d{x,z}) * n.y. Pass the pre-displacement worldPos.
float3 ApplyRollingLogNormal(float3 worldNormal, float3 worldPos)
{
    float3 d = worldPos - GetRollingLogAnchor();
    float3 disp = RollingLogDisplacement(d.x, d.z);
    worldNormal.x -= disp.y * worldNormal.y;
    worldNormal.z -= disp.z * worldNormal.y;
    return normalize(worldNormal);
}

#endif
