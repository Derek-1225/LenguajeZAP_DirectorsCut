using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AnalizadorLexico_LenguajeZAP
{
    public static class GraphicsUtils
    {
        public static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);

            // Top-left arc
            path.AddArc(arc, 180, 90);

            // Top-right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Botón moderno con bordes redondeados, efectos hover y clic, y soporte de iconos
    /// </summary>
    public class ModernButton : Button
    {
        private int _borderRadius = 8;
        private int _borderSize = 1;
        private Color _borderColor = Color.FromArgb(51, 65, 85);
        private Color _hoverBackColor = Color.FromArgb(37, 99, 235);
        private Color _pressedBackColor = Color.FromArgb(29, 78, 216);
        private Color _defaultBackColor = Color.FromArgb(30, 41, 59);
        private bool _isHovered = false;
        private bool _isPressed = false;

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        public int BorderSize
        {
            get => _borderSize;
            set { _borderSize = Math.Max(0, value); Invalidate(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set { _hoverBackColor = value; Invalidate(); }
        }

        public Color PressedBackColor
        {
            get => _pressedBackColor;
            set { _pressedBackColor = value; Invalidate(); }
        }

        public Color CustomBackColor
        {
            get => _defaultBackColor;
            set { _defaultBackColor = value; BackColor = value; Invalidate(); }
        }

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = Color.White;
            BackColor = _defaultBackColor;
            Size = new Size(160, 40);
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rectSurface = ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);

            Color currentBg = _defaultBackColor;
            if (!Enabled)
            {
                currentBg = Color.FromArgb(71, 85, 105);
            }
            else if (_isPressed)
            {
                currentBg = _pressedBackColor;
            }
            else if (_isHovered)
            {
                currentBg = _hoverBackColor;
            }

            // Fill background
            if (_borderRadius > 2)
            {
                using (GraphicsPath pathSurface = GraphicsUtils.GetRoundedRectangle(rectSurface, _borderRadius))
                using (GraphicsPath pathBorder = GraphicsUtils.GetRoundedRectangle(rectBorder, _borderRadius - 1))
                using (SolidBrush brushBg = new SolidBrush(currentBg))
                using (Pen penBorder = new Pen(_borderColor, _borderSize))
                {
                    Region = new Region(pathSurface);
                    g.FillPath(brushBg, pathSurface);

                    if (_borderSize > 0 && Enabled)
                    {
                        g.DrawPath(penBorder, pathBorder);
                    }
                }
            }
            else
            {
                using (SolidBrush brushBg = new SolidBrush(currentBg))
                {
                    g.FillRectangle(brushBg, rectSurface);
                }
                if (_borderSize > 0 && Enabled)
                {
                    using (Pen penBorder = new Pen(_borderColor, _borderSize))
                    {
                        g.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
                    }
                }
            }

            // Draw text centered with crisp font
            Color txtColor = Enabled ? ForeColor : Color.FromArgb(148, 163, 184);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rectSurface,
                txtColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }
    }

    /// <summary>
    /// Panel contenedor con soporte para bordes redondeados y estilo tarjeta
    /// </summary>
    public class ModernCard : Panel
    {
        private int _borderRadius = 8;
        private int _borderSize = 1;
        private Color _borderColor = Color.FromArgb(51, 65, 85);

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        public int BorderSize
        {
            get => _borderSize;
            set { _borderSize = Math.Max(0, value); Invalidate(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public ModernCard()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 41, 59);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                if (_borderRadius > 0)
                {
                    using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(rect, _borderRadius))
                    {
                        g.FillPath(brush, path);
                        if (_borderSize > 0)
                        {
                            using (Pen pen = new Pen(_borderColor, _borderSize))
                            {
                                g.DrawPath(pen, path);
                            }
                        }
                    }
                }
                else
                {
                    g.FillRectangle(brush, rect);
                    if (_borderSize > 0)
                    {
                        using (Pen pen = new Pen(_borderColor, _borderSize))
                        {
                            g.DrawRectangle(pen, rect);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// TabControl moderno con estilo de pestañas planas, indicador activo y sin rebordes clásicos
    /// </summary>
    public class ModernTabControl : TabControl
    {
        private Color _headerBackColor = Color.FromArgb(15, 23, 42);
        private Color _activeTabColor = Color.FromArgb(30, 41, 59);
        private Color _inactiveTabColor = Color.FromArgb(15, 23, 42);
        private Color _activeTextColor = Color.FromArgb(56, 189, 248);
        private Color _inactiveTextColor = Color.FromArgb(148, 163, 184);
        private Color _indicatorColor = Color.FromArgb(56, 189, 248);
        private int _hoveredIndex = -1;

        public ModernTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(185, 44);
            DoubleBuffered = true;
            Font = new Font("Segoe UI Semibold", 9.5F);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHover = -1;
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(e.Location))
                {
                    newHover = i;
                    break;
                }
            }

            if (newHover != _hoveredIndex)
            {
                _hoveredIndex = newHover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Fill the header strip background
            Rectangle headerArea = new Rectangle(0, 0, Width, ItemSize.Height + 2);
            using (SolidBrush headerBrush = new SolidBrush(_headerBackColor))
            {
                g.FillRectangle(headerBrush, headerArea);
            }

            // Draw horizontal border under tabs
            using (Pen borderPen = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.DrawLine(borderPen, 0, ItemSize.Height + 1, Width, ItemSize.Height + 1);
            }

            // Draw each tab
            for (int i = 0; i < TabCount; i++)
            {
                Rectangle tabRect = GetTabRect(i);
                bool isSelected = (SelectedIndex == i);
                bool isHovered = (_hoveredIndex == i && !isSelected);

                Color tabBg = isSelected ? _activeTabColor : (isHovered ? Color.FromArgb(24, 34, 50) : _inactiveTabColor);
                Color txtColor = isSelected ? _activeTextColor : (isHovered ? Color.White : _inactiveTextColor);

                // Tab background
                using (SolidBrush tabBrush = new SolidBrush(tabBg))
                {
                    g.FillRectangle(tabBrush, tabRect);
                }

                // Divider line between tabs
                using (Pen divPen = new Pen(Color.FromArgb(30, 41, 59), 1))
                {
                    g.DrawLine(divPen, tabRect.Right - 1, tabRect.Top + 10, tabRect.Right - 1, tabRect.Bottom - 10);
                }

                // Active tab top indicator accent
                if (isSelected)
                {
                    using (SolidBrush indBrush = new SolidBrush(_indicatorColor))
                    {
                        Rectangle indRect = new Rectangle(tabRect.Left + 8, tabRect.Bottom - 3, tabRect.Width - 16, 3);
                        g.FillRectangle(indBrush, indRect);
                    }
                }

                // Tab text
                string tabText = TabPages[i].Text;
                Font tabFont = isSelected ? new Font(Font, FontStyle.Bold) : Font;
                TextRenderer.DrawText(
                    g,
                    tabText,
                    tabFont,
                    tabRect,
                    txtColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                );
            }
        }
    }

    /// <summary>
    /// Badge de estado tipo píldora
    /// </summary>
    public class StatusBadge : Control
    {
        private Color _dotColor = Color.FromArgb(52, 211, 153);
        private Color _badgeBackColor = Color.FromArgb(6, 78, 59);
        private Color _badgeTextColor = Color.FromArgb(167, 243, 208);

        public Color DotColor
        {
            get => _dotColor;
            set { _dotColor = value; Invalidate(); }
        }

        public Color BadgeBackColor
        {
            get => _badgeBackColor;
            set { _badgeBackColor = value; Invalidate(); }
        }

        public Color BadgeTextColor
        {
            get => _badgeTextColor;
            set { _badgeTextColor = value; Invalidate(); }
        }

        public StatusBadge()
        {
            DoubleBuffered = true;
            Font = new Font("Segoe UI Semibold", 8.5F);
            Size = new Size(130, 26);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(rect, Height / 2))
            using (SolidBrush bgBrush = new SolidBrush(_badgeBackColor))
            using (Pen borderPen = new Pen(Color.FromArgb(60, _dotColor), 1))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }

            // Draw glowing dot
            int dotSize = 8;
            int dotX = 10;
            int dotY = (Height - dotSize) / 2;
            using (SolidBrush dotBrush = new SolidBrush(_dotColor))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }

            // Draw text
            Rectangle textRect = new Rectangle(dotX + dotSize + 6, 0, Width - (dotX + dotSize + 10), Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                _badgeTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }
    }
}
