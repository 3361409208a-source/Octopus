using System;
using System.Drawing;

namespace CockroachPet;

public static class PixelRobotRenderer
{
    private const int PIXEL_SIZE = 4;

    public static void DrawRobot(Graphics g, Robot robot)
    {
        float scale = robot.Size / 64.0f; // 缩放比例
        float x = robot.X + robot.ShakingOffset;
        float y = robot.Y;
        int size = robot.Size;
        bool facingRight = robot.FacingRight;
        
        // 记录世界坐标中心，用于绘制不受翻转影响的效果
        float worldCenterX = x + size / 2;
        float worldCenterY = y + size / 2;

        // 1. 绘制相对于机器人的 UI 元素（始终正向显示）
        DrawName(g, robot, x, y);
        DrawHealthBar(g, robot, x, y);
        DrawAlertBubble(g, robot, x, y);
        DrawEmojiBubble(g, robot, x, y);
        DrawChatBubble(g, robot, x, y);
        DrawThinkingIndicator(g, robot, x, y);
        DrawDamageText(g, robot, x, y);

        // 2. 绘制机器人本体（包含翻转和旋转动画）
        var state = g.Save();

        if (!facingRight)
        {
            g.TranslateTransform(x + size, y);
            g.ScaleTransform(-1, 1);
            x = 0;
            y = 0;
        }

        // 扔出去时的旋转
        if (robot.RotationAngle != 0)
        {
            g.TranslateTransform(x + size / 2, y + size / 2);
            g.RotateTransform(robot.RotationAngle);
            g.TranslateTransform(-(x + size / 2), -(y + size / 2));
        }

        float centerX = x + size / 2;
        float centerY = y + size / 2;

        DrawTentacles(g, robot, centerX, centerY, scale);
        DrawBody(g, robot, centerX, centerY, scale);
        
        // 眼睛和天线单独处理旋转，确保围绕中心转
        if (robot.SpecialState == "SPINNING")
        {
            // 补偿翻转造成的旋转轴偏移
            float rotAngle = facingRight ? robot.RotationAngle : -robot.RotationAngle;
            
            // 围绕局部中心旋转
            var m = g.Transform;
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(rotAngle);
            g.TranslateTransform(-centerX, -centerY);
            
            DrawEyes(g, robot, centerX, centerY, scale);
            DrawAntennas(g, robot, centerX, centerY, scale);
            
            g.Transform = m;
        }
        else
        {
            DrawEyes(g, robot, centerX, centerY, scale);
            DrawAntennas(g, robot, centerX, centerY, scale);
        }
        
        if (robot.SpecialState == "BLUSH")
        {
            DrawBlush(g, robot, centerX, centerY);
        }

        // DrawEyes(g, robot, centerX, centerY); // This line is removed as it's now handled in the if/else above
        // DrawAntenna(g, robot, centerX, centerY); // This line is removed as it's now handled in the if/else above

        g.Restore(state);
        
        // 远程攻击效果 - 多样化增强 (不受翻转影响)
        if (robot.IsFiringLaser)
        {
            var r = new Random();
            Color attackColor = robot.PrimaryColor;
            
            switch (robot.CurrentAttackType)
            {
                case "SHOCK": // 电能震撼 - 单根强力闪电
                    using (var shockPen = new Pen(Color.Cyan, 4))
                    using (var whitePen = new Pen(Color.White, 1))
                    {
                        // 减少线条数量，强调主干
                        DrawElectricArc(g, r, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY, shockPen, whitePen);
                        g.DrawEllipse(new Pen(Color.White, 2), robot.LaserTargetX - 15, robot.LaserTargetY - 15, 30, 30);
                    }
                    break;

                case "INK_BLAST": // 墨汁弹 - 保持现状，属于块状攻击
                    using (var inkBrush = new SolidBrush(Color.FromArgb(230, 10, 10, 10)))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float t = (float)(r.NextDouble()); 
                            float px = worldCenterX + (robot.LaserTargetX - worldCenterX) * t;
                            float py = worldCenterY + (robot.LaserTargetY - worldCenterY) * t;
                            float jitter = (1 - t) * 15;
                            float pSize = 8 + (1-t) * 12;
                            g.FillEllipse(inkBrush, px - pSize/2 + r.Next(-(int)jitter, (int)jitter), 
                                                 py - pSize/2 + r.Next(-(int)jitter, (int)jitter), pSize, pSize);
                        }
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = (float)(r.NextDouble() * Math.PI * 2);
                            float d = (float)(r.NextDouble() * 30);
                            g.FillEllipse(inkBrush, robot.LaserTargetX + (float)Math.Cos(ang)*d - 4, 
                                                 robot.LaserTargetY + (float)Math.Sin(ang)*d - 4, 8, 8);
                        }
                    }
                    break;

                case "BURST": // 像素爆发 - 减少线条数，增加厚度
                    using (var burstBrush = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                    {
                        for (int i = 0; i < 6; i++) // 减少到 6 根
                        {
                            float angleOff = (float)(r.NextDouble() - 0.5) * 0.5f;
                            float baseAngle = (float)Math.Atan2(robot.LaserTargetY - worldCenterY, robot.LaserTargetX - worldCenterX);
                            float pDist = (float)Math.Sqrt(Math.Pow(robot.LaserTargetX - worldCenterX, 2) + Math.Pow(robot.LaserTargetY - worldCenterY, 2));
                            float tx = worldCenterX + (float)Math.Cos(baseAngle + angleOff) * pDist;
                            float ty = worldCenterY + (float)Math.Sin(baseAngle + angleOff) * pDist;
                            DrawPixelLine(g, burstBrush, worldCenterX, worldCenterY, tx, ty, 6); // 变粗
                        }
                    }
                    break;

                default: // LASER - 这一块改为单线条平滑激光，解决“线条太多”的问题
                    using (var glowPen = new Pen(Color.FromArgb(150, robot.PrimaryColor), 12)) 
                    using (var corePen = new Pen(Color.White, 4))
                    {
                        // 使用 GDI+ 自带的 DrawLine 以获得平滑感，减少重复线
                        g.DrawLine(glowPen, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY);
                        g.DrawLine(corePen, worldCenterX, worldCenterY, robot.LaserTargetX, robot.LaserTargetY);
                        
                        g.FillEllipse(Brushes.White, worldCenterX - 10, worldCenterY - 10, 20, 20);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(200, robot.PrimaryColor)), robot.LaserTargetX - 12, robot.LaserTargetY - 12, 24, 24);
                    }
                    break;
            }
        }

        // 4. 格斗碰撞特效 (星型冲击与闪烁)
        if (robot.SpecialState == "SHAKING" && robot.DuelTimer > 0)
        {
            var r = new Random();
            using (var impactBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
            using (var flashBrush = new SolidBrush(Color.FromArgb(100, Color.Yellow)))
            {
                // 冲击星
                PointF[] points = new PointF[10];
                float radiusOuter = 25 + r.Next(15);
                float radiusInner = 10;
                for (int i = 0; i < 10; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / 10);
                    float rad = (i % 2 == 0) ? radiusOuter : radiusInner;
                    points[i] = new PointF(worldCenterX + (float)Math.Cos(angle) * rad, worldCenterY + (float)Math.Sin(angle) * rad);
                }
                g.FillPolygon(impactBrush, points);
                g.FillEllipse(flashBrush, worldCenterX - radiusOuter, worldCenterY - radiusOuter, radiusOuter * 2, radiusOuter * 2);
            }
        }

        // 5. 物理互动视觉 (触手抓住)
        if (robot.PhysicalAction != "NONE" && robot.PhysicalTarget != null)
        {
            DrawPhysicalInteraction(g, robot, worldCenterX, worldCenterY);
        }
    }

    private static void DrawPhysicalInteraction(Graphics g, Robot robot, float cx, float cy)
    {
        var target = robot.PhysicalTarget;
        if (target == null) return;

        float tx = target.X + target.Size / 2;
        float ty = target.Y + target.Size / 2;
        
        using (var armPen = new Pen(robot.PrimaryColor, 10))
        using (var glowPen = new Pen(Color.FromArgb(120, robot.PrimaryColor), 18))
        using (var suckerBrush = new SolidBrush(Color.FromArgb(220, Color.White)))
        {
            armPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            armPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            armPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            
            var r = new Random();
            float dist = (float)Math.Sqrt(Math.Pow(tx - cx, 2) + Math.Pow(ty - cy, 2));
            float angle = (float)Math.Atan2(ty - cy, tx - cx);

            for (int j = 0; j < 2; j++)
            {
                float sideAngle = angle + (j == 0 ? -0.5f : 0.5f);
                float startX = cx + (float)Math.Cos(sideAngle) * 12;
                float startY = cy + (float)Math.Sin(sideAngle) * 12;

                PointF[] pts = new PointF[4];
                pts[0] = new PointF(startX, startY);
                
                float wave = (float)Math.Sin(DateTime.Now.Millisecond * 0.01 + j) * 35;
                float midX = cx + (tx - cx) * 0.5f + (float)Math.Cos(angle + Math.PI/2) * wave;
                float midY = cy + (ty - cy) * 0.5f + (float)Math.Sin(angle + Math.PI/2) * wave;
                
                pts[1] = new PointF(midX, midY);
                pts[2] = new PointF(tx + (float)r.Next(-15, 15), ty + (float)r.Next(-15, 15));
                pts[3] = new PointF(tx, ty);

                g.DrawCurve(glowPen, pts);
                g.DrawCurve(armPen, pts);

                // 吸盘
                for (int i = 1; i < pts.Length - 1; i++)
                {
                    g.FillEllipse(suckerBrush, pts[i].X - 5, pts[i].Y - 5, 10, 10);
                }
                
                g.FillEllipse(new SolidBrush(robot.PrimaryColor), tx-8, ty-8, 16, 16);
            }
        }
    }

    private static void DrawElectricArc(Graphics g, Random r, float x1, float y1, float x2, float y2, Pen mainPen, Pen corePen)
    {
        float curX = x1;
        float curY = y1;
        int segments = 8;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float jitter = r.Next(-25, 25) * (1-t/2);
            float nextX = x1 + (x2 - x1) * t + jitter;
            float nextY = y1 + (y2 - y1) * t + jitter;
            if (i == segments) { nextX = x2; nextY = y2; }

            g.DrawLine(mainPen, curX, curY, nextX, nextY);
            g.DrawLine(corePen, curX, curY, nextX, nextY);
            
            if (r.Next(100) < 40) g.FillRectangle(Brushes.White, nextX - 3, nextY - 3, 6, 6);

            curX = nextX;
            curY = nextY;
        }
    }

    private static void DrawChatBubble(Graphics g, Robot robot, float rx, float ry)
    {
        if (robot.ChatTimer <= 0 || string.IsNullOrEmpty(robot.ChatText)) return;

        using var font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
        float maxWidth = 150; // 气泡最大宽度
        
        // 测量带换行限制的尺寸
        var rawSize = g.MeasureString(robot.ChatText, font, (int)maxWidth);
        
        float bx = rx + robot.Size / 2;
        float by = ry - rawSize.Height - 30; // 根据文字高度动态调整位置
        
        RectangleF bubbleRect = new RectangleF(bx - rawSize.Width / 2 - 10, by - rawSize.Height / 2 - 5, rawSize.Width + 20, rawSize.Height + 10);
        
        // 绘制气泡背景
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillRoundedRectangle(shadowBrush, bubbleRect.X + 2, bubbleRect.Y + 2, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var bgBrush = new SolidBrush(Color.White);
        g.FillRoundedRectangle(bgBrush, bubbleRect.X, bubbleRect.Y, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1);
        g.DrawRoundedRectangle(borderPen, bubbleRect.X, bubbleRect.Y, bubbleRect.Width, bubbleRect.Height, 8);
        
        using var textBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        // 使用 StringFormat 处理自动换行
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(robot.ChatText, font, textBrush, bubbleRect, format);
        
        // 小尾巴
        PointF[] tail = {
            new PointF(bx - 5, bubbleRect.Bottom),
            new PointF(bx + 5, bubbleRect.Bottom),
            new PointF(bx, bubbleRect.Bottom + 6)
        };
        g.FillPolygon(Brushes.White, tail);
    }

    private static void DrawThinkingIndicator(Graphics g, Robot robot, float rx, float ry)
    {
        if (!robot.IsThinking) return;

        float bx = rx + robot.Size / 2 + 15;
        float by = ry + 10;
        
        int pulse = (int)(DateTime.Now.Millisecond / 333) % 3;
        using var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        
        for (int i = 0; i <= pulse; i++)
        {
            g.FillRectangle(brush, bx + i * 6, by, 3, 3);
        }
    }

    // 辅助方法：绘制圆角矩形
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, float x, float y, float width, float height, float radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseAllFigures();
        return path;
    }

    private static void DrawEmojiBubble(Graphics g, Robot robot, float rx, float ry)
    {
        if (robot.EmojiBubbleTimer <= 0) return;

        float bx = rx + robot.Size - 10;
        float by = ry - 20;

        using var font = new Font("Segoe UI Emoji", 14);
        g.DrawString(robot.CurrentEmoji, font, Brushes.White, bx, by);
    }

    private static void DrawBlush(Graphics g, Robot robot, float cx, float cy)
    {
        using var blushBrush = new SolidBrush(Color.FromArgb(150, 255, 182, 193));
        g.FillEllipse(blushBrush, cx - 15, cy, 10, 6);
        g.FillEllipse(blushBrush, cx + 5, cy, 10, 6);
    }

    private static void DrawAlertBubble(Graphics g, Robot robot, float rx, float ry)
    {
        if (!robot.IsWarning || string.IsNullOrEmpty(robot.AlertMessage)) return;

        // 浮动动画
        float floatOffset = (float)Math.Sin(robot.WarningTimer * 0.1) * 5;
        float bx = rx + robot.Size / 2;
        float by = ry - 40 + floatOffset;

        using var font = new Font("Consolas", 9, FontStyle.Bold);
        var size = g.MeasureString(robot.AlertMessage, font);
        
        // 气泡背景 (带圆角和阴影)
        RectangleF bubbleRect = new RectangleF(bx - size.Width / 2 - 8, by - size.Height / 2 - 4, size.Width + 16, size.Height + 8);
        
        using var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillRectangle(shadowBrush, bubbleRect.X + 3, bubbleRect.Y + 3, bubbleRect.Width, bubbleRect.Height);

        // 颜色根据状态变化：Claude 确认显示黄色，错误显示红色
        Color bubbleColor = robot.StatusMessage == "WAITING" ? Color.Gold : Color.Red;
        using var bubbleBrush = new SolidBrush(bubbleColor);
        g.FillRectangle(bubbleBrush, bubbleRect);

        using var textBrush = new SolidBrush(Color.Black);
        g.DrawString(robot.AlertMessage, font, textBrush, bx, by, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

        // 连接宠物的小箭头
        PointF[] arrow = {
            new PointF(bx - 5, bubbleRect.Bottom),
            new PointF(bx + 5, bubbleRect.Bottom),
            new PointF(bx, bubbleRect.Bottom + 8)
        };
        g.FillPolygon(bubbleBrush, arrow);
    }

    private static void DrawTentacles(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        Color tColor = robot.IsDead ? Color.FromArgb(100, 100, 100) : robot.SecondaryColor;
        using var tentacleBrush = new SolidBrush(tColor);

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI / 4 + robot.TentacleOffsets[i] * 0.1);
            float wave = (float)Math.Sin(robot.TentacleOffsets[i] + i) * 5 * scale;

            float startX = cx + (float)Math.Cos(angle) * 15 * scale;
            float startY = cy + (float)Math.Sin(angle) * 15 * scale;

            float length = (20 + wave) * scale;
            float endX = startX + (float)Math.Cos(angle) * length;
            float endY = startY + (float)Math.Sin(angle) * length;

            DrawPixelLine(g, tentacleBrush, startX, startY, endX, endY, (int)Math.Max(1, 3 * scale));
            g.FillRectangle(tentacleBrush, endX - 2 * scale, endY - 2 * scale, 4 * scale, 4 * scale);
        }
    }

    private static void DrawBody(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        Color pColor = robot.IsDead ? Color.FromArgb(130, 130, 130) : robot.PrimaryColor;
        Color sColor = robot.IsDead ? Color.FromArgb(90, 90, 90) : robot.SecondaryColor;
        using var bodyBrush = new SolidBrush(pColor);
        using var bodyDarkBrush = new SolidBrush(sColor);

        float pSize = PIXEL_SIZE * scale;

        for (int dx = -12; dx <= 12; dx++)
        {
            for (int dy = -12; dy <= 12; dy++)
            {
                if (dx * dx + dy * dy <= 144)
                {
                    float px = cx + dx * pSize / 2;
                    float py = cy + dy * pSize / 2;

                    var brush = (dx * dx + dy * dy > 100) ? bodyDarkBrush : bodyBrush;
                    
                    // 受击红闪覆盖
                    if (robot.DamageFeedbackTimer > 0)
                    {
                        int alpha = Math.Min(255, robot.DamageFeedbackTimer * 4);
                        using var hitBrush = new SolidBrush(Color.FromArgb(alpha, Color.Red));
                        g.FillRectangle(brush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                        g.FillRectangle(hitBrush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                    }
                    else
                    {
                        g.FillRectangle(brush, px - pSize / 2, py - pSize / 2, pSize, pSize);
                    }
                }
            }
        }

        using var coreBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        float corePulse = 1 + (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 0.2f;
        float coreSize = 6 * corePulse * scale;
        g.FillRectangle(coreBrush, cx - coreSize / 2, cy - coreSize / 2 + 5 * scale, coreSize, coreSize);
    }

    private static void DrawEyes(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        float eyeY = cy - 5 * scale;
        float leftEyeX = cx - 8 * scale;
        float rightEyeX = cx + 8 * scale;

        int blinkFrame = robot.AnimationFrame;
        bool isBlinking = blinkFrame == 2 || robot.SpecialState == "SLEEPY";
        
        if (robot.SpecialState == "ANGRY")
        {
            DrawAngryEyes(g, robot, cx, cy, scale);
            return;
        }

        float eyeHeight = (isBlinking ? 2 : 8) * scale;
        float eyeWidth = 10 * scale;

        using var eyeWhiteBrush = new SolidBrush(Color.White);
        using var eyeBrush = new SolidBrush(robot.EyeColor);
        using var pupilBrush = new SolidBrush(Color.Black);
        using var heartBrush = new SolidBrush(Color.HotPink);

        // 左眼
        DrawPixelEllipse(g, eyeWhiteBrush, leftEyeX, eyeY, eyeWidth, eyeHeight);
        if (!isBlinking && !robot.IsDead)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, leftEyeX, eyeY, 8 * scale);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, leftEyeX + 1 * scale, eyeY, 6 * scale, 6 * scale);
                g.FillRectangle(pupilBrush, leftEyeX + 1 * scale, eyeY - 1 * scale, 2 * scale, 4 * scale);
            }
        }

        // 右眼
        DrawPixelEllipse(g, eyeWhiteBrush, rightEyeX, eyeY, eyeWidth, eyeHeight);
        if (!isBlinking && !robot.IsDead)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, rightEyeX, eyeY, 8 * scale);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, rightEyeX + 1 * scale, eyeY, 6 * scale, 6 * scale);
                g.FillRectangle(pupilBrush, rightEyeX + 1 * scale, eyeY - 1 * scale, 2 * scale, 4 * scale);
            }
        }
    }

    private static void DrawHeart(Graphics g, Brush brush, float x, float y, float size)
    {
        float s = size / 2;
        PointF[] points = {
            new PointF(x, y + s/2),
            new PointF(x - s, y - s/2),
            new PointF(x - s/2, y - s),
            new PointF(x, y - s/2),
            new PointF(x + s/2, y - s),
            new PointF(x + s, y - s/2)
        };
        g.FillPolygon(brush, points);
    }

    private static void DrawAngryEyes(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        using var eyeBrush = new SolidBrush(Color.Red);
        using var pen = new Pen(eyeBrush, 3 * scale);
        
        // 愤怒的 V 型眼
        g.DrawLine(pen, cx - 12 * scale, cy - 10 * scale, cx - 4 * scale, cy - 4 * scale);
        g.DrawLine(pen, cx - 12 * scale, cy - 4 * scale, cx - 4 * scale, cy - 10 * scale); // 左眼 X
        
        g.DrawLine(pen, cx + 4 * scale, cy - 10 * scale, cx + 12 * scale, cy - 4 * scale);
        g.DrawLine(pen, cx + 4 * scale, cy - 4 * scale, cx + 12 * scale, cy - 10 * scale); // 右眼 X
    }

    private static void DrawAntennas(Graphics g, Robot robot, float cx, float cy, float scale)
    {
        using var antennaBrush = new SolidBrush(robot.SecondaryColor);
        
        // 如果处于警告状态，天线末端闪烁
        Color tipColor = Color.FromArgb(255, 255, 100, 100);
        if (robot.IsWarning && (robot.WarningTimer / 10) % 2 == 0)
        {
            tipColor = Color.Yellow;
        }
        using var tipBrush = new SolidBrush(tipColor);

        float wave = (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 3 * scale;

        DrawPixelLine(g, antennaBrush, cx - 8 * scale, cy - 15 * scale, cx - 12 * scale + wave, cy - 28 * scale, (int)Math.Max(1, 2 * scale));
        g.FillRectangle(tipBrush, cx - 13 * scale + wave, cy - 30 * scale, 4 * scale, 4 * scale);

        DrawPixelLine(g, antennaBrush, cx + 8 * scale, cy - 15 * scale, cx + 12 * scale + wave, cy - 28 * scale, (int)Math.Max(1, 2 * scale));
        g.FillRectangle(tipBrush, cx + 11 * scale + wave, cy - 30 * scale, 4 * scale, 4 * scale);
    }

    private static void DrawName(Graphics g, Robot robot, float rx, float ry)
    {
        if (string.IsNullOrEmpty(robot.Name)) return;

        using var font = new Font("Consolas", 8, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        using var shadowBrush = new SolidBrush(Color.Black);

        float textX = rx + robot.Size / 2;
        float textY = ry - 15;

        g.DrawString(robot.Name, font, shadowBrush, textX + 1, textY + 1,
            new StringFormat { Alignment = StringAlignment.Center });
        g.DrawString(robot.Name, font, brush, textX, textY,
            new StringFormat { Alignment = StringAlignment.Center });
    }

    private static void DrawHealthBar(Graphics g, Robot robot, float rx, float ry)
    {
        if (robot.IsDead) return;
        float barWidth = robot.Size * 0.8f;
        float barHeight = 4;
        float bx = rx + (robot.Size - barWidth) / 2;
        float by = ry - 8;

        // 背景
        g.FillRectangle(Brushes.Gray, bx, by, barWidth, barHeight);
        
        // 血条
        float hpPercent = (float)robot.HP / robot.MaxHP;
        Color hpColor = hpPercent > 0.5 ? Color.Lime : (hpPercent > 0.2 ? Color.Yellow : Color.Red);
        using var hpBrush = new SolidBrush(hpColor);
        g.FillRectangle(hpBrush, bx, by, barWidth * hpPercent, barHeight);
        
        // 边框
        g.DrawRectangle(Pens.Black, bx, by, barWidth, barHeight);
    }

    private static void DrawDamageText(Graphics g, Robot robot, float rx, float ry)
    {
        if (robot.DamageTextTimer <= 0) return;

        float alpha = Math.Min(255, robot.DamageTextTimer * 5);
        using var font = new Font("Impact", 14, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb((int)alpha, Color.OrangeRed));
        
        float floatOffset = (45 - robot.DamageTextTimer) * 1.5f;
        float tx = rx + robot.Size / 2;
        float ty = ry - 20 - floatOffset;

        g.DrawString(robot.LastDamageText, font, Brushes.Black, tx + 1, ty + 1, new StringFormat { Alignment = StringAlignment.Center });
        g.DrawString(robot.LastDamageText, font, brush, tx, ty, new StringFormat { Alignment = StringAlignment.Center });
    }

    private static void DrawPixelLine(Graphics g, Brush brush, float x1, float y1, float x2, float y2, int thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / steps;
            float x = x1 + dx * t;
            float y = y1 + dy * t;
            g.FillRectangle(brush, x - thickness / 2, y - thickness / 2, thickness, thickness);
        }
    }

    private static void DrawPixelEllipse(Graphics g, Brush brush, float cx, float cy, float w, float h)
    {
        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            float angle = (float)(i * 2 * Math.PI / steps);
            float x = cx + (float)Math.Cos(angle) * w / 2;
            float y = cy + (float)Math.Sin(angle) * h / 2;
            g.FillRectangle(brush, x - 2, y - 2, 4, 4);
        }
    }
}
