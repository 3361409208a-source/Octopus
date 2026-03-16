using System;
using System.Drawing;

namespace CockroachPet;

public static class PixelRobotRenderer
{
    private const int PIXEL_SIZE = 4;

    public static void DrawRobot(Graphics g, Robot robot)
    {
        float x = robot.X + robot.ShakingOffset;
        float y = robot.Y;
        int size = robot.Size;
        bool facingRight = robot.FacingRight;

        // 1. 绘制相对于机器人的 UI 元素（始终正向显示）
        DrawName(g, robot, x, y);
        DrawAlertBubble(g, robot, x, y);
        DrawEmojiBubble(g, robot, x, y);
        DrawChatBubble(g, robot, x, y);
        DrawThinkingIndicator(g, robot, x, y);

        // 2. 绘制机器人本体（包含翻转和旋转动画）
        var state = g.Save();

        if (!facingRight)
        {
            g.TranslateTransform(x + size, y);
            g.ScaleTransform(-1, 1);
            x = 0;
            y = 0;
        }

        float centerX = x + size / 2;
        float centerY = y + size / 2;

        DrawTentacles(g, robot, centerX, centerY);
        DrawBody(g, robot, centerX, centerY);
        
        // 眼睛和天线单独处理旋转，确保围绕中心转
        if (robot.SpecialState == "SPINNING")
        {
            // 补偿翻转造成的旋转轴偏移
            float rotAngle = facingRight ? robot.RotationAngle : -robot.RotationAngle;
            
            // 围绕局部中心旋转
            var m = g.Transform;
            m.RotateAt(rotAngle, new PointF(centerX, centerY));
            g.Transform = m;
        }
        
        if (robot.SpecialState == "BLUSH")
        {
            DrawBlush(g, robot, centerX, centerY);
        }

        DrawEyes(g, robot, centerX, centerY);
        DrawAntenna(g, robot, centerX, centerY);

        g.Restore(state);
        
        // 远程攻击效果 - 多样化增强 (不受翻转影响)
        if (robot.IsFiringLaser)
        {
            var r = new Random();
            Color attackColor = robot.PrimaryColor;
            
            switch (robot.CurrentAttackType)
            {
                case "SHOCK": // 电能震撼 - 锯齿状闪电
                    using (var shockPen = new Pen(Color.FromArgb(200, Color.Cyan), 2))
                    {
                        float midX = (centerX + robot.LaserTargetX) / 2;
                        float midY = (centerY + robot.LaserTargetY) / 2;
                        float offsetX = (float)(r.NextDouble() - 0.5) * 40;
                        float offsetY = (float)(r.NextDouble() - 0.5) * 40;
                        
                        g.DrawLine(shockPen, centerX, centerY, midX + offsetX, midY + offsetY);
                        g.DrawLine(shockPen, midX + offsetX, midY + offsetY, robot.LaserTargetX, robot.LaserTargetY);
                        
                        // 目标溅射
                        g.FillRectangle(Brushes.Cyan, robot.LaserTargetX - 8, robot.LaserTargetY - 8, 16, 16);
                    }
                    break;

                case "BURST": // 像素爆发 - 多重极速粒子
                    using (var burstBrush = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float pOffX = (float)(r.NextDouble() - 0.5) * 30;
                            float pOffY = (float)(r.NextDouble() - 0.5) * 30;
                            DrawPixelLine(g, burstBrush, centerX, centerY, robot.LaserTargetX + pOffX, robot.LaserTargetY + pOffY, 3);
                        }
                    }
                    break;

                default: // LASER - 强化脉冲激光
                    using (var coreBrush = new SolidBrush(Color.White))
                    using (var glowBrush = new SolidBrush(Color.FromArgb(150, attackColor)))
                    {
                        // 粗外层发光
                        DrawPixelLine(g, glowBrush, centerX, centerY, robot.LaserTargetX, robot.LaserTargetY, 8);
                        // 细核心白色
                        DrawPixelLine(g, coreBrush, centerX, centerY, robot.LaserTargetX, robot.LaserTargetY, 2);
                        
                        // 起点闪烁
                        g.FillEllipse(Brushes.White, centerX - 10, centerY - 10, 20, 20);
                        // 终点冲击波
                        g.DrawEllipse(new Pen(attackColor, 3), robot.LaserTargetX - 15, robot.LaserTargetY - 15, 30, 30);
                    }
                    break;
            }
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

    private static void DrawTentacles(Graphics g, Robot robot, float cx, float cy)
    {
        using var tentacleBrush = new SolidBrush(robot.SecondaryColor);

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI / 4 + robot.TentacleOffsets[i] * 0.1);
            float wave = (float)Math.Sin(robot.TentacleOffsets[i] + i) * 5;

            float startX = cx + (float)Math.Cos(angle) * 15;
            float startY = cy + (float)Math.Sin(angle) * 15;

            float length = 20 + wave;
            float endX = startX + (float)Math.Cos(angle) * length;
            float endY = startY + (float)Math.Sin(angle) * length;

            DrawPixelLine(g, tentacleBrush, startX, startY, endX, endY, 3);
            g.FillRectangle(tentacleBrush, endX - 2, endY - 2, 4, 4);
        }
    }

    private static void DrawBody(Graphics g, Robot robot, float cx, float cy)
    {
        using var bodyBrush = new SolidBrush(robot.PrimaryColor);
        using var bodyDarkBrush = new SolidBrush(robot.SecondaryColor);

        for (int dx = -12; dx <= 12; dx++)
        {
            for (int dy = -12; dy <= 12; dy++)
            {
                if (dx * dx + dy * dy <= 144)
                {
                    float px = cx + dx * PIXEL_SIZE / 2;
                    float py = cy + dy * PIXEL_SIZE / 2;

                    var brush = (dx * dx + dy * dy > 100) ? bodyDarkBrush : bodyBrush;
                    g.FillRectangle(brush, px - PIXEL_SIZE / 2, py - PIXEL_SIZE / 2, PIXEL_SIZE, PIXEL_SIZE);
                }
            }
        }

        using var coreBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        float corePulse = 1 + (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 0.2f;
        float coreSize = 6 * corePulse;
        g.FillRectangle(coreBrush, cx - coreSize / 2, cy - coreSize / 2 + 5, coreSize, coreSize);
    }

    private static void DrawEyes(Graphics g, Robot robot, float cx, float cy)
    {
        float eyeY = cy - 5;
        float leftEyeX = cx - 8;
        float rightEyeX = cx + 8;

        int blinkFrame = robot.AnimationFrame;
        bool isBlinking = blinkFrame == 2 || robot.SpecialState == "SLEEPY";
        
        if (robot.SpecialState == "ANGRY")
        {
            DrawAngryEyes(g, robot, cx, cy);
            return;
        }

        float eyeHeight = isBlinking ? 2 : 8;

        using var eyeWhiteBrush = new SolidBrush(Color.White);
        using var eyeBrush = new SolidBrush(robot.EyeColor);
        using var pupilBrush = new SolidBrush(Color.Black);
        using var heartBrush = new SolidBrush(Color.HotPink);

        // 左眼
        DrawPixelEllipse(g, eyeWhiteBrush, leftEyeX, eyeY, 10, eyeHeight);
        if (!isBlinking)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, leftEyeX, eyeY, 8);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, leftEyeX + 1, eyeY, 6, 6);
                g.FillRectangle(pupilBrush, leftEyeX + 1, eyeY - 1, 2, 4);
            }
        }

        // 右眼
        DrawPixelEllipse(g, eyeWhiteBrush, rightEyeX, eyeY, 10, eyeHeight);
        if (!isBlinking)
        {
            if (robot.SpecialState == "HEART_EYES")
            {
                DrawHeart(g, heartBrush, rightEyeX, eyeY, 8);
            }
            else
            {
                DrawPixelEllipse(g, eyeBrush, rightEyeX + 1, eyeY, 6, 6);
                g.FillRectangle(pupilBrush, rightEyeX + 1, eyeY - 1, 2, 4);
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

    private static void DrawAngryEyes(Graphics g, Robot robot, float cx, float cy)
    {
        using var eyeBrush = new SolidBrush(Color.Red);
        using var pen = new Pen(eyeBrush, 3);
        
        // 愤怒的 V 型眼
        g.DrawLine(pen, cx - 12, cy - 10, cx - 4, cy - 4);
        g.DrawLine(pen, cx - 12, cy - 4, cx - 4, cy - 10); // 左眼 X
        
        g.DrawLine(pen, cx + 4, cy - 10, cx + 12, cy - 4);
        g.DrawLine(pen, cx + 4, cy - 4, cx + 12, cy - 10); // 右眼 X
    }

    private static void DrawAntenna(Graphics g, Robot robot, float cx, float cy)
    {
        using var antennaBrush = new SolidBrush(robot.SecondaryColor);
        
        // 如果处于警告状态，天线末端闪烁
        Color tipColor = Color.FromArgb(255, 255, 100, 100);
        if (robot.IsWarning && (robot.WarningTimer / 10) % 2 == 0)
        {
            tipColor = Color.Yellow;
        }
        using var tipBrush = new SolidBrush(tipColor);

        float wave = (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 3;

        DrawPixelLine(g, antennaBrush, cx - 8, cy - 15, cx - 12 + wave, cy - 28, 2);
        g.FillRectangle(tipBrush, cx - 13 + wave, cy - 30, 4, 4);

        DrawPixelLine(g, antennaBrush, cx + 8, cy - 15, cx + 12 + wave, cy - 28, 2);
        g.FillRectangle(tipBrush, cx + 11 + wave, cy - 30, 4, 4);
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
