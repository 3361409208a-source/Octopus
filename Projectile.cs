using System.Collections.Generic;
using System.Drawing;

namespace CockroachPet;

public class Projectile
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Dx { get; set; }
    public float Dy { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public Robot Owner { get; set; }
    public Color ProjectileColor { get; set; }
    public bool IsActive { get; set; } = true;
    public string Type { get; set; } // BULLET, ROCKET, PLASMA, CANNON, LIGHTNING, SPIT, INK
    public int LifeTime { get; set; }
    public Robot? TrackingTarget { get; set; } // 锁定目标：实时修正轨迹
    public List<PointF> Trail { get; set; } = new List<PointF>();

    public Projectile(Robot owner, float x, float y, float tx, float ty, string type = "BULLET", Robot? target = null)
    {
        Owner = owner;
        X = x;
        Y = y;
        TargetX = tx;
        TargetY = ty;
        Type = type;
        TrackingTarget = target;
        ProjectileColor = owner.PrimaryColor;

        float dx = tx - x;
        float dy = ty - y;
        float dist = (float)Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        
        float speed = GetBaseSpeed(type);

        LifeTime = type switch {
            "LIGHTNING" => 30,
            "CANNON" => 180,
            _ => 150
        };

        Dx = (dx / dist) * speed;
        Dy = (dy / dist) * speed;
    }

    private float GetBaseSpeed(string type)
    {
        return type switch {
            "ROCKET" => 12.0f,
            "CANNON" => 8.0f,
            "PLASMA" => 20.0f,
            "LIGHTNING" => 35.0f,
            "SPIT" => 15.0f,
            "INK" => 14.0f,
            _ => 25.0f
        };
    }

    public void Update()
    {
        // 锁定追踪逻辑 (目标死亡或子弹生命垂危时停止追踪)
        if (TrackingTarget != null && TrackingTarget.IsActive && !TrackingTarget.IsDead && LifeTime > 50)
        {
            float tx = TrackingTarget.X + TrackingTarget.Size / 2;
            float ty = TrackingTarget.Y + TrackingTarget.Size / 2;
            float curDx = tx - X;
            float curDy = ty - Y;
            float dist = (float)Math.Max(1, Math.Sqrt(curDx * curDx + curDy * curDy));
            
            float targetDx = (curDx / dist) * GetBaseSpeed(Type);
            float targetDy = (curDy / dist) * GetBaseSpeed(Type);

            // 平滑修正轨迹
            float lerp = (Type == "ROCKET" || Type == "PLASMA") ? 0.15f : 0.08f;
            Dx = Dx * (1 - lerp) + targetDx * lerp;
            Dy = Dy * (1 - lerp) + targetDy * lerp;
        }
        else
        {
            TrackingTarget = null; // 失去锁定
        }

        X += Dx;
        Y += Dy;

        // 炮弹模拟重力下坠
        if (Type == "CANNON") Dy += 0.15f;

        // 唾液/墨水 随机波动
        if (Type == "SPIT" || Type == "INK")
        {
            X += (float)(new Random().NextDouble() - 0.5) * 2;
            Y += (float)(new Random().NextDouble() - 0.5) * 2;
        }

        LifeTime--;
        if (LifeTime <= 0) IsActive = false;
    }
}
