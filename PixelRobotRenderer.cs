using System;
using System.Drawing;

namespace CockroachPet;

public static class PixelRobotRenderer
{
    private const int PIXEL_SIZE = 4;

    public static void DrawRobot(Graphics g, Robot robot)
    {
        float x = robot.X;
        float y = robot.Y;
        int size = robot.Size;
        bool facingRight = robot.FacingRight;

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
        DrawEyes(g, robot, centerX, centerY);
        DrawAntenna(g, robot, centerX, centerY);
        DrawName(g, robot);

        g.Restore(state);
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
        bool isBlinking = blinkFrame == 2;
        float eyeHeight = isBlinking ? 2 : 8;

        using var eyeWhiteBrush = new SolidBrush(Color.White);
        using var eyeBrush = new SolidBrush(robot.EyeColor);
        using var pupilBrush = new SolidBrush(Color.Black);

        DrawPixelEllipse(g, eyeWhiteBrush, leftEyeX, eyeY, 10, eyeHeight);
        if (!isBlinking)
        {
            DrawPixelEllipse(g, eyeBrush, leftEyeX + 1, eyeY, 6, 6);
            g.FillRectangle(pupilBrush, leftEyeX + 1, eyeY - 1, 2, 4);
        }

        DrawPixelEllipse(g, eyeWhiteBrush, rightEyeX, eyeY, 10, eyeHeight);
        if (!isBlinking)
        {
            DrawPixelEllipse(g, eyeBrush, rightEyeX + 1, eyeY, 6, 6);
            g.FillRectangle(pupilBrush, rightEyeX + 1, eyeY - 1, 2, 4);
        }
    }

    private static void DrawAntenna(Graphics g, Robot robot, float cx, float cy)
    {
        using var antennaBrush = new SolidBrush(robot.SecondaryColor);
        using var tipBrush = new SolidBrush(Color.FromArgb(255, 255, 100, 100));

        float wave = (float)Math.Sin(robot.AnimationFrame * Math.PI / 2) * 3;

        DrawPixelLine(g, antennaBrush, cx - 8, cy - 15, cx - 12 + wave, cy - 28, 2);
        g.FillRectangle(tipBrush, cx - 13 + wave, cy - 30, 4, 4);

        DrawPixelLine(g, antennaBrush, cx + 8, cy - 15, cx + 12 + wave, cy - 28, 2);
        g.FillRectangle(tipBrush, cx + 11 + wave, cy - 30, 4, 4);
    }

    private static void DrawName(Graphics g, Robot robot)
    {
        if (string.IsNullOrEmpty(robot.Name)) return;

        using var font = new Font("Consolas", 8, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        using var shadowBrush = new SolidBrush(Color.Black);

        float textX = robot.X + robot.Size / 2;
        float textY = robot.Y - 15;

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
